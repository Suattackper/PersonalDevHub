using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Shared;

/// <summary>
/// Behaviour for <c>DownloadButton.razor</c>. Supply either <see cref="Content"/> for text or
/// <see cref="Bytes"/> for binary output; binary wins when both are set.
/// </summary>
public partial class DownloadButton
{
    /// <summary>UTF-8 text payload.</summary>
    [Parameter] public string? Content { get; set; }

    /// <summary>Binary payload, e.g. a generated PNG. Takes precedence over <see cref="Content"/>.</summary>
    [Parameter] public byte[]? Bytes { get; set; }

    /// <summary>Suggested file name including its extension.</summary>
    [Parameter, EditorRequired] public string FileName { get; set; } = "devhub.txt";

    /// <summary>MIME type written to the generated blob.</summary>
    [Parameter] public string MimeType { get; set; } = MimeTypes.PlainText;

    /// <summary>Button label.</summary>
    [Parameter] public string Label { get; set; } = "Download";

    /// <summary>Renders a compact icon-only button.</summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>Button size.</summary>
    [Parameter] public Size Size { get; set; } = Size.Small;

    /// <summary>Variant used by the labelled form.</summary>
    [Parameter] public Variant Variant { get; set; } = Variant.Text;

    /// <summary>Theme color of the labelled form.</summary>
    [Parameter] public Color Color { get; set; } = Color.Default;

    /// <summary>Disables the button regardless of content.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Raised after the download has been handed to the browser.</summary>
    [Parameter] public EventCallback OnDownloaded { get; set; }

    /// <summary>Extra CSS classes appended to the button.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IBrowserInteropService Interop { get; set; } = default!;

    [Inject] private INotificationService Notifications { get; set; } = default!;

    private bool IsDisabled => Disabled || (Bytes is null or { Length: 0 } && string.IsNullOrEmpty(Content));

    private async Task DownloadAsync()
    {
        if (IsDisabled)
        {
            return;
        }

        if (Bytes is { Length: > 0 })
        {
            await Interop.DownloadBytesAsync(FileName, Bytes, MimeType);
        }
        else
        {
            await Interop.DownloadTextAsync(FileName, Content!, MimeType);
        }

        Notifications.Success($"Saved {FileName}.");
        await OnDownloaded.InvokeAsync();
    }
}
