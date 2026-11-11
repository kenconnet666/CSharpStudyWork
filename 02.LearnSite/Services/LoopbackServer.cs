using System.Net;
using System.Net.Sockets;

namespace LearnSite.Services;

/// <summary>
/// 本机回环（127.0.0.1）上的一对真实 TCP：一个监听的服务端 + 一个客户端连接。
/// 学习站运行在本机，所以走的是真正的操作系统 Socket，不是模拟。子类只需要写"服务端怎么回应一条连接"。
/// </summary>
public abstract class LoopbackServer : IDisposable
{
    readonly TcpListener listener = new(IPAddress.Loopback, 0);   // 端口 0 = 让系统挑一个空闲端口
    readonly CancellationTokenSource cts = new();
    TcpClient? client;

    public int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

    public bool Connected => client?.Connected == true;

    /// <summary>客户端用的流；还没连接时抛 <see cref="InvalidOperationException"/>。</summary>
    protected NetworkStream ClientStream => client?.GetStream() ?? throw new InvalidOperationException("还没有连接");

    public void StartServer()
    {
        listener.Start();
        _ = AcceptLoop(cts.Token);
    }

    /// <summary>服务端对一条连接的处理，连接关闭或取消时返回。</summary>
    protected abstract Task ServeAsync(NetworkStream stream, CancellationToken token);

    /// <summary>有客户端连进来时（在后台线程调用）。</summary>
    protected virtual void OnAccepted(TcpClient peer) { }

    async Task AcceptLoop(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var peer = await listener.AcceptTcpClientAsync(token);
                OnAccepted(peer);
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
            try { await ServeAsync(peer.GetStream(), token); }
            catch (Exception ex) when (ex is OperationCanceledException or IOException) { }
        }
    }

    public async Task ConnectAsync()
    {
        client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, Port);
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
