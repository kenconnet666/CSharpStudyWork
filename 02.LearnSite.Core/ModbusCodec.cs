using System.Buffers.Binary;

namespace LearnSite.Services;

/// <summary>一段报文里的一个字段：名字 + 字节。界面按字段上色展示。</summary>
public sealed record FrameField(string Name, byte[] Bytes, string Note);

/// <summary>Modbus 报文的编码与解码（对应 01.CSharpSyntax 的 Exercise06）。</summary>
public static class ModbusCodec
{
    /// <summary>Modbus RTU 用的 CRC16（多项式 0xA001，初值 0xFFFF）。</summary>
    public static ushort Crc16(ReadOnlySpan<byte> data)
    {
        ushort crc = 0xFFFF;
        foreach (var b in data)
        {
            crc ^= b;
            for (var i = 0; i < 8; i++)
                crc = (crc & 1) != 0 ? (ushort)((crc >> 1) ^ 0xA001) : (ushort)(crc >> 1);
        }
        return crc;
    }

    /// <summary>Modbus TCP 请求：7 字节 MBAP 头 + 功能码 + 起始地址 + 数量（或写入值）。</summary>
    public static (byte[] Frame, FrameField[] Fields) BuildTcpRequest(ushort transactionId, byte unit, byte function, ushort address, ushort countOrValue)
    {
        byte[] frame = new byte[12];
        BinaryPrimitives.WriteUInt16BigEndian(frame, transactionId);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(2), 0);   // 协议号固定 0
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4), 6);   // 后面还有几个字节：单元号 + 功能码 + 4
        frame[6] = unit;
        frame[7] = function;
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(8), address);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(10), countOrValue);

        FrameField[] fields =
        [
            new("事务号", frame[..2], "每次请求递增，回复原样带回，用来配对"),
            new("协议号", frame[2..4], "固定 00 00"),
            new("长度", frame[4..6], "后面还有 6 个字节"),
            new("单元号", frame[6..7], "从站地址"),
            new("功能码", frame[7..8], function == 3 ? "03 = 读保持寄存器" : "06 = 写单个寄存器"),
            new("起始地址", frame[8..10], "从第几个寄存器开始"),
            new(function == 3 ? "数量" : "写入值", frame[10..12], function == 3 ? "读几个寄存器" : "要写入的 16 位值"),
        ];
        return (frame, fields);
    }

    /// <summary>Modbus RTU 请求：单元号 + 功能码 + 起始地址 + 数量 + CRC（低字节在前）。</summary>
    public static byte[] BuildRtuRequest(byte unit, byte function, ushort address, ushort countOrValue)
    {
        byte[] frame = new byte[8];
        frame[0] = unit;
        frame[1] = function;
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(2), address);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4), countOrValue);
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(6), Crc16(frame.AsSpan(0, 6)));
        return frame;
    }

    /// <summary>把回复拆成字段（供界面展示）；读寄存器的回复会带上数据区。</summary>
    public static FrameField[] ExplainTcpResponse(byte[] response)
    {
        if (response.Length < 9) return [new("报文", response, "长度不足")];
        List<FrameField> fields =
        [
            new("事务号", response[..2], "和请求一致"),
            new("协议号", response[2..4], "固定 00 00"),
            new("长度", response[4..6], $"后面还有 {BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(4))} 个字节"),
            new("单元号", response[6..7], "从站地址"),
        ];
        var function = response[7];
        if ((function & 0x80) != 0)
        {
            fields.Add(new("功能码", response[7..8], $"{function:X2} = 请求的功能码 + 0x80，表示出错"));
            fields.Add(new("异常码", response[8..9], ExceptionText(response[8])));
        }
        else if (function == 3)
        {
            fields.Add(new("功能码", response[7..8], "03"));
            fields.Add(new("字节数", response[8..9], $"数据区 {response[8]} 字节"));
            fields.Add(new("寄存器数据", response[9..], "每个寄存器 2 字节，大端"));
        }
        else
        {
            fields.Add(new("功能码", response[7..8], function.ToString("X2")));
            fields.Add(new("回显", response[8..], "写操作原样回显地址和值"));
        }
        return [.. fields];
    }

    public static ushort[] ParseRegisters(byte[] response)
    {
        var byteCount = response[8];
        return [.. Enumerable.Range(0, byteCount / 2).Select(i => BinaryPrimitives.ReadUInt16BigEndian(response.AsSpan(9 + i * 2)))];
    }

    public static string ExceptionText(byte code) => code switch
    {
        1 => "01 非法功能码：从站不支持这个功能",
        2 => "02 非法数据地址：寄存器地址超出范围",
        3 => "03 非法数据值：数量或值不合法",
        _ => $"{code:X2} 其他异常",
    };
}
