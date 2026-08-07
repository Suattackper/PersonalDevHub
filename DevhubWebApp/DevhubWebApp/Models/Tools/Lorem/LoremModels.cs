namespace DevhubWebApp.Models.Tools.Lorem;

/// <summary>Unit the generator produces.</summary>
public enum LoremUnit
{
    Words,
    Sentences,
    Paragraphs
}

/// <summary>Output format of the generated placeholder text.</summary>
public enum LoremFormat
{
    PlainText,
    Html,
    Markdown
}

/// <summary>Everything that shapes a generated passage.</summary>
public sealed record LoremOptions
{
    public static LoremOptions Default { get; } = new();

    public LoremUnit Unit { get; init; } = LoremUnit.Paragraphs;

    /// <summary>How many words, sentences or paragraphs to produce.</summary>
    public int Count { get; init; } = 3;

    /// <summary>Opens with the canonical "Lorem ipsum dolor sit amet…".</summary>
    public bool StartWithLoremIpsum { get; init; } = true;

    public LoremFormat Format { get; init; } = LoremFormat.PlainText;

    /// <summary>
    /// Seed for the word picker. The same seed always produces the same passage, which makes the
    /// output usable in tests and screenshots.
    /// </summary>
    public int? Seed { get; init; }
}
