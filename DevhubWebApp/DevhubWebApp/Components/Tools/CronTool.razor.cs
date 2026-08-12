using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Cron;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Cron workspace. Parsing, description and scheduling are <see cref="CronParser"/>; this component
/// owns the expression, the dialect and the saved lists.
/// </summary>
public partial class CronTool : ToolComponentBase<CronWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";
    private const int PreviewCount = 10;
    private const int RecentCapacity = 10;

    private CronExpression _parsed = CronExpression.Empty;
    private IReadOnlyList<DateTimeOffset> _occurrences = [];
    private string _description = string.Empty;

    protected override string ToolId => ToolIds.Cron;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool IsFavorite => Workspace.Favorites.Contains(Workspace.Expression, StringComparer.Ordinal);

    /// <summary>Field captions under the input, so the positions are never a guessing game.</summary>
    private IReadOnlyList<string> FieldLabels => Workspace.Dialect == CronDialect.Quartz
        ? ["second", "minute", "hour", "day of month", "month", "day of week", "year (optional)"]
        : ["minute", "hour", "day of month", "month", "day of week"];

    /// <summary>One editable row per field of the current expression.</summary>
    private IReadOnlyList<CronPart> Parts
    {
        get
        {
            var fields = SplitFields();

            return FieldLabels
                .Take(fields.Length)
                .Select((label, index) => new CronPart(label[..1].ToUpperInvariant() + label[1..], index, fields[index], DescribeField(label)))
                .ToList();
        }
    }

    protected override Task OnWorkspaceLoadedAsync()
    {
        Recalculate();
        return Task.CompletedTask;
    }

    private async Task OnExpressionChangedAsync(string? value)
    {
        Workspace.Expression = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(Recalculate);
    }

    /// <summary>Re-parses, re-describes and re-schedules in one place.</summary>
    private void Recalculate()
    {
        _parsed = CronParser.Parse(Workspace.Expression, Workspace.Dialect);
        _description = CronParser.Describe(_parsed);
        _occurrences = _parsed.IsValid
            ? CronParser.GetNextOccurrences(_parsed, TimestampConverter.Now, PreviewCount)
            : [];
    }

    private string[] SplitFields()
        => Workspace.Expression.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Rewrites one field and leaves the rest of the expression untouched.</summary>
    private async Task SetFieldAsync(int index, string? value)
    {
        var fields = SplitFields();

        if (index < 0 || index >= fields.Length)
        {
            return;
        }

        fields[index] = string.IsNullOrWhiteSpace(value) ? "*" : value.Trim();
        Workspace.Expression = string.Join(' ', fields);

        Recalculate();
        await SaveWorkspaceAsync();
    }

    private async Task SetDialectAsync(CronDialect dialect)
    {
        if (Workspace.Dialect == dialect)
        {
            return;
        }

        Workspace.Dialect = dialect;

        // Switching dialect changes the field count, so seed a valid expression for the new one.
        Workspace.Expression = CronTemplates.For(dialect)[0].Expression;

        Recalculate();
        await SaveWorkspaceAsync();
    }

    private Task ApplyTemplateAsync(CronTemplate template) => UseExpressionAsync(template.Expression);

    private async Task UseExpressionAsync(string expression)
    {
        Workspace.Expression = expression;

        Recalculate();
        RememberRecent(expression);
        await SaveWorkspaceAsync();
        await RecordAsync("Built cron expression", expression);
    }

    /// <summary>Keeps the newest expressions, without duplicates.</summary>
    private void RememberRecent(string expression)
    {
        Workspace.Recent.RemoveAll(entry => string.Equals(entry, expression, StringComparison.Ordinal));
        Workspace.Recent.Insert(0, expression);

        if (Workspace.Recent.Count > RecentCapacity)
        {
            Workspace.Recent.RemoveRange(RecentCapacity, Workspace.Recent.Count - RecentCapacity);
        }
    }

    private async Task ToggleFavoriteAsync()
    {
        if (!_parsed.IsValid)
        {
            return;
        }

        if (Workspace.Favorites.RemoveAll(entry => string.Equals(entry, Workspace.Expression, StringComparison.Ordinal)) == 0)
        {
            Workspace.Favorites.Insert(0, Workspace.Expression);
        }

        await SaveWorkspaceAsync();
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        await UseExpressionAsync(text.Trim());
    }

    private async Task DownloadAsync()
    {
        if (!_parsed.IsValid)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append("# ").AppendLine(_description);
        builder.AppendLine(Workspace.Expression).AppendLine();
        builder.AppendLine("# Next runs (UTC)");

        foreach (var occurrence in _occurrences)
        {
            builder.Append("# ").AppendLine(TimestampConverter.ToSortable(occurrence));
        }

        await DownloadAsync(builder.ToString(), "schedule.cron", MimeTypes.PlainText);
        await RecordAsync("Exported cron schedule", Workspace.Expression);
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadAsync();
            StateHasChanged();
        }
    }

    private static string DescribeField(string label) => label switch
    {
        "second" => "0-59, or */30 for every half minute",
        "minute" => "0-59, lists and ranges allowed",
        "hour" => "0-23 in 24-hour time",
        "day of month" => "1-31, or ? in Quartz for no specific value",
        "month" => "1-12 or JAN-DEC",
        "day of week" => "0-6 or SUN-SAT; Quartz numbers Sunday as 1",
        _ => "Optional four-digit year"
    };

    /// <summary>One row of the builder: caption, position in the expression and current value.</summary>
    private sealed record CronPart(string Label, int Index, string Value, string Hint);
}
