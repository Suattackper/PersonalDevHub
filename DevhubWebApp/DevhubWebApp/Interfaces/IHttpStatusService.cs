using DevhubWebApp.Models.Tools.Http;

namespace DevhubWebApp.Interfaces;

/// <summary>Look-up over the HTTP status catalogue.</summary>
public interface IHttpStatusService
{
    /// <summary>Every known status code, in numeric order.</summary>
    IReadOnlyList<HttpStatusEntry> All { get; }

    /// <summary>The five status classes.</summary>
    IReadOnlyList<HttpStatusClass> Classes { get; }

    /// <summary>The entry for a numeric code, or <c>null</c> when it is not catalogued.</summary>
    HttpStatusEntry? Find(int code);

    /// <summary>
    /// Free-text search across code, name, description and use cases, optionally narrowed to one
    /// class. An exact code match is ranked first.
    /// </summary>
    IReadOnlyList<HttpStatusEntry> Search(string? term, HttpStatusClass? statusClass = null);

    /// <summary>Codes worth comparing against this one, resolved to entries.</summary>
    IReadOnlyList<HttpStatusEntry> GetRelated(HttpStatusEntry entry);

    /// <summary>A pasteable example response for this status.</summary>
    string BuildExample(HttpStatusEntry entry);
}
