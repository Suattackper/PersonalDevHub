namespace DevhubWebApp.Models.Tools.Text;

/// <summary>Auto-saved state of the case converter.</summary>
public sealed class CaseWorkspace
{
    public string Input { get; set; } = string.Empty;

    /// <summary>Trim leading and trailing whitespace from every line before converting.</summary>
    public bool TrimLines { get; set; }

    /// <summary>Drop blank lines before converting.</summary>
    public bool RemoveEmptyLines { get; set; }

    /// <summary>Collapse runs of whitespace inside each line before converting.</summary>
    public bool CollapseSpaces { get; set; }
}
