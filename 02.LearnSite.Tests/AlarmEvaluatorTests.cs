using LearnSite.Services;

namespace LearnSite.Tests;

/// <summary>报警判断：阈值、以及滞回（避免在阈值附近反复报警）。把规则写成测试，改规则时立刻知道有没有改坏。</summary>
public class AlarmEvaluatorTests
{
    readonly AlarmEvaluator evaluator = new(warnAt: 70, dangerAt: 85, hysteresis: 2);

    [Theory]
    [InlineData(60, AlarmLevel.None)]
    [InlineData(70, AlarmLevel.Warning)]    // 刚好等于预警线，算超限
    [InlineData(80, AlarmLevel.Warning)]
    [InlineData(85, AlarmLevel.Danger)]
    [InlineData(99, AlarmLevel.Danger)]
    public void 没有报警时_按阈值判断(double temperature, AlarmLevel expected)
    {
        Assert.Equal(expected, evaluator.Evaluate(temperature, AlarmLevel.None));
    }

    [Fact]
    public void 报警中_回落到预警线以下但没到恢复线_维持原级别()
    {
        // 预警线 70，滞回 2 度：69 还在恢复线（68）之上，不恢复
        Assert.Equal(AlarmLevel.Warning, evaluator.Evaluate(69, AlarmLevel.Warning));
    }

    [Fact]
    public void 报警中_降到恢复线以下才恢复()
    {
        Assert.Equal(AlarmLevel.None, evaluator.Evaluate(67.9, AlarmLevel.Warning));
    }

    [Fact]
    public void 没报警时_69度不会产生报警()
    {
        // 同样是 69 度：本来没报警就不报，有报警才维持——这就是滞回
        Assert.Equal(AlarmLevel.None, evaluator.Evaluate(69, AlarmLevel.None));
    }

    [Fact]
    public void 危险降到预警区间_变成预警()
    {
        Assert.Equal(AlarmLevel.Warning, evaluator.Evaluate(80, AlarmLevel.Danger));
    }
}
