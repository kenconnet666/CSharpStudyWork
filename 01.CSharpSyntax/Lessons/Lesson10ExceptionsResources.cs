namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 10 课：异常、资源释放与 lock。
/// 串口、Socket、文件、数据库连接都是"非托管资源"，必须及时释放，否则端口一直被占用。
/// </summary>
public static class Lesson10ExceptionsResources
{
    /// <summary>自定义异常：继承 Exception，携带业务上下文。</summary>
    private class PlcException(string message, int errorCode, Exception? inner = null) : Exception(message, inner)
    {
        public int ErrorCode { get; } = errorCode;
    }

    /// <summary>
    /// 实现 IDisposable 的资源类。真实场景里它代表 SerialPort / Socket / FileStream 等。
    /// 调用 Dispose() 释放资源；配合 using 可以自动调用。
    /// </summary>
    private class FakeConnection(string name) : IDisposable
    {
        public void Open() => Console.WriteLine($"  [{name}] 打开");
        public void Dispose() => Console.WriteLine($"  [{name}] 已释放");
    }

    public static void Run()
    {
        ExceptionDemo();
        UsingDemo();
        LockDemo();
    }

    private static void ExceptionDemo()
    {
        Print.Section("1. try / catch / finally");

        try
        {
            ReadFromPlc();
        }
        catch (PlcException ex) when (ex.ErrorCode == 10) // when 过滤器：条件满足才捕获，不满足继续向外抛
        {
            Print.Line("PLC 超时", ex.Message);
            Print.Line("内部异常", ex.InnerException?.GetType().Name);
        }
        catch (Exception ex)  // 兜底：先写具体的异常，最后才写基类
        {
            Print.Line("其他异常", ex.Message);
        }
        finally
        {
            Print.Line("finally", "无论是否异常都会执行");
        }

        // 抛出异常的两种写法：
        //   throw;       保留原始堆栈，重新抛出（推荐）
        //   throw ex;    会重置堆栈，丢失出错位置（不要这样写）
        // 原则：只捕获你能处理的异常；包装异常时一定把原异常作为 inner 传入，不要吞掉。
        // 参数校验用现成方法：ArgumentNullException.ThrowIfNull(x)、ArgumentOutOfRangeException.ThrowIfNegative(n)
    }

    private static void ReadFromPlc()
    {
        try
        {
            throw new TimeoutException("底层 Socket 超时");
        }
        catch (TimeoutException ex)
        {
            throw new PlcException("读取 DB1 失败", errorCode: 10, inner: ex); // 包装并保留原因
        }
    }

    private static void UsingDemo()
    {
        Print.Section("2. using：作用域结束自动 Dispose");

        // 传统 using 块：离开 { } 时自动释放，即使中间抛异常（等价 Java try-with-resources）
        using (var conn = new FakeConnection("连接A"))
        {
            conn.Open();
        }

        // using 声明 (C# 8)：不用写花括号，在当前作用域（这里是整个方法）结束时释放，嵌套更少
        using var connB = new FakeConnection("连接B");
        using var connC = new FakeConnection("连接C");
        connB.Open();
        connC.Open();
        Print.Line("提示", "方法结束时按声明的相反顺序释放：先 C 后 B");

        // 异步资源用 await using；实现的是 IAsyncDisposable，原理一样。
    }

    private static void LockDemo()
    {
        Print.Section("3. lock：多线程共享数据要加锁");

        var total = 0;
        // System.Threading.Lock 是 .NET 9 新增的专用锁类型，比用 object 当锁性能更好；lock 语句会自动识别
        var gate = new Lock();

        Parallel.For(0, 1000, _ =>
        {
            lock (gate) // 同一时刻只有一个线程能进入；离开自动释放
            {
                total++;
            }
        });
        Print.Line("1000 个任务各加 1，加锁结果", total); // 一定是 1000；去掉 lock 结果会小于 1000

        // 更简单的场景用原子操作：Interlocked.Increment(ref total)
        // 线程安全集合：ConcurrentDictionary / ConcurrentQueue；生产者消费者用 System.Threading.Channels
    }
}
