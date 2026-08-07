using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;

namespace DevhubWebApp.Services;

/// <summary>
/// Builds standalone HTML exports. The stylesheet is inlined and the document carries no external
/// references, so an exported file opens identically on a machine that has never seen DevHub.
/// </summary>
public sealed class HtmlExportService : IHtmlExportService
{
    private const string BaseCss = """
        :root { color-scheme: light dark; }
        body {
            margin: 0 auto;
            padding: 40px 24px 80px;
            max-width: 860px;
            font-family: Inter, "Segoe UI", system-ui, -apple-system, sans-serif;
            font-size: 16px;
            line-height: 1.65;
            color: #1f2328;
            background-color: #ffffff;
        }
        h1, h2, h3, h4 { line-height: 1.25; margin-top: 1.8em; }
        h1 { padding-bottom: .3em; border-bottom: 1px solid #d0d7de; }
        code, pre { font-family: ui-monospace, "Cascadia Mono", Consolas, monospace; font-size: .875em; }
        code { padding: .2em .4em; border-radius: 6px; background-color: rgba(129,139,152,.16); }
        pre { padding: 16px; overflow: auto; border-radius: 8px; background-color: #f6f8fa; }
        pre code { padding: 0; background: none; }
        blockquote { margin: 0; padding: 0 1em; color: #59636e; border-left: .25em solid #d0d7de; }
        table { border-collapse: collapse; display: block; overflow: auto; }
        th, td { padding: 6px 13px; border: 1px solid #d0d7de; }
        tr:nth-child(2n) { background-color: #f6f8fa; }
        img { max-width: 100%; }
        .devhub-export-footer { margin-top: 64px; padding-top: 16px; border-top: 1px solid #d0d7de; font-size: .8em; color: #59636e; }
        @media (prefers-color-scheme: dark) {
            body { color: #e6edf3; background-color: #0d1117; }
            h1 { border-bottom-color: #30363d; }
            pre { background-color: #161b22; }
            blockquote { color: #8b949e; border-left-color: #30363d; }
            th, td { border-color: #30363d; }
            tr:nth-child(2n) { background-color: #161b22; }
            .devhub-export-footer { border-top-color: #30363d; color: #8b949e; }
        }
        """;

    public string BuildDocument(string title, string bodyHtml, string? extraCss = null)
    {
        var safeTitle = HtmlEncoder.Default.Encode(string.IsNullOrWhiteSpace(title) ? AppConstants.AppName : title);
        var generated = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\" />");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine(CultureInfo.InvariantCulture, $"<title>{safeTitle}</title>");
        builder.AppendLine("<style>");
        builder.AppendLine(BaseCss);

        if (!string.IsNullOrWhiteSpace(extraCss))
        {
            builder.AppendLine(extraCss);
        }

        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine(bodyHtml);
        builder.AppendLine(CultureInfo.InvariantCulture,
            $"<footer class=\"devhub-export-footer\">Exported from {AppConstants.AppName} on {generated}</footer>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");

        return builder.ToString();
    }

    public string ToFileName(string? title, string extension, string fallback = "devhub")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var slug = Slugify(title);
        var name = string.IsNullOrEmpty(slug) ? fallback : slug;

        return $"{name}.{extension.TrimStart('.')}";
    }

    /// <summary>
    /// Keeps letters and digits, folds everything else into single hyphens. The result is safe on
    /// every file system and readable, which matters more than preserving the original text.
    /// </summary>
    private static string Slugify(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasSeparator = false;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                lastWasSeparator = false;
            }
            else if (!lastWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                lastWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
