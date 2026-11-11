using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LearnSite.Services;

/// <summary>
/// 本机回环上的一对真实 TCP 连接：服务端收到什么就回 "ACK:" + 原样数据。
/// 学习站运行在本机，所以这里走的是真正的操作系统 Socket，不是模拟。
/// </summary>
public sealed class LoopbackTcp : IDisposable
{
    readonly TcpListener listener = new(IPAddress.Loopback, 0);   // 端口 0 = 让系统挑一个空闲端口
    readonly CancellationTokenSource cts = new();
    TcpClient? client;

    /// <summary>服务端一侧发生的事（在后台线程触发，界面要用 InvokeAsync 切回 UI 线程）。</summary>
    public event Action<string>? ServerLog;

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
            {
                var peer = await listener.AcceptTcpClientAsync(token);
                ServerLog?.Invoke($"有客户端连进来了：{peer.Client.RemoteEndPoint}");
                _ = Serve(peer, token);
            }
        }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
    }

    async Task Serve(TcpClient peer, CancellationToken token)
    {
        using (peer)
        {
            var stream = peer.GetStream();
            var buffer = new byte[1024];
            try
            {
                while (true)
                {
                    var count = await stream.ReadAsync(buffer, token);
                    if (count == 0) break;   // 读到 0 字节 = 对方正常关闭了连接
                    var received = buffer[..count];
                    ServerLog?.Invoke($"服务端收到 {count} 字节：{Hex(received)}");
                    byte[] reply = [.. "ACK:"u8, .. received];
                    await stream.WriteAsync(reply, token);
                }
                ServerLog?.Invoke("客户端断开了，服务端这条连接结束。");
            }
            catch (OperationCanceledException) { }
            catch (IOException) { ServerLog?.Invoke("连接被中断。"); }
        }
    }

    public async Task ConnectAsync()
    {
        client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, Port);
    }

    /// <summary>发一段字节，等服务端回一次话（学习用：假设回复一次读完）。</summary>
    public async Task<byte[]> SendAsync(byte[] data)
    {
        var stream = client?.GetStream() ?? throw new InvalidOperationException("还没有连接");
        await stream.WriteAsync(data);
        var buffer = new byte[1024];
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var count = await stream.ReadAsync(buffer, timeout.Token);
        return buffer[..count];
    }

    public void Disconnect()
    {
        client?.Dispose();
        client = null;
    }

    public static string Hex(byte[] bytes) =>
        bytes.Length == 0 ? "（空）" : string.Join(' ', bytes.Select(b => b.ToString("X2")));

    public void Dispose()
    {
        cts.Cancel();
        Disconnect();
        listener.Stop();
        cts.Dispose();
    }
}
