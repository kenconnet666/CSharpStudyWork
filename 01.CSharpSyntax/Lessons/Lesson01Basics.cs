namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 1 课：基础语法。
/// 学习方法：从上往下读代码，对照控制台输出，改改数值再运行，观察变化。
/// 对照记忆：C# 的语法和 Java 极像；var/模板字符串/?. 等和 JS/TS 也相通。
/// </summary>
public static class Lesson01Basics
{
    public static void Run()
    {
        Variables();
        Strings();
        NullHandling();
        TuplesAndControlFlow();
    }

    private static void Variables()
    {
        Print.Section("1. 变量与类型");

        // 基本类型（Java 里是 int/long/double，首字母小写；C# 的 int 等是 System.Int32 等的别名）
        int count = 10;
        long big = 10_000_000_000L; // 数字可以用 _ 分隔，增强可读性（Java 也支持）
        double ratio = 3.14;
        float f = 3.14f;            // float 要加 f 后缀
        decimal money = 19.99m;     // decimal 要加 m 后缀，十进制高精度，做金额/精确计算用；工控里一般用 double/float
        bool ok = true;
        char c = 'A';
        Print.Line("int / long / double", $"{count} / {big} / {ratio}");
        Print.Line("decimal 精确计算 0.1m + 0.2m", 0.1m + 0.2m);   // 0.3
        Print.Line("double 浮点误差 0.1 + 0.2", 0.1 + 0.2);        // 0.30000000000000004（JS/Java 也一样）

        // var：编译期自动推断类型，不是 JS 的动态类型！类型一旦确定就不能改。
        var name = "Rider";          // 推断为 string
        var list = new List<int>();  // 推断为 List<int>
        // name = 123;               // 编译错误：不能把 int 赋给 string
        Print.Line("var 推断出的类型", name.GetType().Name);

        // const：编译期常量；readonly 在类里讲（第 3 课）
        const double Pi = 3.14159;
        Print.Line("const Pi", Pi);

        // 类型转换
        double d = 9.99;
        int truncated = (int)d;                 // 强制转换（截断，不是四舍五入）
        int fromString = int.Parse("123");      // 解析失败会抛异常
        bool success = int.TryParse("abc", out int parsed); // 推荐：失败返回 false，不抛异常；out 参数见第 2 课
        Print.Line("(int)9.99", truncated);
        Print.Line("int.Parse(\"123\")", fromString);
        Print.Line("TryParse(\"abc\")", $"{success}, parsed={parsed}");
        Print.Line("整数除法 7 / 2", 7 / 2);     // 3，和 Java 一样，不是 JS 的 3.5
        Print.Line("7 / 2.0", 7 / 2.0);          // 3.5

        _ = (ok, c, f, list, money); // 仅为避免"未使用变量"警告
    }

    private static void Strings()
    {
        Print.Section("2. 字符串");

        var user = "张三";
        var age = 28;

        // 字符串插值：类似 JS 的模板字符串 `${}`，在字符串前加 $，用 {} 嵌入表达式
        Print.Line("插值", $"姓名={user}, 明年 {age + 1} 岁");
        // 格式化：{值:格式}。:F2 保留 2 位小数，:X2 两位十六进制（协议开发常用），:D5 补零
        Print.Line("格式化", $"{3.14159:F2} | {255:X2} | {7:D5} | {0.256:P1}");

        // 逐字字符串 @"..."：不转义反斜杠，写路径很方便
        Print.Line("逐字字符串", @"D:\BaiduDownload\教程");

        // 原始字符串 """...""" (C# 11)：可以直接写引号和多行，类似 JS 模板字符串/Java 文本块
        var json = """
                   {"name": "Rider", "version": 2026}
                   """;
        Print.Line("原始字符串", json);

        // string 不可变（和 Java 一样）；大量拼接用 StringBuilder
        var sb = new System.Text.StringBuilder();
        for (var i = 0; i < 3; i++) sb.Append(i).Append(',');
        Print.Line("StringBuilder", sb.ToString().TrimEnd(','));

        // == 比较字符串内容（Java 里要 equals，这点 C# 更像 JS）
        Print.Line("\"abc\" == \"ab\" + \"c\"", "abc" == "ab" + "c");
        Print.Line("忽略大小写比较", "ABC".Equals("abc", StringComparison.OrdinalIgnoreCase));
    }

    private static void NullHandling()
    {
        Print.Section("3. 空值处理（C# 把这块做得很完善）");

        // 启用 <Nullable>enable</Nullable> 后：
        //   string  表示"不应为 null"；string? 表示"可能为 null"。编译器会帮你检查，几乎消灭 NullPointerException。
        string? maybeNull = null;

        // ?. 空条件运算符（和 JS 的 ?. 一样）：为 null 就整体返回 null
        int? length = maybeNull?.Length;       // int? 是 Nullable<int>，值类型也能表示"没有值"
        // ?? 空合并运算符（和 JS 的 ?? 一样）：为 null 时取右边
        var display = maybeNull ?? "(空)";
        // ??= 为 null 时才赋值
        maybeNull ??= "默认值";
        Print.Line("?.", length.HasValue ? length : "null");
        Print.Line("??", display);
        Print.Line("??=", maybeNull);

        // ! 告诉编译器"我保证这里不是 null"（类似 TS 的 !），用错了运行时照样崩，慎用
        // 判空推荐写法：is null / is not null
        if (maybeNull is not null) Print.Line("is not null", "有值");
    }

    private static void TuplesAndControlFlow()
    {
        Print.Section("4. 元组与控制流");

        // 元组：方法一次返回多个值，比定义一个类轻量（Java 没有，JS 常用数组/对象解构）
        var (min, max) = MinMax([5, 3, 9, 1]);
        Print.Line("元组解构", $"min={min}, max={max}");

        // 控制流：if / for / while / do-while 和 Java 一样
        // foreach 遍历任何集合
        foreach (var n in new[] { 1, 2, 3 }) Console.Write($"  foreach:{n}");
        Console.WriteLine();

        // 三元运算符、switch 表达式（更强，见第 8 课）
        var level = count() >= 60 ? "及格" : "不及格";
        Print.Line("三元运算符", level);

        // 范围与索引（C# 8）：类似 Python 切片，^1 表示倒数第一个
        int[] arr = [10, 20, 30, 40, 50];
        Print.Line("arr[^1] 最后一个", arr[^1]);
        Print.Line("arr[1..3] 切片", string.Join(",", arr[1..3])); // 取下标 1,2 -> 20,30

        static int count() => 75; // 局部函数：在方法内部定义的函数，类似 JS 的内嵌函数
    }

    /// <summary>返回命名元组 (Min, Max)。</summary>
    private static (int Min, int Max) MinMax(int[] values) => (values.Min(), values.Max());
}
