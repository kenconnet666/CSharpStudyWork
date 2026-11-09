namespace CSharpSyntax.Lessons;

/// <summary>第 5 课：泛型与集合。集合接口和 Java 的 Collections 框架思路一致，只是名字不同。</summary>
public static class Lesson05GenericsCollections
{
    /// <summary>
    /// 自定义泛型类：固定容量的环形缓冲区（接收串口数据时很实用）。
    /// where T : struct 是泛型约束，限定 T 必须是值类型。C# 泛型是"真泛型"，运行时保留类型信息（Java 是类型擦除）。
    /// </summary>
    private class RingBuffer<T>(int capacity) where T : struct
    {
        private readonly T[] _items = new T[capacity];
        private int _next;

        public void Add(T item)
        {
            _items[_next] = item;
            _next = (_next + 1) % _items.Length;
        }

        public IReadOnlyList<T> Snapshot() => _items;
    }

    public static void Run()
    {
        ListAndArray();
        DictionaryAndSet();
        GenericMethod();
        YieldDemo();
    }

    private static void ListAndArray()
    {
        Print.Section("1. 数组与 List<T>");

        // 集合表达式（C# 12）：统一用 [] 创建数组、List、Span 等，编译器按目标类型决定
        int[] array = [1, 2, 3];
        List<int> list = [1, 2, 3];
        List<int> merged = [0, .. array, .. list, 99]; // .. 是展开(spread)，和 JS 的 ...array 一样
        Print.Line("展开合并", string.Join(",", merged));

        list.Add(4);
        list.RemoveAt(0);
        list.Insert(0, 100);
        Print.Line("List 操作后", string.Join(",", list));
        Print.Line("Count / Contains(3)", $"{list.Count} / {list.Contains(3)}"); // List 用 Count，数组用 Length

        // 二维数组 与 交错数组
        int[,] grid = new int[2, 3];  // 规则矩形
        grid[1, 2] = 7;
        Print.Line("二维数组 grid[1,2]", grid[1, 2]);
        int[][] jagged = [[1], [2, 3]]; // 数组的数组，每行长度可不同
        Print.Line("交错数组长度", $"{jagged[0].Length}, {jagged[1].Length}");

        // 常用接口：IEnumerable<T>(只能遍历) < ICollection<T>(有 Count/Add) < IList<T>(有下标)
        // 方法参数尽量用最小的接口（如 IEnumerable<T>/IReadOnlyList<T>），调用方更自由
    }

    private static void DictionaryAndSet()
    {
        Print.Section("2. Dictionary / HashSet / Queue");

        // 字典初始化器
        Dictionary<string, int> registers = new() // new() 目标类型推断，省略右边类型
        {
            ["Temperature"] = 365,
            ["Pressure"] = 101,
        };
        registers["Speed"] = 1500;           // 索引器赋值：不存在则新增，存在则覆盖

        // 取值：直接 registers["x"] 不存在会抛 KeyNotFoundException；用 TryGetValue 更安全
        if (registers.TryGetValue("Speed", out var speed)) Print.Line("TryGetValue", speed);
        Print.Line("GetValueOrDefault", registers.GetValueOrDefault("Missing", -1));

        // 遍历得到 KeyValuePair，可以直接解构
        foreach (var (key, value) in registers) Console.Write($"  {key}={value}");
        Console.WriteLine();

        var set = new HashSet<int> { 1, 2, 2, 3 }; // 自动去重
        Print.Line("HashSet 元素个数", set.Count);

        var queue = new Queue<string>();          // 先进先出：适合缓存待发送的指令
        queue.Enqueue("读温度");
        queue.Enqueue("读压力");
        Print.Line("Dequeue", queue.Dequeue());

        var ring = new RingBuffer<int>(3);
        for (var i = 1; i <= 5; i++) ring.Add(i);
        Print.Line("RingBuffer 只保留最近 3 个", string.Join(",", ring.Snapshot())); // 4,5,3 (环形存储顺序)
    }

    private static void GenericMethod()
    {
        Print.Section("3. 泛型方法与约束");
        // 编译器能推断出 T，调用时不必写 <int>
        Print.Line("Max(3, 9)", Max(3, 9));
        Print.Line("Max(\"a\", \"b\")", Max("a", "b"));
    }

    /// <summary>where T : IComparable&lt;T&gt;：要求 T 能比较大小，这样方法体里才能调用 CompareTo。</summary>
    private static T Max<T>(T a, T b) where T : IComparable<T> => a.CompareTo(b) >= 0 ? a : b;

    private static void YieldDemo()
    {
        Print.Section("4. yield return：惰性生成序列");
        // 返回 IEnumerable<T> 的方法里用 yield return，每次被请求才产生下一个值，不会一次性占满内存
        foreach (var n in Fibonacci().Take(8)) Console.Write($"  {n}");
        Console.WriteLine();
    }

    private static IEnumerable<long> Fibonacci()
    {
        long a = 0, b = 1;
        while (true) // 无限序列，调用方用 Take 截取
        {
            yield return a;
            (a, b) = (b, a + b);
        }
    }
}
