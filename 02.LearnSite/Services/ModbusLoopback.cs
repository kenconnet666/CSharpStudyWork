using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace LearnSite.Services;

/// <summary>
/// 本机回环上的 Modbus TCP 从站（模拟设备）+ 主站客户端。走真实的 TCP，
/// 收帧时先读 7 字节头、再按头里的"长度"读剩下的——这就是上一课讲的"分帧"。
/// </summary>
public sealed class ModbusLoopback : IDisposable
{
    readonly TcpListener listener = new(IPAddress.Loopback, 0);
    readonly CancellationTokenSource cts = new();
    TcpClient? client;
    ushort transactionId;

    public ModbusDevice Device { get; } = new();

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

    public bool Connected => client?.Connected == true;

    public void StartServer()
    {
        listener.Start();
        _ = AcceptLoop(cts.Token);
    }

    async Task AcceptLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
                _ = Serve(await listener.AcceptTcpClientAsync(token), token);
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    async Task Serve(TcpClient peer, CancellationToken token)
    {
        using (peer)
        {
            var stream = peer.GetStream();
            try
            {
                while (true)
                {
                    var request = await ReadFrameAsync(stream, token);
                    if (request is null) break;
                    await stream.WriteAsync(Device.Handle(request), token);
                }
            }
            catch (Exception ex) when (ex is OperationCanceledException or IOException) { }
        }
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

    public async Task ConnectAsync()
    {
        client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, Port);
    }

    public ushort NextTransactionId() => ++transactionId;

    /// <summary>发出一帧请求并等待应答（超时 2 秒）。</summary>
    public async Task<byte[]> RequestAsync(byte[] request)
    {
        var stream = client?.GetStream() ?? throw new InvalidOperationException("还没有连接");
        await stream.WriteAsync(request);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        return await ReadFrameAsync(stream, timeout.Token) ?? throw new IOException("对方关闭了连接");
    }

    public void Disconnect()
    {
        client?.Dispose();
        client = null;
    }

    public void Dispose()
    {
        cts.Cancel();
        Disconnect();
        listener.Stop();
        cts.Dispose();
    }
}
