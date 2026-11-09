using System.Buffers.Binary;
using System.Threading.Channels;

namespace CSharpSyntax.Exercises;

/// <summary>
/// 扩展篇：把前 11 课的知识用到真实协议上（Modbus）。
/// 这是通往下一个模块（Socket / Modbus TCP / 串口）的热身，涉及：
///   字节数组 + Span、大端读写、CRC 校验、float 的字序、生产者-消费者。
/// 运行：dotnet run -- 17
/// </summary>
public static class Exercise06ProtocolExtension
{
    /// <summary>
    /// 32 位浮点数占两个 16 位寄存器，不同厂家排列不同。设 float 的 4 个字节从高到低为 A B C D：
    ///   ABCD：寄存器0=AB，寄存器1=CD    （标准大端）
    ///   CDAB：寄存器0=CD，寄存器1=AB    （两个寄存器对调，最常见的"怪"顺序）
    ///   BADC：寄存器0=BA，寄存器1=DC    （寄存器内字节对调）
    ///   DCBA：寄存器0=DC，寄存器1=BA    （完全反过来，小端）
    /// </summary>
    private enum WordOrder { ABCD, CDAB, BADC, DCBA }

    public static void Run()
    {
        Extension1_Crc16();
        Extension2_BuildRequests();
        Extension3_ParseResponse();
        Extension4_FloatWordOrder();
        ChannelDemo().GetAwaiter().GetResult();
    }

    // ==================================================================
    // 扩展 1：Modbus RTU 的 CRC16 校验
    // ------------------------------------------------------------------
    // 【背景】
    //   串口上的 Modbus RTU 每帧末尾有 2 字节 CRC16，接收方重新计算并对比，用来发现传输错误。
    // 【算法】（CRC-16/MODBUS）
    //   1. crc 初始值 0xFFFF。
    //   2. 对每个字节：crc ^= 该字节；然后循环 8 次：
    //        如果 crc 最低位是 1：crc = (crc >> 1) ^ 0xA001；否则：crc >>= 1。
    //   3. 发送时低字节在前、高字节在后（所以 CRC 是少数"小端"的部分）。
    // 【预期输出】
    //   010300000001 的 CRC: 0x0A84，发送顺序 840A
    //   01030000000A 的 CRC: 0xCDC5，发送顺序 C5CD
    //   这是网上最常见的两个标准例子，对不上说明算法写错了。
    // ==================================================================
    private static void Extension1_Crc16()
    {
        Print.Section("扩展 1：CRC16");

        foreach (byte[] data in new[] { new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 }, [0x01, 0x03, 0x00, 0x00, 0x00, 0x0A] })
        {
            var crc = Crc16(data);
            var wire = new byte[2];
            BinaryPrimitives.WriteUInt16LittleEndian(wire, crc);   // 低字节在前
            Print.Line($"{Convert.ToHexString(data)} 的 CRC", $"0x{crc:X4}，发送顺序 {Convert.ToHexString(wire)}");
        }
    }

    /// <summary>参数用 ReadOnlySpan：数组、数组的一段、栈内存都能传进来，而且不产生复制。</summary>
    private static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (ushort)(crc >> 1 ^ 0xA001) : (ushort)(crc >> 1);
        }

        return crc;
    }

    // ==================================================================
    // 扩展 2：构造"读保持寄存器"请求
    // ------------------------------------------------------------------
    // 【任务】 读从站 1 的保持寄存器，起始地址 0，数量 10，分别构造 RTU 帧和 TCP 帧。
    // 【结构】
    //   RTU（串口）：从站(1) 功能码(1) 起始地址(2) 数量(2) CRC(2，低字节在前) = 8 字节
    //   TCP（网络）：事务标识(2) 协议标识(2，固定 0) 后续长度(2) 从站(1) 功能码(1) 起始地址(2) 数量(2) = 12 字节
    //     没有 CRC（TCP 自己保证可靠）；"后续长度"指从"从站"开始的字节数，这里 = 1+1+2+2 = 6。
    //   16 位数都是大端。
    // 【预期输出】
    //   RTU: 01030000000AC5CD        （01 03 | 0000 | 000A | C5 CD）
    //   TCP: 00010000000601030000000A（0001 | 0000 | 0006 | 01 03 | 0000 | 000A）
    // ==================================================================
    private static void Extension2_BuildRequests()
    {
        Print.Section("扩展 2：构造请求帧");

        Print.Line("RTU", Convert.ToHexString(BuildRtuReadRequest(unit: 1, start: 0, count: 10)));
        Print.Line("TCP", Convert.ToHexString(BuildTcpReadRequest(transactionId: 1, unit: 1, start: 0, count: 10)));
    }

    private static byte[] BuildRtuReadRequest(byte unit, ushort start, ushort count)
    {
        var frame = new byte[8];
        frame[0] = unit;
        frame[1] = 0x03;
        // AsSpan(偏移)：对数组的一段做视图，写入的是原数组，不产生复制
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(2), start);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4), count);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6), Crc16(frame.AsSpan(0, 6)));
        return frame;
    }

    private static byte[] BuildTcpReadRequest(ushort transactionId, byte unit, ushort start, ushort count)
    {
        var frame = new byte[12];
        var span = frame.AsSpan();
        BinaryPrimitives.WriteUInt16BigEndian(span, transactionId);
        BinaryPrimitives.WriteUInt16BigEndian(span[2..], 0);   // 协议标识固定为 0
        BinaryPrimitives.WriteUInt16BigEndian(span[4..], 6);   // 后面还有 6 个字节
        span[6] = unit;
        span[7] = 0x03;
        BinaryPrimitives.WriteUInt16BigEndian(span[8..], start);
        BinaryPrimitives.WriteUInt16BigEndian(span[10..], count);
        return frame;
    }

    // ==================================================================
    // 扩展 3：解析"读保持寄存器"响应
    // ------------------------------------------------------------------
    // 【任务】 响应（去掉 CRC 之后）：[从站, 功能码, 字节数, 数据...]，
    //   例如 [01 03 04 12 34 00 0A]：字节数=4，所以有 2 个寄存器：0x1234 和 0x000A。
    // 【思路】 第 i 个寄存器从下标 3 + i*2 开始，读 2 个字节（大端）。
    //   用 ReadOnlySpan 的 Slice 取一段，避免复制数组。
    // 【预期输出】
    //   寄存器: 0x1234(4660), 0x000A(10)
    // ==================================================================
    private static void Extension3_ParseResponse()
    {
        Print.Section("扩展 3：解析响应");

        var registers = ParseRegisters([0x01, 0x03, 0x04, 0x12, 0x34, 0x00, 0x0A]);
        Print.Line("寄存器", string.Join(", ", registers.Select(r => $"0x{r:X4}({r})")));
    }

    private static ushort[] ParseRegisters(ReadOnlySpan<byte> response)
    {
        int byteCount = response[2];
        var registers = new ushort[byteCount / 2];
        for (var i = 0; i < registers.Length; i++)
            registers[i] = BinaryPrimitives.ReadUInt16BigEndian(response.Slice(3 + i * 2));
        return registers;
    }

    // ==================================================================
    // 扩展 4：float 的寄存器字序
    // ------------------------------------------------------------------
    // 【背景】 1.5f 的 IEEE754 位模式是 0x3FC00000，四个字节 A=3F B=C0 C=00 D=00。
    //   不同设备把它放进两个寄存器的方式不同（见 WordOrder 的说明）。
    //   调试时的口诀：读出来的值明显不对（极大、极小、NaN），先试一遍四种字序。
    // 【思路】 统一把四种情况还原成"高 16 位寄存器、低 16 位寄存器"，再按大端读 float：
    //   ABCD → (r0, r1)；CDAB → (r1, r0)；
    //   BADC → 每个寄存器内部字节对调；DCBA → 对调后再交换两个寄存器。
    //   ReverseEndianness 把一个 16 位数的两个字节对调。
    // 【预期输出】
    //   ABCD  [0x3FC0, 0x0000] -> 1.5
    //   CDAB  [0x0000, 0x3FC0] -> 1.5
    //   BADC  [0xC03F, 0x0000] -> 1.5
    //   DCBA  [0x0000, 0xC03F] -> 1.5
    //   故意读错：ABCD 的数据按 CDAB 读 -> 一个接近 0 的极小数（科学计数法，约 2.3E-41）
    // ==================================================================
    private static void Extension4_FloatWordOrder()
    {
        Print.Section("扩展 4：float 字序");

        (WordOrder Order, ushort R0, ushort R1)[] cases =
        [
            (WordOrder.ABCD, 0x3FC0, 0x0000),
            (WordOrder.CDAB, 0x0000, 0x3FC0),
            (WordOrder.BADC, 0xC03F, 0x0000),
            (WordOrder.DCBA, 0x0000, 0xC03F),
        ];

        foreach (var (order, r0, r1) in cases)
            Print.Line($"{order}  [0x{r0:X4}, 0x{r1:X4}]", $"-> {RegistersToFloat(r0, r1, order)}");

        Print.Line("故意读错：ABCD 的数据按 CDAB 读", $"-> {RegistersToFloat(0x3FC0, 0x0000, WordOrder.CDAB)}");
    }

    private static float RegistersToFloat(ushort r0, ushort r1, WordOrder order)
    {
        var (high, low) = order switch
        {
            WordOrder.ABCD => (r0, r1),
            WordOrder.CDAB => (r1, r0),
            WordOrder.BADC => (BinaryPrimitives.ReverseEndianness(r0), BinaryPrimitives.ReverseEndianness(r1)),
            _ => (BinaryPrimitives.ReverseEndianness(r1), BinaryPrimitives.ReverseEndianness(r0)),
        };

        Span<byte> bytes = stackalloc byte[4];   // 栈上的 4 字节临时缓冲，不产生垃圾
        BinaryPrimitives.WriteUInt16BigEndian(bytes, high);
        BinaryPrimitives.WriteUInt16BigEndian(bytes[2..], low);
        return BinaryPrimitives.ReadSingleBigEndian(bytes);
    }

    // ==================================================================
    // 扩展 5：生产者-消费者（Channel）
    // ------------------------------------------------------------------
    // 【背景】
    //   串口/Socket 的接收回调在后台线程，来得快；处理（解析、存库、刷新界面）可能慢。
    //   标准做法：回调里只"把数据放进队列"，另一个任务从队列里取出来慢慢处理。
    //   System.Threading.Channels 就是为此设计的线程安全异步队列，比 BlockingCollection 更现代。
    // 【思路】
    //   - 生产者：writer.WriteAsync 放数据，全部放完调用 Complete() 告诉消费者"没有更多了"。
    //   - 消费者：await foreach (var x in reader.ReadAllAsync())，队列空了就异步等待，
    //     Complete 后自动结束循环。
    // 【预期输出】
    //   消费者收到帧: 0101
    //   消费者收到帧: 0102
    //   消费者收到帧: 0103
    // ==================================================================
    private static async Task ChannelDemo()
    {
        Print.Section("扩展 5：生产者-消费者");

        var channel = Channel.CreateUnbounded<byte[]>();

        var producer = Task.Run(async () =>
        {
            for (byte i = 1; i <= 3; i++)
            {
                await channel.Writer.WriteAsync([0x01, i]);   // 模拟串口回调把帧放进队列
                await Task.Delay(20);
            }

            channel.Writer.Complete();                       // 告诉消费者：没有更多数据了
        });

        await foreach (var frame in channel.Reader.ReadAllAsync())
            Print.Line("消费者收到帧", Convert.ToHexString(frame));

        await producer;
    }
}
