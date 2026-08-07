using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Services;

/// <summary>
/// Joins the tool registry with user preferences. Every query is a projection over the same two
/// sources, so no view of the catalogue can disagree with another.
/// </summary>
public sealed class ToolCatalog : IToolCatalog
{
    private readonly IToolRegistry _registry;
    private readonly IUserPreferenceService _preferences;

    public ToolCatalog(IToolRegistry registry, IUserPreferenceService preferences)
    {
        _registry = registry;
        _preferences = preferences;
    }

    public IReadOnlyList<ToolView> All() => Project(_registry.Tools);

    public IReadOnlyList<ToolView> Search(string? term) => Project(_registry.Search(term));

    public IReadOnlyList<ToolCategoryGroup> ByCategory(string? searchTerm = null)
    {
        var matches = Project(_registry.Search(searchTerm));

        return _registry.Categories
            .Select(category => new ToolCategoryGroup
            {
                Category = category,
                Tools = matches.Where(view => view.Tool.Category.Id == category.Id).ToList()
            })
            .Where(group => group.Count > 0)
            .ToList();
    }

    public IReadOnlyList<ToolView> Favorites()
        => All().Where(view => view.IsFavorite).ToList();

    public IReadOnlyList<ToolView> Pinned()
        => All().Where(view => view.IsPinned).ToList();

    /// <summary>Order comes from the preference service's recency list, not from metadata.</summary>
    public IReadOnlyList<ToolView> Recent(int count)
        => _preferences.Recent
            .Select(Find)
            .Where(view => view is not null)
            .Select(view => view!)
            .Take(count)
            .ToList();

    public IReadOnlyList<ToolView> MostUsed(int count)
        => All()
            .Where(view => view.UsageCount > 0)
            .OrderByDescending(view => view.UsageCount)
            .ThenBy(view => view.Name, StringComparer.OrdinalIgnoreCase)
            .Take(count)
            .ToList();

    public ToolView? Find(string? toolId)
    {
        var descriptor = _registry.Find(toolId);
        return descriptor is null ? null : Project(descriptor);
    }

    public ToolStatistics Statistics()
    {
        var views = All();

        return new ToolStatistics
        {
            TotalTools = views.Count,
            TotalCategories = _registry.Categories.Count,
            FavoriteCount = views.Count(view => view.IsFavorite),
            PinnedCount = views.Count(view => view.IsPinned),
            ToolsUsed = views.Count(view => view.UsageCount > 0),
            TotalRuns = views.Sum(view => view.UsageCount),
            MostUsedTool = views.OrderByDescending(view => view.UsageCount).FirstOrDefault(view => view.UsageCount > 0),
            LastActivityUtc = views.Where(view => view.LastUsedUtc is not null).Max(view => view.LastUsedUtc)
        };
    }

    private List<ToolView> Project(IEnumerable<ToolDescriptor> tools)
        => tools.Select(Project).ToList();

    private ToolView Project(ToolDescriptor tool)
    {
        var usage = _preferences.FindUsage(tool.Id);

        return new ToolView
        {
            Tool = tool,
            IsFavorite = _preferences.IsFavorite(tool.Id),
            IsPinned = _preferences.IsPinned(tool.Id),
            UsageCount = usage?.OpenCount ?? 0,
            LastUsedUtc = usage?.LastUsedUtc
        };
    }
}
