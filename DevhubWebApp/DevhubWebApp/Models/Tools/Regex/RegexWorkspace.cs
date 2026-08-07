namespace DevhubWebApp.Models.Tools.Regex;

/// <summary>Auto-saved state of the regex tester.</summary>
public sealed class RegexWorkspace
{
    public string Pattern { get; set; } = string.Empty;

    public string TestText { get; set; } = string.Empty;

    public string Replacement { get; set; } = string.Empty;

    /// <summary>Shows the replacement panel and its preview.</summary>
    public bool ReplaceMode { get; set; }

    public bool IgnoreCase { get; set; }

    public bool Multiline { get; set; }

    public bool Singleline { get; set; }

    public bool Global { get; set; } = true;

    public bool ExplicitCapture { get; set; }

    public bool IgnoreWhitespace { get; set; }

    /// <summary>Converts the persisted toggles into engine flags.</summary>
    public RegexFlags ToFlags() => new()
    {
        IgnoreCase = IgnoreCase,
        Multiline = Multiline,
        Singleline = Singleline,
        Global = Global,
        ExplicitCapture = ExplicitCapture,
        IgnoreWhitespace = IgnoreWhitespace
    };
}
