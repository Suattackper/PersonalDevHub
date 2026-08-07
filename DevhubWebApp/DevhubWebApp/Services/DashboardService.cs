using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Dashboard;
using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Services;

/// <summary>
/// Read model for the dashboard. It owns no state of its own: every method is a projection over
/// <see cref="IToolCatalog"/> and <see cref="IHistoryService"/>, and the change events of both are
/// merged into one signal the UI can subscribe to.
/// </summary>
public sealed class DashboardService : IDashboardService, IDisposable
{
    private readonly IToolCatalog _catalog;
    private readonly IHistoryService _history;
    private readonly IUserPreferenceService _preferences;

    public DashboardService(IToolCatalog catalog, IHistoryService history, IUserPreferenceService preferences)
    {
        _catalog = catalog;
        _history = history;
        _preferences = preferences;

        _preferences.Changed += Raise;
        _history.Changed += Raise;
    }

    public event Action? Changed;

    public Task<DashboardStatistics> GetStatisticsAsync()
    {
        var toolStatistics = _catalog.Statistics();

        return Task.FromResult(new DashboardStatistics
        {
            ToolCount = toolStatistics.TotalTools,
            CategoryCount = toolStatistics.TotalCategories,
            FavoriteCount = toolStatistics.FavoriteCount,
            PinnedCount = toolStatistics.PinnedCount,
            RecentCount = _preferences.Recent.Count,
            HistoryCount = _history.Entries.Count,
            UsageCount = toolStatistics.TotalRuns,
            ToolsUsed = toolStatistics.ToolsUsed,
            MostUsedTool = toolStatistics.MostUsedTool,
            LastActivityUtc = toolStatistics.LastActivityUtc
        });
    }

    public Task<IReadOnlyList<ToolView>> GetPinnedAsync() => Task.FromResult(_catalog.Pinned());

    public Task<IReadOnlyList<ToolView>> GetFavoritesAsync() => Task.FromResult(_catalog.Favorites());

    public Task<IReadOnlyList<ToolView>> GetRecentAsync(int count) => Task.FromResult(_catalog.Recent(count));

    public Task<IReadOnlyList<ToolView>> GetMostUsedAsync(int count) => Task.FromResult(_catalog.MostUsed(count));

    public Task<IReadOnlyList<ToolView>> SearchAsync(string? term) => Task.FromResult(_catalog.Search(term));

    public Task<IReadOnlyList<HistoryEntry>> GetRecentHistoryAsync(int count)
        => Task.FromResult<IReadOnlyList<HistoryEntry>>(_history.Entries.Take(count).ToList());

    public ToolView? ResolveTool(string toolId) => _catalog.Find(toolId);

    public void Dispose()
    {
        _preferences.Changed -= Raise;
        _history.Changed -= Raise;
    }

    private void Raise() => Changed?.Invoke();
}
