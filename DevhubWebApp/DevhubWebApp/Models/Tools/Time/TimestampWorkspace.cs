using DevhubWebApp.Utilities;

namespace DevhubWebApp.Models.Tools.Time;

/// <summary>Auto-saved state of the timestamp converter.</summary>
public sealed class TimestampWorkspace
{
    /// <summary>Raw Unix timestamp as typed.</summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>Raw date as typed.</summary>
    public string DateText { get; set; } = string.Empty;

    /// <summary>Unit used when rendering and when a timestamp is ambiguous.</summary>
    public TimestampUnit Unit { get; set; } = TimestampUnit.Seconds;

    /// <summary>Time zone the results are shown in. Empty means UTC.</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>Keep the clock ticking and the "now" row live.</summary>
    public bool LiveClock { get; set; } = true;
}
