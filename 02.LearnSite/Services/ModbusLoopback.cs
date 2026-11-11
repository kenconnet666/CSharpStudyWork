using System.Buffers.Binary;
using System.Net.Sockets;

namespace LearnSite.Services;

/// <summary>
/// 本机回环上的 Modbus TCP 从站（模拟设备）+ 主站客户端。
/// 收帧时先读 7 字节头、再按头里的"长度"读剩下的——这就是 TCP 一课讲的"分帧"。
/// </summary>
public sealed class ModbusLoopback : LoopbackServer
{
    ushort transactionId;

    /// <summary>为 true 时从站收到请求也不回应——模拟网线被拔、设备死机，主站会真的等到超时。</summary>
    public bool Silent { get; set; }

    public ModbusDevice Device { get; } = new();

    public ushort NextTransactionId() => ++transactionId;

    protected override async Task ServeAsync(NetworkStream stream, CancellationToken token)
    {
        while (await ReadFrameAsync(stream, token) is { } request)
            if (!Silent) await stream.WriteAsync(Device.Handle(request), token);
    }

    /// <summary>发出一帧请求并等待应答；超过 <paramref name="timeout"/>（默认 2 秒）没有应答抛 <see cref="OperationCanceledException"/>。</summary>
    public async Task<byte[]> RequestAsync(byte[] request, TimeSpan? timeout = null)
    {
        var stream = ClientStream;
        await stream.WriteAsync(request);
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(2));
        return await ReadFrameAsync(stream, cts.Token) ?? throw new IOException("对方关闭了连接");
    }

    /// <summary>读一整帧：先 7 字节头，再按头里的长度字段读剩余字节。对方关闭连接返回 null。</summary>
    static async Task<byte[]?> ReadFrameAsync(NetworkStream stream, CancellationToken token)
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
