namespace DevhubWebApp.Models.Tools.Json;

/// <summary>
/// Everything the editor needs to know about the current document, produced by a single parse:
/// validity, duplicate count and the display tree.
/// </summary>
public sealed record JsonInspection
{
    /// <summary>Empty input is neither valid nor invalid; the UI shows the drop zone instead.</summary>
    public static JsonInspection Empty { get; } = new();

    public JsonError? Error { get; init; }

    public bool IsValid => Error is null;

    /// <summary>Properties shadowed by a later property of the same name, at any depth.</summary>
    public int DuplicateCount { get; init; }

    /// <summary><c>null</c> when the document is invalid or the tree was not requested.</summary>
    public JsonTreeNode? Tree { get; init; }
}
