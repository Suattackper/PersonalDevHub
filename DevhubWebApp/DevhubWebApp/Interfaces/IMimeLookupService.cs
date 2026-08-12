using DevhubWebApp.Models.Tools.Mime;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// Look-up over the media-type catalogue. Behind an interface so the catalogue can later come from
/// a generated file or a remote list without touching the tool.
/// </summary>
public interface IMimeLookupService
{
    /// <summary>Every known entry, ordered by media type.</summary>
    IReadOnlyList<MimeEntry> All { get; }

    /// <summary>Categories that contain at least one entry.</summary>
    IReadOnlyList<MimeCategory> Categories { get; }

    /// <summary>
    /// Free-text search across type, extensions and description, optionally narrowed to one
    /// category. Exact extension and exact type matches are ranked first.
    /// </summary>
    IReadOnlyList<MimeEntry> Search(string? term, MimeCategory? category = null);

    /// <summary>The entry that owns a file extension, with or without its dot.</summary>
    MimeEntry? FindByExtension(string? extension);

    /// <summary>The entry for an exact media type.</summary>
    MimeEntry? FindByMimeType(string? mimeType);

    /// <summary>The media type for a file name, falling back to <c>application/octet-stream</c>.</summary>
    string ResolveForFileName(string? fileName);
}
