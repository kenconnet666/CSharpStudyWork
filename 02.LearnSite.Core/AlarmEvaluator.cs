namespace LearnSite.Services;

/// <summary>报警级别。</summary>
public enum AlarmLevel
{
    None,
    Warning,
    Danger,
}

/// <summary>
/// 温度报警的判断：超过预警线是预警，超过危险线是危险。
/// 带"滞回"：已经在报警时，温度要回落到预警线以下 <see cref="Hysteresis"/> 度才算恢复，
/// 避免温度在阈值附近上下抖动时报警反复产生、恢复。
/// </summary>
public sealed class AlarmEvaluator(double warnAt, double dangerAt, double hysteresis = 2)
{
    public double WarnAt { get; } = warnAt;

    public double DangerAt { get; } = dangerAt;

    public double Hysteresis { get; } = hysteresis;

    /// <summary>给定当前温度和当前级别，返回新的级别。</summary>
    public AlarmLevel Evaluate(double temperature, AlarmLevel current) => temperature switch
    {
        _ when temperature >= DangerAt => AlarmLevel.Danger,
        _ when temperature >= WarnAt => AlarmLevel.Warning,
        _ when current != AlarmLevel.None && temperature >= WarnAt - Hysteresis => current,   // 还没降到恢复线以下，维持原级别
        _ => AlarmLevel.None,
    };
}
