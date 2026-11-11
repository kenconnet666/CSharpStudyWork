namespace LearnSite.Data;

/// <summary>数据库里的时间一律用"本地时间、Kind 不指定"：Npgsql 写入 timestamp（不带时区）的列时不接受 Kind 为 Utc 的值。</summary>
public static class Clock
{
    public static DateTime Now => DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
}
