using DevhubWebApp.Models.Tools.Markdown;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Maps a toolbar action to the Markdown it produces. Pure lookup, so the editor component holds
/// no syntax knowledge and the mapping is testable on its own.
/// </summary>
public static class MarkdownSyntax
{
    private const string TableTemplate = """
        | Column A | Column B |
        | -------- | -------- |
        | Cell     | Cell     |
        """;

    /// <summary>Returns the snippet for an action.</summary>
    public static MarkdownSnippet For(MarkdownAction action) => action switch
    {
        MarkdownAction.Bold => new MarkdownSnippet { Prefix = "**", Suffix = "**", Placeholder = "bold text" },
        MarkdownAction.Italic => new MarkdownSnippet { Prefix = "*", Suffix = "*", Placeholder = "italic text" },
        MarkdownAction.Strikethrough => new MarkdownSnippet { Prefix = "~~", Suffix = "~~", Placeholder = "struck text" },
        MarkdownAction.InlineCode => new MarkdownSnippet { Prefix = "`", Suffix = "`", Placeholder = "code" },
        MarkdownAction.Heading1 => new MarkdownSnippet { Prefix = "# ", Placeholder = "Heading", IsBlock = true },
        MarkdownAction.Heading2 => new MarkdownSnippet { Prefix = "## ", Placeholder = "Heading", IsBlock = true },
        MarkdownAction.Heading3 => new MarkdownSnippet { Prefix = "### ", Placeholder = "Heading", IsBlock = true },
        MarkdownAction.Link => new MarkdownSnippet { Prefix = "[", Suffix = "](https://)", Placeholder = "link text" },
        MarkdownAction.Image => new MarkdownSnippet { Prefix = "![", Suffix = "](https://)", Placeholder = "alt text" },
        MarkdownAction.Quote => new MarkdownSnippet { Prefix = "> ", Placeholder = "quote", IsBlock = true },
        MarkdownAction.BulletList => new MarkdownSnippet { Prefix = "- ", Placeholder = "item", IsBlock = true },
        MarkdownAction.NumberedList => new MarkdownSnippet { Prefix = "1. ", Placeholder = "item", IsBlock = true },
        MarkdownAction.TaskList => new MarkdownSnippet { Prefix = "- [ ] ", Placeholder = "task", IsBlock = true },
        MarkdownAction.CodeBlock => new MarkdownSnippet { Prefix = "```\n", Suffix = "\n```", Placeholder = "code", IsBlock = true },
        MarkdownAction.Table => new MarkdownSnippet { Prefix = TableTemplate, Placeholder = string.Empty, IsBlock = true },
        MarkdownAction.HorizontalRule => new MarkdownSnippet { Prefix = "---", Placeholder = string.Empty, IsBlock = true },
        _ => new MarkdownSnippet()
    };

    /// <summary>
    /// Builds the text an action inserts for the given selection. Block actions are separated from
    /// surrounding content with blank lines when they are not already at the start of a line.
    /// </summary>
    public static string Build(MarkdownAction action, string? selection, bool atLineStart = true)
    {
        var snippet = For(action);
        var text = snippet.Apply(selection);

        return snippet.IsBlock && !atLineStart ? $"\n\n{text}" : text;
    }
}
