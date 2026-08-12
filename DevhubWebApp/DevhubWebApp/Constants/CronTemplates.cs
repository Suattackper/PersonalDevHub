using DevhubWebApp.Models.Tools.Cron;

namespace DevhubWebApp.Constants;

/// <summary>Ready-made schedules offered by the builder, held as data.</summary>
public static class CronTemplates
{
    /// <summary>Five-field Linux cron templates.</summary>
    public static IReadOnlyList<CronTemplate> Linux { get; } =
    [
        new() { Name = "Every minute", Expression = "* * * * *", Description = "Runs continuously; useful for smoke tests." },
        new() { Name = "Every 5 minutes", Expression = "*/5 * * * *", Description = "A common polling interval." },
        new() { Name = "Every hour", Expression = "0 * * * *", Description = "On the hour, every hour." },
        new() { Name = "Every day at 09:00", Expression = "0 9 * * *", Description = "A daily morning job." },
        new() { Name = "Weekdays at 09:00", Expression = "0 9 * * 1-5", Description = "Monday to Friday only." },
        new() { Name = "Every Monday at 08:30", Expression = "30 8 * * 1", Description = "A weekly kick-off job." },
        new() { Name = "First of the month", Expression = "0 0 1 * *", Description = "Midnight on the first day of each month." },
        new() { Name = "Quarterly", Expression = "0 0 1 1,4,7,10 *", Description = "The first day of each quarter." },
        new() { Name = "Every 15 minutes, office hours", Expression = "*/15 9-17 * * 1-5", Description = "Four times an hour during the working day." },
        new() { Name = "Nightly at 02:30", Expression = "30 2 * * *", Description = "A quiet-hours maintenance window." }
    ];

    /// <summary>Six-field Quartz templates, which start with seconds.</summary>
    public static IReadOnlyList<CronTemplate> Quartz { get; } =
    [
        new() { Name = "Every 30 seconds", Expression = "*/30 * * * * ?", Description = "Twice a minute." },
        new() { Name = "Every minute", Expression = "0 * * * * ?", Description = "On the minute." },
        new() { Name = "Every day at 09:00", Expression = "0 0 9 * * ?", Description = "A daily morning job." },
        new() { Name = "Weekdays at 09:00", Expression = "0 0 9 ? * 2-6", Description = "Monday to Friday; Quartz numbers Sunday as 1." },
        new() { Name = "First of the month", Expression = "0 0 0 1 * ?", Description = "Midnight on the first day of each month." },
        new() { Name = "Every hour on the half hour", Expression = "0 30 * * * ?", Description = "Thirty minutes past every hour." }
    ];

    /// <summary>Templates for a dialect.</summary>
    public static IReadOnlyList<CronTemplate> For(CronDialect dialect)
        => dialect == CronDialect.Quartz ? Quartz : Linux;
}
