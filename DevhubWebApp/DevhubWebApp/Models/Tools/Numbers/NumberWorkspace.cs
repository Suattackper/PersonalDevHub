using DevhubWebApp.Utilities;

namespace DevhubWebApp.Models.Tools.Numbers;

/// <summary>Auto-saved state of the number base converter.</summary>
public sealed class NumberWorkspace
{
    /// <summary>The value as the user typed it, in <see cref="SourceBase"/>.</summary>
    public string Input { get; set; } = string.Empty;

    public NumberBase SourceBase { get; set; } = NumberBase.Decimal;

    /// <summary>Render results with their conventional literal prefix.</summary>
    public bool ShowPrefix { get; set; } = true;

    /// <summary>Text whose code points are shown alongside the numeric conversion.</summary>
    public string Text { get; set; } = string.Empty;
}
