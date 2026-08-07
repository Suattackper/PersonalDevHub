using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Barcode;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Barcode workspace. Encoding and SVG rendering are <see cref="BarcodeEncoder"/>; this component
/// owns the form state and the preview markup.
/// </summary>
public partial class BarcodeTool : ToolComponentBase<BarcodeWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private BarcodeSymbol _symbol = BarcodeSymbol.Empty;
    private string _svg = string.Empty;

    protected override string ToolId => ToolIds.Barcode;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool HasSymbol => _symbol.IsSuccess && _svg.Length > 0;

    protected override Task OnWorkspaceLoadedAsync()
    {
        Render();
        return Task.CompletedTask;
    }

    /// <summary>Re-encodes and re-renders. Both are cheap enough to run on every change.</summary>
    private void Render()
    {
        _symbol = BarcodeEncoder.Encode(Workspace.Symbology, Workspace.Data);

        _svg = _symbol.IsSuccess
            ? BarcodeEncoder.ToSvg(_symbol, Workspace.ModuleWidth, Workspace.Height, Workspace.Margin,
                Workspace.Foreground, Workspace.Background, Workspace.ShowText, Workspace.FontSize)
            : string.Empty;
    }

    private void Update(Action<BarcodeWorkspace> change)
    {
        change(Workspace);

        Render();
        ScheduleAutoSave();
    }

    private string GetPlaceholder() => Workspace.Symbology switch
    {
        Symbology.Ean13 => "590123412345",
        Symbology.Ean8 => "9638507",
        Symbology.UpcA => "03600029145",
        Symbology.Itf => "12345678",
        _ => "DEVHUB-001"
    };

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        Update(workspace => workspace.Data = text.Trim());
    }

    private async Task DownloadSvgAsync()
    {
        if (!HasSymbol)
        {
            return;
        }

        await DownloadAsync(_svg, "barcode.svg", MimeTypes.Svg);
        await RecordAsync("Generated barcode", $"{BarcodeEncoder.GetName(Workspace.Symbology)} · {_symbol.Text}");
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadSvgAsync();
            StateHasChanged();
        }
    }
}
