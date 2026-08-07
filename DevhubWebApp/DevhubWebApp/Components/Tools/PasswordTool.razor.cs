using System.Globalization;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Password;
using DevhubWebApp.Utilities;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Password workspace. Generation, entropy and strength are <see cref="PasswordGenerator"/>; this
/// component owns the option state and the current batch.
/// </summary>
public partial class PasswordTool : ToolComponentBase<PasswordWorkspace>
{
    private const string ShortcutGenerate = "ctrl+enter";
    private const string ShortcutDownload = "ctrl+s";

    private static readonly IReadOnlyList<PasswordKind> Kinds = Enum.GetValues<PasswordKind>();

    /// <summary>Advice shown beside the generator. Data, so the markup stays a loop.</summary>
    private static readonly IReadOnlyList<string> SecurityTips =
    [
        "Length beats complexity: a long passphrase resists attack better than a short password full of symbols.",
        "Never reuse a password between services — one breach then unlocks all of them.",
        "Store secrets in a password manager, not in a note, a spreadsheet or a repository.",
        "Turn on multi-factor authentication wherever it is offered; a stolen password alone then gets nobody in.",
        "Change a password when a service reports a breach, not on a fixed schedule."
    ];

    /// <summary>Held as a field because Razor cannot inline text containing '@' in an attribute.</summary>
    private const string SymbolsDescription = "!@#$%^&*()-_=+[]{};:,.<>?/~";

    private readonly List<GeneratedPassword> _batch = [];
    private GeneratedPassword? _current;

    protected override string ToolId => ToolIds.Password;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutGenerate, ShortcutDownload];

    /// <summary>Meter fill, capped at 128 bits — beyond that the difference is academic.</summary>
    private double StrengthPercentage => _current is null ? 0 : Math.Min(100, _current.EntropyBits / 128 * 100);

    private Color StrengthColor => _current?.Strength switch
    {
        PasswordStrength.VeryWeak => Color.Error,
        PasswordStrength.Weak => Color.Warning,
        PasswordStrength.Fair => Color.Info,
        PasswordStrength.Strong => Color.Success,
        PasswordStrength.VeryStrong => Color.Tertiary,
        _ => Color.Default
    };

    private string StrengthStyle => $"color: var(--mud-palette-{StrengthColor.ToDescriptionString()});";

    /// <summary>The first generation happens on open, so the tool is never empty on arrival.</summary>
    protected override async Task OnWorkspaceLoadedAsync() => await GenerateAsync(record: false);

    private Task GenerateAsync() => GenerateAsync(record: true);

    private async Task GenerateAsync(bool record)
    {
        var options = Workspace.ToOptions();

        _batch.Clear();
        _batch.AddRange(PasswordGenerator.GenerateMany(Workspace.BatchSize, options));
        _current = _batch[0];

        if (record)
        {
            // The secret itself is never written to the activity log — only that one was made.
            await RecordAsync("Generated password", $"{Workspace.Kind} · {_current.EntropyBits} bits");
        }
    }

    private Task SetKindAsync(PasswordKind kind) => UpdateAsync(workspace => workspace.Kind = kind);

    /// <summary>Every option change regenerates, so the meter always describes what is on screen.</summary>
    private async Task UpdateAsync(Action<PasswordWorkspace> change)
    {
        change(Workspace);

        await GenerateAsync(record: false);
        await SaveWorkspaceAsync();
    }

    private async Task DownloadAsync()
    {
        if (_batch.Count == 0)
        {
            return;
        }

        var content = string.Join('\n', _batch.Select(password => password.Value));

        await DownloadAsync(content, "passwords.txt", MimeTypes.PlainText);
        await RecordAsync("Exported passwords", _batch.Count.ToString(CultureInfo.InvariantCulture));
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        switch (combo)
        {
            case ShortcutGenerate:
                await GenerateAsync();
                break;

            case ShortcutDownload:
                await DownloadAsync();
                break;

            default:
                return;
        }

        StateHasChanged();
    }

    private static string GetKindLabel(PasswordKind kind) => kind switch
    {
        PasswordKind.Random => "Random",
        PasswordKind.Pronounceable => "Pronounceable",
        PasswordKind.Passphrase => "Passphrase",
        _ => kind.ToString()
    };

    private static string GetKindIcon(PasswordKind kind) => kind switch
    {
        PasswordKind.Random => Icons.Material.Filled.Casino,
        PasswordKind.Pronounceable => Icons.Material.Filled.RecordVoiceOver,
        PasswordKind.Passphrase => Icons.Material.Filled.ShortText,
        _ => Icons.Material.Filled.Password
    };
}
