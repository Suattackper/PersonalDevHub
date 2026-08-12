using System.Globalization;

namespace DevhubWebApp.Utilities;

/// <summary>Unit a Unix timestamp is expressed in.</summary>
public enum TimestampUnit
{
    Seconds,
    Milliseconds
}

/// <summary>Outcome of parsing a timestamp or a date string.</summary>
public sealed record TimestampParseResult
{
    public static TimestampParseResult Empty { get; } = new();

    /// <summary><c>false</c> when nothing was entered, which is not an error.</summary>
    public bool HasValue { get; init; }

    public DateTimeOffset Value { get; init; }

    /// <summary>Why the input could not be read; <c>null</c> when it was.</summary>
    public string? Error { get; init; }

    public bool IsSuccess => HasValue && Error is null;

    public static TimestampParseResult Success(DateTimeOffset value) => new() { HasValue = true, Value = value };

    public static TimestampParseResult Failure(string error) => new() { HasValue = true, Error = error };
}

/// <summary>
/// Conversions between Unix timestamps, absolute instants and the textual formats developers
/// exchange. Pure: an instant goes in, text comes out, and nothing reads the system clock unless
/// the caller asks for <see cref="Now"/>.
/// </summary>
public static class TimestampConverter
{
    /// <summary>Seconds beyond which a value is almost certainly milliseconds: year 5138.</summary>
    private const long MillisecondHeuristic = 100_000_000_000L;

    /// <summary>Formats accepted when parsing a date typed by hand.</summary>
    private static readonly string[] AcceptedFormats =
    [
        "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss.fffZ", "yyyy-MM-ddTHH:mm:sszzz",
        "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd",
        "dd/MM/yyyy HH:mm:ss", "dd/MM/yyyy", "MM/dd/yyyy HH:mm:ss", "MM/dd/yyyy"
    ];

    /// <summary>Current instant. The single place the tool reads the clock.</summary>
    public static DateTimeOffset Now => DateTimeOffset.UtcNow;

    /// <summary>Converts an instant to a Unix timestamp in the requested unit.</summary>
    public static long ToUnix(DateTimeOffset value, TimestampUnit unit)
        => unit == TimestampUnit.Milliseconds ? value.ToUnixTimeMilliseconds() : value.ToUnixTimeSeconds();

    /// <summary>Converts a Unix timestamp to an instant.</summary>
    public static DateTimeOffset FromUnix(long timestamp, TimestampUnit unit)
        => unit == TimestampUnit.Milliseconds
            ? DateTimeOffset.FromUnixTimeMilliseconds(timestamp)
            : DateTimeOffset.FromUnixTimeSeconds(timestamp);

    /// <summary>
    /// Guesses whether a raw number is seconds or milliseconds. Ten-digit values are seconds and
    /// thirteen-digit values are milliseconds; the tool guesses so a paste from either source works.
    /// </summary>
    public static TimestampUnit DetectUnit(long timestamp)
        => Math.Abs(timestamp) >= MillisecondHeuristic ? TimestampUnit.Milliseconds : TimestampUnit.Seconds;

    /// <summary>
    /// Parses a Unix timestamp, auto-detecting its unit unless one is given. Values outside the
    /// representable range are reported rather than thrown.
    /// </summary>
    public static TimestampParseResult ParseTimestamp(string? input, TimestampUnit? unit = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return TimestampParseResult.Empty;
        }

        var text = input.Trim().Replace("_", string.Empty, StringComparison.Ordinal);

        if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
        {
            return TimestampParseResult.Failure("Enter a whole number of seconds or milliseconds.");
        }

        try
        {
            return TimestampParseResult.Success(FromUnix(number, unit ?? DetectUnit(number)));
        }
        catch (ArgumentOutOfRangeException)
        {
            return TimestampParseResult.Failure("That timestamp is outside the representable range of dates.");
        }
    }

    /// <summary>
    /// Parses a date typed by hand. ISO 8601 is tried first, then a small set of common layouts;
    /// a value without an offset is read as UTC so results never depend on the host's locale.
    /// </summary>
    public static TimestampParseResult ParseDate(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return TimestampParseResult.Empty;
        }

        var text = input.Trim();

        if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsed))
        {
            return TimestampParseResult.Success(parsed);
        }

        if (DateTimeOffset.TryParseExact(text, AcceptedFormats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var exact))
        {
            return TimestampParseResult.Success(exact);
        }

        return TimestampParseResult.Failure("Use ISO 8601 (2024-11-30T09:15:00Z) or a common date layout.");
    }

    /// <summary>ISO 8601 with milliseconds, in UTC — the interchange format.</summary>
    public static string ToIso8601(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    /// <summary>
    /// RFC 1123 (the "HTTP-date" of RFC 2822 lineage), which is what appears in HTTP headers.
    /// </summary>
    public static string ToRfc1123(DateTimeOffset value)
        => value.ToUniversalTime().ToString("r", CultureInfo.InvariantCulture);

    /// <summary>Readable absolute form, e.g. <c>Saturday, 30 November 2024 09:15:00</c>.</summary>
    public static string ToLongForm(DateTimeOffset value)
        => value.ToString("dddd, dd MMMM yyyy HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>Sortable form without an offset, for filenames and logs.</summary>
    public static string ToSortable(DateTimeOffset value)
        => value.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    /// <summary>
    /// Distance from <paramref name="reference"/> in words, e.g. "3 hours ago" or "in 2 days".
    /// </summary>
    public static string ToRelative(DateTimeOffset value, DateTimeOffset reference)
    {
        var delta = value - reference;
        var isFuture = delta > TimeSpan.Zero;
        var magnitude = delta.Duration();

        var phrase = magnitude switch
        {
            { TotalSeconds: < 45 } => "a few seconds",
            { TotalMinutes: < 2 } => "a minute",
            { TotalMinutes: < 60 } => Pluralise((int)magnitude.TotalMinutes, "minute"),
            { TotalHours: < 2 } => "an hour",
            { TotalHours: < 24 } => Pluralise((int)magnitude.TotalHours, "hour"),
            { TotalDays: < 2 } => "a day",
            { TotalDays: < 31 } => Pluralise((int)magnitude.TotalDays, "day"),
            { TotalDays: < 365 } => Pluralise((int)(magnitude.TotalDays / 30), "month"),
            _ => Pluralise((int)(magnitude.TotalDays / 365), "year")
        };

        return isFuture ? $"in {phrase}" : $"{phrase} ago";
    }

    /// <summary>Renders a count with its unit, keeping the singular form for one.</summary>
    private static string Pluralise(int count, string unit)
        => count == 1 ? $"1 {unit}" : $"{count} {unit}s";

    /// <summary>
    /// Converts an instant into a named time zone. Returns the UTC value unchanged when the zone is
    /// unknown, which can happen on runtimes shipped without full time-zone data.
    /// </summary>
    public static DateTimeOffset ToZone(DateTimeOffset value, string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return value.ToUniversalTime();
        }

        try
        {
            return TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return value.ToUniversalTime();
        }
    }

    /// <summary>UTC offset of a zone at a given instant, e.g. <c>UTC+07:00</c>.</summary>
    public static string DescribeOffset(DateTimeOffset value, string? timeZoneId)
    {
        var converted = ToZone(value, timeZoneId);
        var offset = converted.Offset;

        return $"UTC{(offset < TimeSpan.Zero ? "-" : "+")}{offset.Duration():hh\\:mm}";
    }

    /// <summary>
    /// Time zones the selector offers. The list is whatever the runtime knows about, so it degrades
    /// to UTC only rather than failing when time-zone data is unavailable.
    /// </summary>
    public static IReadOnlyList<string> GetTimeZoneIds()
    {
        try
        {
            return TimeZoneInfo.GetSystemTimeZones()
                .Select(zone => zone.Id)
                .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException or PlatformNotSupportedException)
        {
            return [TimeZoneInfo.Utc.Id];
        }
    }
}
