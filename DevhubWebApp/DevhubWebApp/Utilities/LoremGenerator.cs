using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using DevhubWebApp.Models.Tools.Lorem;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Generates classical placeholder text. Pure and deterministic for a given seed, which is what
/// lets the same passage be reproduced in a test, a screenshot or a design review.
/// </summary>
public static class LoremGenerator
{
    /// <summary>The opening every reader recognises, used when the caller asks for it.</summary>
    public const string ClassicOpening = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";

    private const int MinWordsPerSentence = 6;
    private const int MaxWordsPerSentence = 18;
    private const int MinSentencesPerParagraph = 3;
    private const int MaxSentencesPerParagraph = 7;

    /// <summary>Largest count accepted, so a stray keystroke cannot ask for a million paragraphs.</summary>
    public const int MaxCount = 500;

    private static readonly string[] Vocabulary =
    [
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit", "sed", "do",
        "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore", "magna", "aliqua", "enim",
        "ad", "minim", "veniam", "quis", "nostrud", "exercitation", "ullamco", "laboris", "nisi", "aliquip",
        "ex", "ea", "commodo", "consequat", "duis", "aute", "irure", "in", "reprehenderit", "voluptate",
        "velit", "esse", "cillum", "eu", "fugiat", "nulla", "pariatur", "excepteur", "sint", "occaecat",
        "cupidatat", "non", "proident", "sunt", "culpa", "qui", "officia", "deserunt", "mollit", "anim",
        "id", "est", "laborum", "perspiciatis", "unde", "omnis", "iste", "natus", "error", "voluptatem",
        "accusantium", "doloremque", "laudantium", "totam", "rem", "aperiam", "eaque", "ipsa", "quae", "ab",
        "illo", "inventore", "veritatis", "quasi", "architecto", "beatae", "vitae", "dicta", "explicabo", "nemo",
        "voluptas", "aspernatur", "odit", "fugit", "consequuntur", "magni", "dolores", "eos", "ratione", "sequi"
    ];

    /// <summary>Generates a passage according to <paramref name="options"/>.</summary>
    public static string Generate(LoremOptions? options = null)
    {
        options ??= LoremOptions.Default;

        var count = Math.Clamp(options.Count, 1, MaxCount);
        var random = options.Seed is null ? new Random() : new Random(options.Seed.Value);

        List<string> blocks = options.Unit switch
        {
            LoremUnit.Words => [BuildWords(random, count, options.StartWithLoremIpsum)],
            LoremUnit.Sentences => [BuildSentences(random, count, options.StartWithLoremIpsum)],
            _ => BuildParagraphs(random, count, options.StartWithLoremIpsum)
        };

        return Render(blocks, options.Format, options.Unit);
    }

    /// <summary>Word, sentence and paragraph counts of a generated passage, for the UI's status line.</summary>
    public static (int Words, int Characters) Measure(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return (0, 0);
        }

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        return (words, text.Length);
    }

    private static string BuildWords(Random random, int count, bool startWithClassic)
    {
        var words = new List<string>(count);

        if (startWithClassic)
        {
            words.AddRange(ClassicOpening.Replace(",", string.Empty, StringComparison.Ordinal)
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(count));
        }

        while (words.Count < count)
        {
            words.Add(Vocabulary[random.Next(Vocabulary.Length)]);
        }

        return Capitalise(string.Join(' ', words.Take(count)));
    }

    private static string BuildSentences(Random random, int count, bool startWithClassic)
    {
        var sentences = new List<string>(count);

        for (var index = 0; index < count; index++)
        {
            sentences.Add(index == 0 && startWithClassic
                ? $"{ClassicOpening}."
                : BuildSentence(random));
        }

        return string.Join(' ', sentences);
    }

    private static List<string> BuildParagraphs(Random random, int count, bool startWithClassic)
    {
        var paragraphs = new List<string>(count);

        for (var index = 0; index < count; index++)
        {
            var sentenceCount = random.Next(MinSentencesPerParagraph, MaxSentencesPerParagraph + 1);
            var sentences = new List<string>(sentenceCount);

            for (var position = 0; position < sentenceCount; position++)
            {
                sentences.Add(index == 0 && position == 0 && startWithClassic
                    ? $"{ClassicOpening}."
                    : BuildSentence(random));
            }

            paragraphs.Add(string.Join(' ', sentences));
        }

        return paragraphs;
    }

    private static string BuildSentence(Random random)
    {
        var length = random.Next(MinWordsPerSentence, MaxWordsPerSentence + 1);
        var words = new List<string>(length);

        for (var index = 0; index < length; index++)
        {
            words.Add(Vocabulary[random.Next(Vocabulary.Length)]);
        }

        // A comma somewhere in the middle keeps the rhythm of real prose.
        if (length > 8)
        {
            var commaAt = random.Next(3, length - 3);
            words[commaAt] += ",";
        }

        return $"{Capitalise(string.Join(' ', words))}.";
    }

    /// <summary>
    /// Renders the blocks. Line endings are always '\n', never the host's: the output is copied,
    /// downloaded and compared in a browser, so it must not depend on the machine that produced it.
    /// </summary>
    private static string Render(List<string> blocks, LoremFormat format, LoremUnit unit)
    {
        const char NewLine = '\n';
        var builder = new StringBuilder();

        foreach (var block in blocks)
        {
            switch (format)
            {
                case LoremFormat.Html:
                    builder.Append(CultureInfo.InvariantCulture, $"<p>{HtmlEncoder.Default.Encode(block)}</p>").Append(NewLine);
                    break;

                case LoremFormat.Markdown:
                    builder.Append(block).Append(NewLine).Append(NewLine);
                    break;

                default:
                    builder.Append(block).Append(NewLine);

                    if (unit == LoremUnit.Paragraphs && blocks.Count > 1)
                    {
                        builder.Append(NewLine);
                    }

                    break;
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string Capitalise(string value)
        => string.IsNullOrEmpty(value) ? value : char.ToUpperInvariant(value[0]) + value[1..];
}

