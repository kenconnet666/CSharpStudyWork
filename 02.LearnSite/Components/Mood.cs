using Rune.Icons;
using Rune.UI;

namespace LearnSite.Components;

/// <summary>
/// 解说气泡的"心情"：每种提示对应一个 Lucide 图标和一种色调。
/// 页面里用一个短键（沿用最早的表情符号，如 "💡"、"✅"）表示这是哪一类提示，显示时统一换成 Lucide 图标，
/// 这样所有页面的图标风格一致，也能跟着主题换色。
/// </summary>
public static class Mood
{
    /// <summary>取某个键对应的图标与色调；没登记的用"提示"图标。</summary>
    public static (IconData Icon, Tone Tone) For(string? key)
    {
        if (key is null) return (Lucide.Info, Tone.Info);
        // 表情符号后面常带一个"以彩色显示"的变体选择符（U+FE0F），查表前去掉
        var normalized = key.Replace("️", "");
        return Map.TryGetValue(normalized, out var look) ? look : (Lucide.Info, Tone.Info);
    }

    static readonly Dictionary<string, (IconData Icon, Tone Tone)> Map = new()
    {
        // 提示、思考
        ["💡"] = (Lucide.Lightbulb, Tone.Info),
        ["🤔"] = (Lucide.CircleQuestionMark, Tone.Info),
        ["🧐"] = (Lucide.Search, Tone.Warning),
        ["🤷"] = (Lucide.CircleQuestionMark, Tone.Warning),
        ["👆"] = (Lucide.MousePointerClick, Tone.Info),

        // 成功、失败、警告
        ["🎉"] = (Lucide.PartyPopper, Tone.Success),
        ["✅"] = (Lucide.CircleCheck, Tone.Success),
        ["📬"] = (Lucide.Inbox, Tone.Success),
        ["🛡"] = (Lucide.ShieldCheck, Tone.Success),
        ["🏖"] = (Lucide.Coffee, Tone.Success),
        ["❌"] = (Lucide.CircleX, Tone.Danger),
        ["🚫"] = (Lucide.Ban, Tone.Danger),
        ["🙅"] = (Lucide.Ban, Tone.Warning),
        ["💥"] = (Lucide.OctagonAlert, Tone.Danger),
        ["🚨"] = (Lucide.Siren, Tone.Danger),
        ["🛑"] = (Lucide.Square, Tone.Danger),
        ["🧊"] = (Lucide.Snowflake, Tone.Warning),
        ["🐢"] = (Lucide.Turtle, Tone.Warning),

        // 流程、顺序
        ["🏁"] = (Lucide.Flag, Tone.Info),
        ["⏸"] = (Lucide.Pause, Tone.Info),
        ["⏭"] = (Lucide.SkipForward, Tone.Info),
        ["▶"] = (Lucide.Play, Tone.Info),
        ["⏱"] = (Lucide.Timer, Tone.Info),
        ["🚦"] = (Lucide.TrafficCone, Tone.Info),
        ["📞"] = (Lucide.Phone, Tone.Info),
        ["🎯"] = (Lucide.Target, Tone.Info),
        ["🧍"] = (Lucide.User, Tone.Info),

        // 数据、对象
        ["📦"] = (Lucide.Package, Tone.Info),
        ["📄"] = (Lucide.FileText, Tone.Info),
        ["📝"] = (Lucide.FilePen, Tone.Info),
        ["📅"] = (Lucide.Calendar, Tone.Info),
        ["📊"] = (Lucide.ChartColumn, Tone.Info),
        ["📏"] = (Lucide.Ruler, Tone.Info),
        ["📚"] = (Lucide.Library, Tone.Info),
        ["📟"] = (Lucide.Cpu, Tone.Info),
        ["🧬"] = (Lucide.Dna, Tone.Info),
        ["🏷"] = (Lucide.Tag, Tone.Info),
        ["🎛"] = (Lucide.SlidersHorizontal, Tone.Info),
        ["🧭"] = (Lucide.Compass, Tone.Info),
        ["🧮"] = (Lucide.Calculator, Tone.Info),
        ["😴"] = (Lucide.Moon, Tone.Info),
        ["🫥"] = (Lucide.Ghost, Tone.Info),
        ["🎨"] = (Lucide.Palette, Tone.Info),
        ["🎭"] = (Lucide.Drama, Tone.Info),

        // 操作
        ["✏"] = (Lucide.Pencil, Tone.Info),
        ["✂"] = (Lucide.Scissors, Tone.Info),
        ["🔗"] = (Lucide.Link, Tone.Info),
        ["🔌"] = (Lucide.Plug, Tone.Info),
        ["🔍"] = (Lucide.Search, Tone.Info),
        ["🔎"] = (Lucide.Search, Tone.Info),
        ["🔬"] = (Lucide.Microscope, Tone.Info),
        ["🔀"] = (Lucide.Shuffle, Tone.Info),
        ["🔄"] = (Lucide.RefreshCw, Tone.Info),
        ["🔁"] = (Lucide.Repeat, Tone.Info),
        ["⬅"] = (Lucide.ArrowLeft, Tone.Info),
        ["➡"] = (Lucide.ArrowRight, Tone.Info),
        ["🔒"] = (Lucide.Lock, Tone.Info),
        ["🔐"] = (Lucide.Lock, Tone.Info),
        ["🔓"] = (Lucide.LockOpen, Tone.Info),
        ["🛠"] = (Lucide.Wrench, Tone.Info),
        ["🧰"] = (Lucide.Briefcase, Tone.Info),
        ["🧹"] = (Lucide.Brush, Tone.Info),
        ["🩹"] = (Lucide.Bandage, Tone.Info),
        ["🧯"] = (Lucide.Shield, Tone.Info),
        ["🧩"] = (Lucide.Puzzle, Tone.Info),
        ["🏭"] = (Lucide.Factory, Tone.Info),
        ["🏋"] = (Lucide.Dumbbell, Tone.Info),
        ["✉"] = (Lucide.Mail, Tone.Info),
    };
}
