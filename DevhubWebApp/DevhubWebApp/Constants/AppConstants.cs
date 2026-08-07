namespace DevhubWebApp.Constants;

/// <summary>
/// Application-wide immutable values. Never inline these as literals in components or services.
/// </summary>
public static class AppConstants
{
    public const string AppName = "DevHub";
    public const string AppVersion = "1.0.0";
    public const string AppTagline = "Offline-first developer toolbox";
    public const string RepositoryUrl = "https://github.com/Suattackper/PersonalDevHub";

    /// <summary>Maximum number of entries kept in the global history log.</summary>
    public const int HistoryCapacity = 200;

    /// <summary>Number of entries surfaced in "recent" widgets.</summary>
    public const int RecentDisplayCount = 8;

    /// <summary>Number of tool ids retained in the recently-used list.</summary>
    public const int RecentToolCapacity = 20;

    /// <summary>Debounce applied to search inputs and auto-save.</summary>
    public const int DebounceMilliseconds = 300;

    /// <summary>Depth of the per-tool undo/redo stack.</summary>
    public const int UndoStackDepth = 50;

    /// <summary>Average adult reading speed used for reading-time estimates.</summary>
    public const int ReadingWordsPerMinute = 200;

    /// <summary>Number of recently opened files remembered per tool.</summary>
    public const int RecentFileCapacity = 10;
}
