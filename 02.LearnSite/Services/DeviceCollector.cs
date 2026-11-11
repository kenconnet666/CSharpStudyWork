using System.Threading.Channels;
using LearnSite.Data;

namespace LearnSite.Services;

/// <summary>
/// 一台设备的采集管线：轮询任务读 Modbus 寄存器 → 放进 Channel 队列 → 写入任务每隔一会儿把队列里的批量写进数据库；
/// 同时判断温度报警，报警的产生与恢复也记进数据库。轮询失败会超时、重试、断线重连（带退避）。
/// 这是"采集 → 缓冲 → 落库"整条链路的最小版本。
/// </summary>
public sealed class DeviceCollector(LearnDb db, ModbusLoopback modbus, long deviceId) : IAsyncDisposable
{
    const int TrendLength = 60;

    readonly Channel<Reading> queue = Channel.CreateUnbounded<Reading>();
    readonly Lock gate = new();
    readonly List<double> temperatures = [];
    readonly List<double> pressures = [];
    CancellationTokenSource? cts;
    Task? pollTask;
    Task? writeTask;
    int queued;
    long? activeAlarmId;

    // ---- 设置（运行中可以改） ----
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromMilliseconds(500);
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromSeconds(1);
    public double WarnAt { get; set; } = 70;
    public double DangerAt { get; set; } = 85;

    // ---- 状态（界面读） ----
    public bool Running => cts is not null;
    public string State { get; private set; } = "已停止";
    public double? Temperature { get; private set; }
    public double? Pressure { get; private set; }
    public int Failures { get; private set; }
    public int Queued => Volatile.Read(ref queued);
    public long Polled { get; private set; }
    public long Written { get; private set; }
    public long Batches { get; private set; }
    public int LastBatchSize { get; private set; }

    /// <summary>当前报警级别："none" / "warning" / "danger"。</summary>
    public string AlarmLevel { get; private set; } = "none";

    /// <summary>状态变了（在后台线程触发，界面用 InvokeAsync 切回 UI 线程）。</summary>
    public event Action? Changed;

    public (IReadOnlyList<double> Temperatures, IReadOnlyList<double> Pressures) Trend()
    {
        lock (gate) return ([.. temperatures], [.. pressures]);
    }

    public void Start()
    {
        if (cts is not null) return;
        cts = new CancellationTokenSource();
        State = "运行中";
        pollTask = PollLoop(cts.Token);
        writeTask = WriteLoop(cts.Token);
        Changed?.Invoke();
    }

    public async Task StopAsync()
    {
        if (cts is null) return;
        await cts.CancelAsync();
        await Task.WhenAll(pollTask ?? Task.CompletedTask, writeTask ?? Task.CompletedTask);
        cts.Dispose();
        cts = null;
        State = "已停止";
        Changed?.Invoke();
    }

    // ---- 轮询：读寄存器、放进队列、判断报警 ----
    async Task PollLoop(CancellationToken token)
    {
        try
        {
            if (!modbus.Connected) await modbus.ConnectAsync();
            while (!token.IsCancellationRequested)
            {
                await PollOnce(token);
                await Task.Delay(PollInterval, token);
            }
        }
        catch (OperationCanceledException) { }
    }

    async Task PollOnce(CancellationToken token)
    {
        try
        {
            var (frame, _) = ModbusCodec.BuildTcpRequest(modbus.NextTransactionId(), 1, 3, 0, 2);
            var reply = await modbus.RequestAsync(frame, TimeSpan.FromMilliseconds(600));
            var registers = ModbusCodec.ParseRegisters(reply);
            var now = Clock.Now;
            Temperature = registers[0] / 10.0;
            Pressure = registers[1];
            Failures = 0;
            State = "运行中";
            Polled++;

            lock (gate)
            {
                Push(temperatures, Temperature.Value);
                Push(pressures, Pressure.Value);
            }
            Enqueue(new Reading { DeviceId = deviceId, Tag = "temperature", Value = Temperature.Value, At = now });
            Enqueue(new Reading { DeviceId = deviceId, Tag = "pressure", Value = Pressure.Value, At = now });
            await EvaluateAlarmAsync(Temperature.Value, now);
        }
        catch (Exception ex) when (ex is OperationCanceledException && !token.IsCancellationRequested || ex is IOException)
        {
            Failures++;
            State = Failures >= 3 ? "重连中" : "超时重试";
            Changed?.Invoke();
            if (Failures >= 3) await ReconnectAsync(token);
        }
        Changed?.Invoke();
    }

    /// <summary>关掉旧连接重连；失败就等 1、2、4 秒（上限 4 秒）再试，直到连上或被停止。</summary>
    async Task ReconnectAsync(CancellationToken token)
    {
        var wait = 1;
        while (!token.IsCancellationRequested)
        {
            modbus.Disconnect();
            try
            {
                await modbus.ConnectAsync();
                // 连得上 TCP 不等于设备在回话：先试读一次，设备仍然沉默就继续退避
                var (frame, _) = ModbusCodec.BuildTcpRequest(modbus.NextTransactionId(), 1, 3, 0, 1);
                await modbus.RequestAsync(frame, TimeSpan.FromMilliseconds(600));
                Failures = 0;
                State = "运行中";
                return;
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException or System.Net.Sockets.SocketException)
            {
                State = $"重连中（{wait} 秒后再试）";
                Changed?.Invoke();
                await Task.Delay(TimeSpan.FromSeconds(wait), token);
                wait = Math.Min(wait * 2, 4);
            }
        }
    }

    void Enqueue(Reading reading)
    {
        Interlocked.Increment(ref queued);
        queue.Writer.TryWrite(reading);
    }

    static void Push(List<double> list, double value)
    {
        list.Add(value);
        if (list.Count > TrendLength) list.RemoveAt(0);
    }

    // ---- 写入：每隔一会儿把队列里攒下的数据批量写进数据库 ----
    async Task WriteLoop(CancellationToken token)
    {
        try
        {
            while (true)
            {
                await Task.Delay(BatchInterval, token);
                await FlushAsync();
            }
        }
        catch (OperationCanceledException)
        {
            await FlushAsync();   // 停止前把队列里剩下的写完，不丢数据
        }
    }

    async Task FlushAsync()
    {
        List<Reading> batch = [];
        while (queue.Reader.TryRead(out var reading)) batch.Add(reading);
        if (batch.Count == 0) return;
        Interlocked.Add(ref queued, -batch.Count);
        await db.Db.Insert(batch).Batch().ExecuteAsync();
        Written += batch.Count;
        Batches++;
        LastBatchSize = batch.Count;
        Changed?.Invoke();
    }

    // ---- 报警：超限产生、回落到阈值以下 2 度才恢复（滞回，避免在阈值附近反复报警）----
    async Task EvaluateAlarmAsync(double temperature, DateTime now)
    {
        var level = temperature >= DangerAt ? "danger"
            : temperature >= WarnAt ? "warning"
            : AlarmLevel != "none" && temperature >= WarnAt - 2 ? AlarmLevel   // 还没降到恢复线以下，维持原级别
            : "none";
        if (level == AlarmLevel) return;

        if (activeAlarmId is { } id)
        {
            await db.Db.Update<Alarm>().Set(a => a.ClearedAt, (DateTime?)now).Where(a => a.Id == id).ExecuteAsync();
            activeAlarmId = null;
        }
        if (level != "none")
        {
            var alarm = new Alarm
            {
                DeviceId = deviceId,
                Level = level,
                Message = level == "danger" ? $"温度 {temperature:F1} ℃ 超过危险线 {DangerAt:F0} ℃" : $"温度 {temperature:F1} ℃ 超过预警线 {WarnAt:F0} ℃",
                RaisedAt = now,
            };
            await db.Db.Insert(alarm).FillGeneratedKeys().ExecuteAsync();
            activeAlarmId = alarm.Id;
        }
        AlarmLevel = level;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
