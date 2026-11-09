namespace CSharpSyntax.Lessons;

/// <summary>
/// 第 4 课：继承、接口、多态。
/// 和 Java 最大的差异：方法默认"不可重写"，必须父类写 virtual、子类写 override（Java 恰好相反，默认可重写）。
/// </summary>
public static class Lesson04Oop
{
    // ---- 接口：约定能力。命名习惯以 I 开头 ----
    private interface IDevice
    {
        string Name { get; }                 // 接口里可以声明属性
        void Connect();

        // 默认实现（C# 8）：和 Java 8 的 default 方法一样
        string Describe() => $"设备 {Name}";
    }

    /// <summary>抽象类：不能直接 new，可以包含未实现的 abstract 成员。</summary>
    private abstract class DeviceBase(string name) : IDevice // : 同时表示继承和实现接口，类在前接口在后
    {
        public string Name { get; } = name;

        // abstract：子类必须实现
        public abstract void Connect();

        // virtual：子类"可以"重写
        public virtual string Status() => "未知状态";
    }

    private class PlcDevice(string name) : DeviceBase(name) // 调用父类构造函数
    {
        public override void Connect() => Console.WriteLine($"  [{Name}] 通过 S7 协议连接");
        public override string Status() => "PLC 运行中";
    }

    private class ScannerDevice(string name) : DeviceBase(name)
    {
        public override void Connect() => Console.WriteLine($"  [{Name}] 通过串口连接");
        // 没有重写 Status，沿用父类的实现
    }

    /// <summary>sealed：禁止被继承（等同 Java 的 final class）。</summary>
    private sealed class SimulatorDevice(string name) : DeviceBase(name)
    {
        public override void Connect() => Console.WriteLine($"  [{Name}] 模拟器直接就绪");
        // base.Xxx() 调用父类实现
        public override string Status() => "模拟-" + base.Status();
    }

    public static void Run()
    {
        Print.Section("1. 多态：同一个类型引用，不同实现");
        List<DeviceBase> devices = [new PlcDevice("西门子1200"), new ScannerDevice("扫码枪"), new SimulatorDevice("模拟")];
        foreach (var d in devices)
        {
            d.Connect();                       // 运行时调用各自的 override
            Print.Line(d.Name, d.Status());
        }

        Print.Section("2. 接口与默认实现");
        IDevice asInterface = devices[0];
        Print.Line("Describe", asInterface.Describe());

        Print.Section("3. 类型判断与转换");
        object obj = devices[1];
        // is：判断并同时转换（模式匹配，推荐）。Java 16 的 instanceof 模式匹配与它类似
        if (obj is ScannerDevice scanner) Print.Line("is 模式", scanner.Name);
        // as：转换失败返回 null 而不是抛异常
        var plc = obj as PlcDevice;
        Print.Line("as 失败", plc is null ? "null" : "成功");
        // (T)obj 强转失败会抛 InvalidCastException

        Print.Section("4. 继承 vs 组合");
        // 经验：优先"组合 + 接口"，继承层级不要超过两三层。
        // 上位机里常见做法：定义 IPlcClient 接口，S7/Modbus/欧姆龙各写一个实现，界面只依赖接口。
        Print.Line("提示", "接口定义能力，类组合能力，比深继承好维护");
    }
}
