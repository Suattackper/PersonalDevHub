using System.Security.Cryptography;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Colors;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Colour workspace. Conversion, contrast and palette generation are utilities; this component
/// owns the current colour, the palette choice and the saved lists.
/// </summary>
public partial class ColorTool : ToolComponentBase<ColorWorkspace>
{
    private const string ShortcutCopy = "ctrl+c";
    private const int HistoryCapacity = 24;

    private static readonly IReadOnlyList<PaletteKind> PaletteKinds = Enum.GetValues<PaletteKind>();

    /// <summary>Export shapes offered by the palette menu.</summary>
    private enum ExportFormat
    {
        Css,
        Scss,
        Json
    }

    private IReadOnlyList<PaletteSwatch> _palette = [];
    private ContrastResult _contrast = new() { Ratio = 1 };
    private RgbColor? _color;
    private RgbColor _background = new() { Red = 255, Green = 255, Blue = 255 };

    protected override string ToolId => ToolIds.Color;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutCopy];

    private bool IsFavorite => _color is not null
        && Workspace.Favorites.Contains(ColorConverter.ToHex(_color), StringComparer.OrdinalIgnoreCase);

    /// <summary>MudBlazor's picker takes a hex string; an unparsable input leaves it untouched.</summary>
    private string PickerColor => _color is null ? "#000000" : ColorConverter.ToHex(_color);

    private string PreviewStyle => _color is null
        ? string.Empty
        : $"background-color: {ColorConverter.ToHex(_color)}; color: {ColorConverter.ToHex(ColorConverter.GetReadableTextColor(_color))};";

    private string ContrastStyle => _color is null
        ? string.Empty
        : $"background-color: {ColorConverter.ToHex(_background)}; color: {ColorConverter.ToHex(_color)};";

    /// <summary>Every notation of the current colour, in the order a developer scans them.</summary>
    private IReadOnlyList<(string Label, string Value)> Formats => _color is null
        ? []
        :
        [
            ("HEX", ColorConverter.ToHex(_color)),
            ("RGB", ColorConverter.ToRgbString(_color)),
            ("HSL", ColorConverter.ToHslString(_color)),
            ("HSV", ColorConverter.ToHsvString(_color)),
            ("CMYK", ColorConverter.ToCmykString(_color))
        ];

    /// <summary>The WCAG thresholds, spelled out rather than left as a single ratio.</summary>
    private IReadOnlyList<(string Label, bool Passes)> AccessibilityChecks =>
    [
        ("AA body text (4.5:1)", _contrast.PassesAaNormal),
        ("AA large text (3:1)", _contrast.PassesAaLarge),
        ("AAA body text (7:1)", _contrast.PassesAaaNormal),
        ("AAA large text (4.5:1)", _contrast.PassesAaaLarge),
        ("Non-text contrast (3:1)", _contrast.PassesNonText)
    ];

    protected override Task OnWorkspaceLoadedAsync()
    {
        Recalculate();
        return Task.CompletedTask;
    }

    private async Task OnInputChangedAsync(string? value)
    {
        Workspace.Input = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(() =>
        {
            Recalculate();
            RememberCurrent();
        });
    }

    private Task OnPickerChangedAsync(MudBlazor.Utilities.MudColor color)
        => UseHexAsync(color.ToString(MudBlazor.Utilities.MudColorOutputFormats.Hex));

    private async Task OnBackgroundChangedAsync(string? value)
    {
        Workspace.ContrastBackground = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(Recalculate);
    }

    private void OnPaletteNameChanged(string? value)
    {
        Workspace.PaletteName = value ?? "palette";
        ScheduleAutoSave();
    }

    /// <summary>Re-parses both colours and rebuilds everything derived from them.</summary>
    private void Recalculate()
    {
        _color = ColorConverter.Parse(Workspace.Input);
        _background = ColorConverter.Parse(Workspace.ContrastBackground)
            ?? new RgbColor { Red = 255, Green = 255, Blue = 255 };

        if (_color is null)
        {
            _palette = [];
            _contrast = new ContrastResult { Ratio = 1 };
            return;
        }

        _palette = PaletteGenerator.Generate(_color, Workspace.Palette);
        _contrast = ColorConverter.GetContrast(_color, _background);
    }

    /// <summary>Keeps the most recent colours, newest first and without duplicates.</summary>
    private void RememberCurrent()
    {
        if (_color is null)
        {
            return;
        }

        var hex = ColorConverter.ToHex(_color);

        Workspace.History.RemoveAll(entry => string.Equals(entry, hex, StringComparison.OrdinalIgnoreCase));
        Workspace.History.Insert(0, hex);

        if (Workspace.History.Count > HistoryCapacity)
        {
            Workspace.History.RemoveRange(HistoryCapacity, Workspace.History.Count - HistoryCapacity);
        }
    }

    private Task SetPaletteAsync(PaletteKind kind) => UpdateAsync(workspace => workspace.Palette = kind);

    private Task UseSwatchAsync(RgbColor color) => UseHexAsync(ColorConverter.ToHex(color));

    private async Task UseHexAsync(string hex)
    {
        Workspace.Input = hex;

        Recalculate();
        RememberCurrent();
        await SaveWorkspaceAsync();
    }

    private async Task UpdateAsync(Action<ColorWorkspace> change)
    {
        change(Workspace);

        Recalculate();
        await SaveWorkspaceAsync();
    }

    private async Task ToggleFavoriteAsync()
    {
        if (_color is null)
        {
            return;
        }

        var hex = ColorConverter.ToHex(_color);

        if (Workspace.Favorites.RemoveAll(entry => string.Equals(entry, hex, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            Workspace.Favorites.Insert(0, hex);
        }

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

        await UseHexAsync(text.Trim());
    }

    /// <summary>A random colour is a design tool, not a secret, but the CSPRNG is already here.</summary>
    private Task RandomAsync()
        => UseHexAsync(ColorConverter.ToHex(new RgbColor
        {
            Red = (byte)RandomNumberGenerator.GetInt32(256),
            Green = (byte)RandomNumberGenerator.GetInt32(256),
            Blue = (byte)RandomNumberGenerator.GetInt32(256)
        }));

    private async Task ExportAsync(ExportFormat format)
    {
        if (_palette.Count == 0)
        {
            return;
        }

        var name = Workspace.PaletteName.Length == 0 ? "palette" : Workspace.PaletteName;

        var (content, fileName, mimeType) = format switch
        {
            ExportFormat.Scss => (PaletteGenerator.ToScss(name, _palette), $"{name}.scss", MimeTypes.PlainText),
            ExportFormat.Json => (PaletteGenerator.ToJson(name, _palette), $"{name}.json", MimeTypes.Json),
            _ => (PaletteGenerator.ToCss(name, _palette), $"{name}.css", MimeTypes.PlainText)
        };

        await DownloadAsync(content, fileName, mimeType);
        await RecordAsync("Exported palette", $"{PaletteGenerator.GetName(Workspace.Palette)} -> {fileName}");
    }

    private static string SwatchStyle(RgbColor color)
        => $"background-color: {ColorConverter.ToHex(color)}; color: {ColorConverter.ToHex(ColorConverter.GetReadableTextColor(color))};";

    private static string ChipStyle(string hex)
    {
        var color = ColorConverter.Parse(hex);

        return color is null
            ? string.Empty
            : $"background-color: {hex}; color: {ColorConverter.ToHex(ColorConverter.GetReadableTextColor(color))};";
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutCopy && _color is not null)
        {
            await CopyAsync(ColorConverter.ToHex(_color), "Colour copied.");
            StateHasChanged();
        }
    }
}
