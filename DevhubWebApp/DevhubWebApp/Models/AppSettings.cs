using DevhubWebApp.Constants;

namespace DevhubWebApp.Models;

/// <summary>Theme preference persisted across sessions. Dark is the product default.</summary>
public enum AppThemeMode
{
    Dark,
    Light,
    System
}

/// <summary>Supported UI languages.</summary>
public enum AppLanguage
{
    English,
    Vietnamese
}

/// <summary>
/// User-controlled preferences. Serialized as a single JSON document so export/import
/// is a straight round-trip of this type.
/// </summary>
public sealed class AppSettings
{
    public AppThemeMode Theme { get; set; } = AppThemeMode.Dark;

    public AppLanguage Language { get; set; } = AppLanguage.Vietnamese;

    /// <summary>Tool opened by the "go to default tool" shortcut. Empty means the dashboard.</summary>
    public string DefaultToolId { get; set; } = string.Empty;

    /// <summary>When enabled, tool workspaces are restored on the next visit.</summary>
    public bool AutoSaveWorkspace { get; set; } = true;

    /// <summary>When disabled, no tool activity is recorded.</summary>
    public bool TrackHistory { get; set; } = true;

    public bool SidebarExpanded { get; set; } = true;

    public string SchemaVersion { get; set; } = AppConstants.AppVersion;

    /// <summary>Deep copy used to hand out isolated instances from the preference service.</summary>
    public AppSettings Clone() => new()
    {
        Theme = Theme,
        Language = Language,
        DefaultToolId = DefaultToolId,
        AutoSaveWorkspace = AutoSaveWorkspace,
        TrackHistory = TrackHistory,
        SidebarExpanded = SidebarExpanded,
        SchemaVersion = SchemaVersion
    };
}
