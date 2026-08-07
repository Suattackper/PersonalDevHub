using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Numbers;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Number base workspace. Parsing, formatting and validation are
/// <see cref="NumberBaseConverter"/>; this component owns the input and the selected source base.
/// </summary>
public partial class NumberBaseTool : ToolComponentBase<NumberWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private NumberParseResult _parse = NumberParseResult.Empty;
    private string _codePoints = string.Empty;

    protected override string ToolId => ToolIds.NumberBase;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private string Placeholder => Workspace.SourceBase switch
    {
        NumberBase.Binary => "1010_1111 or 0b1010",
        NumberBase.Octal => "0o755 or 755",
        NumberBase.Hexadecimal => "0xDEADBEEF or ff",
        _ => "255 or -42"
    };

    private string CharacterLabel
    {
        get
        {
            var character = NumberBaseConverter.ToCharacter(_parse.Value);
            return character.Length == 0 ? "— not a Unicode scalar" : character;
        }
    }

    private string CodePointLabel => NumberBaseConverter.ToCodePoint(_parse.Value) is { Length: > 0 } codePoint
        ? codePoint
        : "—";

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

    private async Task OnTextChangedAsync(string? value)
    {
        Workspace.Text = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(Convert);
    }

    private void Convert()
    {
        _parse = NumberBaseConverter.Parse(Workspace.Input, Workspace.SourceBase);
        _codePoints = NumberBaseConverter.TextToCodePoints(Workspace.Text);
    }

    private string Render(NumberBase numberBase)
        => NumberBaseConverter.Format(_parse.Value, numberBase, Workspace.ShowPrefix);

    private Task SetBaseAsync(NumberBase numberBase) => UpdateAsync(workspace => workspace.SourceBase = numberBase);

    private async Task UpdateAsync(Action<NumberWorkspace> change)
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

        await UpdateAsync(workspace => workspace.Input = text.Trim());
    }

    private Task ClearAsync() => UpdateAsync(workspace =>
    {
        workspace.Input = string.Empty;
        workspace.Text = string.Empty;
    });

    private async Task DownloadAsync()
    {
        if (!_parse.IsSuccess)
        {
            return;
        }

        var builder = new StringBuilder();

        foreach (var numberBase in NumberBaseConverter.All)
        {
            builder.Append(NumberBaseConverter.GetName(numberBase)).Append(": ").AppendLine(Render(numberBase));
        }

        var codePoint = NumberBaseConverter.ToCodePoint(_parse.Value);
        if (codePoint.Length > 0)
        {
            builder.Append("Character: ").AppendLine(NumberBaseConverter.ToCharacter(_parse.Value));
            builder.Append("Code point: ").AppendLine(codePoint);
        }

        await DownloadAsync(builder.ToString(), "number-bases.txt", MimeTypes.PlainText);
        await RecordAsync("Converted number base", Workspace.Input);
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
