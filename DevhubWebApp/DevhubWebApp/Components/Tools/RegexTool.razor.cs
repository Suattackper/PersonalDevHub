using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Regex;
using DevhubWebApp.Utilities;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Regex workspace. Matching and replacement are <see cref="RegexTester"/>; this component owns the
/// pattern, the sample text and the flag toggles.
/// </summary>
public partial class RegexTool : ToolComponentBase<RegexWorkspace>
{
    private const string ShortcutExport = "ctrl+s";

    /// <summary>
    /// Flag descriptors, so the toolbar is one loop rather than six near-identical chips.
    /// </summary>
    private static readonly IReadOnlyList<FlagDescriptor> Flags =
    [
        new("Ignore case", Icons.Material.Filled.TextFormat,
            workspace => workspace.IgnoreCase, (workspace, value) => workspace.IgnoreCase = value),
        new("Multiline", Icons.Material.Filled.FormatLineSpacing,
            workspace => workspace.Multiline, (workspace, value) => workspace.Multiline = value),
        new("Dot matches newline", Icons.Material.Filled.MoreHoriz,
            workspace => workspace.Singleline, (workspace, value) => workspace.Singleline = value),
        new("Global", Icons.Material.Filled.SelectAll,
            workspace => workspace.Global, (workspace, value) => workspace.Global = value),
        new("Explicit capture", Icons.Material.Filled.Label,
            workspace => workspace.ExplicitCapture, (workspace, value) => workspace.ExplicitCapture = value),
        new("Ignore whitespace", Icons.Material.Filled.SpaceBar,
            workspace => workspace.IgnoreWhitespace, (workspace, value) => workspace.IgnoreWhitespace = value)
    ];

    private RegexResult _result = RegexResult.Empty;
    private string _replacePreview = string.Empty;

    protected override string ToolId => ToolIds.Regex;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutExport];

    protected override Task OnWorkspaceLoadedAsync()
    {
        History.Reset(Workspace.Pattern);
        Run();

        return Task.CompletedTask;
    }

    private async Task OnPatternChangedAsync(string? value)
    {
        Workspace.Pattern = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(() =>
        {
            History.Push(Workspace.Pattern);
            Run();
        });
    }

    private async Task OnTestTextChangedAsync(string value)
    {
        Workspace.TestText = value;

        ScheduleAutoSave();
        await DebounceAsync(Run);
    }

    private async Task OnReplacementChangedAsync(string? value)
    {
        Workspace.Replacement = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(Run);
    }

    /// <summary>Runs the pattern, and the replacement too when the replace panel is open.</summary>
    private void Run()
    {
        if (Workspace.ReplaceMode)
        {
            _result = RegexTester.Replace(
                Workspace.Pattern, Workspace.TestText, Workspace.Replacement, Workspace.ToFlags(), out _replacePreview);

            return;
        }

        _result = RegexTester.Match(Workspace.Pattern, Workspace.TestText, Workspace.ToFlags());
        _replacePreview = string.Empty;
    }

    private async Task ToggleFlagAsync(FlagDescriptor flag)
    {
        flag.Set(Workspace, !flag.IsOn(Workspace));

        Run();
        await SaveWorkspaceAsync();
    }

    private async Task ToggleReplaceAsync()
    {
        Workspace.ReplaceMode = !Workspace.ReplaceMode;

        Run();
        await SaveWorkspaceAsync();
    }

    private async Task ApplyTemplateAsync(RegexTemplate template)
    {
        Workspace.Pattern = template.Pattern;

        if (string.IsNullOrWhiteSpace(Workspace.TestText))
        {
            Workspace.TestText = template.Sample;
        }

        History.Push(Workspace.Pattern);
        Run();
        await SaveWorkspaceAsync();

        Notifications.Info($"Applied the {template.Name} pattern.");
    }

    private async Task UndoAsync()
    {
        if (History.Undo() is { } previous)
        {
            Workspace.Pattern = previous;
            Run();
            await SaveWorkspaceAsync();
        }
    }

    private async Task RedoAsync()
    {
        if (History.Redo() is { } next)
        {
            Workspace.Pattern = next;
            Run();
            await SaveWorkspaceAsync();
        }
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        Workspace.TestText = text;
        Run();
        await SaveWorkspaceAsync();
    }

    /// <summary>Exports matches and their groups as text, which is what gets pasted into a ticket.</summary>
    private async Task ExportAsync()
    {
        if (_result.MatchCount == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append("Pattern: ").AppendLine(Workspace.Pattern);
        builder.Append("Matches: ").AppendLine(_result.MatchCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        builder.AppendLine();

        var index = 1;
        foreach (var match in _result.Matches)
        {
            builder.Append('#').Append(index).Append(" [").Append(match.Index).Append("..")
                .Append(match.Index + match.Length).Append("] ").AppendLine(match.Value);

            foreach (var group in match.Groups.Where(group => group.Success))
            {
                builder.Append("    ").Append(group.Name).Append(": ").AppendLine(group.Value);
            }

            index++;
        }

        await DownloadAsync(builder.ToString(), "regex-matches.txt", MimeTypes.PlainText);
        await RecordAsync("Exported regex matches", Workspace.Pattern);
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutExport)
        {
            await ExportAsync();
            StateHasChanged();
        }
    }

    /// <summary>Binds a flag's label and icon to its accessor pair.</summary>
    private sealed record FlagDescriptor(
        string Label,
        string Icon,
        Func<RegexWorkspace, bool> IsOn,
        Action<RegexWorkspace, bool> Set);
}
