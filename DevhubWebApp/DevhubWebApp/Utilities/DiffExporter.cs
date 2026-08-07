using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using DevhubWebApp.Models.Tools.Diff;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Renders a computed diff for export. Pure text in, pure text out — no services, no interop —
/// so both formats can be verified directly.
/// </summary>
public static class DiffExporter
{
    /// <summary>Stylesheet added to the exported HTML, colouring added and removed rows.</summary>
    public const string HtmlCss = """
        .devhub-diff { border-collapse: collapse; width: 100%; font-family: ui-monospace, "Cascadia Mono", Consolas, monospace; font-size: 13px; }
        .devhub-diff td { padding: 2px 10px; vertical-align: top; white-space: pre-wrap; word-break: break-word; }
        .devhub-diff .num { width: 1%; text-align: right; color: #8b949e; user-select: none; }
        .devhub-diff .added { background-color: rgba(63,185,80,.16); }
        .devhub-diff .removed { background-color: rgba(248,81,73,.16); }
        .devhub-diff .sign { width: 1%; user-select: none; }
        """;

    /// <summary>Unified-style plain text, in the shape a reviewer expects from a patch.</summary>
    public static string ToText(DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.Append("--- left").AppendLine();
        builder.Append("+++ right").AppendLine();
        builder.Append(CultureInfo.InvariantCulture,
                $"@@ +{result.Statistics.Added} -{result.Statistics.Removed} ({result.Statistics.ChangePercentage}% changed) @@")
            .AppendLine();

        foreach (var line in result.Lines)
        {
            builder.Append(Sign(line.Kind)).Append(line.Text).AppendLine();
        }

        return builder.ToString();
    }

    /// <summary>HTML table fragment; wrap it with <c>IHtmlExportService</c> for a full document.</summary>
    public static string ToHtmlFragment(DiffResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine("<table class=\"devhub-diff\">");

        foreach (var line in result.Lines)
        {
            var cssClass = line.Kind switch
            {
                DiffLineKind.Added => " class=\"added\"",
                DiffLineKind.Removed => " class=\"removed\"",
                _ => string.Empty
            };

            builder.Append(CultureInfo.InvariantCulture, $"<tr{cssClass}>")
                .Append(CultureInfo.InvariantCulture, $"<td class=\"num\">{line.LeftLineNumber?.ToString(CultureInfo.InvariantCulture)}</td>")
                .Append(CultureInfo.InvariantCulture, $"<td class=\"num\">{line.RightLineNumber?.ToString(CultureInfo.InvariantCulture)}</td>")
                .Append(CultureInfo.InvariantCulture, $"<td class=\"sign\">{Sign(line.Kind)}</td>")
                .Append(CultureInfo.InvariantCulture, $"<td>{HtmlEncoder.Default.Encode(line.Text)}</td>")
                .AppendLine("</tr>");
        }

        builder.AppendLine("</table>");
        return builder.ToString();
    }

    private static char Sign(DiffLineKind kind) => kind switch
    {
        DiffLineKind.Added => '+',
        DiffLineKind.Removed => '-',
        _ => ' '
    };
}
