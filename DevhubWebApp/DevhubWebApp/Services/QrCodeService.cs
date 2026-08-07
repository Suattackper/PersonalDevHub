using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Qr;
using QRCoder;

namespace DevhubWebApp.Services;

/// <summary>
/// QRCoder-backed <see cref="IQrCodeService"/>. Only the managed renderers are used
/// (<see cref="PngByteQRCode"/> and <see cref="SvgQRCode"/>); the bitmap renderers depend on
/// System.Drawing, which does not exist on the browser runtime.
/// </summary>
public sealed class QrCodeService : IQrCodeService
{
    /// <summary>
    /// Version 40 at each correction level, in alphanumeric mode. Used to warn before encoding
    /// rather than letting the generator throw.
    /// </summary>
    private static readonly Dictionary<QrErrorCorrection, int> Capacities = new()
    {
        [QrErrorCorrection.Low] = 4296,
        [QrErrorCorrection.Medium] = 3391,
        [QrErrorCorrection.Quartile] = 2420,
        [QrErrorCorrection.High] = 1852
    };

    public byte[] RenderPng(string content, QrErrorCorrection correction, int pixelsPerModule, int marginModules,
        string foregroundHex, string backgroundHex)
    {
        using var data = Generate(content, correction);
        var png = new PngByteQRCode(data);

        return png.GetGraphic(
            Math.Clamp(pixelsPerModule, 1, 40),
            ToRgb(foregroundHex, [0, 0, 0]),
            ToRgb(backgroundHex, [255, 255, 255]),
            drawQuietZones: marginModules > 0);
    }

    public string RenderSvg(string content, QrErrorCorrection correction, int pixelsPerModule, int marginModules,
        string foregroundHex, string backgroundHex)
    {
        using var data = Generate(content, correction);
        var svg = new SvgQRCode(data);

        return svg.GetGraphic(
            Math.Clamp(pixelsPerModule, 1, 40),
            foregroundHex,
            backgroundHex,
            drawQuietZones: marginModules > 0);
    }

    public int GetCapacity(QrErrorCorrection correction) => Capacities[correction];

    private static QRCodeData Generate(string content, QrErrorCorrection correction)
    {
        ArgumentException.ThrowIfNullOrEmpty(content);

        using var generator = new QRCodeGenerator();
        return generator.CreateQrCode(content, Map(correction));
    }

    private static QRCodeGenerator.ECCLevel Map(QrErrorCorrection correction) => correction switch
    {
        QrErrorCorrection.Low => QRCodeGenerator.ECCLevel.L,
        QrErrorCorrection.Quartile => QRCodeGenerator.ECCLevel.Q,
        QrErrorCorrection.High => QRCodeGenerator.ECCLevel.H,
        _ => QRCodeGenerator.ECCLevel.M
    };

    /// <summary>Parses <c>#rrggbb</c>, falling back rather than failing on malformed input.</summary>
    private static byte[] ToRgb(string? hex, byte[] fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        var text = hex.TrimStart('#');

        if (text.Length == 3)
        {
            text = string.Concat(text[0], text[0], text[1], text[1], text[2], text[2]);
        }

        if (text.Length != 6 || !text.All(Uri.IsHexDigit))
        {
            return fallback;
        }

        return
        [
            System.Convert.ToByte(text[..2], 16),
            System.Convert.ToByte(text.Substring(2, 2), 16),
            System.Convert.ToByte(text.Substring(4, 2), 16)
        ];
    }
}
