using DevhubWebApp.Constants;
using MudBlazor;

namespace DevhubWebApp.Models.Tools;

/// <summary>
/// Immutable metadata for one discovered tool, projected from its <see cref="ToolAttribute"/>.
/// This is what navigation, search, the dashboard and history consume — none of them ever
/// references a concrete tool.
/// </summary>
public sealed record ToolDescriptor
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required string Icon { get; init; }

    public required ToolCategory Category { get; init; }

    /// <summary>Component rendered by the tool host for this route.</summary>
    public required Type ComponentType { get; init; }

    public IReadOnlyList<string> Keywords { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public string Version { get; init; } = "1.0.0";

    public int Order { get; init; } = 100;

    public Color Color { get; init; } = Color.Primary;

    public bool IsExperimental { get; init; }

    /// <summary>Canonical route, derived from the id so metadata is never duplicated.</summary>
    public string Route => AppRoutes.Tool(Id);

    /// <summary>Case-insensitive match across id, name, description, keywords and tags.</summary>
    public bool Matches(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return true;
        }

        return Name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Id.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Description.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Category.Label.Contains(term, StringComparison.OrdinalIgnoreCase)
            || Keywords.Any(keyword => keyword.Contains(term, StringComparison.OrdinalIgnoreCase))
            || Tags.Any(tag => tag.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
