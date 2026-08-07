using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// The read model the UI binds to: registry metadata joined with this browser's favorites, pins
/// and usage counters. Dashboard, sidebar and search all consume this and therefore need no
/// changes when a tool is added.
/// </summary>
public interface IToolCatalog
{
    /// <summary>Every tool in navigation order, with personal state attached.</summary>
    IReadOnlyList<ToolView> All();

    /// <summary>Tools matching a free-text term.</summary>
    IReadOnlyList<ToolView> Search(string? term);

    /// <summary>Tools grouped by category, categories in navigation order.</summary>
    IReadOnlyList<ToolCategoryGroup> ByCategory(string? searchTerm = null);

    IReadOnlyList<ToolView> Favorites();

    IReadOnlyList<ToolView> Pinned();

    /// <summary>Most recently opened first, limited to <paramref name="count"/> entries.</summary>
    IReadOnlyList<ToolView> Recent(int count);

    /// <summary>Highest usage count first, limited to <paramref name="count"/> entries.</summary>
    IReadOnlyList<ToolView> MostUsed(int count);

    /// <summary>Single tool, or <c>null</c> when the id is unknown.</summary>
    ToolView? Find(string? toolId);

    /// <summary>Aggregate numbers for the dashboard statistics.</summary>
    ToolStatistics Statistics();
}
