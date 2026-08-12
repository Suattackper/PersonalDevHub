using System.Globalization;
using System.Text;
using DevhubWebApp.Models.Tools.Cron;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Parses, validates, describes and schedules cron expressions.
/// <para>
/// Both dialects are supported: five-field Linux cron, and Quartz with a leading seconds field and
/// an optional trailing year. Parsing produces the explicit set of values each field allows, which
/// is what makes both the description and the next-run search simple and testable.
/// </para>
/// </summary>
public static class CronParser
{
    /// <summary>Upper bound on the forward search, so an unsatisfiable schedule terminates.</summary>
    private const int MaxSearchYears = 5;

    private static readonly string[] MonthNames =
        ["JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"];

    private static readonly string[] DayNames = ["SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT"];

    private static readonly string[] DayLongNames =
        ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    private static readonly string[] MonthLongNames =
    [
        "January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];

    /// <summary>
    /// Parses an expression. The dialect is inferred from the field count unless one is given:
    /// five fields is Linux, six or seven is Quartz.
    /// </summary>
    public static CronExpression Parse(string? expression, CronDialect? dialect = null)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return CronExpression.Empty;
        }

        var text = expression.Trim();
        var fields = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var resolved = dialect ?? (fields.Length >= 6 ? CronDialect.Quartz : CronDialect.Linux);

        var expected = resolved == CronDialect.Quartz ? "6 or 7" : "5";

        var valid = resolved == CronDialect.Quartz ? fields.Length is 6 or 7 : fields.Length == 5;
        if (!valid)
        {
            return CronExpression.Failure(
                $"{(resolved == CronDialect.Quartz ? "Quartz" : "Linux cron")} expects {expected} fields; this has {fields.Length}.",
                resolved);
        }

        var offset = resolved == CronDialect.Quartz ? 1 : 0;

        try
        {
            var seconds = resolved == CronDialect.Quartz ? ParseField(fields[0], 0, 59, "seconds") : [0];
            var minutes = ParseField(fields[offset], 0, 59, "minutes");
            var hours = ParseField(fields[offset + 1], 0, 23, "hours");
            var daysOfMonth = ParseField(NormaliseDayField(fields[offset + 2]), 1, 31, "day of month");
            var months = ParseField(ReplaceNames(fields[offset + 3], MonthNames, 1), 1, 12, "month");
            var daysOfWeek = ParseDayOfWeek(NormaliseDayField(fields[offset + 4]), resolved);

            return new CronExpression
            {
                Dialect = resolved,
                Expression = text,
                Seconds = seconds,
                Minutes = minutes,
                Hours = hours,
                DaysOfMonth = daysOfMonth,
                Months = months,
                DaysOfWeek = daysOfWeek,
                DayUnion = IsRestricted(fields[offset + 2]) && IsRestricted(fields[offset + 4])
            };
        }
        catch (FormatException exception)
        {
            return CronExpression.Failure(exception.Message, resolved);
        }
    }

    /// <summary>Whether an expression parses. Convenience over <see cref="Parse"/>.</summary>
    public static bool IsValid(string? expression, CronDialect? dialect = null)
        => Parse(expression, dialect).IsValid;

    /// <summary>
    /// The next <paramref name="count"/> times the schedule fires after <paramref name="after"/>.
    /// The search walks minute by minute (or second by second for Quartz) and gives up after
    /// <see cref="MaxSearchYears"/>, so an impossible schedule such as 30 February terminates.
    /// </summary>
    public static IReadOnlyList<DateTimeOffset> GetNextOccurrences(CronExpression expression, DateTimeOffset after, int count)
    {
        ArgumentNullException.ThrowIfNull(expression);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        if (!expression.IsValid)
        {
            return [];
        }

        var results = new List<DateTimeOffset>(count);
        var usesSeconds = expression.Dialect == CronDialect.Quartz && expression.Seconds.Count != 1;

        // Start at the next whole step so "now" itself is never reported as an upcoming run.
        var cursor = usesSeconds
            ? new DateTimeOffset(after.Year, after.Month, after.Day, after.Hour, after.Minute, after.Second, after.Offset).AddSeconds(1)
            : new DateTimeOffset(after.Year, after.Month, after.Day, after.Hour, after.Minute, 0, after.Offset).AddMinutes(1);

        var limit = after.AddYears(MaxSearchYears);
        var step = usesSeconds ? TimeSpan.FromSeconds(1) : TimeSpan.FromMinutes(1);

        while (cursor < limit && results.Count < count)
        {
            if (Matches(expression, cursor, usesSeconds))
            {
                results.Add(cursor);
            }

            cursor = cursor.Add(step);
        }

        return results;
    }

    /// <summary>Whether a specific instant satisfies the schedule.</summary>
    public static bool Matches(CronExpression expression, DateTimeOffset instant, bool checkSeconds = false)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (!expression.Months.Contains(instant.Month) || !expression.Hours.Contains(instant.Hour)
            || !expression.Minutes.Contains(instant.Minute))
        {
            return false;
        }

        if (checkSeconds && !expression.Seconds.Contains(instant.Second))
        {
            return false;
        }

        var dayOfMonth = expression.DaysOfMonth.Contains(instant.Day);
        var dayOfWeek = expression.DaysOfWeek.Contains((int)instant.DayOfWeek);

        // When both day fields are restricted, cron fires if either matches.
        return expression.DayUnion ? dayOfMonth || dayOfWeek : dayOfMonth && dayOfWeek;
    }

    /// <summary>Plain-English description of when the schedule fires.</summary>
    public static string Describe(CronExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        if (!expression.IsValid)
        {
            return expression.Error ?? "Enter a cron expression.";
        }

        var builder = new StringBuilder(DescribeTime(expression));

        var dayPart = DescribeDays(expression);
        if (dayPart.Length > 0)
        {
            builder.Append(' ').Append(dayPart);
        }

        var monthPart = DescribeMonths(expression);
        if (monthPart.Length > 0)
        {
            builder.Append(' ').Append(monthPart);
        }

        return builder.ToString();
    }

    private static string DescribeTime(CronExpression expression)
    {
        var everyMinute = expression.Minutes.Count == 60;
        var everyHour = expression.Hours.Count == 24;

        if (everyMinute && everyHour)
        {
            return expression.Seconds.Count > 1 ? "Every second" : "Every minute";
        }

        if (everyMinute)
        {
            return $"Every minute of {DescribeList(expression.Hours, hour => $"{hour:00}:00")}";
        }

        if (everyHour)
        {
            return $"Every hour at minute {DescribeList(expression.Minutes, minute => minute.ToString(CultureInfo.InvariantCulture))}";
        }

        var times = expression.Hours
            .SelectMany(hour => expression.Minutes.Select(minute => $"{hour:00}:{minute:00}"))
            .Take(6)
            .ToList();

        var suffix = expression.Hours.Count * expression.Minutes.Count > times.Count ? " and more" : string.Empty;

        return $"At {string.Join(", ", times)}{suffix}";
    }

    private static string DescribeDays(CronExpression expression)
    {
        var allDaysOfMonth = expression.DaysOfMonth.Count == 31;
        var allDaysOfWeek = expression.DaysOfWeek.Count == 7;

        if (allDaysOfMonth && allDaysOfWeek)
        {
            return "every day";
        }

        var parts = new List<string>(2);

        if (!allDaysOfMonth)
        {
            parts.Add($"on day {DescribeList(expression.DaysOfMonth, day => day.ToString(CultureInfo.InvariantCulture))} of the month");
        }

        if (!allDaysOfWeek)
        {
            parts.Add($"on {DescribeList(expression.DaysOfWeek, day => DayLongNames[day])}");
        }

        return string.Join(expression.DayUnion ? " or " : " and ", parts);
    }

    private static string DescribeMonths(CronExpression expression)
        => expression.Months.Count == 12
            ? string.Empty
            : $"in {DescribeList(expression.Months, month => MonthLongNames[month - 1])}";

    /// <summary>Renders a value list, collapsing long runs so the sentence stays readable.</summary>
    private static string DescribeList(IReadOnlyList<int> values, Func<int, string> format)
    {
        if (values.Count == 0)
        {
            return string.Empty;
        }

        if (values.Count == 1)
        {
            return format(values[0]);
        }

        if (values.Count > 5)
        {
            return $"{format(values[0])} through {format(values[^1])} ({values.Count} values)";
        }

        var rendered = values.Select(format).ToList();
        return $"{string.Join(", ", rendered[..^1])} and {rendered[^1]}";
    }

    /// <summary>A field is "restricted" when it is neither <c>*</c> nor Quartz's <c>?</c>.</summary>
    private static bool IsRestricted(string field)
        => !field.Equals("*", StringComparison.Ordinal) && !field.Equals("?", StringComparison.Ordinal);

    /// <summary>Quartz's <c>?</c> means "no specific value", which is <c>*</c> for matching.</summary>
    private static string NormaliseDayField(string field)
        => field.Equals("?", StringComparison.Ordinal) ? "*" : field;

    private static List<int> ParseDayOfWeek(string field, CronDialect dialect)
    {
        var replaced = ReplaceNames(field, DayNames, dialect == CronDialect.Quartz ? 1 : 0);
        var max = dialect == CronDialect.Quartz ? 7 : 7;

        var values = ParseField(replaced, 0, max, "day of week");

        // Both dialects allow 7 for Sunday; Quartz numbers Sunday as 1, so shift it back.
        var normalised = values
            .Select(value => dialect == CronDialect.Quartz ? (value == 7 ? 6 : value - 1) : value % 7)
            .Where(value => value >= 0)
            .Distinct()
            .OrderBy(value => value)
            .ToList();

        return normalised;
    }

    /// <summary>Replaces three-letter names with their numeric value before numeric parsing.</summary>
    private static string ReplaceNames(string field, string[] names, int firstValue)
    {
        var text = field.ToUpperInvariant();

        for (var index = 0; index < names.Length; index++)
        {
            text = text.Replace(names[index], (index + firstValue).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        }

        return text;
    }

    /// <summary>
    /// Expands one field into the explicit values it allows, supporting <c>*</c>, lists, ranges
    /// and steps.
    /// </summary>
    private static List<int> ParseField(string field, int min, int max, string name)
    {
        var values = new SortedSet<int>();

        foreach (var part in field.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var step = 1;
            var body = part;

            var slash = part.IndexOf('/', StringComparison.Ordinal);
            if (slash >= 0)
            {
                body = part[..slash];

                if (!int.TryParse(part[(slash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out step) || step <= 0)
                {
                    throw new FormatException($"'{part}' has an invalid step in the {name} field.");
                }
            }

            var (start, end) = ParseRange(body, min, max, name);

            for (var value = start; value <= end; value += step)
            {
                values.Add(value);
            }
        }

        if (values.Count == 0)
        {
            throw new FormatException($"The {name} field matches nothing.");
        }

        return values.ToList();
    }

    private static (int Start, int End) ParseRange(string body, int min, int max, string name)
    {
        if (body is "*" or "")
        {
            return (min, max);
        }

        var dash = body.IndexOf('-', StringComparison.Ordinal);

        if (dash > 0)
        {
            var start = ParseValue(body[..dash], min, max, name);
            var end = ParseValue(body[(dash + 1)..], min, max, name);

            if (end < start)
            {
                throw new FormatException($"'{body}' is a reversed range in the {name} field.");
            }

            return (start, end);
        }

        var single = ParseValue(body, min, max, name);
        return (single, single);
    }

    private static int ParseValue(string text, int min, int max, string name)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new FormatException($"'{text}' is not a number in the {name} field.");
        }

        if (value < min || value > max)
        {
            throw new FormatException($"{value} is outside {min}-{max} in the {name} field.");
        }

        return value;
    }
}
