namespace DevhubWebApp.Models.Tools;

/// <summary>Aggregate counters rendered by the dashboard. Computed, never stored.</summary>
public sealed record ToolStatistics
{
    public int TotalTools { get; init; }

    public int TotalCategories { get; init; }

    public int FavoriteCount { get; init; }

    public int PinnedCount { get; init; }

    /// <summary>Number of distinct tools opened at least once on this device.</summary>
    public int ToolsUsed { get; init; }

    /// <summary>Sum of all open counts.</summary>
    public int TotalRuns { get; init; }

    public ToolView? MostUsedTool { get; init; }

    public DateTimeOffset? LastActivityUtc { get; init; }
}
