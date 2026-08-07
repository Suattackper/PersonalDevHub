using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools.Base64;
using DevhubWebApp.Utilities;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Base64 workspace. All conversion and content detection is <see cref="Base64Converter"/>; this
/// component owns the current input, the loaded file and the preview state.
/// </summary>
public partial class Base64Tool : ToolComponentBase<Base64Workspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private Base64Result _result = new();
    private BinaryDescriptor? _descriptor;
    private FileDropResult? _sourceFile;
    private string? _previewDataUri;

    [Inject] private IHtmlExportService HtmlExport { get; set; } = default!;

    protected override string ToolId => ToolIds.Base64;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool IsEmpty => string.IsNullOrWhiteSpace(Workspace.Input) && _sourceFile is null;

    private bool HasTextOutput => !string.IsNullOrEmpty(_result.Output);

    private bool HasAnyOutput => HasTextOutput || _result.Bytes.Length > 0;

    private string InputPlaceholder => Workspace.Mode == Base64Mode.Encode
        ? "Type or paste text, or drop a file below…"
        : "Paste Base64 or a data: URI…";

    protected override Task OnWorkspaceLoadedAsync()
    {
        Convert();
        return Task.CompletedTask;
    }

    private async Task OnInputChangedAsync(string? value)
    {
        Workspace.Input = value ?? string.Empty;
        _sourceFile = null;

        ScheduleAutoSave();
        await DebounceAsync(Convert);
    }

    /// <summary>
    /// Runs the conversion for the current mode. A loaded file always wins over the text box:
    /// its bytes are the real payload, and re-encoding its Base64 text would be wrong.
    /// </summary>
    private void Convert()
    {
        if (IsEmpty)
        {
            _result = new Base64Result();
            _descriptor = null;
            _previewDataUri = null;
            return;
        }

        _result = Workspace.Mode switch
        {
            Base64Mode.Encode when _sourceFile?.Bytes is { Length: > 0 } bytes
                => Base64Converter.EncodeBytes(bytes, _sourceFile.ContentType, Workspace.AsDataUri),

            Base64Mode.Encode when Workspace.AsDataUri
                => Base64Converter.EncodeBytes(System.Text.Encoding.UTF8.GetBytes(Workspace.Input), MimeTypes.PlainText, asDataUri: true),

            Base64Mode.Encode => Base64Converter.EncodeText(Workspace.Input),

            _ => Base64Converter.Decode(Workspace.Input)
        };

        _descriptor = Workspace.Mode == Base64Mode.Decode ? _result.Descriptor : null;
        _previewDataUri = BuildPreview();
    }

    /// <summary>Images are previewed inline; everything else is offered as a download instead.</summary>
    private string? BuildPreview()
    {
        if (Workspace.Mode != Base64Mode.Decode || _result.Descriptor is not { IsImage: true } descriptor)
        {
            return null;
        }

        return Base64Converter.ToDataUri(_result.Bytes, descriptor.MimeType);
    }

    private async Task SetModeAsync(Base64Mode mode)
    {
        if (Workspace.Mode == mode)
        {
            return;
        }

        Workspace.Mode = mode;
        Convert();
        await SaveWorkspaceAsync();
    }

    private async Task ToggleDataUriAsync()
    {
        Workspace.AsDataUri = !Workspace.AsDataUri;
        Convert();
        await SaveWorkspaceAsync();
    }

    private async Task OnFileLoadedAsync(FileDropResult file)
    {
        _sourceFile = file;
        Workspace.Mode = Base64Mode.Encode;
        Workspace.Input = string.Empty;

        Convert();
        await SaveWorkspaceAsync();

        Notifications.Success($"Loaded {file.FileName} ({Base64Converter.FormatSize(file.Size)}).");
        await RecordAsync("Encoded file", file.FileName);
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        _sourceFile = null;
        Workspace.Input = text;
        Convert();
        await SaveWorkspaceAsync();
    }

    /// <summary>Feeds the result back in, which is how a user chains an encode into a decode.</summary>
    private async Task SwapAsync()
    {
        if (!HasTextOutput)
        {
            return;
        }

        _sourceFile = null;
        Workspace.Input = _result.Output;
        Workspace.Mode = Workspace.Mode == Base64Mode.Encode ? Base64Mode.Decode : Base64Mode.Encode;

        Convert();
        await SaveWorkspaceAsync();
    }

    private async Task ClearAsync()
    {
        _sourceFile = null;
        Workspace.Input = string.Empty;

        Convert();
        await SaveWorkspaceAsync();
    }

    /// <summary>Downloads text results as text and binary results as their detected type.</summary>
    private async Task DownloadAsync()
    {
        if (!HasAnyOutput)
        {
            return;
        }

        if (Workspace.Mode == Base64Mode.Decode && _descriptor is { IsText: false })
        {
            var binaryName = HtmlExport.ToFileName("decoded", _descriptor.Extension, "decoded");

            await Interop.DownloadBytesAsync(binaryName, _result.Bytes, _descriptor.MimeType);
            Notifications.Success($"Saved {binaryName}.");
            await RecordAsync("Decoded to file", binaryName);
            return;
        }

        var fileName = HtmlExport.ToFileName(Workspace.Mode == Base64Mode.Encode ? "encoded" : "decoded", "txt", "base64");

        await DownloadAsync(_result.Output, fileName, MimeTypes.PlainText);
        await RecordAsync(Workspace.Mode == Base64Mode.Encode ? "Encoded text" : "Decoded text", fileName);
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
