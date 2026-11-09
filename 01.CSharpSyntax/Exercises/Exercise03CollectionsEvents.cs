namespace CSharpSyntax.Exercises;

/// <summary>
/// 练习 3：泛型、集合、委托、事件、闭包（对应第 5、6 课）。
/// 运行：dotnet run -- 14
/// </summary>
public static class Exercise03CollectionsEvents
{
    // ------------------------------------------------------------------
    // 本练习用到的类型
    // ------------------------------------------------------------------

    /// <summary>
    /// 能"按时间顺序"取出数据的环形缓冲区。
    /// 第 5 课的版本只能原样返回内部数组，写满后顺序会乱；这里多记一个 _count，并算出起点。
    /// </summary>
    private class OrderedRing<T>(int capacity)
    {
        private readonly T[] _items = new T[capacity];
        private int _next;    // 下一个要写入的位置
        private int _count;   // 已有元素个数（最多 capacity）

        public void Add(T item)
        {
            _items[_next] = item;
            _next = (_next + 1) % _items.Length;
            if (_count < _items.Length) _count++;
        }

        public T[] ToOrderedArray()
        {
            var result = new T[_count];
            // 没写满：最旧的在下标 0；写满了：最旧的就是"下一个要被覆盖的位置" _next
            var start = _count < _items.Length ? 0 : _next;
            for (var i = 0; i < _count; i++)
                result[i] = _items[(start + i) % _items.Length];
            return result;
        }
    }

    /// <summary>
    /// 泛型缓存。where TKey : notnull 表示键不能为 null（Dictionary 本身要求键非空）。
    /// </summary>
    private class Cache<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _store = new();

        public bool TryGet(TKey key, out TValue? value) => _store.TryGetValue(key, out value);

        /// <summary>有就直接返回；没有才调用 factory 生成并存起来。</summary>
        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory)
        {
            if (!_store.TryGetValue(key, out var value))
            {
                value = factory(key);
                _store[key] = value;
            }

            return value;
        }
    }

    private class Ticker
    {
        // 参数只有一个 int，直接用 Action<int> 就够了，不必定义 EventArgs 子类
        public event Action<int>? Ticked;
        private int _n;

        public void Tick() => Ticked?.Invoke(++_n);
    }

    public static void Run()
    {
        Exercise1_OrderedRing();
        Exercise2_GenericCache();
        Exercise3_LazyYield();
        Exercise4_EventSubscribe();
        Exercise5_Closures();
        Exercise6_Retry();
    }

    // ==================================================================
    // 题目 1：按时间顺序输出的环形缓冲区
    // ------------------------------------------------------------------
    // 【题目】 容量 3，依次加入 1~5，要得到 [3,4,5]（最旧的在前）；只加入 1、2 要得到 [1,2]。
    // 【思路】
    //   未写满时，数据就是从下标 0 开始顺序存放的，直接按序读。
    //   写满后，_next 指向的位置存的是"最旧的数据"（下一个就要被覆盖的），从它开始绕一圈读。
    //   用 (start + i) % 容量 实现"绕圈"。
    // 【预期输出】
    //   加入 1~5: 3,4,5
    //   加入 1~2: 1,2
    // ==================================================================
    private static void Exercise1_OrderedRing()
    {
        Print.Section("练习 1：按时间顺序的环形缓冲区");

        var full = new OrderedRing<int>(3);
        for (var i = 1; i <= 5; i++) full.Add(i);
        Print.Line("加入 1~5", string.Join(",", full.ToOrderedArray()));

        var partial = new OrderedRing<int>(3);
        partial.Add(1);
        partial.Add(2);
        Print.Line("加入 1~2", string.Join(",", partial.ToOrderedArray()));
    }

    // ==================================================================
    // 题目 2：泛型缓存 Cache<TKey, TValue>
    // ------------------------------------------------------------------
    // 【题目】
    //   写 TryGet 和 GetOrAdd。调用两次 GetOrAdd("a", ...)，工厂函数应该只执行一次。
    // 【思路】
    //   把"生成值"的逻辑通过 Func 传进来（回调），缓存自己不关心值怎么来的。
    //   闭包变量 calls 用来统计工厂被调用了几次。
    // 【预期输出】
    //   第一次 GetOrAdd: A
    //   第二次 GetOrAdd: A
    //   工厂函数调用次数: 1
    //   TryGet("a"): True, 值=A
    //   TryGet("zzz"): False
    // ==================================================================
    private static void Exercise2_GenericCache()
    {
        Print.Section("练习 2：泛型缓存");

        var cache = new Cache<string, string>();
        var calls = 0;

        string Factory(string key)
        {
            calls++;
            return key.ToUpper();
        }

        Print.Line("第一次 GetOrAdd", cache.GetOrAdd("a", Factory));
        Print.Line("第二次 GetOrAdd", cache.GetOrAdd("a", Factory));
        Print.Line("工厂函数调用次数", calls);

        Print.Line("TryGet(\"a\")", cache.TryGet("a", out var v) ? $"True, 值={v}" : "False");
        Print.Line("TryGet(\"zzz\")", cache.TryGet("zzz", out _));
    }

    // ==================================================================
    // 题目 3：yield 是惰性的
    // ------------------------------------------------------------------
    // 【题目】
    //   写 Evens(limit)：用 yield return 产生 0,2,4,...；每产生一个就记到 produced 列表。
    //   foreach 遍历时遇到 >= 4 就 break。produced 里有哪些数？
    // 【思路】
    //   yield 方法不会一次跑完：每次 foreach 取一个，才执行到下一个 yield return 就暂停。
    //   取到 4 时已经产生了 0、2、4，break 之后后面的 6、8…… 根本不会被计算。
    //   对照：如果先 .ToList()，就会把所有数都产生出来。
    // 【预期输出】
    //   break 前实际产生的数: 0,2,4
    //   ToList 后产生的数: 0,2,4,6
    // ==================================================================
    private static void Exercise3_LazyYield()
    {
        Print.Section("练习 3：yield 的惰性");

        var produced = new List<int>();
        foreach (var n in Evens(100, produced))
        {
            if (n >= 4) break;
        }

        Print.Line("break 前实际产生的数", string.Join(",", produced));

        var producedAll = new List<int>();
        _ = Evens(6, producedAll).ToList();     // ToList 会把序列全部取完
        Print.Line("ToList 后产生的数", string.Join(",", producedAll));
    }

    private static IEnumerable<int> Evens(int limit, List<int> produced)
    {
        for (var i = 0; i <= limit; i += 2)
        {
            produced.Add(i);   // 记录"这个数是什么时候被算出来的"
            yield return i;
        }
    }

    // ==================================================================
    // 题目 4：订阅与退订事件
    // ------------------------------------------------------------------
    // 【题目】
    //   Ticker 每次 Tick() 触发 Ticked 事件。订阅 → Tick 两次 → 退订 → 再 Tick 一次。
    //   订阅者收到了几次？
    // 【思路】
    //   要退订，必须保留"同一个委托变量"再 -=。直接写两遍相同的 Lambda 是两个不同对象，退不掉。
    //   退订之后的 Tick 不会再通知订阅者，所以只收到 2 次。
    // 【预期输出】
    //   订阅者收到: 收到 1, 收到 2
    // ==================================================================
    private static void Exercise4_EventSubscribe()
    {
        Print.Section("练习 4：订阅与退订事件");

        var ticker = new Ticker();
        var received = new List<string>();

        Action<int> handler = n => received.Add($"收到 {n}");
        ticker.Ticked += handler;
        ticker.Tick();
        ticker.Tick();

        ticker.Ticked -= handler;   // 退订同一个变量
        ticker.Tick();              // 没有订阅者了，?.Invoke 安全地什么也不做

        Print.Line("订阅者收到", string.Join(", ", received));
    }

    // ==================================================================
    // 题目 5：闭包（含 for 循环的经典陷阱）
    // ------------------------------------------------------------------
    // 【题目】
    //   (a) MakeAdder(5)(3) 的结果？
    //   (b) MakeCounter() 返回的函数连续调用三次分别返回什么？
    //   (c) for 循环里创建三个 Lambda，每个都"记录 i"，循环结束后依次调用，得到什么？
    //       怎么改成得到 0,1,2？
    // 【思路】
    //   (a) MakeAdder 返回 x => x + n，把 n 捕获了进去，所以 5 + 3 = 8。
    //   (b) 每次 MakeCounter() 调用都有一个独立的 c；返回的 Lambda 持有它并自增 → 1,2,3。
    //   (c) 闭包捕获的是"变量本身"，而 for 的 i 整个循环只有一个变量；
    //       Lambda 真正被调用时循环已结束，i 已经是 3，所以三次都是 3。
    //       解决：每次迭代里 var copy = i; 捕获 copy（每次迭代是新变量）。
    //       foreach 的迭代变量每轮都是新的，所以没有这个问题。
    // 【预期输出】
    //   (a) 8
    //   (b) 1,2,3
    //   (c) 陷阱写法: 3,3,3
    //       修正写法: 0,1,2
    // ==================================================================
    private static void Exercise5_Closures()
    {
        Print.Section("练习 5：闭包");

        Print.Line("(a)", MakeAdder(5)(3));

        var counter = MakeCounter();
        Print.Line("(b)", $"{counter()},{counter()},{counter()}");

        var bad = new List<Func<int>>();
        for (var i = 0; i < 3; i++) bad.Add(() => i);
        Print.Line("(c) 陷阱写法", string.Join(",", bad.Select(f => f())));

        var good = new List<Func<int>>();
        for (var i = 0; i < 3; i++)
        {
            var copy = i;               // 每次迭代一个新变量
            good.Add(() => copy);
        }

        Print.Line("(c) 修正写法", string.Join(",", good.Select(f => f())));
    }

    private static Func<int, int> MakeAdder(int n) => x => x + n;

    private static Func<int> MakeCounter()
    {
        var c = 0;
        return () => ++c;
    }

    // ==================================================================
    // 题目 6：把"动作"当参数的重试方法
    // ------------------------------------------------------------------
    // 【题目】
    //   写 Retry(Func<int, bool> action, int times)：最多尝试 times 次，action 返回 true 表示成功，
    //   参数是第几次尝试。第 3 次才成功的动作、以及一直失败的动作，结果分别是什么？
    // 【思路】
    //   把"做什么"通过委托传进来，Retry 只负责"重复和计数"，通信、读文件、连数据库都能复用。
    // 【预期输出】
    //   第 3 次才成功: True，共尝试 3 次
    //   一直失败: False，共尝试 3 次
    // ==================================================================
    private static void Exercise6_Retry()
    {
        Print.Section("练习 6：重试方法");

        var attempts = 0;
        var ok = Retry(attempt => { attempts++; return attempt == 3; }, 5);
        Print.Line("第 3 次才成功", $"{ok}，共尝试 {attempts} 次");

        attempts = 0;
        ok = Retry(_ => { attempts++; return false; }, 3);
        Print.Line("一直失败", $"{ok}，共尝试 {attempts} 次");
    }

    private static bool Retry(Func<int, bool> action, int times)
    {
        for (var attempt = 1; attempt <= times; attempt++)
        {
            if (action(attempt)) return true;
        }

        return false;
    }
}
