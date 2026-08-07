namespace DevhubWebApp.Models.Tools.Json;

/// <summary>
/// Auto-saved state of the JSON tool. Persisted under the tool's workspace key so a reload, or a
/// trip through another tool, brings the user back exactly where they were.
/// </summary>
public sealed class JsonWorkspace
{
    public string Content { get; set; } = string.Empty;

    /// <summary>Reformat automatically right after a paste.</summary>
    public bool FormatOnPaste { get; set; } = true;

    public bool SortProperties { get; set; }

    public bool RemoveDuplicateProperties { get; set; }

    public bool WordWrap { get; set; }

    public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Shows the parsed document as a tree next to the editor.</summary>
    public bool ShowTree { get; set; } = true;
}
