using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// Owns settings, favorites, pinned tools and the recently-used list. State is loaded once
/// at startup and kept in memory, so components read it synchronously without re-rendering
/// on every storage round-trip.
/// </summary>
public interface IUserPreferenceService
{
    AppSettings Settings { get; }

    IReadOnlyCollection<string> Favorites { get; }

    IReadOnlyCollection<string> Pinned { get; }

    /// <summary>Tool ids ordered most-recent first.</summary>
    IReadOnlyList<string> Recent { get; }

    /// <summary>Raised whenever any preference changes, so the shell can re-render.</summary>
    event Action? Changed;

    /// <summary>Hydrates in-memory state from storage. Called once during application start.</summary>
    Task InitializeAsync();

    /// <summary>Reloads all preferences from storage after an import or reset.</summary>
    Task ReloadAsync();

    Task SaveSettingsAsync(AppSettings settings);

    Task ResetSettingsAsync();

    bool IsFavorite(string toolId);

    bool IsPinned(string toolId);

    Task ToggleFavoriteAsync(string toolId);

    Task TogglePinAsync(string toolId);

    /// <summary>Records a tool visit for the recent list and usage statistics.</summary>
    Task TrackToolOpenedAsync(string toolId);

    IReadOnlyList<ToolUsage> UsageStatistics { get; }

    /// <summary>Usage counter for one tool, or <c>null</c> when it has never been opened.</summary>
    ToolUsage? FindUsage(string toolId);
}
