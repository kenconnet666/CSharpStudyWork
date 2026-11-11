using System.Net.Sockets;

namespace LearnSite.Services;

/// <summary>一次请求—应答的全部信息：原始字节、按字段拆开的样子、解析出的寄存器值或异常。</summary>
public sealed record ModbusExchange(byte[] Request, byte[] Response, FrameField[] RequestFields, FrameField[] ResponseFields, ushort[]? Registers, string? Exception);

/// <summary>
/// 通用的 Modbus TCP 主站：连任意地址（真实设备、仿真软件）。和学习站里内存中的模拟设备用的是同一套编解码。
/// 每次请求都带超时；设备不回应时抛 <see cref="TimeoutException"/>。
/// </summary>
public sealed class ModbusTcpClient(string host, int port, byte unit = 1) : IDisposable
{
    TcpClient? client;
    ushort transactionId;

    public string Host { get; } = host;

    public int Port { get; } = port;

    public bool Connected => client?.Connected == true;

    public async Task ConnectAsync(TimeSpan? timeout = null)
    {
        Disconnect();
        client = new TcpClient();
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(3));
        try
        {
            await client.ConnectAsync(Host, Port, cts.Token);
        }
        catch (OperationCanceledException)
        {
            Disconnect();
            throw new TimeoutException($"连接 {Host}:{Port} 超时");
        }
    }

    /// <summary>读保持寄存器（功能码 03）。</summary>
    public Task<ModbusExchange> ReadHoldingAsync(ushort address, ushort count) => ExchangeAsync(3, address, count);

    /// <summary>写单个寄存器（功能码 06）。</summary>
    public Task<ModbusExchange> WriteSingleAsync(ushort address, ushort value) => ExchangeAsync(6, address, value);

    async Task<ModbusExchange> ExchangeAsync(byte function, ushort address, ushort countOrValue)
    {
        var stream = (client ?? throw new InvalidOperationException("还没有连接")).GetStream();
        var (request, requestFields) = ModbusCodec.BuildTcpRequest(++transactionId, unit, function, address, countOrValue);
        await stream.WriteAsync(request);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        byte[] response;
        try
        {
            response = await ModbusFraming.ReadFrameAsync(stream, cts.Token) ?? throw new IOException("对方关闭了连接");
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("设备 2 秒内没有应答");
        }

        var isException = (response[7] & 0x80) != 0;
        return new ModbusExchange(
            request,
            response,
            requestFields,
            ModbusCodec.ExplainTcpResponse(response),
            function == 3 && !isException ? ModbusCodec.ParseRegisters(response) : null,
            isException ? ModbusCodec.ExceptionText(response[8]) : null);
    }

    public void Disconnect()
    {
        client?.Dispose();
        client = null;
    }

    public void Dispose() => Disconnect();
}
