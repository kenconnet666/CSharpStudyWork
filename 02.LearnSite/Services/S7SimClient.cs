using Sharp7;

namespace LearnSite.Services;

/// <summary>
/// 西门子 S7 的主站：用 Sharp7 库连接 PLC（或仿真 PLC）读写数据块。
/// Sharp7 的调用是同步的、返回错误码而不是抛异常，这里统一包成"出错就抛异常"，并放到后台线程执行，避免卡住界面。
/// </summary>
public sealed class S7SimClient : IDisposable
{
    readonly S7Client client = new();

    public bool Connected => client.Connected;

    /// <summary>连接；机架号、槽号：1200 / 1500 填 0、1，300 通常是 0、2。</summary>
    public Task ConnectAsync(string host, int rack = 0, int slot = 1) =>
        Task.Run(() => Check(client.ConnectTo(host, rack, slot), $"连接 {host} 失败"));

    public Task<byte[]> ReadDbAsync(int db, int start, int size) =>
        Task.Run(() =>
        {
            var buffer = new byte[size];
            Check(client.DBRead(db, start, size, buffer), $"读 DB{db} 失败");
            return buffer;
        });

    public Task WriteDbAsync(int db, int start, byte[] data) =>
        Task.Run(() => Check(client.DBWrite(db, start, data.Length, data), $"写 DB{db} 失败"));

    public void Disconnect() => client.Disconnect();

    void Check(int code, string message)
    {
        if (code != 0) throw new IOException($"{message}：{client.ErrorText(code)}（错误码 0x{code:X}）");
    }

    public void Dispose() => client.Disconnect();
}
