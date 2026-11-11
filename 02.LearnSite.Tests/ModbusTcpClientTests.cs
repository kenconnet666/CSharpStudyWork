using System.Net;
using System.Net.Sockets;
using LearnSite.Services;

namespace LearnSite.Tests;

/// <summary>
/// 主站客户端对着一个真实的本机 TCP 服务端测试（服务端用 ModbusDevice 回应）：连接、读、写、异常、超时。
/// 这类测试比纯逻辑测试慢一点（要走网络栈），但能验证"分帧"这类和 TCP 有关的行为。
/// </summary>
public sealed class ModbusTcpClientTests : IAsyncDisposable
{
    readonly TcpListener listener = new(IPAddress.Loopback, 0);
    readonly CancellationTokenSource cts = new();
    readonly ModbusDevice device = new();
    bool silent;

    int Port => ((IPEndPoint)listener.LocalEndpoint).Port;

    public ModbusTcpClientTests()
    {
        listener.Start();
        _ = Serve();
    }

    async Task Serve()
    {
        try
        {
            using var peer = await listener.AcceptTcpClientAsync(cts.Token);
            var stream = peer.GetStream();
            while (await ModbusFraming.ReadFrameAsync(stream, cts.Token) is { } request)
                if (!silent) await stream.WriteAsync(device.Handle(request), cts.Token);
        }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or ObjectDisposedException) { }
    }

    [Fact]
    public async Task 读寄存器_得到设备里的值()
    {
        using var client = new ModbusTcpClient("127.0.0.1", Port);
        await client.ConnectAsync();

        var exchange = await client.ReadHoldingAsync(0, 3);

        Assert.Equal([device.Registers[0], device.Registers[1], device.Registers[2]], exchange.Registers);
        Assert.Null(exchange.Exception);
    }

    [Fact]
    public async Task 写寄存器_再读回来是新值()
    {
        using var client = new ModbusTcpClient("127.0.0.1", Port);
        await client.ConnectAsync();

        await client.WriteSingleAsync(7, 4321);
        var exchange = await client.ReadHoldingAsync(7, 1);

        Assert.Equal<ushort[]>([4321], exchange.Registers!);
    }

    [Fact]
    public async Task 读越界_报告异常而不是抛出()
    {
        using var client = new ModbusTcpClient("127.0.0.1", Port);
        await client.ConnectAsync();

        var exchange = await client.ReadHoldingAsync(200, 1);

        Assert.Null(exchange.Registers);
        Assert.Contains("非法数据地址", exchange.Exception);
    }

    [Fact]
    public async Task 设备不回应_超时抛出TimeoutException()
    {
        silent = true;
        using var client = new ModbusTcpClient("127.0.0.1", Port);
        await client.ConnectAsync();

        await Assert.ThrowsAsync<TimeoutException>(() => client.ReadHoldingAsync(0, 1));
    }

    [Fact]
    public async Task 连不上的地址_抛出连接错误()
    {
        listener.Stop();   // 端口不再监听
        using var client = new ModbusTcpClient("127.0.0.1", Port);

        await Assert.ThrowsAnyAsync<Exception>(() => client.ConnectAsync(TimeSpan.FromSeconds(1)));
    }

    public async ValueTask DisposeAsync()
    {
        await cts.CancelAsync();
        listener.Stop();
        cts.Dispose();
    }
}
