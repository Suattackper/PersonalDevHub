using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Mime;

namespace DevhubWebApp.Services;

/// <summary>
/// Catalogue-backed <see cref="IMimeLookupService"/>. The extension index is built once at
/// construction, so a look-up is a dictionary hit rather than a scan.
/// </summary>
public sealed class MimeLookupService : IMimeLookupService
{
    private readonly Dictionary<string, MimeEntry> _byExtension;
    private readonly Dictionary<string, MimeEntry> _byMimeType;

    public MimeLookupService()
    {
        _byMimeType = MimeCatalog.All.ToDictionary(entry => entry.MimeType, StringComparer.OrdinalIgnoreCase);

        _byExtension = new Dictionary<string, MimeEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in MimeCatalog.All)
        {
            foreach (var extension in entry.Extensions)
            {
                // The first entry claiming an extension wins; the catalogue is ordered so that is stable.
                _byExtension.TryAdd(extension, entry);
            }
        }
    }

    public IReadOnlyList<MimeEntry> All => MimeCatalog.All;

    public IReadOnlyList<MimeCategory> Categories => MimeCatalog.Categories;

    public IReadOnlyList<MimeEntry> Search(string? term, MimeCategory? category = null)
    {
        var matches = MimeCatalog.All
            .Where(entry => category is null || entry.Category == category)
            .Where(entry => entry.Matches(term));

        if (string.IsNullOrWhiteSpace(term))
        {
            return matches.ToList();
        }

        var needle = term.TrimStart('.').Trim();

        // Rank exact hits above substring hits: typing "png" should not bury image/png.
        return matches
            .OrderByDescending(entry => entry.Extensions.Any(extension => extension.Equals(needle, StringComparison.OrdinalIgnoreCase)))
            .ThenByDescending(entry => entry.MimeType.Equals(needle, StringComparison.OrdinalIgnoreCase))
            .ThenBy(entry => entry.MimeType, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public MimeEntry? FindByExtension(string? extension)
        => string.IsNullOrWhiteSpace(extension)
            ? null
            : _byExtension.GetValueOrDefault(extension.TrimStart('.').Trim());

    public MimeEntry? FindByMimeType(string? mimeType)
        => string.IsNullOrWhiteSpace(mimeType) ? null : _byMimeType.GetValueOrDefault(mimeType.Trim());

    public string ResolveForFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return MimeTypes.OctetStream;
        }

        var extension = Path.GetExtension(fileName);
        return FindByExtension(extension)?.MimeType ?? MimeTypes.OctetStream;
    }
}
