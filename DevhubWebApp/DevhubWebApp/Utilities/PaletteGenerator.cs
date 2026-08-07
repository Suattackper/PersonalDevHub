using System.Globalization;
using System.Text;
using DevhubWebApp.Models.Tools.Colors;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Builds colour scales and harmonies from a base colour, and exports them as CSS, SCSS or JSON.
/// Pure: the same input always produces the same palette.
/// </summary>
public static class PaletteGenerator
{
    /// <summary>Step names of the tint-to-shade scale, matching the convention design systems use.</summary>
    public static IReadOnlyList<int> ScaleSteps { get; } = [50, 100, 200, 300, 400, 500, 600, 700, 800, 900];

    /// <summary>Display name of a palette kind.</summary>
    public static string GetName(PaletteKind kind) => kind switch
    {
        PaletteKind.Scale => "Scale (50-900)",
        PaletteKind.Complementary => "Complementary",
        PaletteKind.Analogous => "Analogous",
        PaletteKind.Triadic => "Triadic",
        PaletteKind.Tetradic => "Tetradic",
        PaletteKind.Shades => "Shades",
        PaletteKind.Tints => "Tints",
        _ => kind.ToString()
    };

    /// <summary>Generates a palette from a base colour.</summary>
    public static IReadOnlyList<PaletteSwatch> Generate(RgbColor baseColor, PaletteKind kind)
    {
        ArgumentNullException.ThrowIfNull(baseColor);

        var hsl = ColorConverter.ToHsl(baseColor);

        return kind switch
        {
            PaletteKind.Scale => BuildScale(hsl),
            PaletteKind.Complementary => BuildHarmony(hsl, [0, 180]),
            PaletteKind.Analogous => BuildHarmony(hsl, [-60, -30, 0, 30, 60]),
            PaletteKind.Triadic => BuildHarmony(hsl, [0, 120, 240]),
            PaletteKind.Tetradic => BuildHarmony(hsl, [0, 90, 180, 270]),
            PaletteKind.Shades => BuildLightnessRamp(hsl, toLighter: false),
            PaletteKind.Tints => BuildLightnessRamp(hsl, toLighter: true),
            _ => []
        };
    }

    /// <summary>Exports a palette as CSS custom properties.</summary>
    public static string ToCss(string name, IEnumerable<PaletteSwatch> swatches)
    {
        ArgumentNullException.ThrowIfNull(swatches);

        var builder = new StringBuilder(":root {");
        builder.Append('\n');

        foreach (var swatch in swatches)
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"  --{Slug(name)}-{Slug(swatch.Name)}: {ColorConverter.ToHex(swatch.Color)};\n");
        }

        return builder.Append("}\n").ToString();
    }

    /// <summary>Exports a palette as SCSS variables plus a map, which is how SCSS palettes are used.</summary>
    public static string ToScss(string name, IEnumerable<PaletteSwatch> swatches)
    {
        ArgumentNullException.ThrowIfNull(swatches);

        var list = swatches.ToList();
        var builder = new StringBuilder();

        foreach (var swatch in list)
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"${Slug(name)}-{Slug(swatch.Name)}: {ColorConverter.ToHex(swatch.Color)};\n");
        }

        builder.Append(CultureInfo.InvariantCulture, $"\n${Slug(name)}: (\n");

        foreach (var swatch in list)
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"  \"{Slug(swatch.Name)}\": {ColorConverter.ToHex(swatch.Color)},\n");
        }

        return builder.Append(");\n").ToString();
    }

    /// <summary>Exports a palette as a JSON object keyed by step name.</summary>
    public static string ToJson(string name, IEnumerable<PaletteSwatch> swatches)
    {
        ArgumentNullException.ThrowIfNull(swatches);

        var payload = new Dictionary<string, Dictionary<string, string>>
        {
            [Slug(name)] = swatches.ToDictionary(swatch => Slug(swatch.Name), swatch => ColorConverter.ToHex(swatch.Color))
        };

        return System.Text.Json.JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
        new(System.Text.Json.JsonSerializerDefaults.Web) { WriteIndented = true };

    /// <summary>
    /// A 50-900 ramp around the base colour: step 500 is the colour itself, lighter steps approach
    /// white and darker steps approach black, keeping the hue.
    /// </summary>
    private static List<PaletteSwatch> BuildScale(HslColor hsl)
    {
        var swatches = new List<PaletteSwatch>(ScaleSteps.Count);

        foreach (var step in ScaleSteps)
        {
            // 50 maps to ~95% lightness, 500 keeps the base, 900 maps to ~15%.
            var lightness = step <= 500
                ? hsl.Lightness + ((95 - hsl.Lightness) * (500 - step) / 450d)
                : hsl.Lightness - ((hsl.Lightness - 15) * (step - 500) / 400d);

            swatches.Add(new PaletteSwatch
            {
                Name = step.ToString(CultureInfo.InvariantCulture),
                Color = ColorConverter.FromHsl(hsl.Hue, hsl.Saturation, lightness)
            });
        }

        return swatches;
    }

    private static List<PaletteSwatch> BuildHarmony(HslColor hsl, IReadOnlyList<int> offsets)
        => offsets.Select(offset => new PaletteSwatch
        {
            Name = offset == 0 ? "base" : $"{(offset > 0 ? "+" : string.Empty)}{offset.ToString(CultureInfo.InvariantCulture)}",
            Color = ColorConverter.FromHsl(hsl.Hue + offset, hsl.Saturation, hsl.Lightness)
        }).ToList();

    private static List<PaletteSwatch> BuildLightnessRamp(HslColor hsl, bool toLighter)
    {
        var swatches = new List<PaletteSwatch>(9);

        for (var step = 1; step <= 9; step++)
        {
            var lightness = toLighter
                ? hsl.Lightness + ((100 - hsl.Lightness) * step / 10d)
                : hsl.Lightness * (10 - step) / 10d;

            swatches.Add(new PaletteSwatch
            {
                Name = $"{step * 10}%",
                Color = ColorConverter.FromHsl(hsl.Hue, hsl.Saturation, lightness)
            });
        }

        return swatches;
    }

    private static string Slug(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '-');
        }

        return builder.ToString().Trim('-');
    }
}

