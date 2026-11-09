// C# 12 特性：给任意类型起别名（包括元组）。using 别名必须写在文件最前面。
using Coordinate = (double Latitude, double Longitude);

namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 11 课：C# 12 ~ 14 的新特性（.NET 10 对应 C# 14）。
/// 前面的课里已经穿插用过：主构造函数、集合表达式、原始字符串、列表模式、required/init。
/// 本课集中补充其余的，并重点介绍 C# 14 的新东西。
/// </summary>
public static class Lesson11NewFeatures
{
    // ---------- C# 14：field 关键字 ----------
    // 以前给属性加校验逻辑，必须手写一个私有字段。现在 `field` 直接代表编译器生成的隐藏字段。
    private class Profile
    {
        public string Name
        {
            get;
            set => field = value.Trim(); // 赋值时自动去掉首尾空格，不用再声明 _name
        } = "";

        public int Age
        {
            get;
            set => field = value < 0 ? 0 : value; // 带"夹紧"逻辑的属性
        }
    }

    private class Customer
    {
        public string? Name { get; set; }
    }

    // 带默认值的 Lambda 参数（C# 12）和 params 集合（C# 13）见 Run 里的演示

    public static void Run()
    {
        Print.Section("1. field 关键字 (C# 14)");
        var profile = new Profile { Name = "  张三  ", Age = -5 };
        Print.Line("Name", $"[{profile.Name}]");  // [张三]
        Print.Line("Age", profile.Age);            // 0

        Print.Section("2. 扩展成员 extension (C# 14)");
        // 以前只能写扩展"方法"；现在可以扩展属性、静态成员，写法也更清晰。见文件底部 StringExtensions
        Print.Line("\"  \".IsBlank", "  ".IsBlank);
        Print.Line("\"abcdefgh\".Truncate(5)", "abcdefgh".Truncate(5));
        Print.Line("string.Repeat(\"=\", 6)", string.Repeat("=", 6)); // 扩展的静态成员，像 string 自带的一样用

        Print.Section("3. 空条件赋值 ?.= (C# 14)");
        Customer? customer = null;
        customer?.Name = "不会执行";           // customer 为 null，整个赋值被跳过，不会报错
        customer = new Customer();
        customer?.Name = "李四";                // 不为 null 才赋值
        Print.Line("Name", customer?.Name);

        Print.Section("4. 其他 C# 12/13 特性");
        // 别名任意类型 (C# 12)
        Coordinate place = (31.23, 121.47);
        Print.Line("类型别名 Coordinate", $"{place.Latitude}, {place.Longitude}");

        // Lambda 默认参数 (C# 12)
        var greet = (string name, string prefix = "你好") => $"{prefix}, {name}";
        Print.Line("Lambda 默认参数", greet("Rider"));

        // params 支持集合类型和 ReadOnlySpan (C# 13)：可以避免分配数组
        Print.Line("params 集合", SumAll(1, 2, 3));

        // nameof 支持未绑定泛型 (C# 14)：nameof(List<>) 得到 "List"
        Print.Line("nameof(List<>)", nameof(List<>));

        // 转义字符 \e (C# 13)：ESC 字符，用来输出 ANSI 终端颜色
        Console.WriteLine("  \e[32m绿色文字（支持 ANSI 的终端可见）\e[0m");

        // 可以访问外层变量的 static lambda 提示：加 static 能防止意外捕获变量
        Func<int, int> square = static x => x * x;
        Print.Line("static lambda", square(9));

        Print.Section("5. 下一步");
        Print.Line("语法部分完成", "接下来学 Socket、串口、Modbus；它们会用到前面全部内容");
    }

    private static int SumAll(params IEnumerable<int> numbers) => numbers.Sum();
}

/// <summary>C# 14 扩展块：extension(接收者) { 成员 }，里面可以写属性、方法，静态成员用 extension(string)。</summary>
public static class StringExtensions
{
    extension(string source)
    {
        /// <summary>扩展属性：以前做不到，只能写 IsBlank() 方法。</summary>
        public bool IsBlank => string.IsNullOrWhiteSpace(source);

        public string Truncate(int maxLength) => source.Length <= maxLength ? source : source[..maxLength] + "…";
    }

    extension(string)
    {
        /// <summary>扩展静态方法：调用时写 string.Repeat(...)。</summary>
        public static string Repeat(string text, int times) => string.Concat(Enumerable.Repeat(text, times));
    }
}
