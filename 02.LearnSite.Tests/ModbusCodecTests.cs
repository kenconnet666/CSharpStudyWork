using LearnSite.Services;

namespace LearnSite.Tests;

/// <summary>Modbus 报文的编码与校验。测试的写法：准备输入 → 调用 → 断言结果，一个测试只验证一件事。</summary>
public class ModbusCodecTests
{
    [Fact]
    public void Crc16_已知报文得到标准校验值()
    {
        // 01 03 00 00 00 03 的 CRC16 是 0xCB05（Modbus 资料里的标准例子，线上低字节在前：05 CB）
        byte[] frame = [0x01, 0x03, 0x00, 0x00, 0x00, 0x03];

        Assert.Equal(0xCB05, ModbusCodec.Crc16(frame));
    }

    [Fact]
    public void Crc16_空数据是初值()
    {
        Assert.Equal(0xFFFF, ModbusCodec.Crc16([]));
    }

    [Fact]
    public void BuildRtuRequest_末尾是低字节在前的Crc()
    {
        var frame = ModbusCodec.BuildRtuRequest(unit: 1, function: 3, address: 0, countOrValue: 3);

        Assert.Equal([0x01, 0x03, 0x00, 0x00, 0x00, 0x03, 0x05, 0xCB], frame);
    }

    [Fact]
    public void BuildRtuRequest_改动任何一个字节Crc就对不上()
    {
        var frame = ModbusCodec.BuildRtuRequest(1, 3, 0, 3);
        var expected = ModbusCodec.Crc16(frame.AsSpan(0, 6));

        frame[2] ^= 0x01;   // 模拟传输中翻转了一位

        Assert.NotEqual(expected, ModbusCodec.Crc16(frame.AsSpan(0, 6)));
    }

    [Fact]
    public void BuildTcpRequest_头部字段按大端排列()
    {
        var (frame, fields) = ModbusCodec.BuildTcpRequest(transactionId: 0x0102, unit: 7, function: 3, address: 0x0010, countOrValue: 2);

        Assert.Equal([0x01, 0x02, 0x00, 0x00, 0x00, 0x06, 0x07, 0x03, 0x00, 0x10, 0x00, 0x02], frame);
        Assert.Equal(7, fields.Length);
        Assert.Equal("事务号", fields[0].Name);
    }

    [Theory]
    [InlineData(1, "01 非法功能码：从站不支持这个功能")]
    [InlineData(2, "02 非法数据地址：寄存器地址超出范围")]
    [InlineData(3, "03 非法数据值：数量或值不合法")]
    public void ExceptionText_常见异常码有说明(byte code, string expected)
    {
        Assert.Equal(expected, ModbusCodec.ExceptionText(code));
    }
}
