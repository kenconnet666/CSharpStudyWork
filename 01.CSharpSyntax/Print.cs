namespace CSharpSyntax;

/// <summary>课程里统一的输出辅助，让运行结果的层次一目了然。</summary>
public static class Print
{
    /// <summary>打印一个小节标题，例如 "1. 变量与类型推断"。</summary>
    public static void Section(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {title} ---");
    }

    /// <summary>打印"说明: 值"，用来展示某行代码的运行结果。</summary>
    public static void Line(string label, object? value) => Console.WriteLine($"  {label}: {value}");
}
