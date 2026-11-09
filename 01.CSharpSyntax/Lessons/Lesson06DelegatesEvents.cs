namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 6 课：委托、事件、Lambda。
/// 这是 WinForm/WPF/串口/Socket 开发的核心机制：按钮点击 Click、串口收到数据 DataReceived 都是"事件"。
/// 对照：委托 ≈ Java 函数式接口 / JS 的函数类型；事件 ≈ addEventListener 的观察者模式，但由语言直接支持。
/// </summary>
public static class Lesson06DelegatesEvents
{
    // 自定义委托类型：声明"什么签名的方法可以赋给它"。
    // 日常几乎不用自己定义，用内置的 Action（无返回值）和 Func（有返回值）即可。
    private delegate int Calculator(int a, int b);

    /// <summary>
    /// 模拟一个会"收到数据"的串口。事件的标准写法：
    ///   EventHandler&lt;TEventArgs&gt;：约定参数为 (object? sender, TEventArgs e)，.NET 全部事件都遵循
    /// </summary>
    private class FakeSerialPort
    {
        // event 关键字：外部只能 += / -= 订阅，不能直接调用或覆盖（比公开委托字段安全）
        public event EventHandler<DataReceivedEventArgs>? DataReceived;

        public void SimulateReceive(string text)
        {
            // ?.Invoke：没有订阅者时 DataReceived 为 null，这样写避免空引用
            DataReceived?.Invoke(this, new DataReceivedEventArgs(text));
        }
    }

    private class DataReceivedEventArgs(string text) : EventArgs
    {
        public string Text { get; } = text;
    }

    public static void Run()
    {
        Print.Section("1. 委托：把方法当变量");
        Calculator add = Add;           // 方法组转换成委托
        Calculator mul = (a, b) => a * b; // Lambda 表达式：(参数) => 表达式，和 JS 箭头函数几乎一样
        Print.Line("add(2,3)", add(2, 3));
        Print.Line("mul(2,3)", mul(2, 3));

        Print.Section("2. Action 与 Func");
        Action<string> log = msg => Console.WriteLine($"  [日志] {msg}"); // 无返回值：Action<参数...>
        Func<int, int, int> max = (a, b) => Math.Max(a, b);               // 有返回值：Func<参数..., 返回值>（最后一个类型是返回值）
        log("Action 调用");
        Print.Line("Func 调用 max(4,7)", max(4, 7));

        // 委托可作为参数，用来传入"策略"（回调）
        Print.Line("Apply", Apply([1, 2, 3], x => x * 10).Aggregate("", (s, x) => s + x + " "));

        Print.Section("3. 闭包：Lambda 会捕获外部变量");
        var counter = 0;
        Action increase = () => counter++; // 捕获的是变量本身，不是值的拷贝（和 JS 一致）
        increase();
        increase();
        Print.Line("counter", counter); // 2

        Print.Section("4. 事件：发布—订阅");
        var port = new FakeSerialPort();

        // 订阅方式一：Lambda
        port.DataReceived += (sender, e) => Print.Line("订阅者 A 收到", e.Text);
        // 订阅方式二：普通方法（需要取消订阅时必须用具名方法）
        port.DataReceived += OnDataReceived;

        port.SimulateReceive("OK 36.5");

        port.DataReceived -= OnDataReceived; // 取消订阅，防止窗体关闭后还被回调（经典内存泄漏来源）
        port.SimulateReceive("第二次");       // 只剩 A 收到

        // 重要提醒：真实串口的 DataReceived 在"后台线程"触发，更新界面要切回 UI 线程，第 9 课会讲。
    }

    private static int Add(int a, int b) => a + b;

    private static IEnumerable<int> Apply(IEnumerable<int> source, Func<int, int> transform)
    {
        foreach (var item in source) yield return transform(item);
    }

    private static void OnDataReceived(object? sender, DataReceivedEventArgs e) =>
        Print.Line("订阅者 B 收到", e.Text);
}
