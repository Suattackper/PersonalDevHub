using System.Globalization;
using System.Text;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Numeral systems the converter understands. Values are the radix itself, so the enum doubles as
/// the parameter the parser needs.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1720:Identifier contains type name",
    Justification = "'Decimal' is the name of the numeral system; renaming it would obscure the domain.")]
public enum NumberBase
{
    Binary = 2,
    Octal = 8,
    Decimal = 10,
    Hexadecimal = 16
}

/// <summary>Outcome of parsing a value in some base.</summary>
public sealed record NumberParseResult
{
    public static NumberParseResult Empty { get; } = new();

    public bool IsSuccess => Error is null && HasValue;

    /// <summary><c>false</c> when nothing was entered, which is not an error.</summary>
    public bool HasValue { get; init; }

    public long Value { get; init; }

    /// <summary>Why the input could not be parsed; <c>null</c> when it parsed.</summary>
    public string? Error { get; init; }

    public static NumberParseResult Failure(string error) => new() { HasValue = true, Error = error };

    public static NumberParseResult Success(long value) => new() { HasValue = true, Value = value };
}

/// <summary>
/// Converts integers between numeral systems, and text to and from its code points.
/// Pure and validation-first: an unparsable input returns an explanation, never an exception.
/// </summary>
public static class NumberBaseConverter
{
    /// <summary>Bases in the order the UI presents them.</summary>
    public static IReadOnlyList<NumberBase> All { get; } = [NumberBase.Binary, NumberBase.Octal, NumberBase.Decimal, NumberBase.Hexadecimal];

    /// <summary>Display name of a base.</summary>
    public static string GetName(NumberBase numberBase) => numberBase switch
    {
        NumberBase.Binary => "Binary",
        NumberBase.Octal => "Octal",
        NumberBase.Decimal => "Decimal",
        NumberBase.Hexadecimal => "Hexadecimal",
        _ => numberBase.ToString()
    };

    /// <summary>Conventional literal prefix, e.g. <c>0x</c> for hexadecimal.</summary>
    public static string GetPrefix(NumberBase numberBase) => numberBase switch
    {
        NumberBase.Binary => "0b",
        NumberBase.Octal => "0o",
        NumberBase.Hexadecimal => "0x",
        _ => string.Empty
    };

    /// <summary>
    /// Parses a value written in <paramref name="numberBase"/>. Accepts the conventional prefix,
    /// a leading minus, and digit separators (<c>_</c> and spaces) — all forms people paste.
    /// </summary>
    public static NumberParseResult Parse(string? input, NumberBase numberBase)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return NumberParseResult.Empty;
        }

        var text = Normalise(input, numberBase, out var isNegative);

        if (text.Length == 0)
        {
            return NumberParseResult.Failure($"Enter a {GetName(numberBase).ToLowerInvariant()} value.");
        }

        var radix = (int)numberBase;

        foreach (var character in text)
        {
            if (DigitValue(character) is not { } digit || digit >= radix)
            {
                return NumberParseResult.Failure($"'{character}' is not a valid {GetName(numberBase).ToLowerInvariant()} digit.");
            }
        }

        try
        {
            var magnitude = System.Convert.ToInt64(text, radix);
            return NumberParseResult.Success(isNegative ? -magnitude : magnitude);
        }
        catch (OverflowException)
        {
            return NumberParseResult.Failure("Value is too large — the converter works with 64-bit integers.");
        }
    }

    /// <summary>Renders a value in the given base, optionally with its conventional prefix.</summary>
    public static string Format(long value, NumberBase numberBase, bool includePrefix = false)
    {
        var isNegative = value < 0;
        var magnitude = isNegative ? (ulong)(-(value + 1)) + 1 : (ulong)value;

        var digits = numberBase == NumberBase.Decimal
            ? magnitude.ToString(CultureInfo.InvariantCulture)
            : ToRadix(magnitude, (int)numberBase);

        var prefix = includePrefix ? GetPrefix(numberBase) : string.Empty;

        return $"{(isNegative ? "-" : string.Empty)}{prefix}{digits}";
    }

    /// <summary>
    /// Renders a value as text when it is a valid Unicode scalar. Surrogate halves and values past
    /// the Unicode range have no character, and are reported as such rather than as a broken glyph.
    /// </summary>
    public static string ToCharacter(long value)
    {
        if (value is < 0 or > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF))
        {
            return string.Empty;
        }

        return char.ConvertFromUtf32((int)value);
    }

    /// <summary>Formats a value as a <c>U+</c> code point when it is a valid Unicode scalar.</summary>
    public static string ToCodePoint(long value)
        => ToCharacter(value).Length == 0 ? string.Empty : $"U+{value:X4}";

    /// <summary>Code points of every character in the text, as space-separated hex values.</summary>
    public static string TextToCodePoints(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();

        // Runes, not chars: an emoji is one code point spread over two UTF-16 units.
        foreach (var rune in text.EnumerateRunes())
        {
            builder.Append(CultureInfo.InvariantCulture, $"U+{rune.Value:X4} ");
        }

        return builder.ToString().TrimEnd();
    }

    private static string Normalise(string input, NumberBase numberBase, out bool isNegative)
    {
        var text = input.Trim().Replace("_", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal);

        isNegative = text.StartsWith('-');
        if (isNegative || text.StartsWith('+'))
        {
            text = text[1..];
        }

        var prefix = GetPrefix(numberBase);
        if (prefix.Length > 0 && text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            text = text[prefix.Length..];
        }

        return text;
    }

    private static int? DigitValue(char character) => character switch
    {
        >= '0' and <= '9' => character - '0',
        >= 'a' and <= 'f' => character - 'a' + 10,
        >= 'A' and <= 'F' => character - 'A' + 10,
        _ => null
    };

    private static string ToRadix(ulong value, int radix)
    {
        if (value == 0)
        {
            return "0";
        }

        const string Digits = "0123456789abcdef";
        var builder = new StringBuilder();

        while (value > 0)
        {
            builder.Insert(0, Digits[(int)(value % (ulong)radix)]);
            value /= (ulong)radix;
        }

        return builder.ToString();
    }
}
