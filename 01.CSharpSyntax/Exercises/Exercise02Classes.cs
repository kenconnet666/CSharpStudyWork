namespace CSharpSyntax.Exercises;

/// <summary>
/// 练习 2：类与面向对象（对应第 3、4 课）。
/// 运行：dotnet run -- 13
/// </summary>
public static class Exercise02Classes
{
    // ------------------------------------------------------------------
    // 本练习用到的类型
    // ------------------------------------------------------------------

    /// <summary>用 C# 14 的 field 关键字写带校验的属性。</summary>
    private class Counter
    {
        // private set：只有类内部能改 Count，外部只读
        public int Count { get; private set; }

        // field 就是编译器生成的隐藏字段。set 里把小于 1 的步长修正成 1。
        // 末尾的 "= 1" 是属性的初始值：它直接写入隐藏字段，不会走 set 里的校验。
        public int Step
        {
            get;
            set => field = value < 1 ? 1 : value;
        } = 1;

        public void Tick() => Count += Step;
    }

    private interface IShape
    {
        string Name { get; }
        double Area { get; }
    }

    // record 也可以实现接口；用主构造函数声明数据，属性自动生成
    private record Circle(double Radius) : IShape
    {
        public string Name => "圆";
        public double Area => Math.PI * Radius * Radius;
    }

    private record Rectangle(double Width, double Height) : IShape
    {
        public string Name => "矩形";
        public double Area => Width * Height;
    }

    private record Triangle(double Base, double Height) : IShape
    {
        public string Name => "三角形";
        public double Area => 0.5 * Base * Height;
    }

    /// <summary>带边界检查的寄存器区。索引器里统一校验地址。</summary>
    private class SafeRegisters(int size)
    {
        private readonly ushort[] _data = new ushort[size];

        public ushort this[int address]
        {
            get
            {
                Check(address);
                return _data[address];
            }
            set
            {
                Check(address);
                _data[address] = value;
            }
        }

        private void Check(int address)
        {
            // .NET 8 起的标准校验方法：不满足就抛 ArgumentOutOfRangeException，
            // 而且参数名会自动取调用处的表达式文本（这里就是 "address"），不用自己写 nameof。
            ArgumentOutOfRangeException.ThrowIfNegative(address);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(address, _data.Length);
        }
    }

    private class Base
    {
        public virtual string Virtual() => "Base.Virtual";
        public string Hidden() => "Base.Hidden";   // 没有 virtual，不能被重写
    }

    private class Derived : Base
    {
        public override string Virtual() => "Derived.Virtual"; // 重写：多态生效
        public new string Hidden() => "Derived.Hidden";        // new：只是"遮住"父类同名方法，不是多态
    }

    // 组合优于继承：轮询服务只依赖接口，不关心背后是什么协议
    private interface IPlcClient
    {
        string Name { get; }
        int ReadInt(int address);
    }

    private class FakeS7Client : IPlcClient
    {
        public string Name => "S7";
        public int ReadInt(int address) => address * 10;
    }

    private class FakeModbusClient : IPlcClient
    {
        public string Name => "Modbus";
        public int ReadInt(int address) => 1000 + address;
    }

    // 主构造函数接收接口：换一种协议，这个类一行都不用改
    private class PollingService(IPlcClient client)
    {
        public IEnumerable<string> Poll(params int[] addresses) =>
            addresses.Select(a => $"{client.Name}[{a}]={client.ReadInt(a)}");
    }

    public static void Run()
    {
        Exercise1_FieldKeyword();
        Exercise2_Polymorphism();
        Exercise3_IndexerValidation();
        Exercise4_OverrideVsNew();
        Exercise5_CompositionOverInheritance();
    }

    // ==================================================================
    // 题目 1：用 field 关键字写带校验的属性
    // ------------------------------------------------------------------
    // 【题目】
    //   写 class Counter：Count 外部只读；Step 属性不接受小于 1 的值（小于 1 自动改成 1）；
    //   Tick() 每次让 Count 增加 Step。
    // 【思路】
    //   C# 14 以前要手写 `private int _step` 再写 get/set；现在 `field` 直接代表隐藏字段。
    //   初始值 `= 1` 直接写进字段，不触发 set。
    // 【预期输出】
    //   初始 Step: 1
    //   设置 Step=0 后: 1      （0 被修正成 1）
    //   设置 Step=5，Tick 两次，Count: 10
    // ==================================================================
    private static void Exercise1_FieldKeyword()
    {
        Print.Section("练习 1：field 关键字");

        var counter = new Counter();
        Print.Line("初始 Step", counter.Step);

        counter.Step = 0;
        Print.Line("设置 Step=0 后", counter.Step);

        counter.Step = 5;
        counter.Tick();
        counter.Tick();
        Print.Line("设置 Step=5，Tick 两次，Count", counter.Count);
        // counter.Count = 99;   // 编译错误：Count 的 set 是 private
    }

    // ==================================================================
    // 题目 2：接口 + 多态
    // ------------------------------------------------------------------
    // 【题目】
    //   定义 IShape，写圆(半径1)、矩形(2×3)、三角形(底4高5)；放进 List<IShape>，求总面积，
    //   并按面积从大到小输出名字。
    // 【思路】
    //   List 的类型是接口，调用 .Area 时运行时自动走各自的实现，这就是多态。
    //   总面积 = π×1² + 6 + 10 = 19.14159...，保留两位小数 19.14。
    // 【预期输出】
    //   总面积: 19.14
    //   按面积降序: 三角形(10.00) > 矩形(6.00) > 圆(3.14)
    // ==================================================================
    private static void Exercise2_Polymorphism()
    {
        Print.Section("练习 2：接口与多态");

        List<IShape> shapes = [new Circle(1), new Rectangle(2, 3), new Triangle(4, 5)];

        Print.Line("总面积", shapes.Sum(s => s.Area).ToString("F2"));
        Print.Line("按面积降序", string.Join(" > ", shapes.OrderByDescending(s => s.Area).Select(s => $"{s.Name}({s.Area:F2})")));
    }

    // ==================================================================
    // 题目 3：索引器 + 参数校验
    // ------------------------------------------------------------------
    // 【题目】
    //   写 SafeRegisters(size)：用 regs[地址] 读写，地址越界（负数或 >= size）时抛
    //   ArgumentOutOfRangeException，并从异常里拿到出问题的参数名。
    // 【思路】
    //   索引器的 get/set 里都先调用同一个 Check 方法，校验逻辑只写一处（不重复）。
    //   ThrowIfNegative / ThrowIfGreaterThanOrEqual 是标准库现成的守卫方法。
    // 【预期输出】
    //   regs[3] = 0x1234
    //   regs[10] 越界，参数名: address
    //   regs[-1] 越界，参数名: address
    // ==================================================================
    private static void Exercise3_IndexerValidation()
    {
        Print.Section("练习 3：索引器与参数校验");

        var regs = new SafeRegisters(10);
        regs[3] = 0x1234;
        Print.Line("regs[3]", $"0x{regs[3]:X4}");

        foreach (var address in new[] { 10, -1 })
        {
            try
            {
                _ = regs[address];
            }
            catch (ArgumentOutOfRangeException ex)
            {
                Print.Line($"regs[{address}] 越界，参数名", ex.ParamName);
            }
        }
    }

    // ==================================================================
    // 题目 4：override 与 new 的区别（面试常考）
    // ------------------------------------------------------------------
    // 【题目】
    //   Base b = new Derived();
    //   分别调用 b.Virtual()、b.Hidden()、((Derived)b).Hidden()，结果各是什么？为什么？
    // 【思路】
    //   - Virtual 是 virtual + override：调用哪个版本，看"对象的真实类型"(Derived)，所以是 Derived 的。
    //   - Hidden 用的是 new：只是子类另外定义了一个同名方法，调用哪个看"变量的声明类型"。
    //     变量是 Base → Base 的；强转成 Derived → Derived 的。
    //   实际写代码几乎不会用 new 隐藏方法，容易造成困惑；知道区别、会读懂别人的代码即可。
    // 【预期输出】
    //   b.Virtual(): Derived.Virtual
    //   b.Hidden(): Base.Hidden
    //   ((Derived)b).Hidden(): Derived.Hidden
    // ==================================================================
    private static void Exercise4_OverrideVsNew()
    {
        Print.Section("练习 4：override 与 new");

        Base b = new Derived();
        Print.Line("b.Virtual()", b.Virtual());
        Print.Line("b.Hidden()", b.Hidden());
        Print.Line("((Derived)b).Hidden()", ((Derived)b).Hidden());
    }

    // ==================================================================
    // 题目 5：组合优于继承
    // ------------------------------------------------------------------
    // 【题目】
    //   定义 IPlcClient，写两个假的实现（S7 / Modbus），再写一个 PollingService 轮询若干地址。
    //   要求：换协议时 PollingService 不用改。
    // 【思路】
    //   PollingService 的构造函数接收接口类型，不接收具体类。
    //   这就是"依赖接口而不是实现"：以后真的接 Sharp7 或 Modbus 库时，只要写新的 IPlcClient 实现。
    // 【预期输出】
    //   S7[1]=10, S7[2]=20
    //   Modbus[1]=1001, Modbus[2]=1002
    // ==================================================================
    private static void Exercise5_CompositionOverInheritance()
    {
        Print.Section("练习 5：组合优于继承");

        Print.Line("S7", string.Join(", ", new PollingService(new FakeS7Client()).Poll(1, 2)));
        Print.Line("Modbus", string.Join(", ", new PollingService(new FakeModbusClient()).Poll(1, 2)));
    }
}
