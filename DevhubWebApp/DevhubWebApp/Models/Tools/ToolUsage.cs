namespace DevhubWebApp.Models.Tools;

/// <summary>Persisted usage counter for a single tool. One record per tool ever opened.</summary>
public sealed record ToolUsage
{
    public required string ToolId { get; init; }

    public int OpenCount { get; init; }

    public DateTimeOffset LastUsedUtc { get; init; }
}
