using System.Diagnostics;

namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 9 课：async/await 与多线程。
/// 写法和 JS 的 async/await 几乎一样：Task ≈ Promise。区别是 C# 有真正的多线程，所以要多留意"线程"。
/// </summary>
public static class Lesson09Async
{
    // 课程入口是同步的 Run()；控制台里用 GetAwaiter().GetResult() 等待异步方法结束。
    // 注意：在 WinForm/WPF 的 UI 线程里绝对不要这样阻塞等待，会死锁！应该让事件处理器本身 async void。
    public static void Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        await BasicAwait();
        await ParallelWait();
        await CancellationDemo();
        await AsyncStreamDemo();
        ThreadAndUiNote();
    }

    private static async Task BasicAwait()
    {
        Print.Section("1. async / await 基础");
        Print.Line("开始线程", Environment.CurrentManagedThreadId);

        // async 方法返回 Task（无返回值）或 Task<T>（有返回值）；约定方法名以 Async 结尾
        var value = await ReadRegisterAsync(1);   // await 不会阻塞线程，等待期间线程可去做别的事
        Print.Line("读到寄存器", value);
        Print.Line("await 之后线程", Environment.CurrentManagedThreadId); // 控制台程序可能与上面不同；UI 程序会自动回到 UI 线程
    }

    private static async Task<int> ReadRegisterAsync(int address)
    {
        await Task.Delay(100); // 模拟网络/串口耗时；永远用 Task.Delay 而不是 Thread.Sleep（后者会阻塞线程）
        return address * 100;
    }

    private static async Task ParallelWait()
    {
        Print.Section("2. 并发：Task.WhenAll");
        var sw = Stopwatch.StartNew();

        // 顺序执行 3 次，每次 100ms，共约 300ms
        for (var i = 0; i < 3; i++) await ReadRegisterAsync(i);
        Print.Line("顺序耗时(ms)", sw.ElapsedMilliseconds);

        // 同时发起 3 个，等全部完成，共约 100ms（类似 JS 的 Promise.all）
        sw.Restart();
        var results = await Task.WhenAll(ReadRegisterAsync(1), ReadRegisterAsync(2), ReadRegisterAsync(3));
        Print.Line("并发耗时(ms)", sw.ElapsedMilliseconds);
        Print.Line("结果", string.Join(",", results));

        // Task.Run：把"耗 CPU 的同步代码"放到线程池运行，避免卡住界面
        var sum = await Task.Run(() => Enumerable.Range(1, 1_000_000).Sum(x => (long)x));
        Print.Line("Task.Run 计算", sum);
    }

    private static async Task CancellationDemo()
    {
        Print.Section("3. 取消：CancellationToken");
        // 协作式取消：调用方发出信号(CancellationTokenSource)，被调用方定期检查/传递 token。
        // 上位机里常用于"点击停止按钮 → 结束轮询循环"。
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(350)); // 350ms 后自动取消
        var polls = 0;
        try
        {
            while (true)
            {
                await Task.Delay(100, cts.Token); // 传入 token，被取消时抛 OperationCanceledException
                polls++;
            }
        }
        catch (OperationCanceledException)
        {
            Print.Line("被取消，已轮询次数", polls);
        }
    }

    private static async Task AsyncStreamDemo()
    {
        Print.Section("4. 异步流 await foreach");
        // IAsyncEnumerable<T>：边生产边异步消费，适合"持续收到的数据"，比如连续轮询
        await foreach (var temperature in PollTemperatureAsync())
            Print.Line("收到温度", temperature);
    }

    private static async IAsyncEnumerable<double> PollTemperatureAsync()
    {
        for (var i = 0; i < 3; i++)
        {
            await Task.Delay(50);
            yield return 36.5 + i * 0.1;
        }
    }

    private static void ThreadAndUiNote()
    {
        Print.Section("5. 上位机开发必须记住的线程规则");
        Print.Line("规则 1", "界面控件只能在 UI 线程修改；串口/Socket 的回调在后台线程");
        Print.Line("规则 2", "WinForm: control.Invoke(() => ...) 或 BeginInvoke；WPF: Dispatcher.Invoke");
        Print.Line("规则 3", "更好的做法：await 之后自动回到 UI 线程，直接更新界面");
        Print.Line("规则 4", "UI 线程里不要写 task.Result / task.Wait() / Thread.Sleep，会卡死界面或死锁");
        Print.Line("规则 5", "async void 只用于事件处理器；其他地方一律返回 Task，否则异常无法被捕获");
    }
}
