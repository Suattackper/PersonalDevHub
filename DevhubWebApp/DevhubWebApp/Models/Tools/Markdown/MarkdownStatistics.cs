namespace DevhubWebApp.Models.Tools.Markdown;

/// <summary>Counters shown under the Markdown editor. Derived from the document, never stored.</summary>
public sealed record MarkdownStatistics
{
    public static MarkdownStatistics Empty { get; } = new();

    public int Words { get; init; }

    /// <summary>Characters including whitespace.</summary>
    public int Characters { get; init; }

    /// <summary>Characters excluding all whitespace.</summary>
    public int CharactersWithoutSpaces { get; init; }

    public int Lines { get; init; }

    /// <summary>Exact estimated reading time. Display rounds it; the value itself does not.</summary>
    public TimeSpan ReadingTime { get; init; }

    /// <summary>
    /// Reading time for display: anything under a full minute reads as "&lt; 1 min", longer
    /// documents round up so the estimate is never optimistic.
    /// </summary>
    public string ReadingTimeLabel => ReadingTime < TimeSpan.FromMinutes(1)
        ? "< 1 min"
        : $"{Math.Ceiling(ReadingTime.TotalMinutes)} min";
}
