using DevhubWebApp.Models.Tools.Barcode;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>
/// Encoding rules of the linear barcode symbologies. Check-digit arithmetic and structural
/// invariants are pinned here â€” a transcription error in a pattern table is the realistic failure
/// mode for this kind of code, and these tests are what catch it.
/// </summary>
public class BarcodeEncoderTests
{
    [Theory]
    [InlineData("590123412345", 7)]
    [InlineData("400638133393", 1)]
    [InlineData("03600029145", 2)]
    public void CalculateModulo10_MatchesPublishedCheckDigits(string digits, int expected)
        => Assert.Equal(expected, BarcodeEncoder.CalculateModulo10(digits));

    [Fact]
    public void Ean13_AppendsTheCheckDigitAndEncodes95Modules()
    {
        var symbol = BarcodeEncoder.Encode(Symbology.Ean13, "590123412345");

        Assert.True(symbol.IsSuccess);
        Assert.Equal("5901234123457", symbol.Text);

        // EAN-13 is always 95 modules: 3 + 42 + 5 + 42 + 3.
        Assert.Equal(95, symbol.Modules.Count);
    }

    [Fact]
    public void Ean13_AcceptsACodeThatAlreadyCarriesItsCheckDigit()
        => Assert.Equal("5901234123457", BarcodeEncoder.Encode(Symbology.Ean13, "5901234123457").Text);

    [Fact]
    public void Ean8_Encodes67Modules()
    {
        var symbol = BarcodeEncoder.Encode(Symbology.Ean8, "9638507");

        Assert.True(symbol.IsSuccess);
        Assert.Equal(8, symbol.Text.Length);
        Assert.Equal(67, symbol.Modules.Count);
    }

    [Fact]
    public void UpcA_IsEncodedAsAZeroPrefixedEan13()
    {
        var symbol = BarcodeEncoder.Encode(Symbology.UpcA, "03600029145");

        Assert.True(symbol.IsSuccess);
        Assert.Equal("036000291452", symbol.Text);
        Assert.Equal(95, symbol.Modules.Count);
    }

    [Theory]
    [InlineData(Symbology.Ean13, "123")]
    [InlineData(Symbology.Ean8, "12")]
    [InlineData(Symbology.UpcA, "1")]
    public void EanFamily_ReportsAWrongDigitCount(Symbology symbology, string data)
    {
        var symbol = BarcodeEncoder.Encode(symbology, data);

        Assert.False(symbol.IsSuccess);
        Assert.NotNull(symbol.Error);
    }

    [Fact]
    public void Code128_EncodesStartDataChecksumAndStop()
    {
        var symbol = BarcodeEncoder.Encode(Symbology.Code128, "DevHub");

        Assert.True(symbol.IsSuccess);

        // Start + 6 data + checksum are 11 modules each; the stop pattern is 13.
        Assert.Equal((8 * 11) + 13, symbol.Modules.Count);
        Assert.True(symbol.Modules[0]);
        Assert.True(symbol.Modules[^1]);
    }

    [Fact]
    public void Code128_RejectsCharactersOutsidePrintableAscii()
        => Assert.False(BarcodeEncoder.Encode(Symbology.Code128, "cafÃ©").IsSuccess);

    /// <summary>
    /// Every printable ASCII character must encode, and each symbol must contribute exactly eleven
    /// modules. That width is the structural invariant a mistyped table entry breaks.
    /// </summary>
    [Fact]
    public void Code128_EncodesEveryPrintableAsciiCharacterAtElevenModules()
    {
        // Starts at '!': input is trimmed, so a lone space is treated as empty input by design.
        for (var character = '!'; character <= '~'; character++)
        {
            var symbol = BarcodeEncoder.Encode(Symbology.Code128, character.ToString());

            Assert.True(symbol.IsSuccess, $"'{character}' failed to encode.");

            // Start + one data symbol + checksum at 11 modules each, plus the 13-module stop.
            Assert.Equal((3 * 11) + 13, symbol.Modules.Count);
        }
    }

    [Fact]
    public void Itf_PadsAnOddDigitCountAndEncodesInPairs()
    {
        var symbol = BarcodeEncoder.Encode(Symbology.Itf, "1234567");

        Assert.True(symbol.IsSuccess);
        Assert.Equal("01234567", symbol.Text);

        // 4 start + 8 digits Ã— 9 modules per interleaved pair-half + 5 stop.
        Assert.Equal(4 + (4 * 18) + 5, symbol.Modules.Count);
    }

    [Fact]
    public void Itf_RejectsNonDigits()
        => Assert.False(BarcodeEncoder.Encode(Symbology.Itf, "abcd").IsSuccess);

    [Fact]
    public void Encode_ReturnsEmptyForBlankInput()
        => Assert.Same(BarcodeSymbol.Empty, BarcodeEncoder.Encode(Symbology.Code128, "   "));

    /// <summary>Every symbology must produce a pattern that starts and ends with a bar.</summary>
    [Theory]
    [InlineData(Symbology.Code128, "DevHub")]
    [InlineData(Symbology.Ean13, "590123412345")]
    [InlineData(Symbology.Ean8, "9638507")]
    [InlineData(Symbology.UpcA, "03600029145")]
    [InlineData(Symbology.Itf, "12345678")]
    public void EverySymbology_StartsAndEndsWithABar(Symbology symbology, string data)
    {
        var symbol = BarcodeEncoder.Encode(symbology, data);

        Assert.True(symbol.IsSuccess);
        Assert.True(symbol.Modules[0]);
        Assert.True(symbol.Modules.Last(module => module));
    }

    [Fact]
    public void ToSvg_ProducesScalableMarkupWithTheHumanReadableText()
    {
        var symbol = BarcodeEncoder.Encode(Symbology.Ean13, "590123412345");

        var svg = BarcodeEncoder.ToSvg(symbol, moduleWidth: 2, height: 80, margin: 10,
            foreground: "#000000", background: "#ffffff", showText: true, fontSize: 14);

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("<rect", svg, StringComparison.Ordinal);
        Assert.Contains("5901234123457", svg, StringComparison.Ordinal);
        Assert.EndsWith("</svg>", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void GetNameAndRequirement_DescribeEverySymbology()
        => Assert.All(BarcodeEncoder.All, symbology =>
        {
            Assert.False(string.IsNullOrWhiteSpace(BarcodeEncoder.GetName(symbology)));
            Assert.False(string.IsNullOrWhiteSpace(BarcodeEncoder.GetRequirement(symbology)));
        });
}


