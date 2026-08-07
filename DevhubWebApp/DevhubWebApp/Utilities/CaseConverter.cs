using System.Globalization;
using System.Text;

namespace DevhubWebApp.Utilities;

/// <summary>Casing styles the converter can produce.</summary>
public enum TextCase
{
    Camel,
    Pascal,
    Snake,
    Kebab,
    Constant,
    Title,
    Sentence,
    Lower,
    Upper,
    Toggle
}

/// <summary>
/// Converts text between naming conventions.
/// <para>
/// Everything routes through one tokeniser: a phrase is split into words once, then re-joined in
/// the requested style. That is what makes <c>parseHTTPResponse</c> → <c>parse_http_response</c>
/// and <c>hello world</c> → <c>helloWorld</c> behave consistently instead of each style inventing
/// its own splitting rules.
/// </para>
/// </summary>
public static class CaseConverter
{
    /// <summary>All styles in the order the UI presents them.</summary>
    public static IReadOnlyList<TextCase> All { get; } = Enum.GetValues<TextCase>();

    /// <summary>Display name of a style, e.g. <c>snake_case</c>.</summary>
    public static string GetName(TextCase style) => style switch
    {
        TextCase.Camel => "camelCase",
        TextCase.Pascal => "PascalCase",
        TextCase.Snake => "snake_case",
        TextCase.Kebab => "kebab-case",
        TextCase.Constant => "CONSTANT_CASE",
        TextCase.Title => "Title Case",
        TextCase.Sentence => "Sentence case",
        TextCase.Lower => "lowercase",
        TextCase.Upper => "UPPERCASE",
        TextCase.Toggle => "tOGGLE cASE",
        _ => style.ToString()
    };

    /// <summary>Converts <paramref name="text"/> to the requested style, line by line.</summary>
    public static string Convert(string? text, TextCase style)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Whole-text styles must not be tokenised: they preserve the original punctuation.
        switch (style)
        {
            case TextCase.Lower:
                return text.ToLowerInvariant();

            case TextCase.Upper:
                return text.ToUpperInvariant();

            case TextCase.Toggle:
                return Toggle(text);

            case TextCase.Sentence:
                return ConvertLines(text, ToSentence);

            case TextCase.Title:
                return ConvertLines(text, line => Join(Tokenize(line), " ", Capitalise));

            case TextCase.Camel:
                return ConvertLines(text, line => JoinCamel(Tokenize(line)));

            case TextCase.Pascal:
                return ConvertLines(text, line => Join(Tokenize(line), string.Empty, Capitalise));

            case TextCase.Snake:
                return ConvertLines(text, line => Join(Tokenize(line), "_", word => word.ToLowerInvariant()));

            case TextCase.Kebab:
                return ConvertLines(text, line => Join(Tokenize(line), "-", word => word.ToLowerInvariant()));

            case TextCase.Constant:
                return ConvertLines(text, line => Join(Tokenize(line), "_", word => word.ToUpperInvariant()));

            default:
                return text;
        }
    }

    /// <summary>
    /// Splits a phrase into words. Handles separators, camel boundaries and acronym runs, so
    /// <c>HTTPResponseCode</c> yields <c>HTTP</c>, <c>Response</c>, <c>Code</c>.
    /// </summary>
    public static IReadOnlyList<string> Tokenize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var words = new List<string>();
        var current = new StringBuilder();

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];

            if (!char.IsLetterOrDigit(character))
            {
                Flush(words, current);
                continue;
            }

            if (current.Length > 0 && IsBoundary(text, index))
            {
                Flush(words, current);
            }

            current.Append(character);
        }

        Flush(words, current);
        return words;
    }

    /// <summary>Removes leading and trailing whitespace from every line.</summary>
    public static string TrimLines(string? text)
        => string.IsNullOrEmpty(text)
            ? string.Empty
            : string.Join('\n', SplitLines(text).Select(line => line.Trim()));

    /// <summary>Drops lines that contain nothing but whitespace.</summary>
    public static string RemoveEmptyLines(string? text)
        => string.IsNullOrEmpty(text)
            ? string.Empty
            : string.Join('\n', SplitLines(text).Where(line => !string.IsNullOrWhiteSpace(line)));

    /// <summary>Collapses runs of whitespace inside each line into single spaces.</summary>
    public static string CollapseSpaces(string? text)
        => string.IsNullOrEmpty(text)
            ? string.Empty
            : string.Join('\n', SplitLines(text)
                .Select(line => string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))));

    /// <summary>
    /// A boundary sits before an upper-case letter that follows a lower-case letter or digit, and
    /// before the last upper-case letter of an acronym run that precedes a lower-case letter.
    /// </summary>
    private static bool IsBoundary(string text, int index)
    {
        var character = text[index];
        var previous = text[index - 1];

        if (char.IsUpper(character) && (char.IsLower(previous) || char.IsDigit(previous)))
        {
            return true;
        }

        if (char.IsUpper(character) && char.IsUpper(previous) && index + 1 < text.Length && char.IsLower(text[index + 1]))
        {
            return true;
        }

        return char.IsDigit(character) != char.IsDigit(previous) && char.IsLetter(previous) && char.IsDigit(character);
    }

    private static void Flush(List<string> words, StringBuilder current)
    {
        if (current.Length > 0)
        {
            words.Add(current.ToString());
            current.Clear();
        }
    }

    private static string ConvertLines(string text, Func<string, string> convert)
        => string.Join('\n', SplitLines(text).Select(convert));

    private static string[] SplitLines(string text)
        => text.ReplaceLineEndings("\n").Split('\n');

    private static string Join(IReadOnlyList<string> words, string separator, Func<string, string> transform)
        => words.Count == 0 ? string.Empty : string.Join(separator, words.Select(transform));

    private static string JoinCamel(IReadOnlyList<string> words)
    {
        if (words.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(words[0].ToLowerInvariant());

        for (var index = 1; index < words.Count; index++)
        {
            builder.Append(Capitalise(words[index]));
        }

        return builder.ToString();
    }

    private static string Capitalise(string word)
        => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant();

    /// <summary>Lower-cases the line, then capitalises the first letter it contains.</summary>
    private static string ToSentence(string line)
    {
        var lowered = line.ToLowerInvariant();

        for (var index = 0; index < lowered.Length; index++)
        {
            if (char.IsLetter(lowered[index]))
            {
                return lowered[..index] + char.ToUpper(lowered[index], CultureInfo.InvariantCulture) + lowered[(index + 1)..];
            }
        }

        return lowered;
    }

    private static string Toggle(string text)
    {
        var builder = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            builder.Append(char.IsUpper(character) ? char.ToLowerInvariant(character) : char.ToUpperInvariant(character));
        }

        return builder.ToString();
    }
}
