using System.Globalization;
using System.Text.Json;
using DevhubWebApp.Constants;
using DevhubWebApp.Extensions;
using DevhubWebApp.Models.Tools.Jwt;
using DevhubWebApp.Utilities;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// JWT inspection workspace. Decoding is <see cref="JwtDecoder"/>; this component owns the token
/// text and the presentation of the decoded claims.
/// </summary>
public partial class JwtTool : ToolComponentBase<JwtWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private static readonly JsonSerializerOptions ExportOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private JwtToken _token = JwtToken.Empty;

    protected override string ToolId => ToolIds.Jwt;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool IsEmpty => string.IsNullOrWhiteSpace(Workspace.Token);

    private string ValidityLabel => _token.Validity switch
    {
        JwtValidity.Active => _token.TimeRemaining is { } remaining ? $"valid for {Humanise(remaining)}" : "active",
        JwtValidity.Expired => "expired",
        JwtValidity.NotYetValid => "not yet valid",
        _ => "no expiry claim"
    };

    private Color ValidityColor => _token.Validity switch
    {
        JwtValidity.Active => Color.Success,
        JwtValidity.Expired => Color.Error,
        JwtValidity.NotYetValid => Color.Warning,
        _ => Color.Default
    };

    private string ValidityIcon => _token.Validity switch
    {
        JwtValidity.Active => Icons.Material.Filled.CheckCircle,
        JwtValidity.Expired => Icons.Material.Filled.Cancel,
        JwtValidity.NotYetValid => Icons.Material.Filled.Schedule,
        _ => Icons.Material.Filled.HelpOutline
    };

    /// <summary>Registered claims rendered beside the signature, in a fixed reading order.</summary>
    private IReadOnlyList<(string Label, string Value)> Claims =>
    [
        ("Issuer", _token.Issuer.OrDefault("—")),
        ("Subject", _token.Subject.OrDefault("—")),
        ("Audience", _token.Audience.OrDefault("—")),
        ("Key id", _token.KeyId.OrDefault("—")),
        ("Issued at", Format(_token.IssuedAt)),
        ("Not before", Format(_token.NotBefore)),
        ("Expires at", Format(_token.ExpiresAt))
    ];

    protected override Task OnWorkspaceLoadedAsync()
    {
        Decode();
        return Task.CompletedTask;
    }

    /// <summary>Decoding is cheap, but debouncing keeps a long paste from decoding character by character.</summary>
    private async Task OnTokenChangedAsync(string? value)
    {
        Workspace.Token = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(Decode);
    }

    private void Decode() => _token = JwtDecoder.Decode(Workspace.Token);

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        Workspace.Token = text.Trim();
        Decode();
        await SaveWorkspaceAsync();
    }

    private async Task ClearAsync()
    {
        Workspace.Token = string.Empty;
        Decode();
        await SaveWorkspaceAsync();
    }

    private async Task DownloadAsync()
    {
        if (!_token.IsValid)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            header = JsonDocument.Parse(_token.HeaderJson).RootElement,
            payload = JsonDocument.Parse(_token.PayloadJson).RootElement,
            signature = _token.Signature,
            validity = _token.Validity.ToString(),
            sizeInBytes = _token.SizeInBytes
        }, ExportOptions);

        await DownloadAsync(payload, "jwt.json", MimeTypes.Json);
        await RecordAsync("Decoded JWT", _token.Algorithm.OrDefault("token"));
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadAsync();
            StateHasChanged();
        }
    }

    private static string Format(DateTimeOffset? value)
        => value is null
            ? "—"
            : $"{value.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)} ({value.Value.ToRelativeLabel()})";

    /// <summary>Renders a remaining lifetime in the largest unit that still reads naturally.</summary>
    private static string Humanise(TimeSpan value) => value switch
    {
        { TotalDays: >= 1 } => $"{(int)value.TotalDays}d",
        { TotalHours: >= 1 } => $"{(int)value.TotalHours}h",
        { TotalMinutes: >= 1 } => $"{(int)value.TotalMinutes}m",
        _ => "< 1m"
    };
}
