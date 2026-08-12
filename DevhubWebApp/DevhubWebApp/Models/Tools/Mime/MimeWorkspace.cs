namespace DevhubWebApp.Models.Tools.Mime;

/// <summary>Auto-saved state of the MIME explorer.</summary>
public sealed class MimeWorkspace
{
    public string Search { get; set; } = string.Empty;

    /// <summary>Selected category, or <c>null</c> for all of them.</summary>
    public MimeCategory? Category { get; set; }

    /// <summary>Media type currently shown in the detail panel.</summary>
    public string SelectedMimeType { get; set; } = string.Empty;

    /// <summary>Media types the user kept, newest first.</summary>
    public List<string> Favorites { get; set; } = [];

    /// <summary>Recently viewed media types, newest first.</summary>
    public List<string> Recent { get; set; } = [];
}
