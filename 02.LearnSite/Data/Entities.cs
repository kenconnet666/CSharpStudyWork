using EasyQuerySharp.Metadata;

namespace LearnSite.Data;

/// <summary>一台设备（比如一台带温度、压力传感器的 PLC）。</summary>
[Table("t_device", Comment = "设备")]
public class Device
{
    [Column(PrimaryKey = true, Generated = true)]
    public long Id { get; set; }

    [Column(Comment = "设备名")]
    public string Name { get; set; } = "";

    [Column(Comment = "安装位置")]
    public string Location { get; set; } = "";

    [Navigate(RelationType.OneToMany, TargetProperties = [nameof(Reading.DeviceId)])]
    public List<Reading>? Readings { get; set; }

    [Navigate(RelationType.OneToMany, TargetProperties = [nameof(Alarm.DeviceId)])]
    public List<Alarm>? Alarms { get; set; }
}

/// <summary>一次采样：某台设备在某个时刻某个量的读数。每秒入库一批，所以这是最大的一张表。</summary>
[Table("t_reading", Comment = "采样值")]
[TableIndex(nameof(DeviceId), nameof(At), Descending = [nameof(At)])]
public class Reading
{
    [Column(PrimaryKey = true, Generated = true)]
    public long Id { get; set; }

    public long DeviceId { get; set; }

    [Column(Comment = "量的名字：temperature / pressure")]
    public string Tag { get; set; } = "";

    public double Value { get; set; }

    [Column(Comment = "采样时间（本地时间）")]
    public DateTime At { get; set; }

    [Navigate(RelationType.ManyToOne, SelfProperties = [nameof(DeviceId)]), ForeignKey(Action = "ON DELETE CASCADE")]
    public Device? Device { get; set; }
}

/// <summary>一条报警：超限时产生，恢复正常时填上 ClearedAt。</summary>
[Table("t_alarm", Comment = "报警")]
public class Alarm
{
    [Column(PrimaryKey = true, Generated = true)]
    public long Id { get; set; }

    public long DeviceId { get; set; }

    [Column(Comment = "级别：warning / danger")]
    public string Level { get; set; } = "";

    public string Message { get; set; } = "";

    public DateTime RaisedAt { get; set; }

    [Column(Comment = "恢复时间；为空表示还在报警")]
    public DateTime? ClearedAt { get; set; }

    [Navigate(RelationType.ManyToOne, SelfProperties = [nameof(DeviceId)]), ForeignKey(Action = "ON DELETE CASCADE")]
    public Device? Device { get; set; }
}
