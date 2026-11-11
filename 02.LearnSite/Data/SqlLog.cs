using EasyQuerySharp.Execution;

namespace LearnSite.Data;

/// <summary>一条被执行的 SQL：文本、参数、耗时、行数。</summary>
public sealed record SqlEntry(DateTime At, string Text, string Parameters, double Milliseconds, long? Rows, string? Error);

/// <summary>
/// 记录 EasyQuerySharp 执行过的 SQL，页面用它把"生成的 SQL"展示出来。
/// 监听器在执行 SQL 的线程上调用，所以加锁；界面订阅 <see cref="Changed"/> 后用 InvokeAsync 切回 UI 线程。
/// </summary>
public sealed class SqlLog : IExecutionListener
{
    const int Capacity = 200;

    readonly Lock gate = new();
    readonly List<SqlEntry> entries = [];
    int notifyPending;

    /// <summary>有新的 SQL 记录了。短时间内的多次变化合并成一次通知（见 <see cref="Notify"/>）。</summary>
    public event Action? Changed;

    /// <summary>暂停记录：跑成百上千条 SQL 的演示（逐条写入测速）期间不记，免得日志和界面被刷爆。</summary>
    public bool Paused { get; set; }

    /// <summary>最近的若干条（新的在后）。</summary>
    public IReadOnlyList<SqlEntry> Recent(int count)
    {
        lock (gate) return [.. entries.TakeLast(count)];
    }

    public void Clear()
    {
        lock (gate) entries.Clear();
        Changed?.Invoke();
    }

    /// <summary>
    /// 通知界面刷新，但最多每 150 毫秒一次：连续执行很多条 SQL 时，每条都重画一次页面（还要给 SQL 做语法高亮）会把界面线程堵死。
    /// </summary>
    void Notify()
    {
        if (Interlocked.Exchange(ref notifyPending, 1) == 1) return;
        _ = Task.Delay(150).ContinueWith(_ =>
        {
            Volatile.Write(ref notifyPending, 0);
            Changed?.Invoke();
        });
    }

    void IExecutionListener.OnExecuted(SqlExecutionEvent e)
    {
        if (Paused) return;
        var parameters = string.Join(", ", e.Statement.Parameters.Select(p => p.Value is string s ? $"'{s}'" : p.Value is DateTime d ? $"'{d:yyyy-MM-dd HH:mm:ss}'" : p.Value?.ToString() ?? "NULL"));
        var suffix = e.Statements.Count > 1 ? $"（这一批共 {e.Statements.Count} 组参数，只显示第一组）" : "";
        var entry = new SqlEntry(DateTime.Now, e.Statement.Text, parameters + suffix, e.Elapsed.TotalMilliseconds, e.Rows, e.Exception?.Message);
        lock (gate)
        {
            entries.Add(entry);
            if (entries.Count > Capacity) entries.RemoveRange(0, entries.Count - Capacity);
        }
        Notify();
    }
}
