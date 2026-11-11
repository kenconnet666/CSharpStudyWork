using System.Net.Sockets;

namespace LearnSite.Services;

/// <summary>"Socket / TCP" 一课用：服务端收到什么就回 "ACK:" + 原样数据。</summary>
public sealed class LoopbackTcp : LoopbackServer
{
    /// <summary>服务端一侧发生的事（在后台线程触发，界面要用 InvokeAsync 切回 UI 线程）。</summary>
    public event Action<string>? ServerLog;

    protected override void OnAccepted(TcpClient peer) =>
        ServerLog?.Invoke($"有客户端连进来了：{peer.Client.RemoteEndPoint}");

    protected override async Task ServeAsync(NetworkStream stream, CancellationToken token)
    {
        var buffer = new byte[1024];
        while (true)
        {
            var count = await stream.ReadAsync(buffer, token);
            if (count == 0) break;   // 读到 0 字节 = 对方正常关闭了连接
            var received = buffer[..count];
            ServerLog?.Invoke($"服务端收到 {count} 字节：{ByteText.Hex(received)}");
            byte[] reply = [.. "ACK:"u8, .. received];
            await stream.WriteAsync(reply, token);
        }
        ServerLog?.Invoke("客户端断开了，服务端这条连接结束。");
    }

    /// <summary>发一段字节，等服务端回一次话（学习用：假设回复一次读完）。</summary>
    public async Task<byte[]> SendAsync(byte[] data)
    {
        var stream = ClientStream;
        await stream.WriteAsync(data);
        var buffer = new byte[1024];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var count = await stream.ReadAsync(buffer, timeout.Token);
        return buffer[..count];
    }
}
