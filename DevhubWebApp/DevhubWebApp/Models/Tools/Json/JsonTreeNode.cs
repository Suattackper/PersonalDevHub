using System.Text.Json;

namespace DevhubWebApp.Models.Tools.Json;

/// <summary>One node of the read-only document tree shown beside the editor.</summary>
public sealed record JsonTreeNode
{
    /// <summary>Property name, array index, or "root".</summary>
    public required string Name { get; init; }

    /// <summary>Rendered value for scalars; a summary such as "3 items" for containers.</summary>
    public required string Value { get; init; }

    public required JsonValueKind Kind { get; init; }

    /// <summary>JSON path of this node, e.g. <c>$.users[0].name</c>.</summary>
    public required string Path { get; init; }

    public IReadOnlyList<JsonTreeNode> Children { get; init; } = Array.Empty<JsonTreeNode>();

    public bool HasChildren => Children.Count > 0;
}
