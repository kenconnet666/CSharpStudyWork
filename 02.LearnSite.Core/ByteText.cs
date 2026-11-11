namespace LearnSite.Services;

/// <summary>字节与文字之间的小工具（界面里到处要把字节显示成十六进制）。</summary>
public static class ByteText
{
    /// <summary>"41 43 4B"；空数组显示"（空）"。</summary>
    public static string Hex(byte[] bytes) =>
        bytes.Length == 0 ? "（空）" : string.Join(' ', bytes.Select(b => b.ToString("X2")));
}
