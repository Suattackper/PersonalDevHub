using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using DevhubWebApp.Models.Tools.Barcode;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Managed encoders for linear barcodes. Each symbology turns text into a module pattern -
/// <c>true</c> for a bar, <c>false</c> for a space - which the renderer draws.
/// <para>
/// Implemented here rather than taken from a library because DevHub runs entirely in the browser:
/// the common .NET barcode packages depend on System.Drawing, which WebAssembly does not provide.
/// </para>
/// </summary>
public static class BarcodeEncoder
{
    /// <summary>Symbologies in the order the UI presents them.</summary>
    public static IReadOnlyList<Symbology> All { get; } = Enum.GetValues<Symbology>();

    /// <summary>Display name of a symbology.</summary>
    public static string GetName(Symbology symbology) => symbology switch
    {
        Symbology.Code128 => "Code 128",
        Symbology.Ean13 => "EAN-13",
        Symbology.Ean8 => "EAN-8",
        Symbology.UpcA => "UPC-A",
        Symbology.Itf => "ITF (2 of 5)",
        _ => symbology.ToString()
    };

    /// <summary>What the symbology accepts, shown as input guidance.</summary>
    public static string GetRequirement(Symbology symbology) => symbology switch
    {
        Symbology.Code128 => "Any printable ASCII text",
        Symbology.Ean13 => "12 digits (the 13th is calculated)",
        Symbology.Ean8 => "7 digits (the 8th is calculated)",
        Symbology.UpcA => "11 digits (the 12th is calculated)",
        Symbology.Itf => "Digits; an odd count is padded with a leading zero",
        _ => string.Empty
    };

    /// <summary>Encodes <paramref name="data"/>. Invalid input returns an explanation, never throws.</summary>
    public static BarcodeSymbol Encode(Symbology symbology, string? data)
    {
        var text = data?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            return BarcodeSymbol.Empty;
        }

        return symbology switch
        {
            Symbology.Code128 => Code128(text),
            Symbology.Ean13 => Ean(text, 12, Symbology.Ean13),
            Symbology.Ean8 => Ean(text, 7, Symbology.Ean8),
            Symbology.UpcA => Ean(text, 11, Symbology.UpcA),
            Symbology.Itf => Itf(text),
            _ => BarcodeSymbol.Failure("Unsupported symbology.")
        };
    }

    /// <summary>
    /// Modulo-10 check digit used by EAN, UPC and ITF-14: weights alternate 3 and 1 from the right.
    /// </summary>
    public static int CalculateModulo10(string digits)
    {
        ArgumentNullException.ThrowIfNull(digits);

        var sum = 0;

        for (var index = 0; index < digits.Length; index++)
        {
            var digit = digits[digits.Length - 1 - index] - '0';
            sum += index % 2 == 0 ? digit * 3 : digit;
        }

        return (10 - (sum % 10)) % 10;
    }

    // ---------- Code 128 ----------

    /// <summary>
    /// The 107 Code 128 patterns, each six run-lengths (bar, space, bar, space, bar, space)
    /// totalling eleven modules; the stop pattern has seven runs and thirteen modules. The index is
    /// the code value, and 103-106 are Start A, Start B, Start C and Stop.
    /// <para>
    /// A structural test asserts those widths for every entry, which is what catches a mistyped
    /// digit in a table this size.
    /// </para>
    /// </summary>
    private static readonly string[] Code128Patterns =
    [
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213",
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132",
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211",
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313",
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331",
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111",
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214",
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111",
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141",
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141",
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"
    ];

    private const int Code128StartB = 104;
    private const int Code128Stop = 106;

    /// <summary>
    /// Encodes in Code Set B, which covers printable ASCII 32-126. Set C would pack digit pairs
    /// more densely; a single set keeps the checksum arithmetic simple and verifiable.
    /// </summary>
    private static BarcodeSymbol Code128(string text)
    {
        var values = new List<int> { Code128StartB };

        foreach (var character in text)
        {
            if (character is < ' ' or > '~')
            {
                return BarcodeSymbol.Failure($"Code 128 (set B) cannot encode '{character}'; use printable ASCII.");
            }

            values.Add(character - 32);
        }

        // Checksum: the start value plus each symbol weighted by its position, modulo 103.
        var checksum = values[0];
        for (var index = 1; index < values.Count; index++)
        {
            checksum += values[index] * index;
        }

        values.Add(checksum % 103);
        values.Add(Code128Stop);

        var modules = new List<bool>();
        foreach (var value in values)
        {
            AppendRunLengths(modules, Code128Patterns[value]);
        }

        return new BarcodeSymbol { Modules = modules, Text = text };
    }

    // ---------- EAN / UPC ----------

    private static readonly string[] EanLeftOdd =
        ["0001101", "0011001", "0010011", "0111101", "0100011", "0110001", "0101111", "0111011", "0110111", "0001011"];

    private static readonly string[] EanLeftEven =
        ["0100111", "0110011", "0011011", "0100001", "0011101", "0111001", "0000101", "0010001", "0001001", "0010111"];

    private static readonly string[] EanRight =
        ["1110010", "1100110", "1101100", "1000010", "1011100", "1001110", "1010000", "1000100", "1001000", "1110100"];

    /// <summary>Which left-hand digits use the even-parity table, selected by the first digit.</summary>
    private static readonly string[] Ean13Parity =
        ["OOOOOO", "OOEOEE", "OOEEOE", "OOEEEO", "OEOOEE", "OEEEOO", "OEEOOE", "OEOEOE", "OEOEEO", "OEEOEO"];

    private static BarcodeSymbol Ean(string text, int requiredDigits, Symbology symbology)
    {
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());

        // A pasted code usually carries its check digit already; drop it and recompute.
        if (digits.Length == requiredDigits + 1)
        {
            digits = digits[..requiredDigits];
        }

        if (digits.Length != requiredDigits)
        {
            return BarcodeSymbol.Failure($"{GetName(symbology)} needs {requiredDigits} digits (a check digit is added).");
        }

        var full = digits + CalculateModulo10(digits).ToString(CultureInfo.InvariantCulture);

        return symbology switch
        {
            Symbology.Ean8 => new BarcodeSymbol { Modules = BuildEan8(full), Text = full },

            // UPC-A is an EAN-13 whose number system digit is zero.
            Symbology.UpcA => new BarcodeSymbol { Modules = BuildEan13("0" + full), Text = full },

            _ => new BarcodeSymbol { Modules = BuildEan13(full), Text = full }
        };
    }

    private static List<bool> BuildEan13(string digits)
    {
        var modules = new List<bool>();
        var parity = Ean13Parity[digits[0] - '0'];

        AppendBits(modules, "101");

        for (var index = 1; index <= 6; index++)
        {
            var digit = digits[index] - '0';
            AppendBits(modules, parity[index - 1] == 'O' ? EanLeftOdd[digit] : EanLeftEven[digit]);
        }

        AppendBits(modules, "01010");

        for (var index = 7; index <= 12; index++)
        {
            AppendBits(modules, EanRight[digits[index] - '0']);
        }

        AppendBits(modules, "101");
        return modules;
    }

    private static List<bool> BuildEan8(string digits)
    {
        var modules = new List<bool>();

        AppendBits(modules, "101");

        for (var index = 0; index < 4; index++)
        {
            AppendBits(modules, EanLeftOdd[digits[index] - '0']);
        }

        AppendBits(modules, "01010");

        for (var index = 4; index < 8; index++)
        {
            AppendBits(modules, EanRight[digits[index] - '0']);
        }

        AppendBits(modules, "101");
        return modules;
    }

    // ---------- ITF ----------

    /// <summary>Five elements per digit: two wide and three narrow.</summary>
    private static readonly string[] ItfPatterns =
        ["nnwwn", "wnnnw", "nwnnw", "wwnnn", "nnwnw", "wnwnn", "nwwnn", "nnnww", "wnnwn", "nwnwn"];

    private static BarcodeSymbol Itf(string text)
    {
        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());

        if (digits.Length == 0)
        {
            return BarcodeSymbol.Failure("ITF encodes digits only.");
        }

        // Digits are interleaved in pairs, so an odd count gets a leading zero.
        if (digits.Length % 2 == 1)
        {
            digits = "0" + digits;
        }

        var modules = new List<bool>();
        AppendBits(modules, "1010");

        for (var index = 0; index < digits.Length; index += 2)
        {
            var bars = ItfPatterns[digits[index] - '0'];
            var spaces = ItfPatterns[digits[index + 1] - '0'];

            for (var element = 0; element < 5; element++)
            {
                Append(modules, true, bars[element] == 'w' ? 3 : 1);
                Append(modules, false, spaces[element] == 'w' ? 3 : 1);
            }
        }

        AppendBits(modules, "11101");
        return new BarcodeSymbol { Modules = modules, Text = digits };
    }

    // ---------- shared helpers ----------

    /// <summary>Appends alternating bars and spaces from a run-length string such as "212222".</summary>
    private static void AppendRunLengths(List<bool> modules, string pattern)
    {
        var isBar = true;

        foreach (var run in pattern)
        {
            Append(modules, isBar, run - '0');
            isBar = !isBar;
        }
    }

    /// <summary>Appends modules from a bit string, where '1' is a bar.</summary>
    private static void AppendBits(List<bool> modules, string bits)
    {
        foreach (var bit in bits)
        {
            modules.Add(bit == '1');
        }
    }

    private static void Append(List<bool> modules, bool isBar, int count)
    {
        for (var index = 0; index < count; index++)
        {
            modules.Add(isBar);
        }
    }

    /// <summary>
    /// Renders a symbol as SVG. Adjacent bar modules are merged into one rectangle: identical
    /// output, far fewer nodes.
    /// </summary>
    public static string ToSvg(BarcodeSymbol symbol, int moduleWidth, int height, int margin,
        string foreground, string background, bool showText, int fontSize)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        var textHeight = showText ? fontSize + 6 : 0;
        var width = (symbol.Modules.Count * moduleWidth) + (margin * 2);
        var totalHeight = height + (margin * 2) + textHeight;

        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture,
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{totalHeight}\" viewBox=\"0 0 {width} {totalHeight}\">");
        builder.Append(CultureInfo.InvariantCulture,
            $"<rect width=\"{width}\" height=\"{totalHeight}\" fill=\"{background}\"/>");

        var position = margin;
        var index = 0;

        while (index < symbol.Modules.Count)
        {
            if (!symbol.Modules[index])
            {
                index++;
                position += moduleWidth;
                continue;
            }

            var run = 0;
            while (index + run < symbol.Modules.Count && symbol.Modules[index + run])
            {
                run++;
            }

            builder.Append(CultureInfo.InvariantCulture,
                $"<rect x=\"{position}\" y=\"{margin}\" width=\"{run * moduleWidth}\" height=\"{height}\" fill=\"{foreground}\"/>");

            position += run * moduleWidth;
            index += run;
        }

        if (showText && symbol.Text.Length > 0)
        {
            builder.Append(CultureInfo.InvariantCulture,
                $"<text x=\"{width / 2}\" y=\"{height + margin + fontSize}\" fill=\"{foreground}\" font-family=\"monospace\" " +
                $"font-size=\"{fontSize}\" text-anchor=\"middle\">{HtmlEncoder.Default.Encode(symbol.Text)}</text>");
        }

        return builder.Append("</svg>").ToString();
    }
}
