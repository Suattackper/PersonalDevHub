using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Models.Dashboard;

/// <summary>
/// Every counter shown on the dashboard, computed on demand from the catalogue and the activity
/// log. Nothing here is persisted: the numbers are derived, so they can never go stale.
/// </summary>
public sealed record DashboardStatistics
{
    /// <summary>Tools declared in the application.</summary>
    public int ToolCount { get; init; }

    public int CategoryCount { get; init; }

    public int FavoriteCount { get; init; }

    public int PinnedCount { get; init; }

    /// <summary>Tools present in the recently-used list.</summary>
    public int RecentCount { get; init; }

    /// <summary>Entries currently kept in the activity log.</summary>
    public int HistoryCount { get; init; }

    /// <summary>Total number of tool opens recorded on this device.</summary>
    public int UsageCount { get; init; }

    /// <summary>Distinct tools opened at least once.</summary>
    public int ToolsUsed { get; init; }

    public ToolView? MostUsedTool { get; init; }

    public DateTimeOffset? LastActivityUtc { get; init; }
}
