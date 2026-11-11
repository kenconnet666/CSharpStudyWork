namespace LearnSite.Services;

/// <summary>整套监控的配置：从 appsettings.json 这样的配置源绑定出来的强类型对象。属性的默认值就是"没配置时用什么"。</summary>
public sealed class PlantOptions
{
    public DeviceOptions Device { get; set; } = new();

    public AlarmOptions Alarm { get; set; } = new();

    /// <summary>要采集的量。</summary>
    public string[] Tags { get; set; } = [];

    /// <summary>配置有没有问题；没问题返回 null。配置在启动时就检查，比运行到一半才出错好得多。</summary>
    public string? Validate() =>
        Device.Port is < 1 or > 65535 ? $"Device:Port = {Device.Port} 不在 1~65535 之内"
        : Device.PollMs < 50 ? $"Device:PollMs = {Device.PollMs} 太小，设备来不及回应（至少 50）"
        : Alarm.WarnAt >= Alarm.DangerAt ? $"Alarm:WarnAt（{Alarm.WarnAt}）必须小于 Alarm:DangerAt（{Alarm.DangerAt}）"
        : null;
}

public sealed class DeviceOptions
{
    public string Host { get; set; } = "127.0.0.1";

    public int Port { get; set; } = 502;

    public int PollMs { get; set; } = 500;
}

public sealed class AlarmOptions
{
    public double WarnAt { get; set; } = 70;

    public double DangerAt { get; set; } = 85;
}

/// <summary>读设备温度的抽象：业务代码只依赖它，真实设备、模拟设备、测试里的假设备都可以换进来。</summary>
public interface IDeviceReader
{
    Task<double> ReadTemperatureAsync();
}

/// <summary>固定返回一个温度的假设备——测试里最常用。</summary>
public sealed class FakeReader(double temperature) : IDeviceReader
{
    public Task<double> ReadTemperatureAsync() => Task.FromResult(temperature);
}

/// <summary>按 Modbus 报文读（在内存里的模拟设备上，不走网络）：真正走一遍编码、应答、解析。</summary>
public sealed class ModbusReader(ModbusDevice device) : IDeviceReader
{
    public Task<double> ReadTemperatureAsync()
    {
        var (frame, _) = ModbusCodec.BuildTcpRequest(1, 1, 3, 0, 1);
        var registers = ModbusCodec.ParseRegisters(device.Handle(frame));
        return Task.FromResult(registers[0] / 10.0);
    }
}

/// <summary>报警检查服务：依赖"读设备"和"报警配置"，都由构造函数交进来（依赖注入）。</summary>
public sealed class AlarmService(IDeviceReader reader, AlarmOptions options)
{
    public async Task<(double Temperature, AlarmLevel Level)> CheckAsync()
    {
        var temperature = await reader.ReadTemperatureAsync();
        return (temperature, new AlarmEvaluator(options.WarnAt, options.DangerAt).Evaluate(temperature, AlarmLevel.None));
    }
}
