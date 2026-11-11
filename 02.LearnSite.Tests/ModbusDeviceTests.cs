using LearnSite.Services;

namespace LearnSite.Tests;

/// <summary>模拟设备对请求的应答：正常读、写，以及各种出错的情况。设备是纯内存对象，不需要网络。</summary>
public class ModbusDeviceTests
{
    static byte[] Request(byte function, ushort address, ushort value) =>
        ModbusCodec.BuildTcpRequest(1, 1, function, address, value).Frame;

    [Fact]
    public void 读寄存器_返回对应的数据()
    {
        var device = new ModbusDevice();

        var reply = device.Handle(Request(function: 3, address: 0, value: 2));

        Assert.Equal(0x03, reply[7]);                       // 功能码原样返回
        Assert.Equal(4, reply[8]);                          // 2 个寄存器 = 4 字节
        Assert.Equal([device.Registers[0], device.Registers[1]], ModbusCodec.ParseRegisters(reply));
    }

    [Fact]
    public void 写寄存器_设备的值真的变了_并回显()
    {
        var device = new ModbusDevice();

        var reply = device.Handle(Request(function: 6, address: 4, value: 1234));

        Assert.Equal(1234, device.Registers[4]);
        Assert.Equal([0x00, 0x04, 0x04, 0xD2], reply[8..12]);   // 地址和值原样回显
    }

    [Fact]
    public void 读越界_返回异常码02()
    {
        var device = new ModbusDevice();

        var reply = device.Handle(Request(function: 3, address: 8, value: 5));   // 8+5 超过了 10 个寄存器

        Assert.Equal(0x83, reply[7]);                       // 功能码 + 0x80 表示出错
        Assert.Equal(2, reply[8]);
    }

    [Fact]
    public void 读数量为0_返回异常码03()
    {
        var reply = new ModbusDevice().Handle(Request(function: 3, address: 0, value: 0));

        Assert.Equal(0x83, reply[7]);
        Assert.Equal(3, reply[8]);
    }

    [Fact]
    public void 不支持的功能码_返回异常码01()
    {
        var reply = new ModbusDevice().Handle(Request(function: 8, address: 0, value: 0));

        Assert.Equal(0x88, reply[7]);
        Assert.Equal(1, reply[8]);
    }

    [Fact]
    public void 应答的事务号与请求一致()
    {
        var request = ModbusCodec.BuildTcpRequest(transactionId: 0x1234, unit: 1, function: 3, address: 0, countOrValue: 1).Frame;

        var reply = new ModbusDevice().Handle(request);

        Assert.Equal([0x12, 0x34], reply[..2]);
    }
}
