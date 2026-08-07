using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Services;

/// <summary>
/// Single owner of user state (settings, favorites, pins, recents, usage counters).
/// State is hydrated once at start-up and then served from memory; writes are persisted
/// immediately and broadcast through <see cref="Changed"/> so the shell re-renders once.
/// </summary>
public sealed class UserPreferenceService : IUserPreferenceService
{
    private readonly IStorageService _storage;

    private readonly HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _pinned = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _recent = [];
    private readonly Dictionary<string, ToolUsage> _usage = new(StringComparer.OrdinalIgnoreCase);

    private bool _initialized;

    public UserPreferenceService(IStorageService storage) => _storage = storage;

    public AppSettings Settings { get; private set; } = new();

    public IReadOnlyCollection<string> Favorites => _favorites;

    public IReadOnlyCollection<string> Pinned => _pinned;

    public IReadOnlyList<string> Recent => _recent;

    public IReadOnlyList<ToolUsage> UsageStatistics =>
        _usage.Values.OrderByDescending(usage => usage.OpenCount).ToList();

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        Settings = await _storage.GetAsync(StorageKeys.Settings, new AppSettings());
        Replace(_favorites, await _storage.GetAsync(StorageKeys.Favorites, new List<string>()));
        Replace(_pinned, await _storage.GetAsync(StorageKeys.Pinned, new List<string>()));

        _recent.Clear();
        _recent.AddRange(await _storage.GetAsync(StorageKeys.Recent, new List<string>()));

        _usage.Clear();
        foreach (var usage in await _storage.GetAsync(StorageKeys.UsageStats, new List<ToolUsage>()))
        {
            _usage[usage.ToolId] = usage;
        }

        _initialized = true;
        NotifyChanged();
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        Settings = settings.Clone();
        await _storage.SetAsync(StorageKeys.Settings, Settings);
        NotifyChanged();
    }

    public Task ResetSettingsAsync() => SaveSettingsAsync(new AppSettings());

    public ToolUsage? FindUsage(string toolId)
        => string.IsNullOrWhiteSpace(toolId) ? null : _usage.GetValueOrDefault(toolId);

    public bool IsFavorite(string toolId) => _favorites.Contains(toolId);

    public bool IsPinned(string toolId) => _pinned.Contains(toolId);

    public Task ToggleFavoriteAsync(string toolId)
        => ToggleAsync(_favorites, toolId, StorageKeys.Favorites);

    public Task TogglePinAsync(string toolId)
        => ToggleAsync(_pinned, toolId, StorageKeys.Pinned);

    public async Task TrackToolOpenedAsync(string toolId)
    {
        if (string.IsNullOrWhiteSpace(toolId))
        {
            return;
        }

        _recent.RemoveAll(id => string.Equals(id, toolId, StringComparison.OrdinalIgnoreCase));
        _recent.Insert(0, toolId);
        if (_recent.Count > AppConstants.RecentToolCapacity)
        {
            _recent.RemoveRange(AppConstants.RecentToolCapacity, _recent.Count - AppConstants.RecentToolCapacity);
        }

        var previousCount = _usage.TryGetValue(toolId, out var existing) ? existing.OpenCount : 0;
        _usage[toolId] = new ToolUsage
        {
            ToolId = toolId,
            OpenCount = previousCount + 1,
            LastUsedUtc = DateTimeOffset.UtcNow
        };

        await _storage.SetAsync(StorageKeys.Recent, _recent);
        await _storage.SetAsync(StorageKeys.UsageStats, _usage.Values.ToList());
        NotifyChanged();
    }

    private async Task ToggleAsync(HashSet<string> set, string toolId, string storageKey)
    {
        if (!set.Add(toolId))
        {
            set.Remove(toolId);
        }

        await _storage.SetAsync(storageKey, set.ToList());
        NotifyChanged();
    }

    private static void Replace(HashSet<string> target, IEnumerable<string> values)
    {
        target.Clear();
        foreach (var value in values)
        {
            target.Add(value);
        }
    }

    private void NotifyChanged() => Changed?.Invoke();
}
