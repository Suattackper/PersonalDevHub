using DevhubWebApp.Interfaces;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Shared;

/// <summary>
/// Behaviour for <c>CopyButton.razor</c>. The component owns nothing but its transient
/// "copied" feedback; the actual clipboard access is delegated to the interop service.
/// </summary>
public partial class CopyButton : IDisposable
{
    private static readonly TimeSpan FeedbackDuration = TimeSpan.FromSeconds(2);

    private Timer? _resetTimer;
    private bool _copied;

    /// <summary>Text placed on the clipboard.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Button label in the idle state.</summary>
    [Parameter] public string Label { get; set; } = "Copy";

    /// <summary>Button label shown briefly after a successful copy.</summary>
    [Parameter] public string CopiedLabel { get; set; } = "Copied";

    /// <summary>Renders a compact icon-only button instead of a labelled one.</summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>Button size.</summary>
    [Parameter] public Size Size { get; set; } = Size.Small;

    /// <summary>Variant used by the labelled form.</summary>
    [Parameter] public Variant Variant { get; set; } = Variant.Text;

    /// <summary>Disables the button regardless of content.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Shows a snackbar in addition to the inline feedback.</summary>
    [Parameter] public bool Notify { get; set; } = true;

    /// <summary>Raised after the text has been copied successfully.</summary>
    [Parameter] public EventCallback OnCopied { get; set; }

    /// <summary>Extra CSS classes appended to the button.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IBrowserInteropService Interop { get; set; } = default!;

    [Inject] private INotificationService Notifications { get; set; } = default!;

    /// <summary>Empty content is never copyable, so the button disables itself.</summary>
    private bool IsDisabled => Disabled || string.IsNullOrEmpty(Text);

    private string CurrentIcon => _copied ? Icons.Material.Filled.Check : Icons.Material.Filled.ContentCopy;

    private string CurrentLabel => _copied ? CopiedLabel : Label;

    private Color CurrentColor => _copied ? Color.Success : Color.Default;

    private async Task CopyAsync()
    {
        if (IsDisabled)
        {
            return;
        }

        var succeeded = await Interop.CopyToClipboardAsync(Text!);

        if (!succeeded)
        {
            Notifications.Error("Clipboard is not available in this browser context.");
            return;
        }

        ShowFeedback();

        if (Notify)
        {
            Notifications.Success($"{Label} — done.");
        }

        await OnCopied.InvokeAsync();
    }

    /// <summary>Flips to the confirmed state and schedules a single reset back to idle.</summary>
    private void ShowFeedback()
    {
        _copied = true;
        _resetTimer?.Dispose();
        _resetTimer = new Timer(_ => InvokeAsync(ResetFeedback), null, FeedbackDuration, Timeout.InfiniteTimeSpan);
    }

    private void ResetFeedback()
    {
        _copied = false;
        StateHasChanged();
    }

    public void Dispose() => _resetTimer?.Dispose();
}
