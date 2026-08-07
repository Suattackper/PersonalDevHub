using DevhubWebApp.Models.Tools.Colors;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Parsing and conversion rules across the colour models.</summary>
public class ColorConverterTests
{
    private static readonly RgbColor Blue = new() { Red = 0x4F, Green = 0x9C, Blue = 0xF9 };

    [Theory]
    [InlineData("#4f9cf9", 0x4F, 0x9C, 0xF9)]
    [InlineData("4F9CF9", 0x4F, 0x9C, 0xF9)]
    [InlineData("#fff", 255, 255, 255)]
    [InlineData("#000", 0, 0, 0)]
    public void Parse_ReadsHexInEveryCommonForm(string input, byte red, byte green, byte blue)
    {
        var color = ColorConverter.Parse(input);

        Assert.NotNull(color);
        Assert.Equal((red, green, blue), (color!.Red, color.Green, color.Blue));
    }

    [Fact]
    public void Parse_ReadsAlphaFromEightDigitHex()
    {
        var color = ColorConverter.Parse("#4f9cf980");

        Assert.NotNull(color);
        Assert.Equal(0.5, color!.Alpha, 1);
        Assert.False(color.IsOpaque);
    }

    [Theory]
    [InlineData("rgb(79, 156, 249)")]
    [InlineData("rgba(79, 156, 249, 1)")]
    public void Parse_ReadsTheRgbFunctionForms(string input)
    {
        var color = ColorConverter.Parse(input);

        Assert.NotNull(color);
        Assert.Equal((79, 156, 249), (color!.Red, color.Green, color.Blue));
    }

    [Fact]
    public void Parse_ReadsTheHslFunctionForm()
    {
        var color = ColorConverter.Parse("hsl(210, 100%, 50%)");

        // The exact green channel is 127.5; it rounds up, which is what browsers do too.
        Assert.NotNull(color);
        Assert.Equal((0, 128, 255), (color!.Red, color.Green, color.Blue));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a colour")]
    [InlineData("#12345")]
    [InlineData("rgb(1, 2)")]
    public void Parse_ReturnsNullForInputThatIsNotAColour(string? input)
        => Assert.Null(ColorConverter.Parse(input));

    [Fact]
    public void ToHex_OmitsTheAlphaWhenOpaque()
    {
        Assert.Equal("#4f9cf9", ColorConverter.ToHex(Blue));
        Assert.Equal("#4f9cf980", ColorConverter.ToHex(Blue with { Alpha = 0.5 }));
    }

    [Fact]
    public void ToRgbString_SwitchesToRgbaWhenTranslucent()
    {
        Assert.Equal("rgb(79, 156, 249)", ColorConverter.ToRgbString(Blue));
        Assert.Equal("rgba(79, 156, 249, 0.5)", ColorConverter.ToRgbString(Blue with { Alpha = 0.5 }));
    }

    [Fact]
    public void ToHsl_MatchesTheKnownValuesForPrimaries()
    {
        var red = ColorConverter.ToHsl(new RgbColor { Red = 255, Green = 0, Blue = 0 });

        Assert.Equal(0, red.Hue, 1);
        Assert.Equal(100, red.Saturation, 1);
        Assert.Equal(50, red.Lightness, 1);

        var green = ColorConverter.ToHsl(new RgbColor { Red = 0, Green = 255, Blue = 0 });
        Assert.Equal(120, green.Hue, 1);

        var blue = ColorConverter.ToHsl(new RgbColor { Red = 0, Green = 0, Blue = 255 });
        Assert.Equal(240, blue.Hue, 1);
    }

    [Fact]
    public void ToHsv_AndToCmyk_MatchKnownValues()
    {
        var hsv = ColorConverter.ToHsv(new RgbColor { Red = 255, Green = 0, Blue = 0 });
        Assert.Equal((0, 100, 100), (Math.Round(hsv.Hue), Math.Round(hsv.Saturation), Math.Round(hsv.Value)));

        var cmyk = ColorConverter.ToCmyk(new RgbColor { Red = 255, Green = 0, Blue = 0 });
        Assert.Equal((0, 100, 100, 0), (Math.Round(cmyk.Cyan), Math.Round(cmyk.Magenta), Math.Round(cmyk.Yellow), Math.Round(cmyk.Key)));
    }

    [Fact]
    public void ToCmyk_ReportsPureBlackAsFullKey()
    {
        var cmyk = ColorConverter.ToCmyk(new RgbColor { Red = 0, Green = 0, Blue = 0 });

        Assert.Equal(100, cmyk.Key, 1);
        Assert.Equal(0, cmyk.Cyan, 1);
    }

    [Fact]
    public void FromHsl_AndToHsl_RoundTrip()
    {
        var hsl = ColorConverter.ToHsl(Blue);
        var roundTripped = ColorConverter.FromHsl(hsl.Hue, hsl.Saturation, hsl.Lightness);

        Assert.Equal(ColorConverter.ToHex(Blue), ColorConverter.ToHex(roundTripped));
    }

    [Fact]
    public void FromHsl_WrapsHueAndClampsTheRest()
    {
        Assert.Equal(ColorConverter.ToHex(ColorConverter.FromHsl(10, 50, 50)),
                     ColorConverter.ToHex(ColorConverter.FromHsl(370, 50, 50)));

        Assert.Equal("#ffffff", ColorConverter.ToHex(ColorConverter.FromHsl(0, 200, 200)));
    }

    /// <summary>Black on white is the reference maximum of 21:1.</summary>
    [Fact]
    public void GetContrast_MatchesTheWcagReferenceValues()
    {
        var black = new RgbColor { Red = 0, Green = 0, Blue = 0 };
        var white = new RgbColor { Red = 255, Green = 255, Blue = 255 };

        var contrast = ColorConverter.GetContrast(black, white);

        Assert.Equal(21, contrast.Ratio, 2);
        Assert.True(contrast.PassesAaaNormal);
        Assert.Equal("AAA", contrast.Grade);
    }

    [Fact]
    public void GetContrast_IsSymmetricAndOneForIdenticalColours()
    {
        var black = new RgbColor { Red = 0, Green = 0, Blue = 0 };
        var white = new RgbColor { Red = 255, Green = 255, Blue = 255 };

        Assert.Equal(ColorConverter.GetContrast(black, white).Ratio, ColorConverter.GetContrast(white, black).Ratio);
        Assert.Equal(1, ColorConverter.GetContrast(Blue, Blue).Ratio, 2);
    }

    [Fact]
    public void ContrastResult_GradesEachThreshold()
    {
        Assert.Equal("Fails AA", new ContrastResult { Ratio = 2 }.Grade);
        Assert.Equal("AA large text only", new ContrastResult { Ratio = 3.5 }.Grade);
        Assert.Equal("AA", new ContrastResult { Ratio = 5 }.Grade);
        Assert.True(new ContrastResult { Ratio = 3 }.PassesNonText);
    }

    [Fact]
    public void GetReadableTextColor_PicksTheHigherContrastOption()
    {
        Assert.Equal("#000000", ColorConverter.ToHex(ColorConverter.GetReadableTextColor(new RgbColor { Red = 255, Green = 255, Blue = 200 })));
        Assert.Equal("#ffffff", ColorConverter.ToHex(ColorConverter.GetReadableTextColor(new RgbColor { Red = 20, Green = 20, Blue = 40 })));
    }
}

/// <summary>Palette generation and export rules.</summary>
public class PaletteGeneratorTests
{
    private static readonly RgbColor Base = new() { Red = 0x4F, Green = 0x9C, Blue = 0xF9 };

    [Fact]
    public void Scale_ProducesTheConventionalStepsFromLightToDark()
    {
        var swatches = PaletteGenerator.Generate(Base, PaletteKind.Scale);

        Assert.Equal(PaletteGenerator.ScaleSteps.Count, swatches.Count);
        Assert.Equal("50", swatches[0].Name);
        Assert.Equal("900", swatches[^1].Name);

        var lightest = ColorConverter.ToHsl(swatches[0].Color).Lightness;
        var darkest = ColorConverter.ToHsl(swatches[^1].Color).Lightness;
        Assert.True(lightest > darkest);
    }

    [Fact]
    public void Scale_KeepsTheBaseColourAtStep500()
    {
        var swatch = PaletteGenerator.Generate(Base, PaletteKind.Scale).Single(entry => entry.Name == "500");

        Assert.Equal(ColorConverter.ToHex(Base), ColorConverter.ToHex(swatch.Color));
    }

    [Fact]
    public void Complementary_RotatesTheHueByHalfTheWheel()
    {
        var swatches = PaletteGenerator.Generate(Base, PaletteKind.Complementary);

        Assert.Equal(2, swatches.Count);

        var baseHue = ColorConverter.ToHsl(swatches[0].Color).Hue;
        var otherHue = ColorConverter.ToHsl(swatches[1].Color).Hue;

        Assert.Equal(180, Math.Abs(baseHue - otherHue), 0);
    }

    [Theory]
    [InlineData(PaletteKind.Analogous, 5)]
    [InlineData(PaletteKind.Triadic, 3)]
    [InlineData(PaletteKind.Tetradic, 4)]
    [InlineData(PaletteKind.Shades, 9)]
    [InlineData(PaletteKind.Tints, 9)]
    public void EveryKind_ProducesTheExpectedNumberOfSwatches(PaletteKind kind, int expected)
        => Assert.Equal(expected, PaletteGenerator.Generate(Base, kind).Count);

    [Fact]
    public void TintsGetLighterAndShadesGetDarker()
    {
        var baseLightness = ColorConverter.ToHsl(Base).Lightness;

        var tint = ColorConverter.ToHsl(PaletteGenerator.Generate(Base, PaletteKind.Tints)[^1].Color).Lightness;
        var shade = ColorConverter.ToHsl(PaletteGenerator.Generate(Base, PaletteKind.Shades)[^1].Color).Lightness;

        Assert.True(tint > baseLightness);
        Assert.True(shade < baseLightness);
    }

    [Fact]
    public void ToCss_EmitsCustomProperties()
    {
        var css = PaletteGenerator.ToCss("Brand Blue", PaletteGenerator.Generate(Base, PaletteKind.Scale));

        Assert.StartsWith(":root {", css, StringComparison.Ordinal);
        Assert.Contains("--brand-blue-500: #4f9cf9;", css, StringComparison.Ordinal);
        Assert.EndsWith("}\n", css, StringComparison.Ordinal);
    }

    [Fact]
    public void ToScss_EmitsVariablesAndAMap()
    {
        var scss = PaletteGenerator.ToScss("brand", PaletteGenerator.Generate(Base, PaletteKind.Scale));

        Assert.Contains("$brand-500: #4f9cf9;", scss, StringComparison.Ordinal);
        Assert.Contains("$brand: (", scss, StringComparison.Ordinal);
        Assert.Contains("\"500\": #4f9cf9,", scss, StringComparison.Ordinal);
    }

    [Fact]
    public void ToJson_NestsSwatchesUnderThePaletteName()
    {
        var json = PaletteGenerator.ToJson("brand", PaletteGenerator.Generate(Base, PaletteKind.Scale));

        using var document = System.Text.Json.JsonDocument.Parse(json);

        Assert.Equal("#4f9cf9", document.RootElement.GetProperty("brand").GetProperty("500").GetString());
    }

    [Fact]
    public void Generate_RejectsANullBaseColour()
        => Assert.Throws<ArgumentNullException>(() => PaletteGenerator.Generate(null!, PaletteKind.Scale));

    [Fact]
    public void GetName_DescribesEveryKind()
        => Assert.All(Enum.GetValues<PaletteKind>(),
            kind => Assert.False(string.IsNullOrWhiteSpace(PaletteGenerator.GetName(kind))));
}

