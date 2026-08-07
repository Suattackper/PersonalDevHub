namespace DevhubWebApp.Interfaces;

/// <summary>
/// Wraps an HTML fragment into a standalone, self-contained document. Shared by every tool that
/// exports HTML, so exported files look and behave the same wherever they come from.
/// </summary>
public interface IHtmlExportService
{
    /// <summary>
    /// Produces a complete HTML document. <paramref name="bodyHtml"/> is inserted verbatim and must
    /// already be safe to render; callers pass output from a sanitising renderer.
    /// </summary>
    /// <param name="title">Document title, also used as the page heading in the export.</param>
    /// <param name="bodyHtml">Trusted HTML fragment.</param>
    /// <param name="extraCss">Optional additional stylesheet rules.</param>
    string BuildDocument(string title, string bodyHtml, string? extraCss = null);

    /// <summary>Turns arbitrary text into a safe file name, e.g. <c>My notes</c> → <c>my-notes</c>.</summary>
    string ToFileName(string? title, string extension, string fallback = "devhub");
}
