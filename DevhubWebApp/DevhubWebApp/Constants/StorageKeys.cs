namespace DevhubWebApp.Constants;

/// <summary>
/// Canonical keys used by <c>IStorageService</c>. Keys are namespaced so DevHub never
/// collides with other applications served from the same origin.
/// </summary>
public static class StorageKeys
{
    private const string Prefix = "devhub";

    public const string Settings = Prefix + ".settings";
    public const string Favorites = Prefix + ".favorites";
    public const string Pinned = Prefix + ".pinned";
    public const string Recent = Prefix + ".recent";
    public const string History = Prefix + ".history";
    public const string UsageStats = Prefix + ".usage";

    /// <summary>Per-tool workspace state (auto-saved editor content).</summary>
    public static string Workspace(string toolId) => $"{Prefix}.workspace.{toolId}";
}
