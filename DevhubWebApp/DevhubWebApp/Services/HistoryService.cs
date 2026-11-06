using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using System.Text.Json;

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
        _entries.AddRange((await _storage.GetAsync(StorageKeys.History, new List<HistoryEntry>()))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Id) &&
                            !string.IsNullOrWhiteSpace(entry.ToolId) &&
                            !string.IsNullOrWhiteSpace(entry.Action))
            .OrderByDescending(entry => entry.TimestampUtc)
            .Take(AppConstants.HistoryCapacity));
        _initialized = true;
        Changed?.Invoke();
    }

    public async Task ReloadAsync()
    {
        _initialized = false;
        await InitializeAsync();
    }

    public async Task RecordAsync(string toolId, string action, string? preview = null)
    {
        if (!_preferences.Settings.TrackHistory)
        {
            return;
        }

        var workspace = await _storage.GetOrDefaultAsync<JsonElement>(StorageKeys.Workspace(toolId));
        _entries.Insert(0, new HistoryEntry
        {
            ToolId = toolId,
            Action = action,
            Preview = Truncate(preview),
            WorkspaceJson = workspace.ValueKind == JsonValueKind.Undefined ? null : workspace.GetRawText()
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

    public async Task<bool> RestoreAsync(string entryId)
    {
        var entry = _entries.FirstOrDefault(item => item.Id == entryId);
        if (entry?.WorkspaceJson is null)
        {
            return false;
        }

        using var document = JsonDocument.Parse(entry.WorkspaceJson);
        await _storage.SetAsync(StorageKeys.Workspace(entry.ToolId), document.RootElement.Clone());
        return true;
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
