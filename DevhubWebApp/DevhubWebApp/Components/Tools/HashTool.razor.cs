using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Extensions;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools.Hash;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Hash workspace. Digests, detection and verification are <see cref="HashCalculator"/>; this
/// component owns the current input, the loaded file and the display options.
/// </summary>
public partial class HashTool : ToolComponentBase<HashWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    /// <summary>Above this size hashing takes long enough that the user needs a progress state.</summary>
    private const int LargeInputThreshold = 1_000_000;

    private IReadOnlyList<HashResult> _results = [];
    private HashVerification _verification = HashVerification.None;
    private FileDropResult? _file;
    private bool _isHashing;

    protected override string ToolId => ToolIds.Hash;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool IsEmpty => string.IsNullOrEmpty(Workspace.Text) && _file is null;

    private bool HasResults => _results.Count > 0;

    private string InputSubtitle => _file is not null
        ? "Hashing the dropped file — clear it to hash text again"
        : "Hashed as UTF-8 bytes";

    protected override async Task OnWorkspaceLoadedAsync() => await ComputeAsync();

    private async Task OnTextChangedAsync(string? value)
    {
        Workspace.Text = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(() => _ = ComputeAsync());
    }

    private async Task OnExpectedChangedAsync(string? value)
    {
        Workspace.ExpectedHash = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(VerifyOnly);
    }

    /// <summary>
    /// Computes every digest. Large inputs get a visible loading state and a yield first, because
    /// WebAssembly is single-threaded and would otherwise freeze mid-hash with no explanation.
    /// </summary>
    private async Task ComputeAsync()
    {
        if (IsEmpty)
        {
            _results = [];
            _verification = HashVerification.None;
            return;
        }

        var payload = _file?.Bytes ?? Encoding.UTF8.GetBytes(Workspace.Text);

        if (payload.Length >= LargeInputThreshold)
        {
            _isHashing = true;
            StateHasChanged();
            await Task.Delay(1);
        }

        _results = HashCalculator.ComputeAll(payload);
        _isHashing = false;

        VerifyOnly();
    }

    /// <summary>Re-checks the expected digest without recomputing the hashes.</summary>
    private void VerifyOnly() => _verification = HashCalculator.Verify(_results, Workspace.ExpectedHash);

    /// <summary>Applies the display options; the underlying digest is always computed once.</summary>
    private string Render(HashResult result)
    {
        if (Workspace.ShowBase64)
        {
            return result.Base64;
        }

        return Workspace.Uppercase ? result.Hex.ToUpperInvariant() : result.Hex;
    }

    private async Task UpdateAsync(Action<HashWorkspace> change)
    {
        change(Workspace);
        await SaveWorkspaceAsync();
    }

    private async Task OnFileLoadedAsync(FileDropResult file)
    {
        _file = file;
        Workspace.Text = string.Empty;

        await ComputeAsync();
        await SaveWorkspaceAsync();

        Notifications.Success($"Hashed {file.FileName} ({Base64Converter.FormatSize(file.Size)}).");
        await RecordAsync("Hashed file", file.FileName);
    }

    private async Task RemoveFileAsync()
    {
        _file = null;
        await ComputeAsync();
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        _file = null;
        Workspace.Text = text;

        await ComputeAsync();
        await SaveWorkspaceAsync();
    }

    private async Task ClearAsync()
    {
        _file = null;
        Workspace.Text = string.Empty;
        Workspace.ExpectedHash = string.Empty;

        await ComputeAsync();
        await SaveWorkspaceAsync();
    }

    /// <summary>Exports all digests in the layout checksum files conventionally use.</summary>
    private async Task DownloadAsync()
    {
        if (!HasResults)
        {
            return;
        }

        var source = _file?.FileName ?? "text input";
        var builder = new StringBuilder();
        builder.Append("# Digests for ").AppendLine(source);

        foreach (var result in _results)
        {
            builder.Append(Render(result)).Append("  ").AppendLine(HashCalculator.GetName(result.Algorithm));
        }

        await DownloadAsync(builder.ToString(), "hashes.txt", MimeTypes.PlainText);
        await RecordAsync("Exported digests", source);
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
