namespace DevhubWebApp.Models.Tools.Base64;

/// <summary>Kind of payload detected inside a decoded Base64 blob.</summary>
public enum BinaryKind
{
    /// <summary>Decodes cleanly as UTF-8 text.</summary>
    Text,

    Png,
    Jpeg,
    Gif,
    Webp,
    Svg,
    Pdf,
    Zip,

    /// <summary>Recognisably binary, but of no known type.</summary>
    Unknown
}

/// <summary>What a decoded blob turned out to contain.</summary>
public sealed record BinaryDescriptor
{
    public required BinaryKind Kind { get; init; }

    /// <summary>Best-guess MIME type, e.g. <c>image/png</c>.</summary>
    public required string MimeType { get; init; }

    /// <summary>Conventional extension without the dot, e.g. <c>png</c>.</summary>
    public required string Extension { get; init; }

    /// <summary>Images can be shown directly in a preview panel.</summary>
    public bool IsImage => Kind is BinaryKind.Png or BinaryKind.Jpeg or BinaryKind.Gif or BinaryKind.Webp or BinaryKind.Svg;

    public bool IsText => Kind == BinaryKind.Text;
}

/// <summary>Outcome of a Base64 operation. Invalid input is a value, not an exception.</summary>
public sealed record Base64Result
{
    public bool IsSuccess => Error is null;

    /// <summary>Base64 text, or decoded text, depending on the direction.</summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>Decoded bytes. Empty when encoding or when the input failed to decode.</summary>
    public byte[] Bytes { get; init; } = Array.Empty<byte>();

    /// <summary>What the decoded bytes contain. <c>null</c> when encoding.</summary>
    public BinaryDescriptor? Descriptor { get; init; }

    public string? Error { get; init; }

    public static Base64Result Failure(string error) => new() { Error = error };
}

/// <summary>A Base64 data URI split into its parts.</summary>
public sealed record DataUri
{
    public required string MimeType { get; init; }

    public required string Base64 { get; init; }
}
