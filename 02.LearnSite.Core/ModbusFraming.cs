using System.Buffers.Binary;
using System.Net.Sockets;

namespace LearnSite.Services;

/// <summary>
/// 从 TCP 流里切出 Modbus TCP 的一整帧：先读 7 字节头，再按头里的"长度"字段读剩下的字节。
/// TCP 是字节流（见 Socket / TCP 一课），一次读到的可能只是半帧，所以必须读够再返回。
/// </summary>
public static class ModbusFraming
{
    /// <summary>读一整帧；对方关闭连接返回 null。</summary>
    public static async Task<byte[]?> ReadFrameAsync(NetworkStream stream, CancellationToken token)
    {
        var header = new byte[7];
        if (!await ReadExactAsync(stream, header, token)) return null;
        var rest = BinaryPrimitives.ReadUInt16BigEndian(header.AsSpan(4)) - 1;   // 长度里已包含单元号这 1 字节
        var frame = new byte[7 + rest];
        header.CopyTo(frame, 0);
        return await ReadExactAsync(stream, frame.AsMemory(7), token) ? frame : null;
    }

    static async Task<bool> ReadExactAsync(NetworkStream stream, Memory<byte> buffer, CancellationToken token)
    {
        var filled = 0;
        while (filled < buffer.Length)
        {
            var count = await stream.ReadAsync(buffer[filled..], token);
            if (count == 0) return false;
            filled += count;
        }
        return true;
    }
}
