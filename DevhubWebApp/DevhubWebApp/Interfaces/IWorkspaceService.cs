namespace DevhubWebApp.Interfaces;

/// <summary>
/// Per-tool workspace persistence. Auto-save is a global preference, so honouring it lives here
/// once instead of in every tool.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Restores a tool's workspace, or returns <paramref name="fallback"/> when nothing was saved
    /// or auto-save is disabled.
    /// </summary>
    ValueTask<T> LoadAsync<T>(string toolId, T fallback);

    /// <summary>Persists a workspace. No-op while auto-save is disabled.</summary>
    ValueTask SaveAsync<T>(string toolId, T workspace);

    /// <summary>Removes a tool's saved workspace.</summary>
    ValueTask ClearAsync(string toolId);
}
