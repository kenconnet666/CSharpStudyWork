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

    public event Action? Changed;

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

    void IExecutionListener.OnExecuted(SqlExecutionEvent e)
    {
        var parameters = string.Join(", ", e.Statement.Parameters.Select(p => p.Value is string s ? $"'{s}'" : p.Value is DateTime d ? $"'{d:yyyy-MM-dd HH:mm:ss}'" : p.Value?.ToString() ?? "NULL"));
        var suffix = e.Statements.Count > 1 ? $"（这一批共 {e.Statements.Count} 组参数，只显示第一组）" : "";
        var entry = new SqlEntry(DateTime.Now, e.Statement.Text, parameters + suffix, e.Elapsed.TotalMilliseconds, e.Rows, e.Exception?.Message);
        lock (gate)
        {
            entries.Add(entry);
            if (entries.Count > Capacity) entries.RemoveRange(0, entries.Count - Capacity);
        }
        Changed?.Invoke();
    }
}
