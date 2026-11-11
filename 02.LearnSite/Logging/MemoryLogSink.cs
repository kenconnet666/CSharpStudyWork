using Serilog.Core;
using Serilog.Events;

namespace LearnSite.Logging;

/// <summary>一条日志：级别、时间、渲染后的文字、结构化属性。</summary>
public sealed record LogEntry(DateTime At, LogEventLevel Level, string Message, string Template, IReadOnlyDictionary<string, string> Properties);

/// <summary>
/// 把日志留在内存里（最近 300 条），让页面能把"刚才记了什么"展示出来；同时日志照常写进文件（见 <see cref="LoggingSetup"/>）。
/// 界面订阅 <see cref="Changed"/>，通知合并成最多每 150 毫秒一次。
/// </summary>
public sealed class MemoryLogSink : ILogEventSink
{
    const int Capacity = 300;

    readonly Lock gate = new();
    readonly List<LogEntry> entries = [];
    int notifyPending;

    public event Action? Changed;

    public IReadOnlyList<LogEntry> Recent(int count)
    {
        lock (gate) return [.. entries.TakeLast(count)];
    }

    public void Clear()
    {
        lock (gate) entries.Clear();
        Changed?.Invoke();
    }

    public void Emit(LogEvent logEvent)
    {
        var properties = logEvent.Properties
            .Where(p => p.Key != "SourceContext")
            .ToDictionary(p => p.Key, p => p.Value.ToString());
        var entry = new LogEntry(logEvent.Timestamp.LocalDateTime, logEvent.Level, logEvent.RenderMessage(), logEvent.MessageTemplate.Text, properties);
        lock (gate)
        {
            entries.Add(entry);
            if (entries.Count > Capacity) entries.RemoveRange(0, entries.Count - Capacity);
        }
        if (Interlocked.Exchange(ref notifyPending, 1) == 1) return;
        _ = Task.Delay(150).ContinueWith(_ =>
        {
            Volatile.Write(ref notifyPending, 0);
            Changed?.Invoke();
        });
    }
}
