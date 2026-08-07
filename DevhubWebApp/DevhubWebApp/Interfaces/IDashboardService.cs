using DevhubWebApp.Models;
using DevhubWebApp.Models.Dashboard;
using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// The dashboard's only data source. It composes the tool catalogue and the activity log so no
/// dashboard component has to know where a number came from — and so none of them can hardcode one.
/// </summary>
public interface IDashboardService
{
    /// <summary>Raised when tool state or history changes, so sections can refresh independently.</summary>
    event Action? Changed;

    Task<DashboardStatistics> GetStatisticsAsync();

    Task<IReadOnlyList<ToolView>> GetPinnedAsync();

    Task<IReadOnlyList<ToolView>> GetFavoritesAsync();

    /// <summary>Most recently opened tools, newest first.</summary>
    Task<IReadOnlyList<ToolView>> GetRecentAsync(int count);

    /// <summary>Tools with the highest open count.</summary>
    Task<IReadOnlyList<ToolView>> GetMostUsedAsync(int count);

    /// <summary>Free-text search across the catalogue.</summary>
    Task<IReadOnlyList<ToolView>> SearchAsync(string? term);

    /// <summary>Latest activity entries, newest first.</summary>
    Task<IReadOnlyList<HistoryEntry>> GetRecentHistoryAsync(int count);

    /// <summary>Resolves display metadata for a history entry's tool, or <c>null</c> if unknown.</summary>
    ToolView? ResolveTool(string toolId);
}
