using DevhubWebApp.Models.Tools.Markdown;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// Markdown rendering and analysis. Kept behind an interface so the editor never depends on the
/// Markdown engine directly.
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Renders Markdown to an HTML fragment. Raw HTML in the source is neutralised: the preview
    /// renders untrusted text and must not be able to execute it.
    /// </summary>
    string ToHtml(string? markdown);

    /// <summary>Renders Markdown to plain text, used for word counts and reading time.</summary>
    string ToPlainText(string? markdown);

    /// <summary>Word, character, line counts and reading-time estimate.</summary>
    MarkdownStatistics Analyse(string? markdown);
}
