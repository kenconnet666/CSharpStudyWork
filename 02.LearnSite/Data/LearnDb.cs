using System.Data;
using EasyQuerySharp;
using Npgsql;

namespace LearnSite.Data;

/// <summary>
/// 学习站的数据库入口：WSL 里 PostgreSQL 中的 learnsite 库。
/// 连接串（含密码）不进仓库：先读环境变量 LEARNSITE_PG，没有再读用户目录下的 ~/.learnsite/connection.txt。
/// </summary>
public sealed class LearnDb : IAsyncDisposable
{
    public const string StartCommand = "bash infra/dev/db.sh up -d    # 在 EasyQuerySharp 目录里，通过 WSL 启动 PostgreSQL";

    readonly SemaphoreSlim initLock = new(1, 1);
    NpgsqlDataSource? dataSource;

    public SqlLog Log { get; } = new();

    /// <summary>库已连上、表已建好。</summary>
    public bool Ready { get; private set; }

    /// <summary>连不上时的原因（给界面显示）。</summary>
    public string? Error { get; private set; }

    public EasyDb Db => db ?? throw new InvalidOperationException("数据库还没有就绪，先调用 EnsureReadyAsync。");

    EasyDb? db;

    /// <summary>直接给 ADO.NET 用的数据源（SQL 基础一课手写 SQL 用）。</summary>
    public NpgsqlDataSource DataSource => dataSource ?? throw new InvalidOperationException("数据库还没有就绪。");

    /// <summary>连接、建表、放入示例设备。已经就绪时直接返回；失败时 <see cref="Error"/> 有原因，下次调用会重试。</summary>
    public async Task<bool> EnsureReadyAsync()
    {
        if (Ready) return true;
        await initLock.WaitAsync();
        try
        {
            if (Ready) return true;
            Error = null;
            var connection = ReadConnectionString();
            if (connection is null)
            {
                Error = "没有找到连接串：设置环境变量 LEARNSITE_PG，或把连接串放进用户目录下的 .learnsite\\connection.txt。";
                return false;
            }

            dataSource ??= NpgsqlDataSource.Create(connection);
            db ??= new EasyDb(o => o.UsePostgreSql(dataSource).AddListener(Log));

            // 先试连一下：库没启动时给出清楚的提示，而不是等到第一条查询才报一长串异常
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await using (await dataSource.OpenConnectionAsync(timeout.Token)) { }

            await CreateTablesIfMissingAsync();
            await SeedDevicesIfEmptyAsync();
            Log.Clear();
            Ready = true;
            return true;
        }
        catch (Exception ex) when (ex is NpgsqlException or OperationCanceledException or System.Net.Sockets.SocketException or TimeoutException)
        {
            Error = $"连不上数据库（{ex.GetBaseException().Message}）。请确认 WSL 里的 PostgreSQL 已启动。";
            return false;
        }
        finally
        {
            initLock.Release();
        }
    }

    async Task CreateTablesIfMissingAsync()
    {
        // 建表语句遇到已存在的表会跳过，但索引和外键会重复创建而报错，所以已经建过就不再建
        var existing = await Db.SqlQueryAsync<string>($"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public' AND table_name = {"t_device"}");
        if (existing.Count == 0)
            await Db.CodeFirst.CreateTablesAsync(typeof(Device), typeof(Reading), typeof(Alarm));
    }

    async Task SeedDevicesIfEmptyAsync()
    {
        if (await Db.Query<Device>().CountAsync() > 0) return;
        await Db.Insert(
            new Device { Name = "1 号反应釜", Location = "A 车间" },
            new Device { Name = "2 号反应釜", Location = "A 车间" },
            new Device { Name = "冷却水泵", Location = "B 车间" }).ExecuteAsync();
    }

    /// <summary>清掉所有采样和报警（设备保留），让演示可以重来。</summary>
    public async Task ResetDataAsync()
    {
        await Db.ExecuteSqlAsync($"TRUNCATE TABLE t_reading, t_alarm RESTART IDENTITY");
    }

    /// <summary>
    /// 手写 SQL 的演示：用 ADO.NET 直接执行，放在只读事务里（写操作会被数据库拒绝），限时 3 秒，最多取 <paramref name="maxRows"/> 行。
    /// 值用 <c>@name</c> 参数传入，不拼进 SQL 文本。
    /// </summary>
    public async Task<QueryResult> QueryReadOnlyAsync(string sql, IReadOnlyDictionary<string, object?>? parameters = null, int maxRows = 50)
    {
        var started = System.Diagnostics.Stopwatch.StartNew();
        await using var connection = await DataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        await using (var guard = new NpgsqlCommand("SET TRANSACTION READ ONLY; SET LOCAL statement_timeout = 3000", connection, transaction))
            await guard.ExecuteNonQueryAsync();

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters ?? new Dictionary<string, object?>())
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();
        var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
        List<IReadOnlyList<string>> rows = [];
        var truncated = false;
        while (await reader.ReadAsync())
        {
            if (rows.Count >= maxRows) { truncated = true; break; }
            rows.Add([.. Enumerable.Range(0, reader.FieldCount).Select(i => Format(reader.GetValue(i)))]);
        }
        return new QueryResult(columns, rows, started.Elapsed.TotalMilliseconds, truncated);
    }

    static string Format(object value) => value switch
    {
        DBNull => "NULL",
        double d => d.ToString("0.##"),
        DateTime t => t.ToString("HH:mm:ss"),
        _ => value.ToString() ?? "",
    };

    /// <summary>
    /// 保证有一批示例采样：3 台设备、最近 10 分钟、每 5 秒一条温度和压力。已经有数据就什么都不做。
    /// 一次批量写入几千行，也是"批量写入比逐条快"的现成例子。
    /// </summary>
    public async Task<bool> EnsureSampleReadingsAsync()
    {
        if (await Db.Query<Reading>().CountAsync() > 0) return false;
        var devices = await Db.Query<Device>().OrderBy(d => d.Id).ToListAsync();
        var random = new Random(42);
        var start = Clock.Now.AddMinutes(-10);
        List<Reading> readings = [];
        foreach (var device in devices)
            for (var i = 0; i < 120; i++)
            {
                var at = start.AddSeconds(i * 5);
                readings.Add(new Reading { DeviceId = device.Id, Tag = "temperature", Value = Math.Round(55 + device.Id * 5 + Math.Sin(i / 12.0) * 8 + random.NextDouble() * 2, 1), At = at });
                readings.Add(new Reading { DeviceId = device.Id, Tag = "pressure", Value = Math.Round(100 + Math.Cos(i / 20.0) * 6 + random.NextDouble(), 1), At = at });
            }
        await Db.Insert(readings).Batch().ExecuteAsync();
        return true;
    }

    static string? ReadConnectionString()
    {
        if (Environment.GetEnvironmentVariable("LEARNSITE_PG") is { Length: > 0 } fromEnvironment) return fromEnvironment;
        var file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".learnsite", "connection.txt");
        return File.Exists(file) ? File.ReadAllText(file).Trim() : null;
    }

    /// <summary>当前库里的表与行数，给"库的状态"面板用。</summary>
    public async Task<(long Devices, long Readings, long Alarms)> CountsAsync() =>
        (await Db.Query<Device>().CountAsync(), await Db.Query<Reading>().CountAsync(), await Db.Query<Alarm>().CountAsync());

    public async ValueTask DisposeAsync()
    {
        if (dataSource is not null) await dataSource.DisposeAsync();
        initLock.Dispose();
    }
}
