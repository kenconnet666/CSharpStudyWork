namespace LearnSite.Components;

/// <summary>C# 14 的扩展块：除了扩展方法，还能扩展属性和静态成员。供"新特性"一课演示。</summary>
public static class StringExtensions
{
    extension(string source)
    {
        /// <summary>扩展属性：以前做不到，只能写 IsBlank() 方法。</summary>
        public bool IsBlank => string.IsNullOrWhiteSpace(source);

        public string Truncate(int maxLength) => source.Length <= maxLength ? source : source[..maxLength] + "…";
    }

    extension(string)
    {
        /// <summary>扩展静态方法：调用时写 string.Repeat(...)，像 string 自带的一样。</summary>
        public static string Repeat(string text, int times) => string.Concat(Enumerable.Repeat(text, times));
    }
}
