namespace DevhubWebApp.Models.Tools.Mime;

/// <summary>Broad groupings used by the category filter.</summary>
public enum MimeCategory
{
    Image,
    Audio,
    Video,
    Document,
    Archive,
    Font,
    Code,
    Executable,
    Data
}

/// <summary>One media type and everything the explorer knows about it.</summary>
public sealed record MimeEntry
{
    /// <summary>The media type itself, e.g. <c>image/png</c>.</summary>
    public required string MimeType { get; init; }

    /// <summary>File extensions, without the dot, most common first.</summary>
    public required IReadOnlyList<string> Extensions { get; init; }

    public required MimeCategory Category { get; init; }

    /// <summary>One-line description of what the format is.</summary>
    public required string Description { get; init; }

    /// <summary>Applications that typically open it.</summary>
    public IReadOnlyList<string> Applications { get; init; } = Array.Empty<string>();

    /// <summary>Primary extension, used for labels and download names.</summary>
    public string PrimaryExtension => Extensions.Count > 0 ? Extensions[0] : string.Empty;

    /// <summary>Case-insensitive match across type, extensions and description.</summary>
    public bool Matches(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return true;
        }

        var needle = term.TrimStart('.').Trim();

        return MimeType.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || Description.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || Extensions.Any(extension => extension.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
