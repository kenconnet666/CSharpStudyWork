using CSharpSyntax;
using CSharpSyntax.Exercises;
using CSharpSyntax.Lessons;

// 程序入口：C# 9 起支持"顶级语句"，不用再写 class Program { static void Main }，
// 这里的代码就是 Main 方法体（类似 JS 的脚本文件）。

// 控制台默认编码可能不是 UTF-8，设置后中文不会乱码
Console.OutputEncoding = System.Text.Encoding.UTF8;

// 课程清单：(标题, 入口方法)。新增一课只需在这里加一行。
// "Lesson01Basics.Run" 是方法组，会自动转换成 Action 委托（第 6 课会详细讲）
(string Title, Action Run)[] lessons =
[
    ("基础：变量、类型、字符串、控制流", Lesson01Basics.Run),
    ("类型系统：值类型/引用类型、struct、record、enum、byte 与字节序", Lesson02Types.Run),
    ("类与对象：属性、构造函数、主构造函数、索引器", Lesson03Classes.Run),
    ("面向对象：继承、接口、抽象类、多态", Lesson04Oop.Run),
    ("泛型与集合：List、Dictionary、集合表达式、yield", Lesson05GenericsCollections.Run),
    ("委托、事件、Lambda（上位机核心）", Lesson06DelegatesEvents.Run),
    ("LINQ：集合查询", Lesson07Linq.Run),
    ("模式匹配、switch 表达式、空值处理", Lesson08PatternMatching.Run),
    ("async/await 与多线程", Lesson09Async.Run),
    ("异常、using 与资源释放、lock", Lesson10ExceptionsResources.Run),
    ("C# 12~14 新特性（.NET 10）", Lesson11NewFeatures.Run),

    // 强化练习与扩展：每题都带题目、思路、答案和预期输出
    ("练习 1：基础与类型系统", Exercise01BasicsTypes.Run),
    ("练习 2：类与面向对象", Exercise02Classes.Run),
    ("练习 3：泛型、集合、事件、闭包", Exercise03CollectionsEvents.Run),
    ("练习 4：LINQ 与模式匹配", Exercise04LinqPatterns.Run),
    ("练习 5：异步、超时、资源、线程安全", Exercise05AsyncResources.Run),
    ("扩展篇：Modbus 协议热身（CRC、字节序、Span、Channel）", Exercise06ProtocolExtension.Run),
];

// 用法：
//   dotnet run            -> 显示菜单，输入编号运行
//   dotnet run -- 3       -> 直接运行第 3 课
//   dotnet run -- all     -> 依次运行全部
if (args.Length > 0)
{
    RunByInput(args[0]);
    return;
}

while (true)
{
    Console.WriteLine("==== C# 语法课程（面向有 Java/JS 基础的同学）====");
    for (var i = 0; i < lessons.Length; i++)
        Console.WriteLine($"  {i + 1,2}. {lessons[i].Title}");
    Console.WriteLine("   a. 全部运行      q. 退出");
    Console.Write("请输入编号: ");

    var input = Console.ReadLine()?.Trim();
    if (input is null or "q" or "Q") break;
    RunByInput(input);
    Console.WriteLine();
}

void RunByInput(string input)
{
    if (input.Equals("a", StringComparison.OrdinalIgnoreCase) || input.Equals("all", StringComparison.OrdinalIgnoreCase))
    {
        for (var i = 0; i < lessons.Length; i++) RunLesson(i);
    }
    else if (int.TryParse(input, out var n) && n >= 1 && n <= lessons.Length)
    {
        RunLesson(n - 1);
    }
    else
    {
        Console.WriteLine($"无效输入: {input}");
    }
}

void RunLesson(int index)
{
    Console.WriteLine();
    Console.WriteLine($"################ 第 {index + 1} 课：{lessons[index].Title} ################");
    lessons[index].Run();
}
