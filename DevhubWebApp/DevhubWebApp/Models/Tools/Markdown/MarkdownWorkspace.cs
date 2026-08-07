namespace DevhubWebApp.Models.Tools.Markdown;

/// <summary>Auto-saved state of the Markdown editor.</summary>
public sealed class MarkdownWorkspace
{
    public string Content { get; set; } = string.Empty;

    /// <summary>Document title, used for export file names.</summary>
    public string Title { get; set; } = string.Empty;

    public bool ShowPreview { get; set; } = true;

    public bool WordWrap { get; set; } = true;

    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Names of recently opened files, most recent first.</summary>
    public List<string> RecentFiles { get; set; } = [];
}
