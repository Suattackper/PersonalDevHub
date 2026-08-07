namespace DevhubWebApp.Models;

/// <summary>
/// One recorded tool action. Tools push entries through <c>IHistoryService</c>;
/// the dashboard and history page render them.
/// </summary>
public sealed record HistoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public required string ToolId { get; init; }

    /// <summary>Short action label, e.g. "Formatted" or "Decoded".</summary>
    public required string Action { get; init; }

    /// <summary>Preview of the payload. Truncated before persisting to keep storage small.</summary>
    public string Preview { get; init; } = string.Empty;

    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}
