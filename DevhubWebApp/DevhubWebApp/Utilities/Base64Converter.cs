using System.Text;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Base64;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Base64 encoding, decoding and content sniffing. Pure: no interop, no services, so every rule
/// here is directly testable.
/// </summary>
public static class Base64Converter
{
    private const string DataUriPrefix = "data:";
    private const string Base64Marker = ";base64,";

    /// <summary>UTF-8 that reports invalid byte sequences instead of silently substituting them.</summary>
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Encodes UTF-8 text.</summary>
    public static Base64Result EncodeText(string? text)
        => string.IsNullOrEmpty(text)
            ? new Base64Result()
            : new Base64Result { Output = Convert.ToBase64String(Encoding.UTF8.GetBytes(text)) };

    /// <summary>Encodes raw bytes, optionally as a data URI ready to paste into HTML or CSS.</summary>
    public static Base64Result EncodeBytes(byte[]? bytes, string? mimeType = null, bool asDataUri = false)
    {
        if (bytes is null or { Length: 0 })
        {
            return new Base64Result();
        }

        var base64 = Convert.ToBase64String(bytes);
        var output = asDataUri
            ? $"{DataUriPrefix}{mimeType.OrDefaultMime()}{Base64Marker}{base64}"
            : base64;

        return new Base64Result { Output = output, Bytes = bytes, Descriptor = Describe(bytes) };
    }

    /// <summary>
    /// Decodes Base64, accepting data URIs, URL-safe alphabets and missing padding — all forms a
    /// user is likely to paste. The decoded bytes are described so the UI can preview them.
    /// </summary>
    public static Base64Result Decode(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new Base64Result();
        }

        var payload = TryParseDataUri(input, out var dataUri) ? dataUri!.Base64 : input;

        if (!TryDecode(payload, out var bytes))
        {
            return Base64Result.Failure("Input is not valid Base64.");
        }

        var descriptor = Describe(bytes);

        return new Base64Result
        {
            Bytes = bytes,
            Descriptor = descriptor,
            Output = descriptor.IsText ? StrictUtf8.GetString(bytes) : string.Empty
        };
    }

    /// <summary>Splits a <c>data:</c> URI into MIME type and payload.</summary>
    public static bool TryParseDataUri(string? value, out DataUri? dataUri)
    {
        dataUri = null;

        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(DataUriPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var markerIndex = value.IndexOf(Base64Marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        var mimeType = value[DataUriPrefix.Length..markerIndex];

        dataUri = new DataUri
        {
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? MimeTypes.PlainText : mimeType,
            Base64 = value[(markerIndex + Base64Marker.Length)..]
        };

        return true;
    }

    /// <summary>Builds a data URI from raw bytes, for previewing or embedding.</summary>
    public static string ToDataUri(byte[] bytes, string mimeType)
    {
        ArgumentNullException.ThrowIfNull(bytes);

        return $"{DataUriPrefix}{mimeType.OrDefaultMime()}{Base64Marker}{Convert.ToBase64String(bytes)}";
    }

    /// <summary>
    /// Identifies the payload from its magic bytes, falling back to a strict UTF-8 decode. Sniffing
    /// beats trusting a declared MIME type: the bytes are the only thing actually present.
    /// </summary>
    public static BinaryDescriptor Describe(byte[]? bytes)
    {
        if (bytes is null or { Length: 0 })
        {
            return new BinaryDescriptor { Kind = BinaryKind.Text, MimeType = MimeTypes.PlainText, Extension = "txt" };
        }

        if (StartsWith(bytes, [0x89, 0x50, 0x4E, 0x47]))
        {
            return new BinaryDescriptor { Kind = BinaryKind.Png, MimeType = MimeTypes.Png, Extension = "png" };
        }

        if (StartsWith(bytes, [0xFF, 0xD8, 0xFF]))
        {
            return new BinaryDescriptor { Kind = BinaryKind.Jpeg, MimeType = "image/jpeg", Extension = "jpg" };
        }

        if (StartsWith(bytes, "GIF8"u8))
        {
            return new BinaryDescriptor { Kind = BinaryKind.Gif, MimeType = "image/gif", Extension = "gif" };
        }

        if (bytes.Length > 12 && StartsWith(bytes, "RIFF"u8) && StartsWith(bytes.AsSpan(8), "WEBP"u8))
        {
            return new BinaryDescriptor { Kind = BinaryKind.Webp, MimeType = "image/webp", Extension = "webp" };
        }

        if (StartsWith(bytes, "%PDF"u8))
        {
            return new BinaryDescriptor { Kind = BinaryKind.Pdf, MimeType = "application/pdf", Extension = "pdf" };
        }

        if (StartsWith(bytes, [0x50, 0x4B, 0x03, 0x04]))
        {
            return new BinaryDescriptor { Kind = BinaryKind.Zip, MimeType = "application/zip", Extension = "zip" };
        }

        if (TryDecodeText(bytes, out var text))
        {
            return text.TrimStart().StartsWith("<svg", StringComparison.OrdinalIgnoreCase)
                ? new BinaryDescriptor { Kind = BinaryKind.Svg, MimeType = MimeTypes.Svg, Extension = "svg" }
                : new BinaryDescriptor { Kind = BinaryKind.Text, MimeType = MimeTypes.PlainText, Extension = "txt" };
        }

        return new BinaryDescriptor { Kind = BinaryKind.Unknown, MimeType = MimeTypes.OctetStream, Extension = "bin" };
    }

    /// <summary>Human-readable byte size, e.g. "1.4 KB".</summary>
    public static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;

        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} {units[unit]}"
            : $"{size:0.#} {units[unit]}";
    }

    /// <summary>Accepts the URL-safe alphabet and restores stripped padding before decoding.</summary>
    private static bool TryDecode(string payload, out byte[] bytes)
    {
        var normalised = payload
            .Trim()
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace(' ', '+')
            .Replace('-', '+')
            .Replace('_', '/');

        var padding = normalised.Length % 4;
        if (padding is 2 or 3)
        {
            normalised = normalised.PadRight(normalised.Length + (4 - padding), '=');
        }
        else if (padding == 1)
        {
            bytes = [];
            return false;
        }

        return Convert.TryFromBase64String(normalised, new byte[normalised.Length], out _)
            ? TryConvert(normalised, out bytes)
            : Fail(out bytes);
    }

    private static bool TryConvert(string normalised, out byte[] bytes)
    {
        bytes = Convert.FromBase64String(normalised);
        return true;
    }

    private static bool Fail(out byte[] bytes)
    {
        bytes = [];
        return false;
    }

    private static bool TryDecodeText(byte[] bytes, out string text)
    {
        try
        {
            text = StrictUtf8.GetString(bytes);

            // Control characters other than tab, newline and carriage return mean binary content.
            return !text.Any(character => char.IsControl(character) && character is not '\t' and not '\n' and not '\r');
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static bool StartsWith(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> signature)
        => bytes.Length >= signature.Length && bytes[..signature.Length].SequenceEqual(signature);

    private static string OrDefaultMime(this string? mimeType)
        => string.IsNullOrWhiteSpace(mimeType) ? MimeTypes.OctetStream : mimeType;
}
