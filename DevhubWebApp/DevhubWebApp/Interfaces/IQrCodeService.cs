using DevhubWebApp.Models.Tools.Qr;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// Renders QR codes. Behind an interface so tools depend on the capability rather than on the
/// encoding library.
/// </summary>
public interface IQrCodeService
{
    /// <summary>Renders a PNG. <paramref name="pixelsPerModule"/> controls the output size.</summary>
    byte[] RenderPng(string content, QrErrorCorrection correction, int pixelsPerModule, int marginModules,
        string foregroundHex, string backgroundHex);

    /// <summary>Renders scalable SVG markup, which is what belongs in print and in documents.</summary>
    string RenderSvg(string content, QrErrorCorrection correction, int pixelsPerModule, int marginModules,
        string foregroundHex, string backgroundHex);

    /// <summary>
    /// Longest content this configuration can encode. A QR code has a hard capacity, and exceeding
    /// it is a normal user mistake that must be reported before rendering fails.
    /// </summary>
    int GetCapacity(QrErrorCorrection correction);
}
