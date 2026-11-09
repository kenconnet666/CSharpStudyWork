using System.Buffers.Binary;
using System.Globalization;
using System.Numerics;

namespace CSharpSyntax.Exercises;

/// <summary>
/// 练习 1：基础与类型系统（对应第 1、2 课）。
/// 读法：每道题先看【题目】，自己想一想再往下看【答案】；【预期输出】可以和你的推演对照。
/// 想验证就运行：dotnet run -- 12
/// </summary>
public static class Exercise01BasicsTypes
{
    // ------------------------------------------------------------------
    // 本练习用到的类型
    // ------------------------------------------------------------------

    /// <summary>用 byte 作底层类型的报警标志，每个成员占 1 位。</summary>
    [Flags]
    private enum Alarm : byte
    {
        None = 0,
        Overheat = 1 << 0,    // 0000_0001
        Overload = 1 << 1,    // 0000_0010
        Offline = 1 << 2,     // 0000_0100
        LowPressure = 1 << 3, // 0000_1000
    }

    /// <summary>record：值相等的不可变数据类型。静态方法放在 record 里也可以。</summary>
    private record Point3D(double X, double Y, double Z)
    {
        public static double Distance(Point3D a, Point3D b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            var dz = a.Z - b.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    /// <summary>普通 class：没有重写 Equals，== 比较的是"是不是同一个对象"。</summary>
    private class Point3DClass
    {
        public double X, Y, Z;
    }

    public static void Run()
    {
        Exercise1_TupleAndTryParse();
        Exercise2_RecordVsClass();
        Exercise3_FlagsOperations();
        Exercise4_Endianness();
        Exercise5_ByteTraps();
        Extension_Conversions();
    }

    // ==================================================================
    // 题目 1：元组返回多个值 + TryParse
    // ------------------------------------------------------------------
    // 【题目】
    //   写 TryParseNumbers(string line, out List<double> numbers)：
    //   把 "3, 5.5, 10" 这样用逗号分隔的字符串解析成数字列表，任何一项解析失败就返回 false。
    //   再写 Stats(IEnumerable<double>)，用"命名元组"一次返回 (平均值, 最大值)。
    // 【思路】
    //   1. Split 时用 TrimEntries 去掉每段两端空格，RemoveEmptyEntries 丢掉空段。
    //   2. 解析小数要指定 CultureInfo.InvariantCulture，否则在某些语言环境下
    //      小数点会被当成逗号，同一份代码在别人电脑上就解析失败（上位机软件很容易遇到）。
    //   3. out 参数在方法返回前必须赋值，所以一进来先给 numbers 一个空列表。
    // 【预期输出】
    //   "3, 5.5, 10"  -> 解析成功：平均 6.17，最大 10
    //   "1, abc"      -> 解析失败
    //   ""            -> 解析失败（没有任何数字）
    // ==================================================================
    private static void Exercise1_TupleAndTryParse()
    {
        Print.Section("练习 1：元组 + TryParse");

        foreach (var line in new[] { "3, 5.5, 10", "1, abc", "" })
        {
            if (TryParseNumbers(line, out var numbers))
            {
                var (avg, max) = Stats(numbers);   // 解构命名元组
                Print.Line($"\"{line}\"", $"解析成功：平均 {avg:F2}，最大 {max}");
            }
            else
            {
                Print.Line($"\"{line}\"", "解析失败");
            }
        }
    }

    private static bool TryParseNumbers(string line, out List<double> numbers)
    {
        numbers = [];                                   // out 参数必须先赋值
        var parts = line.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                return false;                           // 早返回：任何一项失败就整体失败
            numbers.Add(value);
        }

        return numbers.Count > 0;                       // 空字符串解析出 0 个数字，也算失败
    }

    private static (double Avg, double Max) Stats(IEnumerable<double> values) =>
        (values.Average(), values.Max());

    // ==================================================================
    // 题目 2：record 与 class 的相等性
    // ------------------------------------------------------------------
    // 【题目】
    //   定义 record Point3D(X, Y, Z) 和一个 static 方法求两点距离；
    //   再比较"两个值相同的 record"与"两个值相同的普通 class"。
    // 【思路】
    //   record 自动生成"按成员值比较"的 Equals 和 ==；class 默认比较引用（是不是同一个对象）。
    //   距离 = sqrt(dx² + dy² + dz²)。(0,0,0) 到 (1,2,2)：1+4+4=9，开方得 3。
    // 【预期输出】
    //   距离: 3
    //   record 值相同 ==: True
    //   class  值相同 ==: False
    //   with 复制后: Point3D { X = 1, Y = 2, Z = 99 }
    // ==================================================================
    private static void Exercise2_RecordVsClass()
    {
        Print.Section("练习 2：record 与 class 相等性");

        var origin = new Point3D(0, 0, 0);
        var p = new Point3D(1, 2, 2);
        Print.Line("距离", Point3D.Distance(origin, p));

        Print.Line("record 值相同 ==", new Point3D(1, 2, 3) == new Point3D(1, 2, 3));

        var c1 = new Point3DClass { X = 1, Y = 2, Z = 3 };
        var c2 = new Point3DClass { X = 1, Y = 2, Z = 3 };
        Print.Line("class  值相同 ==", c1 == c2);   // 两个不同对象，引用不等

        // with：复制一份，只改指定的成员；原对象不变（不可变数据的常用写法）
        Print.Line("with 复制后", p with { Z = 99 });
    }

    // ==================================================================
    // 题目 3：[Flags] 枚举的位操作
    // ------------------------------------------------------------------
    // 【题目】
    //   从 Alarm.None 开始依次做：置位 Overload → 置位 Offline → 翻转 Offline → 翻转 LowPressure。
    //   然后判断 LowPressure 是否为 1；再把字节 0b0000_1010 转成 Alarm；最后数一数有几位是 1。
    // 【思路】 四个运算符：
    //     |=  置位（把某位设为 1）    &= ~  清除（设为 0）
    //     ^=  翻转（0↔1）             (x & flag) != 0  检测
    //   0b0000_1010：第 1 位(Overload=2) 和第 3 位(LowPressure=8) 为 1。
    //   数 1 的个数叫"汉明重量"，BitOperations.PopCount 一步到位，不用自己写循环。
    // 【预期输出】
    //   置位 Overload 后: Overload
    //   置位 Offline 后: Overload, Offline
    //   翻转 Offline 后: Overload                (Offline 本来是 1，翻转后变 0)
    //   翻转 LowPressure 后: Overload, LowPressure  (LowPressure 本来是 0，翻转后变 1)
    //   LowPressure 是否为 1: True
    //   字节 0b00001010 转枚举: Overload, LowPressure
    //   其中为 1 的位数: 2
    // ==================================================================
    private static void Exercise3_FlagsOperations()
    {
        Print.Section("练习 3：Flags 位操作");

        var alarm = Alarm.None;

        alarm |= Alarm.Overload;
        Print.Line("置位 Overload 后", alarm);

        alarm |= Alarm.Offline;
        Print.Line("置位 Offline 后", alarm);

        alarm ^= Alarm.Offline;
        Print.Line("翻转 Offline 后", alarm);

        alarm ^= Alarm.LowPressure;
        Print.Line("翻转 LowPressure 后", alarm);

        Print.Line("LowPressure 是否为 1", (alarm & Alarm.LowPressure) != 0);

        // 从协议里收到一个状态字节，直接强转成枚举即可
        var fromByte = (Alarm)0b0000_1010;
        Print.Line("字节 0b00001010 转枚举", fromByte);

        // 先转 byte 再转 uint，是因为 PopCount 的参数是 uint
        Print.Line("其中为 1 的位数", BitOperations.PopCount((uint)(byte)fromByte));
    }

    // ==================================================================
    // 题目 4：大端与小端
    // ------------------------------------------------------------------
    // 【题目】
    //   (a) 字节 [0x00, 0x0A] 分别按大端、小端读成 ushort，各是多少？
    //   (b) 字节 [0x00, 0x01, 0x00, 0x02] 按大端读成 uint，再按小端读，各是多少？
    //   (c) 把 short 值 -2 按大端写成 2 个字节，内容是什么？
    // 【思路】
    //   (a) 大端：高字节在前 → 0x000A = 10；小端：低字节在前 → 0x0A00 = 2560。
    //   (b) 大端 0x00010002 = 65536 + 2 = 65538；小端 0x02000100 = 33554432 + 256 = 33554688。
    //   (c) -2 的 16 位补码是 0xFFFE（-1 是 0xFFFF，-2 再少 1）；大端写出来就是 FF FE。
    // 【预期输出】
    //   (a) 大端=10, 小端=2560
    //   (b) 大端=65538, 小端=33554688
    //   (c) FFFE
    // ==================================================================
    private static void Exercise4_Endianness()
    {
        Print.Section("练习 4：大端与小端");

        byte[] a = [0x00, 0x0A];
        Print.Line("(a)", $"大端={BinaryPrimitives.ReadUInt16BigEndian(a)}, 小端={BinaryPrimitives.ReadUInt16LittleEndian(a)}");

        byte[] b = [0x00, 0x01, 0x00, 0x02];
        Print.Line("(b)", $"大端={BinaryPrimitives.ReadUInt32BigEndian(b)}, 小端={BinaryPrimitives.ReadUInt32LittleEndian(b)}");

        var buffer = new byte[2];
        BinaryPrimitives.WriteInt16BigEndian(buffer, -2);
        Print.Line("(c)", Convert.ToHexString(buffer));
    }

    // ==================================================================
    // 题目 5：byte 的三个陷阱
    // ------------------------------------------------------------------
    // 【题目】 设 byte b = 0xF0（二进制 1111_0000，十进制 240），求：
    //   (a) 把它当有符号字节 sbyte，值是多少？
    //   (b) int shifted = b << 4，是多少？
    //   (c) 把 (b << 4) 强转回 byte，是多少？
    //   (d) b + b 强转回 byte，是多少？
    // 【思路】
    //   (a) 最高位 1 表示负数，补码 1111_0000 = 240 - 256 = -16。
    //   (b) C# 里 byte 做运算会先"提升"成 int，所以 0xF0 << 4 = 0xF00 = 3840，不会丢位。
    //   (c) 强转回 byte 只保留低 8 位：0xF00 → 0x00 = 0。
    //   (d) 240 + 240 = 480 = 0x1E0，低 8 位 0xE0 = 224。
    //   这三点的共同教训：byte 运算的结果是 int，转回 byte 会静默截断；
    //   想让溢出报错就用 checked(...)，想明确允许截断就写 unchecked(...)。
    // 【预期输出】
    //   (a) sbyte = -16
    //   (b) b << 4 = 3840
    //   (c) (byte)(b << 4) = 0
    //   (d) (byte)(b + b) = 224
    // ==================================================================
    private static void Exercise5_ByteTraps()
    {
        Print.Section("练习 5：byte 的陷阱");

        byte b = 0xF0;
        Print.Line("(a) sbyte", unchecked((sbyte)b));
        Print.Line("(b) b << 4", b << 4);
        Print.Line("(c) (byte)(b << 4)", unchecked((byte)(b << 4)));
        Print.Line("(d) (byte)(b + b)", unchecked((byte)(b + b)));
    }

    // ==================================================================
    // 扩展：进制转换与格式化（调试协议时天天用）
    // ------------------------------------------------------------------
    //   整数 → 十六进制：$"{255:X2}" 得 "FF"（X2 表示至少 2 位大写十六进制）
    //   整数 → 二进制：  Convert.ToString(5, 2).PadLeft(8, '0') 得 "00000101"
    //   十六进制文本 → 整数：int.Parse("FF", NumberStyles.HexNumber) 得 255
    //   字节数组 ↔ 十六进制字符串：Convert.ToHexString / Convert.FromHexString
    // 【预期输出】
    //   255 的十六进制: FF
    //   5 的 8 位二进制: 00000101
    //   "FF" 解析为: 255
    //   [1,2,255] 的十六进制串: 0102FF
    //   "0A0B" 还原为字节: 10,11
    // ==================================================================
    private static void Extension_Conversions()
    {
        Print.Section("扩展：进制转换");

        Print.Line("255 的十六进制", $"{255:X2}");
        Print.Line("5 的 8 位二进制", Convert.ToString(5, 2).PadLeft(8, '0'));
        Print.Line("\"FF\" 解析为", int.Parse("FF", NumberStyles.HexNumber));
        Print.Line("[1,2,255] 的十六进制串", Convert.ToHexString([1, 2, 255]));
        Print.Line("\"0A0B\" 还原为字节", string.Join(",", Convert.FromHexString("0A0B")));
    }
}
