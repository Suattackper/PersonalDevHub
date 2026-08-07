using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// The validated, ordered tool catalogue. It answers "which tools exist" and nothing else —
/// personal state (favorites, pins, usage) is layered on top by <see cref="IToolCatalog"/>.
/// </summary>
public interface IToolRegistry
{
    /// <summary>All tools in navigation order: category order, then tool order, then name.</summary>
    IReadOnlyList<ToolDescriptor> Tools { get; }

    /// <summary>Categories that actually contain at least one tool, in navigation order.</summary>
    IReadOnlyList<ToolCategory> Categories { get; }

    ToolDescriptor? Find(string? toolId);

    /// <summary>Tools matching a free-text term, preserving navigation order.</summary>
    IReadOnlyList<ToolDescriptor> Search(string? term);

    /// <summary>Tools of one category, in navigation order.</summary>
    IReadOnlyList<ToolDescriptor> InCategory(string categoryId);
}
