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
        while (await ModbusFraming.ReadFrameAsync(stream, token) is { } request)
            if (!Silent) await stream.WriteAsync(Device.Handle(request), token);
    }

    /// <summary>发出一帧请求并等待应答；超过 <paramref name="timeout"/>（默认 2 秒）没有应答抛 <see cref="OperationCanceledException"/>。</summary>
    public async Task<byte[]> RequestAsync(byte[] request, TimeSpan? timeout = null)
    {
        var stream = ClientStream;
        await stream.WriteAsync(request);
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(2));
        return await ModbusFraming.ReadFrameAsync(stream, cts.Token) ?? throw new IOException("对方关闭了连接");
    }
}
