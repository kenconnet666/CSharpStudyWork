using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace LearnSite.Logging;

/// <summary>
/// 应用的日志配置：Serilog 同时写两个地方——按天滚动的文件（现场排错靠它）和内存（页面上展示用）。
/// 业务代码只依赖 <see cref="ILogger{T}"/>（Microsoft.Extensions.Logging 的标准接口），不知道背后是 Serilog。
/// </summary>
public static class LoggingSetup
{
    /// <summary>日志文件所在目录：用户目录下的 .learnsite\logs。</summary>
    public static string LogDirectory { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".learnsite", "logs");

    public static IServiceCollection AddLearnSiteLogging(this IServiceCollection services)
    {
        var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);   // 运行中可以改最小级别
        var memory = new MemoryLogSink();
        var logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            // 框架自己（Blazor 渲染、ASP.NET、EF……）的日志只留警告以上。否则把级别调到 Debug 时，每次渲染都会产生一条日志，
            // 日志界面刷新又触发渲染，形成"日志 → 重绘 → 日志"的死循环
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(LogDirectory, "learnsite-.log"),
                rollingInterval: RollingInterval.Day,      // 每天一个文件：learnsite-20261111.log
                retainedFileCountLimit: 7,                  // 只留最近 7 天，防止磁盘被日志写满
                shared: true,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.Sink(memory)
            .CreateLogger();

        services.AddSingleton(levelSwitch);
        services.AddSingleton(memory);
        services.AddLogging(builder => builder.ClearProviders().AddSerilog(logger, dispose: true));
        return services;
    }

    /// <summary>读今天日志文件的最后几行（文件正被写入，要以共享方式打开）。</summary>
    public static async Task<string[]> ReadTodayTailAsync(int lines)
    {
        var file = Path.Combine(LogDirectory, $"learnsite-{DateTime.Now:yyyyMMdd}.log");
        if (!File.Exists(file)) return [];
        await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var all = (await reader.ReadToEndAsync()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return [.. all.TakeLast(lines).Select(l => l.TrimEnd('\r'))];
    }
}
