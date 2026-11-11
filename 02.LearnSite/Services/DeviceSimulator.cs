namespace LearnSite.Services;

/// <summary>
/// 让模拟设备的寄存器"活"起来：温度朝目标温度慢慢靠拢并带一点噪声，压力跟着温度走。
/// 就像真实设备里的传感器在不断刷新寄存器，上位机轮询时读到的是当下的值。
/// </summary>
public sealed class DeviceSimulator : IDisposable
{
    readonly ModbusDevice device;
    readonly Random random = new();
    readonly PeriodicTimer timer = new(TimeSpan.FromMilliseconds(200));
    readonly CancellationTokenSource cts = new();
    double temperature = 55;

    public DeviceSimulator(ModbusDevice device)
    {
        this.device = device;
        _ = Run(cts.Token);
    }

    /// <summary>温度会慢慢靠向这个值（℃）。</summary>
    public double Target { get; set; } = 55;

    async Task Run(CancellationToken token)
    {
        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                temperature += (Target - temperature) * 0.08 + (random.NextDouble() - 0.5) * 0.6;
                var pressure = 100 + (temperature - 50) * 0.4 + (random.NextDouble() - 0.5);
                device.Registers[0] = (ushort)Math.Clamp(Math.Round(temperature * 10), 0, ushort.MaxValue);   // 温度 ×10
                device.Registers[1] = (ushort)Math.Clamp(Math.Round(pressure), 0, ushort.MaxValue);
            }
        }
        catch (OperationCanceledException) { }
    }

    public void Dispose()
    {
        cts.Cancel();
        timer.Dispose();
        cts.Dispose();
    }
}
