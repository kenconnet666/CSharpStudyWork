using System.Buffers.Binary;

namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 2 课：类型系统。重点是"值类型 vs 引用类型"和"byte 的细节"，
/// 这两点决定你后面写串口/Modbus/S7 协议时会不会踩坑。
/// </summary>
public static class Lesson02Types
{
    // ---- 本课用到的类型定义（C# 允许在类内部嵌套定义类型）----

    /// <summary>struct：值类型。赋值时整个复制（类似 Java 里的基本类型，但可以自定义）。</summary>
    private struct PointStruct
    {
        public int X;
        public int Y;
    }

    /// <summary>class：引用类型。赋值只复制引用（和 Java/JS 的对象一致）。</summary>
    private class PointClass
    {
        public int X;
        public int Y;
    }

    /// <summary>
    /// record：专为"数据载体"设计的引用类型，自动生成：
    /// 构造函数、属性、值相等比较(Equals)、ToString、解构、with 复制。Java 17 的 record 与它类似。
    /// </summary>
    private record Device(string Name, int Address);

    /// <summary>enum 枚举；[Flags] 表示可以按位组合，常用于状态字/权限位。</summary>
    [Flags]
    private enum AlarmFlags : byte // 可以指定底层类型，这里用 byte，对应协议里的 1 个字节
    {
        None = 0,
        Overheat = 1 << 0, // 0000_0001
        Overload = 1 << 1, // 0000_0010
        Offline = 1 << 2,  // 0000_0100
    }

    public static void Run()
    {
        ValueVsReference();
        RecordAndEnum();
        RefOutIn();
        ByteDetails();
    }

    private static void ValueVsReference()
    {
        Print.Section("1. 值类型 vs 引用类型");

        var s1 = new PointStruct { X = 1, Y = 1 };
        var s2 = s1;       // 复制了一份
        s2.X = 99;
        Print.Line("struct：改 s2 不影响 s1", $"s1.X={s1.X}, s2.X={s2.X}");

        var c1 = new PointClass { X = 1, Y = 1 };
        var c2 = c1;       // 两个变量指向同一个对象
        c2.X = 99;
        Print.Line("class：改 c2 会影响 c1", $"c1.X={c1.X}, c2.X={c2.X}");

        // 记忆：int/double/bool/char/struct/enum 是值类型；string/class/数组/委托是引用类型。
        // 值类型默认不能为 null；要表示"没有值"用 int? 这样的可空类型。
    }

    private static void RecordAndEnum()
    {
        Print.Section("2. record 与 enum");

        var a = new Device("PLC1", 1);
        var b = new Device("PLC1", 1);
        Print.Line("record 值相等 a == b", a == b);          // True（普通 class 比较的是引用，会是 False）
        Print.Line("record 自带 ToString", a);               // Device { Name = PLC1, Address = 1 }

        var c = a with { Address = 2 };                      // with：复制并修改部分字段（不可变对象的惯用法）
        Print.Line("with 复制修改", c);

        var (name, address) = a;                             // 解构
        Print.Line("解构", $"{name}, {address}");

        // 枚举的位运算：| 组合，& 检测，& ~ 清除
        var alarm = AlarmFlags.Overheat | AlarmFlags.Offline;
        Print.Line("组合状态", alarm);                       // Overheat, Offline
        Print.Line("是否包含 Offline", alarm.HasFlag(AlarmFlags.Offline));
        alarm &= ~AlarmFlags.Overheat;                       // 清除某一位
        Print.Line("清除 Overheat 后", alarm);
        Print.Line("枚举转字节", (byte)alarm);               // 4
    }

    private static void RefOutIn()
    {
        Print.Section("3. ref / out / in 参数修饰符");

        // out：方法"输出"多个结果，调用时变量可以不用先赋值（TryParse 就是这个套路）
        if (int.TryParse("42", out var number)) Print.Line("out", number);

        // ref：按引用传递，方法内修改会影响调用方的变量（Java 没有这个能力）
        var x = 1;
        var y = 2;
        Swap(ref x, ref y);
        Print.Line("ref 交换后", $"x={x}, y={y}");

        // in：按引用只读传递，避免复制大 struct，可以先不管
    }

    private static void Swap(ref int a, ref int b) => (a, b) = (b, a); // 元组交换，不需要临时变量

    private static void ByteDetails()
    {
        Print.Section("4. byte 与字节序（协议开发必会）");

        // 关键差异：Java 的 byte 是有符号 (-128~127)，C# 的 byte 是无符号 (0~255)，sbyte 才是有符号
        byte b = 200;
        var signed = unchecked((sbyte)b); // unchecked：允许溢出截断；默认情况下 int 溢出不报错，用 checked 才抛异常
        Print.Line("byte 200 作为 sbyte", signed);          // -56

        // checked：溢出时抛出 OverflowException，适合"不能静默出错"的场景
        try
        {
            var big = int.MaxValue;
            _ = checked(big + 1);
        }
        catch (OverflowException)
        {
            Print.Line("checked 溢出", "抛出 OverflowException");
        }

        // 字节数组：Modbus 里 0x1234 这样的 16 位寄存器值，传输时是 2 个字节
        byte[] frame = [0x12, 0x34];

        // 手工拼装：高字节左移 8 位 | 低字节 （大端：高字节在前，Modbus 就是大端）
        ushort manual = (ushort)(frame[0] << 8 | frame[1]);
        // 推荐用标准库：BinaryPrimitives 明确指定字节序，不会写错
        ushort bigEndian = BinaryPrimitives.ReadUInt16BigEndian(frame);
        ushort littleEndian = BinaryPrimitives.ReadUInt16LittleEndian(frame);
        Print.Line("手工拼装(大端)", $"0x{manual:X4}");
        Print.Line("BigEndian", $"0x{bigEndian:X4}");        // 0x1234
        Print.Line("LittleEndian", $"0x{littleEndian:X4}");  // 0x3412

        // 把 float 转成 4 个字节（PLC 的 REAL 类型），以及转回来
        var bytes = BitConverter.GetBytes(1.5f);             // 注意：BitConverter 用本机字节序（x86 为小端）
        Print.Line("1.5f 的字节", Convert.ToHexString(bytes));
        Print.Line("还原", BitConverter.ToSingle(bytes, 0));

        // 十六进制字符串互转
        Print.Line("字节数组转十六进制", Convert.ToHexString(frame));          // 1234
        Print.Line("十六进制转字节数组", string.Join(",", Convert.FromHexString("0A0B")));

        // 位操作：& | ^ ~ << >>，取第 n 位：(value >> n) & 1
        const byte status = 0b0000_0101;                      // 0b 开头是二进制字面量
        Print.Line("第 2 位是否为 1", (status >> 2 & 1) == 1);
    }
}
