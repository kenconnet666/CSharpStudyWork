namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 7 课：LINQ（语言集成查询）。对任何集合做过滤、转换、分组、统计。
/// 对照：Java Stream / JS 的 filter-map-reduce。方法名对应：Where=filter, Select=map, Aggregate=reduce。
/// </summary>
public static class Lesson07Linq
{
    private record Reading(string Sensor, double Value, int Hour);

    public static void Run()
    {
        List<Reading> readings =
        [
            new("温度", 36.5, 8), new("温度", 38.2, 9), new("温度", 41.0, 10),
            new("压力", 101.3, 8), new("压力", 99.8, 9), new("压力", 105.1, 10),
        ];

        Print.Section("1. 过滤、转换、排序");
        var hot = readings
            .Where(r => r.Sensor == "温度" && r.Value > 37)  // 过滤
            .OrderByDescending(r => r.Value)                  // 排序（升序用 OrderBy，多级排序接 ThenBy）
            .Select(r => $"{r.Hour}点:{r.Value}");            // 转换
        Print.Line("高温记录", string.Join(" | ", hot));

        Print.Section("2. 聚合与判断");
        Print.Line("温度平均值", readings.Where(r => r.Sensor == "温度").Average(r => r.Value));
        Print.Line("最大值", readings.Max(r => r.Value));
        Print.Line("是否存在超过 105 的值", readings.Any(r => r.Value > 105));
        Print.Line("是否全部为正", readings.All(r => r.Value > 0));
        // First 找不到会抛异常；FirstOrDefault 找不到返回 null/默认值
        Print.Line("第一条压力记录", readings.FirstOrDefault(r => r.Sensor == "压力"));

        Print.Section("3. 分组");
        foreach (var group in readings.GroupBy(r => r.Sensor))
            Print.Line(group.Key, $"共 {group.Count()} 条，最大 {group.Max(r => r.Value)}");

        // .NET 9 新增 CountBy / AggregateBy：一步完成"分组计数/分组聚合"
        foreach (var (sensor, count) in readings.CountBy(r => r.Sensor))
            Print.Line($"CountBy {sensor}", count);

        Print.Section("4. 惰性求值：查询在被遍历时才真正执行");
        var numbers = new List<int> { 1, 2, 3 };
        var query = numbers.Select(n => n * 2);   // 此刻并没有执行
        numbers.Add(4);                            // 在遍历前修改了源数据
        Print.Line("结果包含新增元素", string.Join(",", query)); // 2,4,6,8
        // 想立即固定结果：.ToList() / .ToArray()。多次遍历同一个查询前，先 ToList 避免重复计算。

        Print.Section("5. 其他常用方法");
        Print.Line("Chunk 分块(每 2 个)", string.Join(" | ", Enumerable.Range(1, 5).Chunk(2).Select(c => string.Join(",", c))));
        Print.Line("Zip 合并", string.Join(",", new[] { 1, 2 }.Zip(["a", "b"], (n, s) => n + s)));
        Print.Line("Distinct 去重", string.Join(",", new[] { 1, 1, 2 }.Distinct()));
        Print.Line("Skip/Take 分页", string.Join(",", Enumerable.Range(1, 10).Skip(3).Take(3)));

        // 另一种写法：查询表达式（类似 SQL），功能等价，实际项目中方法链更常见
        var sqlStyle = from r in readings where r.Hour == 8 select r.Sensor;
        Print.Line("查询表达式", string.Join(",", sqlStyle));
    }
}
