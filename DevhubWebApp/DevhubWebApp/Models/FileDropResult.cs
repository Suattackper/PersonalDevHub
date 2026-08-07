namespace DevhubWebApp.Models;

/// <summary>Payload produced by the file drop zone once a file has been read.</summary>
public sealed record FileDropResult
{
    public required string FileName { get; init; }

    public string ContentType { get; init; } = string.Empty;

    public long Size { get; init; }

    /// <summary>UTF-8 decoded content, or <c>null</c> when the zone reads binary.</summary>
    public string? Text { get; init; }

    /// <summary>Raw content, or <c>null</c> when the zone reads text.</summary>
    public byte[]? Bytes { get; init; }
}
