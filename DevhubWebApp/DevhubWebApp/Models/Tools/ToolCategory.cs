namespace DevhubWebApp.Models.Tools;

/// <summary>
/// A tool grouping, expressed as data rather than as an enum: label, icon and sort order travel
/// with the category itself, so rendering a category never requires a switch statement.
/// </summary>
public sealed record ToolCategory
{
    public required string Id { get; init; }

    public required string Label { get; init; }

    /// <summary>MudBlazor icon path used in navigation and headings.</summary>
    public required string Icon { get; init; }

    /// <summary>Sort position in navigation. Lower values come first.</summary>
    public int Order { get; init; } = 100;

    /// <summary>One-line description shown when the category is presented on its own.</summary>
    public string Description { get; init; } = string.Empty;
}
