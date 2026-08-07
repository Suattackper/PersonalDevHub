namespace DevhubWebApp.Models.Tools.Diff;

/// <summary>Auto-saved state of the text diff tool.</summary>
public sealed class DiffWorkspace
{
    public string Left { get; set; } = string.Empty;

    public string Right { get; set; } = string.Empty;

    /// <summary>Side-by-side when <c>true</c>, inline when <c>false</c>.</summary>
    public bool SideBySide { get; set; } = true;

    public bool IgnoreWhitespace { get; set; }

    public bool IgnoreCase { get; set; }

    public bool IgnoreBlankLines { get; set; }

    public bool WordWrap { get; set; }

    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Converts the persisted toggles into engine options.</summary>
    public DiffOptions ToOptions() => new()
    {
        IgnoreWhitespace = IgnoreWhitespace,
        IgnoreCase = IgnoreCase,
        IgnoreBlankLines = IgnoreBlankLines
    };
}
