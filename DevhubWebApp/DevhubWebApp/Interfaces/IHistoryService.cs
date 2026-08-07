using DevhubWebApp.Models;

namespace DevhubWebApp.Interfaces;

/// <summary>Append-only activity log capped at <c>AppConstants.HistoryCapacity</c> entries.</summary>
public interface IHistoryService
{
    /// <summary>Entries ordered most-recent first.</summary>
    IReadOnlyList<HistoryEntry> Entries { get; }

    event Action? Changed;

    Task InitializeAsync();

    /// <summary>Records an action. No-op when history tracking is disabled in settings.</summary>
    Task RecordAsync(string toolId, string action, string? preview = null);

    Task ClearAsync();

    Task RemoveAsync(string entryId);
}
