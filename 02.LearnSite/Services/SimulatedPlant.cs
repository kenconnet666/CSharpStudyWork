using LearnSite.Data;

namespace LearnSite.Services;

/// <summary>
/// 一套"假现场"：一台本机的 Modbus TCP 模拟设备、让它的读数不断变化的模拟器、以及读它并写库的采集器。
/// 采集落库一课和综合实战都用它，页面离开时整套一起释放。
/// </summary>
public sealed class SimulatedPlant : IAsyncDisposable
{
    public SimulatedPlant(LearnDb db, long deviceId = 1)
    {
        Modbus.StartServer();
        Simulator = new DeviceSimulator(Modbus.Device);
        Collector = new DeviceCollector(db, Modbus, deviceId);
    }

    public ModbusLoopback Modbus { get; } = new();

    public DeviceSimulator Simulator { get; }

    public DeviceCollector Collector { get; }

    /// <summary>设备收到请求也不回应（模拟断网、死机）。</summary>
    public bool DeviceSilent
    {
        get => Modbus.Silent;
        set => Modbus.Silent = value;
    }

    public async ValueTask DisposeAsync()
    {
        await Collector.DisposeAsync();
        Simulator.Dispose();
        Modbus.Dispose();
    }
}
