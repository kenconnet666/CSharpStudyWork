namespace LearnSite.Data;

/// <summary>手写 SQL 的查询结果：列名 + 行（值已转成显示用的文字）。</summary>
public sealed record QueryResult(IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows, double Milliseconds, bool Truncated);
