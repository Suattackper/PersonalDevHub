using System.Globalization;
using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Time;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Timestamp workspace. Every conversion is <see cref="TimestampConverter"/>; this component owns
/// the two linked inputs, the selected zone and the live clock.
/// </summary>
public partial class TimestampTool : ToolComponentBase<TimestampWorkspace>
{
    private const string ShortcutNow = "ctrl+enter";
    private const string ShortcutDownload = "ctrl+s";
    private const string UtcLabel = "UTC";

    private IReadOnlyList<string> _zones = [];
    private Timer? _clock;
    private DateTimeOffset? _instant;
    private string _nowUnix = string.Empty;
    private string _nowIso = string.Empty;
    private string? _error;

    /// <summary>Which field the user edited last, so the error lands on the right input.</summary>
    private bool _lastEditWasTimestamp = true;

    protected override string ToolId => ToolIds.Timestamp;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutNow, ShortcutDownload];

    /// <summary>The picker binds to the zone-local date, so what is picked is what is shown.</summary>
    private DateTime? PickedDate => _instant is null ? null : Local(_instant.Value).DateTime.Date;

    private TimeSpan? PickedTime => _instant is null ? null : Local(_instant.Value).TimeOfDay;

    private string ZoneValue => string.IsNullOrEmpty(Workspace.TimeZoneId) ? UtcLabel : Workspace.TimeZoneId;

    /// <summary>Every rendering of the current instant, in the order a developer scans them.</summary>
    private IReadOnlyList<(string Label, string Value)> Rows
    {
        get
        {
            if (_instant is null)
            {
                return [];
            }

            var utc = _instant.Value.ToUniversalTime();
            var local = Local(_instant.Value);

            return
            [
                ("Unix seconds", TimestampConverter.ToUnix(utc, TimestampUnit.Seconds).ToString(CultureInfo.InvariantCulture)),
                ("Unix milliseconds", TimestampConverter.ToUnix(utc, TimestampUnit.Milliseconds).ToString(CultureInfo.InvariantCulture)),
                ("ISO 8601 (UTC)", TimestampConverter.ToIso8601(utc)),
                ("RFC 1123", TimestampConverter.ToRfc1123(utc)),
                ("UTC", TimestampConverter.ToSortable(utc)),
                ($"Local ({ZoneValue})", TimestampConverter.ToSortable(local)),
                ("Long form", TimestampConverter.ToLongForm(local)),
                ("Relative", TimestampConverter.ToRelative(utc, TimestampConverter.Now))
            ];
        }
    }

    protected override Task OnWorkspaceLoadedAsync()
    {
        _zones = BuildZoneList();
        Recalculate();
        Tick();
        StartClock();

        return Task.CompletedTask;
    }

    /// <summary>UTC first, then whatever the runtime knows: it is the value most users want.</summary>
    private static List<string> BuildZoneList()
    {
        var zones = new List<string> { UtcLabel };
        zones.AddRange(TimestampConverter.GetTimeZoneIds().Where(id => !string.Equals(id, UtcLabel, StringComparison.OrdinalIgnoreCase)));

        return zones;
    }

    private DateTimeOffset Local(DateTimeOffset value) => TimestampConverter.ToZone(value, Workspace.TimeZoneId);

    private async Task OnTimestampChangedAsync(string? value)
    {
        Workspace.Timestamp = value ?? string.Empty;
        _lastEditWasTimestamp = true;

        ScheduleAutoSave();
        await DebounceAsync(Recalculate);
    }

    private async Task OnDateChangedAsync(string? value)
    {
        Workspace.DateText = value ?? string.Empty;
        Workspace.Timestamp = string.Empty;
        _lastEditWasTimestamp = false;

        ScheduleAutoSave();
        await DebounceAsync(Recalculate);
    }

    /// <summary>
    /// Resolves the instant from whichever field the user last touched, then mirrors it into the
    /// other one so the two inputs always describe the same moment.
    /// </summary>
    private void Recalculate()
    {
        var result = _lastEditWasTimestamp
            ? TimestampConverter.ParseTimestamp(Workspace.Timestamp, ExplicitUnit())
            : TimestampConverter.ParseDate(Workspace.DateText);

        _error = result.Error;
        _instant = result.IsSuccess ? result.Value : null;

        if (_instant is null)
        {
            return;
        }

        if (_lastEditWasTimestamp)
        {
            Workspace.DateText = TimestampConverter.ToIso8601(_instant.Value);
        }
        else
        {
            Workspace.Timestamp = TimestampConverter.ToUnix(_instant.Value, Workspace.Unit)
                .ToString(CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// A value the user typed is only forced into the selected unit when it is short enough to be
    /// ambiguous; a 13-digit paste is milliseconds whatever the toggle says.
    /// </summary>
    private TimestampUnit? ExplicitUnit()
        => Workspace.Timestamp.Trim().Length <= 11 ? Workspace.Unit : null;

    private async Task OnDatePickedAsync(DateTime? date)
    {
        if (date is null)
        {
            return;
        }

        var time = PickedTime ?? TimeSpan.Zero;
        await SetFromLocalAsync(date.Value.Date + time);
    }

    private async Task OnTimePickedAsync(TimeSpan? time)
    {
        if (time is null)
        {
            return;
        }

        var date = PickedDate ?? TimestampConverter.Now.Date;
        await SetFromLocalAsync(date + time.Value);
    }

    /// <summary>Interprets a picked wall-clock time in the selected zone, not in the host's zone.</summary>
    private async Task SetFromLocalAsync(DateTime local)
    {
        var offset = Local(TimestampConverter.Now).Offset;
        var instant = new DateTimeOffset(local, offset);

        Workspace.DateText = TimestampConverter.ToIso8601(instant);
        _lastEditWasTimestamp = false;

        Recalculate();
        await SaveWorkspaceAsync();
    }

    private Task OnZoneChangedAsync(string? zone)
        => UpdateAsync(workspace => workspace.TimeZoneId = string.Equals(zone, UtcLabel, StringComparison.Ordinal)
            ? string.Empty
            : zone ?? string.Empty);

    private async Task UpdateAsync(Action<TimestampWorkspace> change)
    {
        change(Workspace);

        Recalculate();
        Tick();
        await SaveWorkspaceAsync();
    }

    private async Task UseNowAsync()
    {
        Workspace.Timestamp = TimestampConverter.ToUnix(TimestampConverter.Now, Workspace.Unit)
            .ToString(CultureInfo.InvariantCulture);
        _lastEditWasTimestamp = true;

        Recalculate();
        await SaveWorkspaceAsync();
        await RecordAsync("Captured timestamp", Workspace.Timestamp);
    }

    private async Task ToggleClockAsync()
    {
        Workspace.LiveClock = !Workspace.LiveClock;

        StartClock();
        await SaveWorkspaceAsync();
    }

    /// <summary>Runs the clock only while the toggle is on, so a paused tool costs nothing.</summary>
    private void StartClock()
    {
        _clock?.Dispose();
        _clock = null;

        if (!Workspace.LiveClock)
        {
            return;
        }

        _clock = new Timer(_ => _ = InvokeAsync(() =>
        {
            Tick();
            StateHasChanged();
        }), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    }

    private void Tick()
    {
        var now = TimestampConverter.Now;

        _nowUnix = TimestampConverter.ToUnix(now, Workspace.Unit).ToString(CultureInfo.InvariantCulture);
        _nowIso = $"{TimestampConverter.ToSortable(Local(now))} · {TimestampConverter.DescribeOffset(now, Workspace.TimeZoneId)}";
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        var trimmed = text.Trim();
        _lastEditWasTimestamp = trimmed.All(character => char.IsAsciiDigit(character) || character is '-' or '_');

        if (_lastEditWasTimestamp)
        {
            Workspace.Timestamp = trimmed;
        }
        else
        {
            Workspace.DateText = trimmed;
        }

        Recalculate();
        await SaveWorkspaceAsync();
    }

    private async Task ClearAsync()
    {
        Workspace.Timestamp = string.Empty;
        Workspace.DateText = string.Empty;
        _instant = null;
        _error = null;

        await SaveWorkspaceAsync();
    }

    private async Task DownloadAsync()
    {
        if (_instant is null)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var (label, value) in Rows)
        {
            builder.Append(label).Append(": ").AppendLine(value);
        }

        await DownloadAsync(builder.ToString(), "timestamp.txt", MimeTypes.PlainText);
        await RecordAsync("Converted timestamp", TimestampConverter.ToIso8601(_instant.Value));
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        switch (combo)
        {
            case ShortcutNow:
                await UseNowAsync();
                break;

            case ShortcutDownload:
                await DownloadAsync();
                break;

            default:
                return;
        }

        StateHasChanged();
    }

    public override async ValueTask DisposeAsync()
    {
        _clock?.Dispose();
        await base.DisposeAsync();
    }
}
