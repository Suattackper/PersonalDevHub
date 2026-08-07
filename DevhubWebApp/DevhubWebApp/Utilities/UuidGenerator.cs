using System.Globalization;
using System.Text;

namespace DevhubWebApp.Utilities;

/// <summary>Textual layouts a generated identifier can take.</summary>
public enum UuidFormat
{
    /// <summary>Canonical hyphenated form, e.g. <c>3f2a…-…</c>.</summary>
    Standard,

    /// <summary>Hyphenated and upper-case.</summary>
    Uppercase,

    /// <summary>32 hex characters with no hyphens.</summary>
    Compact,

    /// <summary>Braced form used by some Windows APIs.</summary>
    Braced,

    /// <summary>URN form, <c>urn:uuid:…</c>.</summary>
    Urn
}

/// <summary>
/// Generation and export of RFC 4122 version 4 identifiers. Pure: it produces strings, and every
/// format and export shape is testable without a browser.
/// </summary>
public static class UuidGenerator
{
    /// <summary>Upper bound on a single batch; the UI offers this as its maximum.</summary>
    public const int MaxBatchSize = 1000;

    /// <summary>
    /// Generates <paramref name="count"/> identifiers. <see cref="Guid.NewGuid"/> is a
    /// cryptographically random version 4 UUID on every platform DevHub runs on.
    /// </summary>
    public static IReadOnlyList<string> Generate(int count, UuidFormat format = UuidFormat.Standard)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxBatchSize);

        return Enumerable.Range(0, count).Select(_ => Format(Guid.NewGuid(), format)).ToList();
    }

    /// <summary>Renders one identifier in the requested layout.</summary>
    public static string Format(Guid value, UuidFormat format) => format switch
    {
        UuidFormat.Standard => value.ToString("d", CultureInfo.InvariantCulture),
        UuidFormat.Uppercase => value.ToString("d", CultureInfo.InvariantCulture).ToUpperInvariant(),
        UuidFormat.Compact => value.ToString("n", CultureInfo.InvariantCulture),
        UuidFormat.Braced => value.ToString("b", CultureInfo.InvariantCulture),
        UuidFormat.Urn => $"urn:uuid:{value.ToString("d", CultureInfo.InvariantCulture)}",
        _ => value.ToString("d", CultureInfo.InvariantCulture)
    };

    /// <summary>Display name of a format, for the UI.</summary>
    public static string GetName(UuidFormat format) => format switch
    {
        UuidFormat.Standard => "Standard",
        UuidFormat.Uppercase => "Uppercase",
        UuidFormat.Compact => "No hyphens",
        UuidFormat.Braced => "Braced",
        UuidFormat.Urn => "URN",
        _ => format.ToString()
    };

    /// <summary>One identifier per line — the form most tools and shells expect.</summary>
    public static string ToText(IEnumerable<string> identifiers)
    {
        ArgumentNullException.ThrowIfNull(identifiers);

        return string.Join('\n', identifiers);
    }

    /// <summary>
    /// CSV with a header row and a 1-based index, so a batch can be pasted straight into a sheet.
    /// Values are quoted because a future format could contain a separator.
    /// </summary>
    public static string ToCsv(IEnumerable<string> identifiers)
    {
        ArgumentNullException.ThrowIfNull(identifiers);

        var builder = new StringBuilder();
        builder.AppendLine("index,uuid");

        var index = 1;
        foreach (var identifier in identifiers)
        {
            builder.Append(index.ToString(CultureInfo.InvariantCulture))
                .Append(",\"")
                .Append(identifier.Replace("\"", "\"\"", StringComparison.Ordinal))
                .AppendLine("\"");

            index++;
        }

        return builder.ToString();
    }

    /// <summary>JSON array, for feeding a batch into a test fixture or a seed script.</summary>
    public static string ToJson(IEnumerable<string> identifiers)
    {
        ArgumentNullException.ThrowIfNull(identifiers);

        return System.Text.Json.JsonSerializer.Serialize(identifiers, JsonOptions);
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true };
}
