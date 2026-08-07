using DevhubWebApp.Models.Tools.Qr;
using DevhubWebApp.Services;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Payload construction rules — the part a scanner is strict about.</summary>
public class QrPayloadBuilderTests
{
    [Fact]
    public void Text_IsEncodedVerbatim()
        => Assert.Equal("hello", QrPayloadBuilder.Build(new QrPayload { Text = "hello" }));

    [Theory]
    [InlineData("example.com", "https://example.com")]
    [InlineData("https://example.com", "https://example.com")]
    [InlineData("ftp://files.example.com", "ftp://files.example.com")]
    public void Url_AddsAMissingScheme(string input, string expected)
        => Assert.Equal(expected, QrPayloadBuilder.Build(new QrPayload { Kind = QrPayloadKind.Url, Text = input }));

    [Fact]
    public void Email_BuildsAMailtoWithEncodedSubjectAndBody()
    {
        var payload = new QrPayload
        {
            Kind = QrPayloadKind.Email,
            Email = "dev@example.com",
            Subject = "hello there",
            Body = "a & b"
        };

        Assert.Equal("mailto:dev@example.com?subject=hello%20there&body=a%20%26%20b", QrPayloadBuilder.Build(payload));
    }

    [Fact]
    public void Phone_AndSms_UseTheirSchemes()
    {
        Assert.Equal("tel:+84901234567",
            QrPayloadBuilder.Build(new QrPayload { Kind = QrPayloadKind.Phone, Phone = " +84901234567 " }));

        Assert.Equal("SMSTO:+84901234567?body=ping",
            QrPayloadBuilder.Build(new QrPayload { Kind = QrPayloadKind.Sms, Phone = "+84901234567", Body = "ping" }));
    }

    [Fact]
    public void WiFi_BuildsTheStandardPayload()
    {
        var payload = new QrPayload
        {
            Kind = QrPayloadKind.WiFi,
            Ssid = "DevHub",
            Password = "secret",
            Security = WiFiSecurity.Wpa,
            HiddenNetwork = true
        };

        Assert.Equal("WIFI:T:WPA;S:DevHub;P:secret;H:true;;", QrPayloadBuilder.Build(payload));
    }

    /// <summary>Unescaped separators truncate the field, so a scanner would read the wrong SSID.</summary>
    [Fact]
    public void WiFi_EscapesSeparatorsInTheSsidAndPassword()
    {
        var payload = new QrPayload
        {
            Kind = QrPayloadKind.WiFi,
            Ssid = "My;Net",
            Password = @"pa:ss\word",
            Security = WiFiSecurity.Wpa
        };

        Assert.Equal(@"WIFI:T:WPA;S:My\;Net;P:pa\:ss\\word;;", QrPayloadBuilder.Build(payload));
    }

    [Fact]
    public void WiFi_OmitsThePasswordOnAnOpenNetwork()
    {
        var payload = new QrPayload
        {
            Kind = QrPayloadKind.WiFi,
            Ssid = "Guest",
            Password = "ignored",
            Security = WiFiSecurity.None
        };

        Assert.Equal("WIFI:T:nopass;S:Guest;;", QrPayloadBuilder.Build(payload));
    }

    [Fact]
    public void VCard_BuildsAValidCardAndSkipsEmptyFields()
    {
        var payload = new QrPayload
        {
            Kind = QrPayloadKind.VCard,
            FirstName = "Tan",
            LastName = "Nguyen",
            Organisation = "DevHub",
            Email = "tan@example.com"
        };

        var vcard = QrPayloadBuilder.Build(payload);

        Assert.StartsWith("BEGIN:VCARD", vcard, StringComparison.Ordinal);
        Assert.EndsWith("END:VCARD", vcard, StringComparison.Ordinal);
        Assert.Contains("N:Nguyen;Tan", vcard, StringComparison.Ordinal);
        Assert.Contains("FN:Tan Nguyen", vcard, StringComparison.Ordinal);
        Assert.Contains("ORG:DevHub", vcard, StringComparison.Ordinal);
        Assert.DoesNotContain("TITLE:", vcard, StringComparison.Ordinal);
    }

    [Fact]
    public void Location_BuildsAGeoUri()
        => Assert.Equal("geo:10.762622,106.660172",
            QrPayloadBuilder.Build(new QrPayload { Kind = QrPayloadKind.Location, Latitude = "10.762622", Longitude = "106.660172" }));

    [Theory]
    [InlineData(QrPayloadKind.Email)]
    [InlineData(QrPayloadKind.WiFi)]
    [InlineData(QrPayloadKind.VCard)]
    [InlineData(QrPayloadKind.Location)]
    public void IncompletePayloads_ProduceNothingRatherThanABrokenCode(QrPayloadKind kind)
        => Assert.Empty(QrPayloadBuilder.Build(new QrPayload { Kind = kind }));

    [Fact]
    public void Build_RejectsANullPayload()
        => Assert.Throws<ArgumentNullException>(() => QrPayloadBuilder.Build(null!));
}

/// <summary>Rendering guarantees of the QR service.</summary>
public class QrCodeServiceTests
{
    private readonly QrCodeService _service = new();

    [Fact]
    public void RenderPng_ProducesAPngSignature()
    {
        var png = _service.RenderPng("https://example.com", QrErrorCorrection.Medium, 10, 4, "#000000", "#ffffff");

        Assert.True(png.Length > 100);
        Assert.Equal([0x89, 0x50, 0x4E, 0x47], png.Take(4));
    }

    [Fact]
    public void RenderSvg_ProducesScalableMarkup()
    {
        var svg = _service.RenderSvg("https://example.com", QrErrorCorrection.Medium, 10, 4, "#000000", "#ffffff");

        Assert.StartsWith("<svg", svg, StringComparison.Ordinal);
        Assert.Contains("viewBox", svg, StringComparison.Ordinal);
    }

    [Fact]
    public void HigherCorrectionLevels_ProduceDenserCodes()
    {
        var low = _service.RenderPng("DevHub", QrErrorCorrection.Low, 5, 4, "#000", "#fff");
        var high = _service.RenderPng("DevHub", QrErrorCorrection.High, 5, 4, "#000", "#fff");

        Assert.NotEqual(low, high);
    }

    [Fact]
    public void RenderPng_AcceptsShorthandAndMalformedColoursWithoutFailing()
    {
        Assert.NotEmpty(_service.RenderPng("DevHub", QrErrorCorrection.Low, 5, 4, "#0d1", "#fff"));
        Assert.NotEmpty(_service.RenderPng("DevHub", QrErrorCorrection.Low, 5, 4, "not-a-colour", string.Empty));
    }

    [Fact]
    public void Render_RejectsEmptyContent()
        => Assert.Throws<ArgumentException>(() => _service.RenderPng(string.Empty, QrErrorCorrection.Low, 5, 4, "#000", "#fff"));

    [Fact]
    public void GetCapacity_FallsAsCorrectionRises()
    {
        Assert.True(_service.GetCapacity(QrErrorCorrection.Low) > _service.GetCapacity(QrErrorCorrection.Medium));
        Assert.True(_service.GetCapacity(QrErrorCorrection.Medium) > _service.GetCapacity(QrErrorCorrection.High));
    }
}
