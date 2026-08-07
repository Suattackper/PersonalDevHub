using System.Globalization;

namespace DevhubWebApp.Extensions;

/// <summary>Display helpers for timestamps shown in history and dashboard widgets.</summary>
public static class DateTimeExtensions
{
    /// <summary>Renders a compact relative age, e.g. "just now", "12m ago", "3d ago".</summary>
    public static string ToRelativeLabel(this DateTimeOffset value)
    {
        var delta = DateTimeOffset.UtcNow - value;

        return delta switch
        {
            { TotalSeconds: < 60 } => "just now",
            { TotalMinutes: < 60 } => $"{(int)delta.TotalMinutes}m ago",
            { TotalHours: < 24 } => $"{(int)delta.TotalHours}h ago",
            { TotalDays: < 30 } => $"{(int)delta.TotalDays}d ago",
            _ => value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        };
    }
}
