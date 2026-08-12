using System.Globalization;
using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Http;

namespace DevhubWebApp.Services;

/// <summary>
/// Catalogue-backed <see cref="IHttpStatusService"/>. Search ranks an exact code first, because
/// typing "404" almost always means "show me 404".
/// </summary>
public sealed class HttpStatusService : IHttpStatusService
{
    private readonly Dictionary<int, HttpStatusEntry> _byCode =
        HttpStatusCatalog.All.ToDictionary(entry => entry.Code);

    public IReadOnlyList<HttpStatusEntry> All => HttpStatusCatalog.All;

    public IReadOnlyList<HttpStatusClass> Classes => HttpStatusCatalog.Classes;

    public HttpStatusEntry? Find(int code) => _byCode.GetValueOrDefault(code);

    public IReadOnlyList<HttpStatusEntry> Search(string? term, HttpStatusClass? statusClass = null)
    {
        var matches = HttpStatusCatalog.All
            .Where(entry => statusClass is null || entry.Class == statusClass)
            .Where(entry => entry.Matches(term));

        if (string.IsNullOrWhiteSpace(term))
        {
            return matches.ToList();
        }

        var needle = term.Trim();

        return matches
            .OrderByDescending(entry => entry.Code.ToString(CultureInfo.InvariantCulture).Equals(needle, StringComparison.Ordinal))
            .ThenBy(entry => entry.Code)
            .ToList();
    }

    public IReadOnlyList<HttpStatusEntry> GetRelated(HttpStatusEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry.Related.Select(Find).Where(related => related is not null).Select(related => related!).ToList();
    }

    /// <summary>
    /// Builds a response sketch a developer can paste into a test or a ticket: the status line,
    /// the headers that matter for this code, and a body only where one is appropriate.
    /// </summary>
    public string BuildExample(HttpStatusEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"HTTP/1.1 {entry.Code} {entry.Name}").Append('\n');

        foreach (var header in entry.Headers)
        {
            builder.Append(header).Append(": ").Append(DescribeHeader(header)).Append('\n');
        }

        // 204 and 304 must not carry a body, and 1xx never does.
        if (entry.Code is 204 or 304 || entry.Class == HttpStatusClass.Informational)
        {
            return builder.ToString().TrimEnd();
        }

        builder.Append("Content-Type: application/json\n\n");
        builder.Append(CultureInfo.InvariantCulture,
            $$"""
            {
              "status": {{entry.Code}},
              "title": "{{entry.Name}}",
              "detail": "{{entry.Description}}"
            }
            """);

        return builder.ToString();
    }

    private static string DescribeHeader(string header) => header switch
    {
        "Location" => "https://api.example.com/resources/42",
        "Retry-After" => "120",
        "WWW-Authenticate" => "Bearer realm=\"api\"",
        "Allow" => "GET, HEAD",
        "ETag" => "\"a1b2c3\"",
        "Content-Range" => "bytes 0-1023/4096",
        "Upgrade" => "websocket",
        "Link" => "</styles.css>; rel=preload; as=style",
        _ => "…"
    };
}
