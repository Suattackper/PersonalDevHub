using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Url;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// URL encode/decode workspace. Escaping is <see cref="UrlToolkit"/>; this component owns the
/// direction, the scope and the current value.
/// </summary>
public partial class UrlEncodeTool : ToolComponentBase<UrlWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private string _output = string.Empty;

    protected override string ToolId => ToolIds.Url;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool IsEmpty => string.IsNullOrEmpty(Workspace.Input);

    private string ScopeDescription => Workspace switch
    {
        { IsEncoding: false } => "Percent-decoded, with '+' treated as a space",
        { ComponentMode: true } => "Every reserved character escaped — for a single query value",
        _ => "Only illegal characters escaped — separators preserved"
    };

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

    private void Convert() => _output = Workspace switch
    {
        { IsEncoding: false } => UrlToolkit.Decode(Workspace.Input),
        { ComponentMode: true } => UrlToolkit.EncodeComponent(Workspace.Input),
        _ => UrlToolkit.EncodeFull(Workspace.Input)
    };

    private Task SetDirectionAsync(bool isEncoding) => UpdateAsync(workspace => workspace.IsEncoding = isEncoding);

    private Task SetScopeAsync(bool componentMode) => UpdateAsync(workspace => workspace.ComponentMode = componentMode);

    private async Task UpdateAsync(Action<UrlWorkspace> change)
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

    /// <summary>Feeds the result back in and flips the direction, so a value can be round-tripped.</summary>
    private Task SwapAsync() => UpdateAsync(workspace =>
    {
        workspace.Input = _output;
        workspace.IsEncoding = !workspace.IsEncoding;
    });

    private async Task DownloadResultAsync()
    {
        if (IsEmpty)
        {
            return;
        }

        const string fileName = "url.txt";

        await DownloadAsync(_output, fileName, MimeTypes.PlainText);
        await RecordAsync(Workspace.IsEncoding ? "Encoded URL" : "Decoded URL", _output);
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadResultAsync();
            StateHasChanged();
        }
    }
}
