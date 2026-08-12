namespace DevhubWebApp.Models.Tools.Cron;

/// <summary>Which cron dialect an expression is written in.</summary>
public enum CronDialect
{
    /// <summary>Five fields: minute, hour, day of month, month, day of week.</summary>
    Linux,

    /// <summary>Six or seven fields, starting with seconds and optionally ending with a year.</summary>
    Quartz
}

/// <summary>A parsed cron expression, or the reason it could not be parsed.</summary>
public sealed record CronExpression
{
    public static CronExpression Empty { get; } = new();

    public CronDialect Dialect { get; init; } = CronDialect.Linux;

    /// <summary>The expression as written.</summary>
    public string Expression { get; init; } = string.Empty;

    /// <summary>Second values the schedule fires on. Always <c>0</c> for the Linux dialect.</summary>
    public IReadOnlyList<int> Seconds { get; init; } = Array.Empty<int>();

    public IReadOnlyList<int> Minutes { get; init; } = Array.Empty<int>();

    public IReadOnlyList<int> Hours { get; init; } = Array.Empty<int>();

    public IReadOnlyList<int> DaysOfMonth { get; init; } = Array.Empty<int>();

    public IReadOnlyList<int> Months { get; init; } = Array.Empty<int>();

    /// <summary>Days of week as 0-6, Sunday first.</summary>
    public IReadOnlyList<int> DaysOfWeek { get; init; } = Array.Empty<int>();

    /// <summary>
    /// <c>true</c> when both day fields are restricted. Cron treats that as a union, not an
    /// intersection: the schedule fires when *either* matches.
    /// </summary>
    public bool DayUnion { get; init; }

    /// <summary>Why the expression is invalid; <c>null</c> when it parsed.</summary>
    public string? Error { get; init; }

    public bool IsValid => Error is null && Minutes.Count > 0;

    public static CronExpression Failure(string error, CronDialect dialect) =>
        new() { Dialect = dialect, Error = error };
}

/// <summary>A ready-made expression offered by the builder.</summary>
public sealed record CronTemplate
{
    public required string Name { get; init; }

    public required string Expression { get; init; }

    public required string Description { get; init; }
}
