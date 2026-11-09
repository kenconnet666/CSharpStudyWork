namespace CSharpSyntax.Lessons;

/// <summary>第 8 课：模式匹配与 switch 表达式。C# 近几年进步最大的部分，写协议解析、状态机很方便。</summary>
public static class Lesson08PatternMatching
{
    private record Frame(byte FunctionCode, byte[] Payload);
    private record Point(int X, int Y);

    public static void Run()
    {
        Print.Section("1. switch 表达式（有返回值，可直接赋给变量）");
        byte code = 0x03;
        // 比传统 switch 语句简洁：不需要 break，每个分支是 `模式 => 结果`，_ 表示其他情况
        var name = code switch
        {
            0x01 => "读线圈",
            0x03 => "读保持寄存器",
            0x06 => "写单个寄存器",
            _ => "未知功能码",
        };
        Print.Line("功能码 0x03", name);

        Print.Section("2. 关系模式与逻辑模式：and / or / not");
        foreach (var score in new[] { 95, 72, 30 })
        {
            var grade = score switch
            {
                >= 90 => "优",
                >= 60 and < 90 => "及格",
                _ => "不及格",
            };
            Print.Line($"分数 {score}", grade);
        }

        Print.Section("3. 类型模式与属性模式");
        object?[] things = [42, "hello", new Point(0, 5), new Point(3, 3), null];
        foreach (var thing in things) Print.Line("Describe", Describe(thing));

        Print.Section("4. 解析协议帧");
        Print.Line("帧 1", Parse(new Frame(0x03, [0x00, 0x02])));
        Print.Line("帧 2", Parse(new Frame(0x06, [0x00])));

        Print.Section("5. 列表模式 (C# 11)");
        Print.Line("[1,2,3]", Classify([1, 2, 3]));
        Print.Line("[]", Classify([]));
        Print.Line("[9]", Classify([9]));
        Print.Line("[5,6,7,8]", Classify([5, 6, 7, 8]));

        Print.Section("6. 元组模式：多个条件一起判断");
        Print.Line("(true, false)", NextAction(isConnected: true, hasError: false));
        Print.Line("(true, true)", NextAction(isConnected: true, hasError: true));
        Print.Line("(false, _)", NextAction(isConnected: false, hasError: false));
    }

    private static string Describe(object? obj) => obj switch
    {
        null => "null",
        int n when n > 40 => $"大整数 {n}",         // when：附加条件
        int n => $"整数 {n}",
        string { Length: > 3 } s => $"长字符串 {s}",  // 属性模式：检查属性值 { Length: > 3 }
        Point { X: 0 } p => $"在 Y 轴上的点 {p}",
        Point(var x, var y) when x == y => $"对角线点 ({x},{y})", // 位置模式：利用 record 的解构
        _ => "其他",
    };

    private static string Parse(Frame frame) => frame switch
    {
        { FunctionCode: 0x03, Payload: [var hi, var lo] } => $"读寄存器，数量={hi << 8 | lo}",
        { FunctionCode: 0x06 } => "写寄存器（数据长度不对，演示用）",
        _ => "未知帧",
    };

    private static string Classify(int[] values) => values switch
    {
        [] => "空",
        [var only] => $"只有一个元素 {only}",
        [var first, .., var last] => $"首 {first} 尾 {last}", // .. 匹配中间任意个
    };

    private static string NextAction(bool isConnected, bool hasError) => (isConnected, hasError) switch
    {
        (false, _) => "重连",
        (true, true) => "复位",
        (true, false) => "继续轮询",
    };
}
