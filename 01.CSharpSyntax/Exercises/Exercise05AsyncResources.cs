using System.Collections.Concurrent;

namespace CSharpSyntax.Exercises;

/// <summary>
/// 练习 5：异步、取消、超时、资源释放、线程安全（对应第 9、10 课）。
/// 运行：dotnet run -- 16
/// 注意：本练习里有等待（几十到几百毫秒），运行会稍慢一点，属于正常。
/// </summary>
public static class Exercise05AsyncResources
{
    // ------------------------------------------------------------------
    // 本练习用到的类型
    // ------------------------------------------------------------------

    /// <summary>实现 IAsyncDisposable 的资源：释放本身也是异步的（比如要等缓冲区发送完）。</summary>
    private class AsyncResource(string name) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await Task.Delay(10);   // 模拟异步清理
            Console.WriteLine($"  [{name}] 已异步释放");
        }
    }

    // 课程入口是同步的 Run，用 GetAwaiter().GetResult() 等异步方法跑完。
    // 控制台里这样写没问题；在 WinForm/WPF 的 UI 线程里不能这样写（会死锁）。
    public static void Run() => RunAsync().GetAwaiter().GetResult();

    private static async Task RunAsync()
    {
        await Exercise1_RetryWithCancellation();
        await Exercise2_Timeout();
        await Exercise3_AsyncDispose();
        Exercise4_ThreadSafety();
    }

    // ==================================================================
    // 题目 1：带取消的异步重试
    // ------------------------------------------------------------------
    // 【题目】
    //   写 RetryAsync<T>(Func<int, Task<T>> action, int times, TimeSpan delay, CancellationToken ct)：
    //   失败就等 delay 后重试，最多 times 次；全部失败抛出最后一次的异常；
    //   外部取消时立刻停止，不再重试。
    //   测试 (a)：前两次失败，第三次成功。 (b)：永远失败，但 80ms 后被取消。
    // 【思路】
    //   - 循环 + try/catch；异常过滤器 `when (attempt < times)`：最后一次失败就不捕获，让异常自然抛出。
    //   - 取消产生的 OperationCanceledException 不应被当成"业务失败"去重试，用 `ex is not OperationCanceledException` 排除。
    //   - 等待间隔要用 Task.Delay(delay, ct)：取消时它会立刻抛 OperationCanceledException，
    //     而不是傻等到间隔结束。
    //   - (b) 的时间线：0ms 第 1 次失败 → 等 50ms → 50ms 第 2 次失败 → 等 50ms，
    //     在 80ms 被取消，等待被打断 → 抛取消异常（TaskCanceledException，是它的子类）。
    // 【预期输出】
    //   (a) 第 1 次失败
    //   (a) 第 2 次失败
    //   (a) 成功，结果=第 3 次的数据
    //   (b) 第 1 次失败
    //   (b) 第 2 次失败
    //   (b) 被取消，不再重试
    // ==================================================================
    private static async Task Exercise1_RetryWithCancellation()
    {
        Print.Section("练习 1：带取消的异步重试");

        // (a) 第三次成功
        var result = await RetryAsync(
            attempt =>
            {
                if (attempt < 3)
                {
                    Print.Line("(a)", $"第 {attempt} 次失败");
                    throw new IOException("模拟通信失败");
                }

                return Task.FromResult("第 3 次的数据");
            },
            times: 5, delay: TimeSpan.FromMilliseconds(20), CancellationToken.None);
        Print.Line("(a)", $"成功，结果={result}");

        // (b) 永远失败，80ms 后外部取消
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(80));
        try
        {
            await RetryAsync<string>(
                attempt =>
                {
                    Print.Line("(b)", $"第 {attempt} 次失败");
                    throw new IOException("模拟通信失败");
                },
                times: 10, delay: TimeSpan.FromMilliseconds(50), cts.Token);
        }
        catch (OperationCanceledException)
        {
            Print.Line("(b)", "被取消，不再重试");
        }
    }

    private static async Task<T> RetryAsync<T>(Func<int, Task<T>> action, int times, TimeSpan delay, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await action(attempt);
            }
            catch (Exception ex) when (attempt < times && ex is not OperationCanceledException)
            {
                await Task.Delay(delay, ct);   // 取消会在这里被立刻响应
            }
        }
    }

    // ==================================================================
    // 题目 2：给任意异步操作加超时
    // ------------------------------------------------------------------
    // 【题目】
    //   读设备要 300ms，但我们最多等 100ms，超时了要得到 TimeoutException，而不是一直卡着。
    // 【思路】
    //   Task.WaitAsync(TimeSpan)（.NET 6 起）：给任何 Task 加超时，到时间抛 TimeoutException。
    //   重要认知：WaitAsync 只是"不再等了"，被包装的原任务并不会被取消，它还在后台跑。
    //   真正需要停掉的操作，要把 CancellationToken 一直传进去（见练习 1）。
    // 【预期输出】
    //   正常读取（50ms，限时 200ms）: 42
    //   慢速读取（300ms，限时 100ms）: 超时
    // ==================================================================
    private static async Task Exercise2_Timeout()
    {
        Print.Section("练习 2：超时");

        var fast = await ReadDeviceAsync(50, 42).WaitAsync(TimeSpan.FromMilliseconds(200));
        Print.Line("正常读取（50ms，限时 200ms）", fast);

        try
        {
            await ReadDeviceAsync(300, 1).WaitAsync(TimeSpan.FromMilliseconds(100));
        }
        catch (TimeoutException)
        {
            Print.Line("慢速读取（300ms，限时 100ms）", "超时");
        }
    }

    private static async Task<int> ReadDeviceAsync(int milliseconds, int value)
    {
        await Task.Delay(milliseconds);
        return value;
    }

    // ==================================================================
    // 题目 3：await using 与异常
    // ------------------------------------------------------------------
    // 【题目】
    //   在 try 块里用 await using 创建资源，然后抛异常。资源释放、异常被捕获，哪个先发生？
    // 【思路】
    //   和 using 一样：异常沿调用栈向外传播时，先退出资源所在的作用域，触发 DisposeAsync；
    //   异常传到外层 catch 时，资源已经释放完了。
    //   所以串口/文件/连接放进 using，哪怕中途出错也不会泄漏，也就不会出现"端口一直被占用"。
    // 【预期输出】（注意顺序）
    //   使用资源: 即将抛出异常
    //     [连接A] 已异步释放
    //   捕获异常: 模拟故障
    // ==================================================================
    private static async Task Exercise3_AsyncDispose()
    {
        Print.Section("练习 3：await using 与异常");

        try
        {
            await using var resource = new AsyncResource("连接A");
            Print.Line("使用资源", "即将抛出异常");
            throw new InvalidOperationException("模拟故障");
        }
        catch (InvalidOperationException ex)
        {
            Print.Line("捕获异常", ex.Message);
        }
    }

    // ==================================================================
    // 题目 4：多线程计数——三种做法
    // ------------------------------------------------------------------
    // 【题目】
    //   (a) 10000 个并行任务各给计数器加 1，用 Interlocked 保证结果正确。
    //   (b) 同样的事，不加任何保护，结果会怎样？
    //   (c) 并行统计单词出现次数，用线程安全字典。
    // 【思路】
    //   (a) counter++ 实际是"读-加-写"三步，多线程会互相覆盖。Interlocked.Increment 是原子操作，
    //       只是简单计数时，比 lock 更轻量。
    //   (b) 结果不固定，通常小于 10000，运行几次看看；偶尔碰巧等于 10000 并不代表代码正确。
    //   (c) ConcurrentDictionary.AddOrUpdate(键, 不存在时的初始值, (键, 旧值) => 新值) 是原子的。
    //       不要用普通 Dictionary 在多线程里写，会破坏内部结构，甚至死循环。
    // 【预期输出】
    //   (a) Interlocked: 10000
    //   (b) 不保护: 一个小于或等于 10000 的数（每次运行不同）
    //   (c) a=3, b=2, c=1
    // ==================================================================
    private static void Exercise4_ThreadSafety()
    {
        Print.Section("练习 4：多线程计数");

        var safe = 0;
        Parallel.For(0, 10_000, _ => Interlocked.Increment(ref safe));
        Print.Line("(a) Interlocked", safe);

        var unsafeCounter = 0;
        Parallel.For(0, 10_000, _ => unsafeCounter++);
        Print.Line("(b) 不保护", $"{unsafeCounter}（每次运行可能不同）");

        var words = new ConcurrentDictionary<string, int>();
        Parallel.ForEach(new[] { "a", "b", "a", "c", "a", "b" },
            word => words.AddOrUpdate(word, 1, (_, old) => old + 1));
        Print.Line("(c)", string.Join(", ", words.OrderBy(p => p.Key).Select(p => $"{p.Key}={p.Value}")));
    }
}
