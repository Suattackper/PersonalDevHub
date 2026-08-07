using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;

namespace DevhubWebApp.Services;

/// <summary>
/// Stores per-tool workspaces under <c>devhub.workspace.{toolId}</c>. Reads are unconditional so a
/// user who turns auto-save back on does not lose what was already stored; writes respect the
/// setting, which is what "auto-save off" actually means.
/// </summary>
public sealed class WorkspaceService : IWorkspaceService
{
    private readonly IStorageService _storage;
    private readonly IUserPreferenceService _preferences;

    public WorkspaceService(IStorageService storage, IUserPreferenceService preferences)
    {
        _storage = storage;
        _preferences = preferences;
    }

    public async ValueTask<T> LoadAsync<T>(string toolId, T fallback)
        => _preferences.Settings.AutoSaveWorkspace
            ? await _storage.GetAsync(StorageKeys.Workspace(toolId), fallback)
            : fallback;

    public async ValueTask SaveAsync<T>(string toolId, T workspace)
    {
        if (!_preferences.Settings.AutoSaveWorkspace)
        {
            return;
        }

        await _storage.SetAsync(StorageKeys.Workspace(toolId), workspace);
    }

    public ValueTask ClearAsync(string toolId) => _storage.RemoveAsync(StorageKeys.Workspace(toolId));
}
