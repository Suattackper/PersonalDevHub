namespace DevhubWebApp.Models.Tools.Diff;

/// <summary>How a line relates to the other document.</summary>
public enum DiffLineKind
{
    /// <summary>Present and identical on both sides.</summary>
    Unchanged,

    /// <summary>Present only on the right side.</summary>
    Added,

    /// <summary>Present only on the left side.</summary>
    Removed
}

/// <summary>One line of a computed diff.</summary>
public sealed record DiffLine
{
    public required DiffLineKind Kind { get; init; }

    public required string Text { get; init; }

    /// <summary>1-based line number on the left, or <c>null</c> for added lines.</summary>
    public int? LeftLineNumber { get; init; }

    /// <summary>1-based line number on the right, or <c>null</c> for removed lines.</summary>
    public int? RightLineNumber { get; init; }
}

/// <summary>Comparison rules applied before two lines are considered equal.</summary>
public sealed record DiffOptions
{
    public static DiffOptions Default { get; } = new();

    /// <summary>Collapses whitespace runs and trims both ends before comparing.</summary>
    public bool IgnoreWhitespace { get; init; }

    public bool IgnoreCase { get; init; }

    /// <summary>Drops blank lines from both documents before comparing.</summary>
    public bool IgnoreBlankLines { get; init; }
}

/// <summary>Counters describing the size of a change set.</summary>
public sealed record DiffStatistics
{
    public int Added { get; init; }

    public int Removed { get; init; }

    public int Unchanged { get; init; }

    /// <summary>Lines that differ, in either direction.</summary>
    public int Changed => Added + Removed;

    public int TotalLines => Added + Removed + Unchanged;

    /// <summary>Share of compared lines that changed, 0–100.</summary>
    public double ChangePercentage => TotalLines == 0 ? 0 : Math.Round(Changed * 100d / TotalLines, 1);

    public bool AreIdentical => Changed == 0;
}

/// <summary>Result of comparing two documents.</summary>
public sealed record DiffResult
{
    public static DiffResult Empty { get; } = new() { Lines = Array.Empty<DiffLine>(), Statistics = new DiffStatistics() };

    public required IReadOnlyList<DiffLine> Lines { get; init; }

    public required DiffStatistics Statistics { get; init; }
}
