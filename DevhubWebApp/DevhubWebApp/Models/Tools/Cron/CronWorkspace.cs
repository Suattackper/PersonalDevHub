namespace DevhubWebApp.Models.Tools.Cron;

/// <summary>Auto-saved state of the cron builder.</summary>
public sealed class CronWorkspace
{
    public CronDialect Dialect { get; set; } = CronDialect.Linux;

    /// <summary>The expression as written, whether typed or built.</summary>
    public string Expression { get; set; } = "0 9 * * 1-5";

    /// <summary>Expressions the user kept, newest first.</summary>
    public List<string> Favorites { get; set; } = [];

    /// <summary>Recently used expressions, newest first.</summary>
    public List<string> Recent { get; set; } = [];
}
