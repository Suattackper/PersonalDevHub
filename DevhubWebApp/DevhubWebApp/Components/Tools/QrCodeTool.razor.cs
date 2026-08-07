using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Qr;
using DevhubWebApp.Utilities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// QR workspace. Payload construction is <see cref="QrPayloadBuilder"/> and rendering is
/// <see cref="IQrCodeService"/>; this component owns the form state and the preview.
/// </summary>
public partial class QrCodeTool : ToolComponentBase<QrWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private static readonly IReadOnlyList<QrPayloadKind> Kinds = Enum.GetValues<QrPayloadKind>();
    private static readonly IReadOnlyList<QrErrorCorrection> CorrectionLevels = Enum.GetValues<QrErrorCorrection>();
    private static readonly IReadOnlyList<WiFiSecurity> SecurityModes = Enum.GetValues<WiFiSecurity>();

    private string _content = string.Empty;
    private string _previewDataUri = string.Empty;
    private string? _error;
    private int _capacity;
    private bool _capacityExceeded;

    [Inject] private IQrCodeService QrCodes { get; set; } = default!;

    protected override string ToolId => ToolIds.QrCode;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool HasCode => _previewDataUri.Length > 0;

    private string PreviewSubtitle => HasCode
        ? $"{_content.Length:N0} of {_capacity:N0} characters used"
        : string.Empty;

    private string CorrectionDescription => Workspace.Correction switch
    {
        QrErrorCorrection.Low => "Recovers ~7% — most data, least resilience.",
        QrErrorCorrection.Medium => "Recovers ~15% — the usual choice.",
        QrErrorCorrection.Quartile => "Recovers ~25%.",
        _ => "Recovers ~30% — needed if a logo covers the code."
    };

    protected override Task OnWorkspaceLoadedAsync()
    {
        Render();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rebuilds the payload and the preview. Rendering is cheap enough to run on every change, and
    /// the capacity check runs first so an over-long payload reports rather than throws.
    /// </summary>
    private void Render()
    {
        _error = null;
        _content = QrPayloadBuilder.Build(Workspace.ToPayload());
        _capacity = QrCodes.GetCapacity(Workspace.Correction);
        _capacityExceeded = _content.Length > _capacity;

        if (string.IsNullOrEmpty(_content) || _capacityExceeded)
        {
            _previewDataUri = string.Empty;
            return;
        }

        try
        {
            var png = QrCodes.RenderPng(_content, Workspace.Correction, Workspace.PixelsPerModule,
                Workspace.MarginModules, Workspace.Foreground, Workspace.Background);

            _previewDataUri = Base64Converter.ToDataUri(png, MimeTypes.Png);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            _previewDataUri = string.Empty;
            _error = exception.Message;
        }
    }

    private void SetKindAsync(QrPayloadKind kind) => Update(workspace => workspace.Kind = kind);

    /// <summary>Applies a change, re-renders and lets the debounced auto-save persist it.</summary>
    private void Update(Action<QrWorkspace> change)
    {
        change(Workspace);

        Render();
        ScheduleAutoSave();
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        Update(workspace => workspace.Text = text.Trim());
    }

    private async Task DownloadPngAsync()
    {
        if (!HasCode)
        {
            return;
        }

        var png = QrCodes.RenderPng(_content, Workspace.Correction, Workspace.PixelsPerModule,
            Workspace.MarginModules, Workspace.Foreground, Workspace.Background);

        await Interop.DownloadBytesAsync("qr-code.png", png, MimeTypes.Png);
        Notifications.Success("Saved qr-code.png.");
        await RecordAsync("Generated QR code", QrPayloadBuilder.GetName(Workspace.Kind));
    }

    private async Task DownloadSvgAsync()
    {
        if (!HasCode)
        {
            return;
        }

        var svg = QrCodes.RenderSvg(_content, Workspace.Correction, Workspace.PixelsPerModule,
            Workspace.MarginModules, Workspace.Foreground, Workspace.Background);

        await DownloadAsync(svg, "qr-code.svg", MimeTypes.Svg);
        await RecordAsync("Exported QR SVG", QrPayloadBuilder.GetName(Workspace.Kind));
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadPngAsync();
            StateHasChanged();
        }
    }

    private static string GetKindIcon(QrPayloadKind kind) => kind switch
    {
        QrPayloadKind.Text => Icons.Material.Filled.TextFields,
        QrPayloadKind.Url => Icons.Material.Filled.Link,
        QrPayloadKind.Email => Icons.Material.Filled.Email,
        QrPayloadKind.Phone => Icons.Material.Filled.Phone,
        QrPayloadKind.Sms => Icons.Material.Filled.Sms,
        QrPayloadKind.WiFi => Icons.Material.Filled.Wifi,
        QrPayloadKind.VCard => Icons.Material.Filled.ContactPage,
        QrPayloadKind.Location => Icons.Material.Filled.Place,
        _ => Icons.Material.Filled.QrCode2
    };
}
