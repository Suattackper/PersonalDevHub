namespace DevhubWebApp.Models.Tools;

/// <summary>
/// A tool descriptor joined with this browser's personal state. Favorite, pin and usage counters
/// live in preferences, not in metadata, so there is exactly one source of truth for each fact;
/// this projection is what the UI binds to.
/// </summary>
public sealed record ToolView
{
    public required ToolDescriptor Tool { get; init; }

    public bool IsFavorite { get; init; }

    public bool IsPinned { get; init; }

    /// <summary>How many times the tool has been opened on this device.</summary>
    public int UsageCount { get; init; }

    /// <summary><c>null</c> when the tool has never been opened.</summary>
    public DateTimeOffset? LastUsedUtc { get; init; }

    public string Id => Tool.Id;

    public string Name => Tool.Name;

    public string Route => Tool.Route;
}
