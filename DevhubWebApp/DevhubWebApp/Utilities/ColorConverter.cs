using System.Globalization;
using DevhubWebApp.Models.Tools.Colors;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Conversions between the colour models a developer actually pastes: HEX, RGB(A), HSL, HSV and
/// CMYK. Pure, so every rounding rule is pinned by tests rather than discovered in the UI.
/// </summary>
public static class ColorConverter
{
    /// <summary>
    /// Parses <c>#rgb</c>, <c>#rrggbb</c>, <c>#rrggbbaa</c>, <c>rgb()</c>, <c>rgba()</c> and
    /// <c>hsl()</c>. Returns <c>null</c> when the text is not a colour.
    /// </summary>
    public static RgbColor? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();

        if (text.StartsWith('#') || text.All(Uri.IsHexDigit))
        {
            return ParseHex(text.TrimStart('#'));
        }

        if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return ParseFunctional(text, isHsl: false);
        }

        if (text.StartsWith("hsl", StringComparison.OrdinalIgnoreCase))
        {
            return ParseFunctional(text, isHsl: true);
        }

        return null;
    }

    /// <summary>Renders <c>#rrggbb</c>, or <c>#rrggbbaa</c> when the colour is translucent.</summary>
    public static string ToHex(RgbColor color, bool forceAlpha = false)
    {
        ArgumentNullException.ThrowIfNull(color);

        var hex = $"#{color.Red:x2}{color.Green:x2}{color.Blue:x2}";

        return color.IsOpaque && !forceAlpha
            ? hex
            : $"{hex}{(byte)Math.Round(color.Alpha * 255):x2}";
    }

    /// <summary>Renders <c>rgb(r, g, b)</c> or <c>rgba(r, g, b, a)</c>.</summary>
    public static string ToRgbString(RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        return color.IsOpaque
            ? $"rgb({color.Red}, {color.Green}, {color.Blue})"
            : $"rgba({color.Red}, {color.Green}, {color.Blue}, {Round(color.Alpha, 2).ToString(CultureInfo.InvariantCulture)})";
    }

    /// <summary>Renders <c>hsl(h, s%, l%)</c>.</summary>
    public static string ToHslString(RgbColor color)
    {
        var hsl = ToHsl(color);

        return $"hsl({Round(hsl.Hue, 0).ToString(CultureInfo.InvariantCulture)}, " +
               $"{Round(hsl.Saturation, 1).ToString(CultureInfo.InvariantCulture)}%, " +
               $"{Round(hsl.Lightness, 1).ToString(CultureInfo.InvariantCulture)}%)";
    }

    /// <summary>Renders <c>hsv(h, s%, v%)</c>.</summary>
    public static string ToHsvString(RgbColor color)
    {
        var hsv = ToHsv(color);

        return $"hsv({Round(hsv.Hue, 0).ToString(CultureInfo.InvariantCulture)}, " +
               $"{Round(hsv.Saturation, 1).ToString(CultureInfo.InvariantCulture)}%, " +
               $"{Round(hsv.Value, 1).ToString(CultureInfo.InvariantCulture)}%)";
    }

    /// <summary>Renders <c>cmyk(c%, m%, y%, k%)</c>.</summary>
    public static string ToCmykString(RgbColor color)
    {
        var cmyk = ToCmyk(color);

        return $"cmyk({Round(cmyk.Cyan, 1).ToString(CultureInfo.InvariantCulture)}%, " +
               $"{Round(cmyk.Magenta, 1).ToString(CultureInfo.InvariantCulture)}%, " +
               $"{Round(cmyk.Yellow, 1).ToString(CultureInfo.InvariantCulture)}%, " +
               $"{Round(cmyk.Key, 1).ToString(CultureInfo.InvariantCulture)}%)";
    }

    /// <summary>Converts to hue, saturation and lightness.</summary>
    public static HslColor ToHsl(RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        var (red, green, blue) = Normalise(color);
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;

        var lightness = (max + min) / 2;
        var saturation = delta == 0 ? 0 : delta / (1 - Math.Abs((2 * lightness) - 1));

        return new HslColor
        {
            Hue = Hue(red, green, blue, max, delta),
            Saturation = saturation * 100,
            Lightness = lightness * 100
        };
    }

    /// <summary>Converts to hue, saturation and value.</summary>
    public static HsvColor ToHsv(RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        var (red, green, blue) = Normalise(color);
        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var delta = max - min;

        return new HsvColor
        {
            Hue = Hue(red, green, blue, max, delta),
            Saturation = max == 0 ? 0 : delta / max * 100,
            Value = max * 100
        };
    }

    /// <summary>Converts to CMYK. This is the naive conversion, without an ICC profile.</summary>
    public static CmykColor ToCmyk(RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        var (red, green, blue) = Normalise(color);
        var key = 1 - Math.Max(red, Math.Max(green, blue));

        if (Math.Abs(key - 1) < 0.0001)
        {
            return new CmykColor { Cyan = 0, Magenta = 0, Yellow = 0, Key = 100 };
        }

        return new CmykColor
        {
            Cyan = (1 - red - key) / (1 - key) * 100,
            Magenta = (1 - green - key) / (1 - key) * 100,
            Yellow = (1 - blue - key) / (1 - key) * 100,
            Key = key * 100
        };
    }

    /// <summary>Builds a colour from hue, saturation and lightness.</summary>
    public static RgbColor FromHsl(double hue, double saturation, double lightness, double alpha = 1)
    {
        var h = ((hue % 360) + 360) % 360;
        var s = Math.Clamp(saturation, 0, 100) / 100;
        var l = Math.Clamp(lightness, 0, 100) / 100;

        var chroma = (1 - Math.Abs((2 * l) - 1)) * s;
        var second = chroma * (1 - Math.Abs(((h / 60) % 2) - 1));
        var match = l - (chroma / 2);

        var (red, green, blue) = h switch
        {
            < 60 => (chroma, second, 0d),
            < 120 => (second, chroma, 0d),
            < 180 => (0d, chroma, second),
            < 240 => (0d, second, chroma),
            < 300 => (second, 0d, chroma),
            _ => (chroma, 0d, second)
        };

        return new RgbColor
        {
            Red = ToByte(red + match),
            Green = ToByte(green + match),
            Blue = ToByte(blue + match),
            Alpha = Math.Clamp(alpha, 0, 1)
        };
    }

    /// <summary>
    /// Relative luminance as defined by WCAG 2: the sRGB channels are linearised, then weighted by
    /// human sensitivity to each.
    /// </summary>
    public static double GetRelativeLuminance(RgbColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        static double Channel(byte value)
        {
            var normalised = value / 255d;
            return normalised <= 0.03928 ? normalised / 12.92 : Math.Pow((normalised + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.Red)) + (0.7152 * Channel(color.Green)) + (0.0722 * Channel(color.Blue));
    }

    /// <summary>Contrast ratio and the WCAG levels it satisfies.</summary>
    public static ContrastResult GetContrast(RgbColor foreground, RgbColor background)
    {
        var first = GetRelativeLuminance(foreground);
        var second = GetRelativeLuminance(background);

        var lighter = Math.Max(first, second);
        var darker = Math.Min(first, second);

        return new ContrastResult { Ratio = Round((lighter + 0.05) / (darker + 0.05), 2) };
    }

    /// <summary>
    /// Whether black or white text reads better on this colour. Deciding by contrast rather than by
    /// a lightness threshold is what makes mid-tone backgrounds come out right.
    /// </summary>
    public static RgbColor GetReadableTextColor(RgbColor background)
    {
        var black = new RgbColor { Red = 0, Green = 0, Blue = 0 };
        var white = new RgbColor { Red = 255, Green = 255, Blue = 255 };

        return GetContrast(black, background).Ratio >= GetContrast(white, background).Ratio ? black : white;
    }

    private static RgbColor? ParseHex(string hex)
    {
        if (!hex.All(Uri.IsHexDigit))
        {
            return null;
        }

        hex = hex.Length switch
        {
            3 => string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]),
            4 => string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2], hex[3], hex[3]),
            _ => hex
        };

        if (hex.Length is not (6 or 8))
        {
            return null;
        }

        return new RgbColor
        {
            Red = System.Convert.ToByte(hex[..2], 16),
            Green = System.Convert.ToByte(hex.Substring(2, 2), 16),
            Blue = System.Convert.ToByte(hex.Substring(4, 2), 16),
            Alpha = hex.Length == 8 ? System.Convert.ToByte(hex.Substring(6, 2), 16) / 255d : 1
        };
    }

    /// <summary>Parses the <c>rgb()</c>/<c>hsl()</c> function forms, with or without an alpha.</summary>
    private static RgbColor? ParseFunctional(string text, bool isHsl)
    {
        var open = text.IndexOf('(', StringComparison.Ordinal);
        var close = text.LastIndexOf(')');

        if (open < 0 || close < open)
        {
            return null;
        }

        var parts = text[(open + 1)..close]
            .Replace("%", string.Empty, StringComparison.Ordinal)
            .Replace("/", ",", StringComparison.Ordinal)
            .Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length is < 3 or > 4)
        {
            return null;
        }

        var values = new double[parts.Length];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!double.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out values[index]))
            {
                return null;
            }
        }

        var alpha = parts.Length == 4 ? Math.Clamp(values[3], 0, 1) : 1;

        return isHsl
            ? FromHsl(values[0], values[1], values[2], alpha)
            : new RgbColor
            {
                Red = ToByte(values[0] / 255),
                Green = ToByte(values[1] / 255),
                Blue = ToByte(values[2] / 255),
                Alpha = alpha
            };
    }

    private static (double Red, double Green, double Blue) Normalise(RgbColor color)
        => (color.Red / 255d, color.Green / 255d, color.Blue / 255d);

    private static double Hue(double red, double green, double blue, double max, double delta)
    {
        if (delta == 0)
        {
            return 0;
        }

        double hue;

        if (Math.Abs(max - red) < double.Epsilon)
        {
            hue = 60 * (((green - blue) / delta) % 6);
        }
        else if (Math.Abs(max - green) < double.Epsilon)
        {
            hue = 60 * (((blue - red) / delta) + 2);
        }
        else
        {
            hue = 60 * (((red - green) / delta) + 4);
        }

        return (hue + 360) % 360;
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);

    private static double Round(double value, int digits) => Math.Round(value, digits, MidpointRounding.AwayFromZero);
}

