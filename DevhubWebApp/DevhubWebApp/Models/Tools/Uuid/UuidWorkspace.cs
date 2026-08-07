using DevhubWebApp.Utilities;

namespace DevhubWebApp.Models.Tools.Uuid;

/// <summary>Auto-saved state of the UUID generator.</summary>
public sealed class UuidWorkspace
{
    public int Quantity { get; set; } = 10;

    public UuidFormat Format { get; set; } = UuidFormat.Standard;

    /// <summary>Copy the batch to the clipboard as soon as it is generated.</summary>
    public bool AutoCopy { get; set; }

    /// <summary>Table when <c>true</c>, cards when <c>false</c>.</summary>
    public bool TableView { get; set; } = true;

    /// <summary>Previously generated identifiers, newest first.</summary>
    public List<string> History { get; set; } = [];
}
