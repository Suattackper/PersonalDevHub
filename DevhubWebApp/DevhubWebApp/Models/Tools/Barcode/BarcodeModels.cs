namespace DevhubWebApp.Models.Tools.Barcode;

/// <summary>
/// Linear symbologies the generator supports.
/// <para>
/// Code 39, Codabar and UPC-E are deliberately absent: their pattern tables could not be verified
/// against a published specification in this environment, and a barcode that looks right but does
/// not scan is worse than one that is missing. They are tracked as future work.
/// </para>
/// </summary>
public enum Symbology
{
    /// <summary>Full printable ASCII, variable length. The general-purpose choice.</summary>
    Code128,

    /// <summary>13 digits including a check digit — retail products worldwide.</summary>
    Ean13,

    /// <summary>8 digits including a check digit — small retail packages.</summary>
    Ean8,

    /// <summary>12 digits including a check digit — retail in North America.</summary>
    UpcA,

    /// <summary>Interleaved 2 of 5: digits only, always an even count.</summary>
    Itf
}

/// <summary>An encoded symbol: the module pattern plus the text printed beneath it.</summary>
public sealed record BarcodeSymbol
{
    public static BarcodeSymbol Empty { get; } = new() { Modules = Array.Empty<bool>(), Text = string.Empty };

    /// <summary><c>true</c> is a bar, <c>false</c> a space. One entry per module.</summary>
    public required IReadOnlyList<bool> Modules { get; init; }

    /// <summary>Human-readable text, including any check digit the encoder added.</summary>
    public required string Text { get; init; }

    /// <summary>Why the input could not be encoded; <c>null</c> when it was.</summary>
    public string? Error { get; init; }

    public bool IsSuccess => Error is null && Modules.Count > 0;

    public static BarcodeSymbol Failure(string error) => new() { Modules = [], Text = string.Empty, Error = error };
}
