using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Text;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Case conversion workspace. All transformation is <see cref="CaseConverter"/>; this component
/// owns the input, the cleanup toggles and the rendered result set.
/// </summary>
public partial class CaseConverterTool : ToolComponentBase<CaseWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private readonly Dictionary<TextCase, string> _results = [];

    protected override string ToolId => ToolIds.CaseConverter;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool IsEmpty => string.IsNullOrWhiteSpace(Workspace.Input);

    private string InputSubtitle
    {
        get
        {
            var active = new List<string>(3);

            if (Workspace.TrimLines)
            {
                active.Add("trimmed");
            }

            if (Workspace.RemoveEmptyLines)
            {
                active.Add("blank lines removed");
            }

            if (Workspace.CollapseSpaces)
            {
                active.Add("spaces collapsed");
            }

            return active.Count == 0 ? "Converted as typed" : $"Cleaned first: {string.Join(", ", active)}";
        }
    }

    protected override Task OnWorkspaceLoadedAsync()
    {
        Convert();
        return Task.CompletedTask;
    }

    private async Task OnInputChangedAsync(string? value)
    {
        Workspace.Input = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(Convert);
    }

    /// <summary>Cleans the text once, then renders every style from the cleaned form.</summary>
    private void Convert()
    {
        _results.Clear();

        if (IsEmpty)
        {
            return;
        }

        var cleaned = Clean(Workspace.Input);

        foreach (var style in CaseConverter.All)
        {
            _results[style] = CaseConverter.Convert(cleaned, style);
        }
    }

    private string Clean(string text)
    {
        if (Workspace.TrimLines)
        {
            text = CaseConverter.TrimLines(text);
        }

        if (Workspace.CollapseSpaces)
        {
            text = CaseConverter.CollapseSpaces(text);
        }

        if (Workspace.RemoveEmptyLines)
        {
            text = CaseConverter.RemoveEmptyLines(text);
        }

        return text;
    }

    private async Task UpdateAsync(Action<CaseWorkspace> change)
    {
        change(Workspace);

        Convert();
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

        await UpdateAsync(workspace => workspace.Input = text);
    }

    private Task ClearAsync() => UpdateAsync(workspace => workspace.Input = string.Empty);

    /// <summary>Exports every conversion, labelled — the form that is useful to paste into a review.</summary>
    private async Task DownloadAsync()
    {
        if (IsEmpty)
        {
            return;
        }

        var builder = new StringBuilder();

        foreach (var style in CaseConverter.All)
        {
            builder.Append("# ").AppendLine(CaseConverter.GetName(style));
            builder.AppendLine(_results[style]).AppendLine();
        }

        await DownloadAsync(builder.ToString(), "cases.txt", MimeTypes.PlainText);
        await RecordAsync("Converted case", Workspace.Input);
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadAsync();
            StateHasChanged();
        }
    }
}
