namespace CSharpSyntax.Exercises;

/// <summary>
/// 练习 4：LINQ 与模式匹配（对应第 7、8 课）。
/// 运行：dotnet run -- 15
/// </summary>
public static class Exercise04LinqPatterns
{
    // ------------------------------------------------------------------
    // 本练习用到的类型与数据
    // ------------------------------------------------------------------
    private record Order(string Customer, string Product, int Qty, decimal Price)
    {
        /// <summary>这一单的金额。</summary>
        public decimal Amount => Qty * Price;
    }

    // 数据表（后面的答案都基于它，可以先手算一遍）：
    //   Alice  Valve 2 x 10 = 20
    //   Bob    Pump  1 x 50 = 50
    //   Alice  Pump  1 x 50 = 50
    //   Carol  Valve 4 x 10 = 40
    //   Bob    Valve 3 x 10 = 30
    private static readonly List<Order> Orders =
    [
        new("Alice", "Valve", 2, 10m),
        new("Bob", "Pump", 1, 50m),
        new("Alice", "Pump", 1, 50m),
        new("Carol", "Valve", 4, 10m),
        new("Bob", "Valve", 3, 10m),
    ];

    private enum State { Idle, Running, Paused }

    private enum Command { Start, Pause, Stop }

    public static void Run()
    {
        Exercise1_GroupAndSum();
        Exercise2_TopProduct();
        Exercise3_ProductsPerCustomer();
        Exercise4_LazyPitfall();
        Exercise5_ParseFrame();
        Exercise6_StateMachine();
    }

    // ==================================================================
    // 题目 1：按客户统计总金额，从大到小
    // ------------------------------------------------------------------
    // 【题目】 求每个客户的总金额，按金额从高到低输出。
    // 【思路】 GroupBy(客户) → 每组 Sum(金额) → OrderByDescending。
    //   Alice = 20 + 50 = 70；Bob = 50 + 30 = 80；Carol = 40。
    // 【预期输出】
    //   Bob: 80
    //   Alice: 70
    //   Carol: 40
    // ==================================================================
    private static void Exercise1_GroupAndSum()
    {
        Print.Section("练习 1：分组求和");

        var totals = Orders
            .GroupBy(o => o.Customer)
            .Select(g => (Customer: g.Key, Total: g.Sum(o => o.Amount)))   // 每组转成一个命名元组
            .OrderByDescending(x => x.Total);

        foreach (var (customer, total) in totals) Print.Line(customer, total);
    }

    // ==================================================================
    // 题目 2：销售额最高的产品
    // ------------------------------------------------------------------
    // 【题目】 哪种产品的总销售额最高？是多少？
    // 【思路】 按产品分组求和；Valve = 20 + 40 + 30 = 90，Pump = 50 + 50 = 100。
    //   取最大值的办法：OrderByDescending(...).First()，或者 MaxBy。
    //   MaxBy(key) 一步返回"让 key 最大的那个元素"（.NET 6 起），比"排序再取第一个"更直接。
    // 【预期输出】
    //   销售额最高的产品: Pump，金额 100
    // ==================================================================
    private static void Exercise2_TopProduct()
    {
        Print.Section("练习 2：销售额最高的产品");

        var top = Orders
            .GroupBy(o => o.Product)
            .Select(g => (Product: g.Key, Total: g.Sum(o => o.Amount)))
            .MaxBy(x => x.Total);

        Print.Line("销售额最高的产品", $"{top.Product}，金额 {top.Total}");
    }

    // ==================================================================
    // 题目 3：每个客户买过哪些产品（去重），以及能否分页
    // ------------------------------------------------------------------
    // 【题目】 (a) 每个客户买过的产品名，去重后用 "/" 连接。
    //          (b) 把订单每 2 条分成一页，输出每页的客户名。
    // 【思路】 (a) 每组 Select(产品).Distinct()；string.Join 连接。
    //          (b) Chunk(2) 把序列切成固定大小的数组块，最后一块可能不满。
    // 【预期输出】
    //   (a) Alice: Valve/Pump
    //   (a) Bob: Pump/Valve
    //   (a) Carol: Valve
    //   (b) 第 1 页: Alice,Bob
    //   (b) 第 2 页: Alice,Carol
    //   (b) 第 3 页: Bob
    // ==================================================================
    private static void Exercise3_ProductsPerCustomer()
    {
        Print.Section("练习 3：去重与分页");

        foreach (var g in Orders.GroupBy(o => o.Customer))
            Print.Line($"(a) {g.Key}", string.Join("/", g.Select(o => o.Product).Distinct()));

        var page = 1;
        foreach (var chunk in Orders.Chunk(2))
            Print.Line($"(b) 第 {page++} 页", string.Join(",", chunk.Select(o => o.Customer)));
    }

    // ==================================================================
    // 题目 4：延迟执行的陷阱——重复遍历同一个查询
    // ------------------------------------------------------------------
    // 【题目】
    //   query = Orders.Where(谓词里 calls++ 并判断 Qty > 1)。
    //   先调用 query.Count()，再调用 query.Any()，谓词一共被执行了几次？
    //   如果先 ToList()，再对列表调用 Count 和 Any，谓词又执行几次？
    // 【思路】
    //   LINQ 查询是"每次遍历都重新执行"：
    //   - Count() 要遍历全部 5 条 → 5 次。
    //   - Any() 遇到第一条满足的就停：第 1 条 Alice Valve 数量 2 > 1 → 只执行 1 次。
    //     所以合计 6 次，而不是你以为的 5 次。
    //   - 先 ToList() 把结果固定下来：谓词只执行遍历那一次的 5 次，之后对列表的操作不再触发谓词。
    //   经验：一个查询要遍历多次，或谓词有开销/副作用时，先 ToList()。
    // 【预期输出】
    //   不缓存：Count()+Any() 谓词执行了 6 次
    //   先 ToList：谓词执行了 5 次
    // ==================================================================
    private static void Exercise4_LazyPitfall()
    {
        Print.Section("练习 4：延迟执行陷阱");

        var calls = 0;
        var query = Orders.Where(o =>
        {
            calls++;
            return o.Qty > 1;
        });

        _ = query.Count();
        _ = query.Any();
        Print.Line("不缓存：Count()+Any()", $"谓词执行了 {calls} 次");

        calls = 0;
        var cached = query.ToList();
        _ = cached.Count;
        _ = cached.Any();
        Print.Line("先 ToList", $"谓词执行了 {calls} 次");
    }

    // ==================================================================
    // 题目 5：用模式匹配解析 Modbus 帧
    // ------------------------------------------------------------------
    // 【题目】
    //   写 Describe(byte[] frame)，识别：
    //     读保持寄存器请求：[从站, 0x03, 地址高, 地址低, 数量高, 数量低, ...(后面的 CRC)]
    //     写单个寄存器请求：[从站, 0x06, 地址高, 地址低, 值高, 值低]
    //     异常响应：        [从站, 功能码|0x80, 异常码]     （功能码最高位置 1 表示出错）
    //     空帧、其他        → 对应提示
    // 【思路】
    //   - 列表模式 [a, b, ..] 匹配"至少 2 个元素"，.. 吞掉其余（这里是 CRC）。
    //   - 要求帧长度恰好为 6/3 的模式，就不要写 ..。
    //   - var x 把元素捕获成变量；字面量 0x03 直接比较。
    //   - 异常帧的功能码是 0x83 这样的数，无法用字面量匹配，所以用 `when` 子句加条件。
    //   - 16 位数 = 高字节 << 8 | 低字节（大端）。
    // 【预期输出】
    //   读请求: 从站 1 读保持寄存器，起始地址 0，数量 10
    //   异常帧: 从站 1 异常响应：功能码 0x03，非法数据地址
    //   写请求: 从站 17 写单个寄存器，地址 1，值 3
    //   空帧: 空帧
    //   短帧: 未知帧
    // ==================================================================
    private static void Exercise5_ParseFrame()
    {
        Print.Section("练习 5：模式匹配解析帧");

        Print.Line("读请求", Describe([0x01, 0x03, 0x00, 0x00, 0x00, 0x0A, 0xC5, 0xCD]));
        Print.Line("异常帧", Describe([0x01, 0x83, 0x02]));
        Print.Line("写请求", Describe([0x11, 0x06, 0x00, 0x01, 0x00, 0x03]));
        Print.Line("空帧", Describe([]));
        Print.Line("短帧", Describe([0x01]));
    }

    private static string Describe(byte[] frame) => frame switch
    {
        // 顺序重要：先匹配更具体的异常帧
        [var unit, var function, var code] when (function & 0x80) != 0 =>
            $"从站 {unit} 异常响应：功能码 0x{function & 0x7F:X2}，{ExceptionText(code)}",
        [var unit, 0x03, var addrHigh, var addrLow, var countHigh, var countLow, ..] =>
            $"从站 {unit} 读保持寄存器，起始地址 {addrHigh << 8 | addrLow}，数量 {countHigh << 8 | countLow}",
        [var unit, 0x06, var addrHigh, var addrLow, var valueHigh, var valueLow] =>
            $"从站 {unit} 写单个寄存器，地址 {addrHigh << 8 | addrLow}，值 {valueHigh << 8 | valueLow}",
        [] => "空帧",
        _ => "未知帧",
    };

    private static string ExceptionText(byte code) => code switch
    {
        1 => "非法功能",
        2 => "非法数据地址",
        3 => "非法数据值",
        _ => "其他异常",
    };

    // ==================================================================
    // 题目 6：用元组模式写状态机
    // ------------------------------------------------------------------
    // 【题目】
    //   设备有三个状态 Idle/Running/Paused，三个命令 Start/Pause/Stop。规则：
    //     Idle    + Start → Running
    //     Running + Pause → Paused
    //     Paused  + Start → Running
    //     Running 或 Paused + Stop → Idle
    //     其他组合：状态不变
    //   依次执行 Start, Pause, Start, Stop, Pause，每一步后的状态是什么？
    // 【思路】
    //   switch 表达式的输入写成元组 (当前状态, 命令)，每个分支写一种组合；
    //   `State.Running or State.Paused` 是逻辑模式，一行表示两个状态；`_` 兜底返回原状态。
    //   这样"状态转移表"直接就是代码，比一堆 if/else 清晰。
    // 【预期输出】
    //   Start: -> Running
    //   Pause: -> Paused
    //   Start: -> Running
    //   Stop: -> Idle
    //   Pause: -> Idle        （Idle 收到 Pause，不满足任何规则，状态不变）
    // ==================================================================
    private static void Exercise6_StateMachine()
    {
        Print.Section("练习 6：状态机");

        var state = State.Idle;
        foreach (var command in new[] { Command.Start, Command.Pause, Command.Start, Command.Stop, Command.Pause })
        {
            state = Next(state, command);
            Print.Line(command.ToString(), $"-> {state}");
        }
    }

    private static State Next(State state, Command command) => (state, command) switch
    {
        (State.Idle, Command.Start) => State.Running,
        (State.Running, Command.Pause) => State.Paused,
        (State.Paused, Command.Start) => State.Running,
        (State.Running or State.Paused, Command.Stop) => State.Idle,
        _ => state,
    };
}
