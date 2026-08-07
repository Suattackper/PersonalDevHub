using DevhubWebApp.Components.Shared;
using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Diff;
using DevhubWebApp.Utilities;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Text comparison workspace. Statistics come from <see cref="DiffEngine"/> and exports from
/// <see cref="DiffExporter"/>; this component owns the two documents and the option toggles.
/// </summary>
public partial class TextDiffTool : ToolComponentBase<DiffWorkspace>
{
    private const string ShortcutExport = "ctrl+s";
    private const string ShortcutSearch = "ctrl+f";

    private DiffEditor? _diffEditor;
    private DiffStatistics _statistics = new();
    private bool _isApproximate;

    [Inject] private IHtmlExportService HtmlExport { get; set; } = default!;

    protected override string ToolId => ToolIds.TextDiff;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutExport, ShortcutSearch];

    private bool HasContent => !string.IsNullOrEmpty(Workspace.Left) || !string.IsNullOrEmpty(Workspace.Right);

    private string IdenticalDescription => Workspace.ToOptions() switch
    {
        { IgnoreWhitespace: false, IgnoreCase: false, IgnoreBlankLines: false } => "Every line matches.",
        _ => "Every line matches under the comparison rules you selected."
    };

    protected override Task OnWorkspaceLoadedAsync()
    {
        Recompute();
        return Task.CompletedTask;
    }

    private Task OnLeftChangedAsync(string value) => OnSideChangedAsync(value, isLeft: true);

    private Task OnRightChangedAsync(string value) => OnSideChangedAsync(value, isLeft: false);

    /// <summary>Editing either side schedules a recomputation rather than running one per keystroke.</summary>
    private async Task OnSideChangedAsync(string value, bool isLeft)
    {
        if (isLeft)
        {
            Workspace.Left = value;
        }
        else
        {
            Workspace.Right = value;
        }

        ScheduleAutoSave();
        await DebounceAsync(Recompute);
    }

    /// <summary>
    /// Recomputes statistics only. The rendered diff is Monaco's own, so the engine is used for the
    /// numbers and the exports — never to re-render what the editor already shows.
    /// </summary>
    private void Recompute()
    {
        var result = DiffEngine.Compare(Workspace.Left, Workspace.Right, Workspace.ToOptions());

        _statistics = result.Statistics;
        _isApproximate = CountLines(Workspace.Left) > DiffEngine.MaxLinesForExactDiff
            || CountLines(Workspace.Right) > DiffEngine.MaxLinesForExactDiff;
    }

    private static int CountLines(string? text)
        => string.IsNullOrEmpty(text) ? 0 : text.AsSpan().Count('\n') + 1;

    private DiffResult BuildResult()
        => DiffEngine.Compare(Workspace.Left, Workspace.Right, Workspace.ToOptions());

    private async Task UpdateAsync(Action<DiffWorkspace> change)
    {
        change(Workspace);
        Recompute();
        await SaveWorkspaceAsync();
    }

    private async Task SwapAsync()
    {
        (Workspace.Left, Workspace.Right) = (Workspace.Right, Workspace.Left);
        Recompute();
        await SaveWorkspaceAsync();
    }

    private async Task ClearAsync()
    {
        Workspace.Left = string.Empty;
        Workspace.Right = string.Empty;
        Recompute();
        await SaveWorkspaceAsync();
    }

    private async Task FocusSearchAsync()
    {
        if (_diffEditor is not null)
        {
            await _diffEditor.FocusSearchAsync();
        }
    }

    private async Task CopyDiffAsync()
    {
        if (!HasContent)
        {
            return;
        }

        await CopyAsync(DiffExporter.ToText(BuildResult()), "Diff copied.");
    }

    private async Task ExportTextAsync()
    {
        if (!HasContent)
        {
            return;
        }

        var fileName = HtmlExport.ToFileName("diff", "txt", "diff");

        await DownloadAsync(DiffExporter.ToText(BuildResult()), fileName, MimeTypes.PlainText);
        await RecordAsync("Exported diff", $"+{_statistics.Added} -{_statistics.Removed}");
    }

    private async Task ExportHtmlAsync()
    {
        if (!HasContent)
        {
            return;
        }

        var result = BuildResult();
        var fileName = HtmlExport.ToFileName("diff", "html", "diff");
        var document = HtmlExport.BuildDocument(
            $"Diff · +{result.Statistics.Added} −{result.Statistics.Removed}",
            DiffExporter.ToHtmlFragment(result),
            DiffExporter.HtmlCss);

        await DownloadAsync(document, fileName, MimeTypes.Html);
        await RecordAsync("Exported diff HTML", fileName);
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        switch (combo)
        {
            case ShortcutExport:
                await ExportTextAsync();
                break;

            case ShortcutSearch:
                await FocusSearchAsync();
                break;

            default:
                return;
        }

        StateHasChanged();
    }
}
