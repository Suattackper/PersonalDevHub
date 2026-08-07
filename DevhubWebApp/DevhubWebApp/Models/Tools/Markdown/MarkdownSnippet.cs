namespace DevhubWebApp.Models.Tools.Markdown;

/// <summary>Formatting actions the Markdown toolbar can apply.</summary>
public enum MarkdownAction
{
    Bold,
    Italic,
    Strikethrough,
    InlineCode,
    Heading1,
    Heading2,
    Heading3,
    Link,
    Image,
    Quote,
    BulletList,
    NumberedList,
    TaskList,
    CodeBlock,
    Table,
    HorizontalRule
}

/// <summary>
/// The text a formatting action produces around the current selection. Computed by
/// <c>MarkdownSyntax</c> so the editor component only has to apply the replacement.
/// </summary>
public sealed record MarkdownSnippet
{
    /// <summary>Text inserted before the selection.</summary>
    public string Prefix { get; init; } = string.Empty;

    /// <summary>Text inserted after the selection.</summary>
    public string Suffix { get; init; } = string.Empty;

    /// <summary>Used in place of an empty selection, so the result is never a bare marker pair.</summary>
    public string Placeholder { get; init; } = string.Empty;

    /// <summary>Block actions start on their own line rather than wrapping inline text.</summary>
    public bool IsBlock { get; init; }

    /// <summary>Builds the replacement text for a selection.</summary>
    public string Apply(string? selection)
    {
        var body = string.IsNullOrEmpty(selection) ? Placeholder : selection;
        return $"{Prefix}{body}{Suffix}";
    }
}
