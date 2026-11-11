using System.Buffers.Binary;

namespace LearnSite.Services;

/// <summary>一台模拟的 Modbus 设备：保持寄存器 0~9。</summary>
public sealed class ModbusDevice
{
    public const int RegisterCount = 10;

    /// <summary>0 温度（×10）、1 压力、2 状态字、3~4 一个 float（高字在前）、其余是计数。</summary>
    public ushort[] Registers { get; } = [253, 101, 0b0000_0101, 0x41C8, 0x0000, 0, 0, 0, 0, 0];

    /// <summary>处理一帧 Modbus TCP 请求，返回应答。</summary>
    public byte[] Handle(byte[] request)
    {
        var function = request[7];
        var address = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(8));
        var value = BinaryPrimitives.ReadUInt16BigEndian(request.AsSpan(10));

        switch (function)
        {
            case 3 when value is >= 1 and <= 125 && address + value <= RegisterCount:
                var body = new byte[value * 2];
                for (var i = 0; i < value; i++) BinaryPrimitives.WriteUInt16BigEndian(body.AsSpan(i * 2), Registers[address + i]);
                return Reply(request, [3, (byte)body.Length, .. body]);
            case 3:
                return Reply(request, [0x83, value is 0 or > 125 ? (byte)3 : (byte)2]);
            case 6 when address < RegisterCount:
                Registers[address] = value;
                return Reply(request, [6, .. request[8..12]]);
            case 6:
                return Reply(request, [0x86, 2]);
            default:
                return Reply(request, [(byte)(function | 0x80), 1]);
        }
    }

    /// <summary>应答 = 请求的前 4 字节（事务号、协议号）+ 新长度 + 单元号 + PDU。</summary>
    static byte[] Reply(byte[] request, byte[] pdu)
    {
        byte[] frame = new byte[7 + pdu.Length];
        request.AsSpan(0, 4).CopyTo(frame);
        BinaryPrimitives.WriteUInt16BigEndian(frame.AsSpan(4), (ushort)(1 + pdu.Length));
        frame[6] = request[6];
        pdu.CopyTo(frame, 7);
        return frame;
    }
}
