namespace DevhubWebApp.Models.Tools;

/// <summary>A category together with the tools it contains, ready to render.</summary>
public sealed record ToolCategoryGroup
{
    public required ToolCategory Category { get; init; }

    public required IReadOnlyList<ToolView> Tools { get; init; }

    public int Count => Tools.Count;
}
