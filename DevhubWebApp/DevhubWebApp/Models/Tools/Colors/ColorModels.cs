namespace DevhubWebApp.Models.Tools.Colors;

/// <summary>A colour in sRGB with an alpha channel.</summary>
public sealed record RgbColor
{
    public required byte Red { get; init; }

    public required byte Green { get; init; }

    public required byte Blue { get; init; }

    /// <summary>Opacity from 0 (transparent) to 1 (opaque).</summary>
    public double Alpha { get; init; } = 1;

    public bool IsOpaque => Alpha >= 1;
}

/// <summary>Hue, saturation and lightness. Hue is degrees, the rest are 0–100.</summary>
public sealed record HslColor
{
    public required double Hue { get; init; }

    public required double Saturation { get; init; }

    public required double Lightness { get; init; }
}

/// <summary>Hue, saturation and value. Hue is degrees, the rest are 0–100.</summary>
public sealed record HsvColor
{
    public required double Hue { get; init; }

    public required double Saturation { get; init; }

    public required double Value { get; init; }
}

/// <summary>Cyan, magenta, yellow and key, each 0–100.</summary>
public sealed record CmykColor
{
    public required double Cyan { get; init; }

    public required double Magenta { get; init; }

    public required double Yellow { get; init; }

    public required double Key { get; init; }
}

/// <summary>WCAG conformance of a foreground/background pair.</summary>
public sealed record ContrastResult
{
    /// <summary>Contrast ratio between 1 and 21.</summary>
    public required double Ratio { get; init; }

    /// <summary>Meets 4.5:1 — the minimum for body text.</summary>
    public bool PassesAaNormal => Ratio >= 4.5;

    /// <summary>Meets 3:1 — the minimum for text at 18pt or 14pt bold.</summary>
    public bool PassesAaLarge => Ratio >= 3;

    /// <summary>Meets 7:1 — enhanced contrast for body text.</summary>
    public bool PassesAaaNormal => Ratio >= 7;

    /// <summary>Meets 4.5:1 for large text.</summary>
    public bool PassesAaaLarge => Ratio >= 4.5;

    /// <summary>Meets 3:1 — the minimum for icons, borders and other non-text elements.</summary>
    public bool PassesNonText => Ratio >= 3;

    /// <summary>Highest conformance reached, e.g. "AAA" or "Fails AA".</summary>
    public string Grade => Ratio switch
    {
        >= 7 => "AAA",
        >= 4.5 => "AA",
        >= 3 => "AA large text only",
        _ => "Fails AA"
    };
}

/// <summary>Named colour scales the palette generator produces.</summary>
public enum PaletteKind
{
    /// <summary>A tint-to-shade ramp, the shape Material and Tailwind scales use.</summary>
    Scale,

    /// <summary>The colour opposite on the wheel.</summary>
    Complementary,

    /// <summary>Neighbours on the wheel.</summary>
    Analogous,

    /// <summary>Three colours evenly spaced around the wheel.</summary>
    Triadic,

    /// <summary>Four colours in two complementary pairs.</summary>
    Tetradic,

    /// <summary>Steps of decreasing lightness.</summary>
    Shades,

    /// <summary>Steps of increasing lightness.</summary>
    Tints
}

/// <summary>One entry of a generated palette.</summary>
public sealed record PaletteSwatch
{
    /// <summary>Step label, e.g. <c>500</c> for a scale or <c>Analogous +30</c> for a harmony.</summary>
    public required string Name { get; init; }

    public required RgbColor Color { get; init; }
}

