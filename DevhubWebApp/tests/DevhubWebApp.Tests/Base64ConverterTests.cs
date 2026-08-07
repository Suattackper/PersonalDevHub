using System.Text;
using DevhubWebApp.Models.Tools.Base64;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Encoding, decoding and content sniffing rules of the Base64 toolkit.</summary>
public class Base64ConverterTests
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void EncodeText_ProducesStandardBase64()
        => Assert.Equal("SGVsbG8=", Base64Converter.EncodeText("Hello").Output);

    [Fact]
    public void EncodeText_HandlesNonAsciiViaUtf8()
    {
        var result = Base64Converter.EncodeText("chào bạn");

        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("chào bạn")), result.Output);
        Assert.Equal("chào bạn", Base64Converter.Decode(result.Output).Output);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EncodeText_ReturnsEmptyForBlankInput(string? input)
        => Assert.Equal(string.Empty, Base64Converter.EncodeText(input).Output);

    [Fact]
    public void Decode_ReturnsTextForTextPayloads()
    {
        var result = Base64Converter.Decode("SGVsbG8=");

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello", result.Output);
        Assert.Equal(BinaryKind.Text, result.Descriptor!.Kind);
    }

    [Fact]
    public void Decode_RejectsInvalidInput()
    {
        var result = Base64Converter.Decode("not base64!!!");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    /// <summary>Pasted Base64 often arrives without padding or in the URL-safe alphabet.</summary>
    [Theory]
    [InlineData("SGVsbG8")]
    [InlineData("SGVsbG8=")]
    public void Decode_RestoresMissingPadding(string input)
        => Assert.Equal("Hello", Base64Converter.Decode(input).Output);

    [Fact]
    public void Decode_AcceptsTheUrlSafeAlphabet()
    {
        var bytes = new byte[] { 0xFB, 0xFF, 0xBE };
        var urlSafe = Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        Assert.Equal(bytes, Base64Converter.Decode(urlSafe).Bytes);
    }

    [Fact]
    public void Decode_AcceptsWhitespaceAndLineBreaks()
        => Assert.Equal("Hello", Base64Converter.Decode("SGVs\nbG8=\r\n").Output);

    [Fact]
    public void Decode_UnwrapsDataUris()
    {
        var result = Base64Converter.Decode("data:text/plain;base64,SGVsbG8=");

        Assert.True(result.IsSuccess);
        Assert.Equal("Hello", result.Output);
    }

    [Fact]
    public void Decode_DetectsBinaryPayloadsAndLeavesTextEmpty()
    {
        var result = Base64Converter.Decode(Convert.ToBase64String(PngHeader));

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Output);
        Assert.Equal(BinaryKind.Png, result.Descriptor!.Kind);
        Assert.True(result.Descriptor.IsImage);
    }

    [Theory]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, BinaryKind.Png, "image/png")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF }, BinaryKind.Jpeg, "image/jpeg")]
    [InlineData(new byte[] { 0x50, 0x4B, 0x03, 0x04 }, BinaryKind.Zip, "application/zip")]
    public void Describe_IdentifiesKnownSignatures(byte[] bytes, BinaryKind kind, string mimeType)
    {
        var descriptor = Base64Converter.Describe(bytes);

        Assert.Equal(kind, descriptor.Kind);
        Assert.Equal(mimeType, descriptor.MimeType);
    }

    [Fact]
    public void Describe_IdentifiesPdfAndGifAndSvg()
    {
        Assert.Equal(BinaryKind.Pdf, Base64Converter.Describe("%PDF-1.7"u8.ToArray()).Kind);
        Assert.Equal(BinaryKind.Gif, Base64Converter.Describe("GIF89a"u8.ToArray()).Kind);
        Assert.Equal(BinaryKind.Svg, Base64Converter.Describe("<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray()).Kind);
    }

    [Fact]
    public void Describe_FallsBackToUnknownForUnrecognisedBinary()
        => Assert.Equal(BinaryKind.Unknown, Base64Converter.Describe([0x00, 0x01, 0x02, 0x7F, 0x05]).Kind);

    [Fact]
    public void EncodeBytes_CanProduceADataUri()
    {
        var result = Base64Converter.EncodeBytes(PngHeader, "image/png", asDataUri: true);

        Assert.StartsWith("data:image/png;base64,", result.Output, StringComparison.Ordinal);
        Assert.Equal(BinaryKind.Png, result.Descriptor!.Kind);
    }

    [Fact]
    public void EncodeBytes_ReturnsEmptyForNoInput()
        => Assert.Equal(string.Empty, Base64Converter.EncodeBytes([]).Output);

    [Fact]
    public void TryParseDataUri_SplitsMimeTypeAndPayload()
    {
        Assert.True(Base64Converter.TryParseDataUri("data:image/png;base64,AAAA", out var parsed));
        Assert.Equal("image/png", parsed!.MimeType);
        Assert.Equal("AAAA", parsed.Base64);

        Assert.False(Base64Converter.TryParseDataUri("https://example.com", out _));
        Assert.False(Base64Converter.TryParseDataUri("data:image/png,notbase64", out _));
    }

    [Fact]
    public void ToDataUri_RoundTripsThroughDecode()
    {
        var uri = Base64Converter.ToDataUri(PngHeader, "image/png");

        Assert.Equal(PngHeader, Base64Converter.Decode(uri).Bytes);
    }

    [Theory]
    [InlineData(512, "512 B")]
    [InlineData(2048, "2 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(5_242_880, "5 MB")]
    public void FormatSize_UsesTheLargestSensibleUnit(long bytes, string expected)
        => Assert.Equal(expected, Base64Converter.FormatSize(bytes));
}
