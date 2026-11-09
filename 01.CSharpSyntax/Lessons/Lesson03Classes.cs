namespace CSharpSyntax.Lessons;

/// <summary>第 3 课：类与对象。属性（property）是 C# 相对 Java 最大的语法差异。</summary>
public static class Lesson03Classes
{
    // ---- 本课用到的类型 ----

    /// <summary>
    /// 传统写法的类：展示属性、构造函数、readonly、static、索引器。
    /// 访问修饰符：public 公开；private 仅本类（类成员的默认值）；protected 本类和子类；
    /// internal 同一个项目(程序集)内可见（顶层类型的默认值，Java 的包私有类似）。
    /// </summary>
    private class Motor
    {
        // 字段：通常 private，命名习惯 _小驼峰
        private readonly string _id;       // readonly：只能在声明处或构造函数里赋值（类似 Java 的 final 字段）
        private int _speed;

        // 静态成员：属于类型本身（和 Java 的 static 一样）
        public static int Count { get; private set; }

        // 构造函数：和类同名
        public Motor(string id)
        {
            _id = id;
            Count++;
        }

        // 自动属性：编译器自动生成隐藏字段。{ get; set; } 相当于 Java 的 getX()/setX()，但调用时像字段一样用
        public string Name { get; set; } = "未命名";   // 可以直接给初始值
        public int Id => int.Parse(_id);               // 只读属性（表达式体）：每次访问都计算

        // 带逻辑的属性：set 里校验，value 是赋的新值
        public int Speed
        {
            get => _speed;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "转速不能为负"); // nameof：获取标识符名字的字符串，重构安全
                _speed = value;
            }
        }

        // 表达式体方法：只有一个表达式时用 => 简化
        public string Describe() => $"电机[{_id}] {Name} 转速={Speed}";

        // 重写 ToString（所有类型都继承自 object）
        public override string ToString() => Describe();
    }

    /// <summary>
    /// 主构造函数（C# 12）：参数直接写在类名后面，整个类里都能使用。
    /// 适合依赖注入、简单数据类，省掉"字段 + 构造函数赋值"的样板代码。
    /// </summary>
    private class Sensor(string name, double value)
    {
        public string Name { get; } = name;       // 只能在构造时赋值的属性（只读自动属性）
        public double Value { get; set; } = value;
        public string Summary => $"{Name}={Value}";
    }

    /// <summary>
    /// init 与 required：创建对象时才能赋值，之后不可修改；required 强制调用方必须赋值。
    /// 配合对象初始化器 new T { ... } 写配置类特别舒服。
    /// </summary>
    private class ConnectionOptions
    {
        public required string Host { get; init; }   // 必填
        public int Port { get; init; } = 502;        // 选填，有默认值
        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(3);
    }

    /// <summary>索引器：让对象能像数组一样用 obj[i] 访问。</summary>
    private class Registers
    {
        private readonly ushort[] _data = new ushort[10];

        public ushort this[int address]
        {
            get => _data[address];
            set => _data[address] = value;
        }
    }

    // 说明：WinForm 里你会看到 Form1.cs 和 Form1.Designer.cs 两个文件，
    // 它们是同一个类，用 partial 关键字（"部分类"）拆成多个文件：public partial class Form1 : Form。

    public static void Run()
    {
        Print.Section("1. 属性、构造函数、静态成员");
        var motor = new Motor("1") { Name = "主轴", Speed = 1500 }; // 对象初始化器：构造后直接设置属性
        Print.Line("对象", motor);
        _ = new Motor("2");
        Print.Line("静态计数 Motor.Count", Motor.Count);

        try
        {
            motor.Speed = -1; // 触发属性里的校验
        }
        catch (ArgumentOutOfRangeException ex)
        {
            Print.Line("属性校验", ex.ParamName + " -> " + ex.Message.Split('\n')[0]);
        }

        Print.Section("2. 主构造函数 (C# 12)");
        var sensor = new Sensor("温度", 36.5);
        Print.Line("Sensor", sensor.Summary);

        Print.Section("3. init / required");
        var options = new ConnectionOptions { Host = "192.168.0.10" }; // 不写 Host 会编译报错
        // options.Port = 503;  // 编译错误：init 属性只能在初始化时赋值
        Print.Line("配置", $"{options.Host}:{options.Port}, 超时 {options.Timeout.TotalSeconds}s");

        Print.Section("4. 索引器");
        var regs = new Registers();
        regs[3] = 0x1234;
        Print.Line("regs[3]", $"0x{regs[3]:X4}");

        Print.Section("5. 命名参数、可选参数、params");
        // 可选参数 + 命名参数：不需要 Java 那样写一堆重载
        Print.Line("Connect", Connect("10.0.0.1"));
        Print.Line("Connect 命名参数", Connect("10.0.0.1", timeoutMs: 500));
        // params：可变参数，类似 Java 的 T... / JS 的 ...args
        Print.Line("Sum", Sum(1, 2, 3, 4));
    }

    private static string Connect(string host, int port = 502, int timeoutMs = 3000) =>
        $"{host}:{port} timeout={timeoutMs}ms";

    private static int Sum(params int[] numbers) => numbers.Sum();
}
