using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;

namespace DevhubWebApp.Services;

/// <summary>
/// Capped activity log persisted to browser storage. Recording honours the
/// <c>TrackHistory</c> setting, so opting out stops collection everywhere at once.
/// </summary>
public sealed class HistoryService : IHistoryService
{
    private const int PreviewLength = 120;

    private readonly IStorageService _storage;
    private readonly IUserPreferenceService _preferences;
    private readonly List<HistoryEntry> _entries = [];

    private bool _initialized;

    public HistoryService(IStorageService storage, IUserPreferenceService preferences)
    {
        _storage = storage;
        _preferences = preferences;
    }

    public IReadOnlyList<HistoryEntry> Entries => _entries;

    public event Action? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _entries.Clear();
        _entries.AddRange(await _storage.GetAsync(StorageKeys.History, new List<HistoryEntry>()));
        _initialized = true;
        Changed?.Invoke();
    }

    public async Task RecordAsync(string toolId, string action, string? preview = null)
    {
        if (!_preferences.Settings.TrackHistory)
        {
            return;
        }

        _entries.Insert(0, new HistoryEntry
        {
            ToolId = toolId,
            Action = action,
            Preview = Truncate(preview)
        });

        if (_entries.Count > AppConstants.HistoryCapacity)
        {
            _entries.RemoveRange(AppConstants.HistoryCapacity, _entries.Count - AppConstants.HistoryCapacity);
        }

        await PersistAsync();
    }

    public async Task ClearAsync()
    {
        _entries.Clear();
        await PersistAsync();
    }

    public async Task RemoveAsync(string entryId)
    {
        if (_entries.RemoveAll(entry => entry.Id == entryId) > 0)
        {
            await PersistAsync();
        }
    }

    private async Task PersistAsync()
    {
        await _storage.SetAsync(StorageKeys.History, _entries);
        Changed?.Invoke();
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var single = value.ReplaceLineEndings(" ").Trim();
        return single.Length <= PreviewLength ? single : single[..PreviewLength] + "…";
    }
}
