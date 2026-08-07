using System.Text;
using System.Text.Json;
using DevhubWebApp.Models.Tools.Jwt;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Decodes JSON Web Tokens for inspection.
/// <para>
/// It reads a token; it does not verify one. Verifying a signature requires the issuer's key,
/// which a client-side tool has no business holding — so the result always reports what the token
/// <em>claims</em>, never that the token is trustworthy.
/// </para>
/// </summary>
public static class JwtDecoder
{
    private const string ClaimAlgorithm = "alg";
    private const string ClaimType = "typ";
    private const string ClaimKeyId = "kid";
    private const string ClaimIssuer = "iss";
    private const string ClaimSubject = "sub";
    private const string ClaimAudience = "aud";
    private const string ClaimIssuedAt = "iat";
    private const string ClaimNotBefore = "nbf";
    private const string ClaimExpiration = "exp";

    private static readonly JsonSerializerOptions PrettyOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Decodes a token. Malformed input is reported as an error value, not an exception.</summary>
    public static JwtToken Decode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return JwtToken.Empty;
        }

        var trimmed = StripBearerPrefix(token.Trim());
        var segments = trimmed.Split('.');

        if (segments.Length is < 2 or > 3)
        {
            return Failure("A JWT has three dot-separated segments: header.payload.signature.", trimmed);
        }

        if (!TryDecodeSegment(segments[0], out var headerJson))
        {
            return Failure("The header segment is not valid Base64Url-encoded JSON.", trimmed);
        }

        if (!TryDecodeSegment(segments[1], out var payloadJson))
        {
            return Failure("The payload segment is not valid Base64Url-encoded JSON.", trimmed);
        }

        using var header = JsonDocument.Parse(headerJson);
        using var payload = JsonDocument.Parse(payloadJson);

        var expiresAt = ReadUnixTime(payload.RootElement, ClaimExpiration);
        var notBefore = ReadUnixTime(payload.RootElement, ClaimNotBefore);

        return new JwtToken
        {
            HeaderJson = Prettify(header.RootElement),
            PayloadJson = Prettify(payload.RootElement),
            Signature = segments.Length == 3 ? segments[2] : string.Empty,
            Algorithm = ReadString(header.RootElement, ClaimAlgorithm),
            Type = ReadString(header.RootElement, ClaimType),
            KeyId = ReadString(header.RootElement, ClaimKeyId),
            Issuer = ReadString(payload.RootElement, ClaimIssuer),
            Subject = ReadString(payload.RootElement, ClaimSubject),
            Audience = ReadAudience(payload.RootElement),
            IssuedAt = ReadUnixTime(payload.RootElement, ClaimIssuedAt),
            NotBefore = notBefore,
            ExpiresAt = expiresAt,
            Validity = Evaluate(notBefore, expiresAt),
            SizeInBytes = Encoding.UTF8.GetByteCount(trimmed)
        };
    }

    /// <summary>Determines the lifetime state at the current instant.</summary>
    public static JwtValidity Evaluate(DateTimeOffset? notBefore, DateTimeOffset? expiresAt)
    {
        var now = DateTimeOffset.UtcNow;

        if (expiresAt is not null && expiresAt <= now)
        {
            return JwtValidity.Expired;
        }

        if (notBefore is not null && notBefore > now)
        {
            return JwtValidity.NotYetValid;
        }

        return expiresAt is null && notBefore is null ? JwtValidity.Unknown : JwtValidity.Active;
    }

    /// <summary>Tokens are routinely pasted straight from an Authorization header.</summary>
    private static string StripBearerPrefix(string token)
        => token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? token[7..].Trim() : token;

    /// <summary>Base64Url differs from Base64 in two characters and in dropping padding.</summary>
    private static bool TryDecodeSegment(string segment, out string json)
    {
        json = string.Empty;

        if (string.IsNullOrWhiteSpace(segment))
        {
            return false;
        }

        var normalised = segment.Replace('-', '+').Replace('_', '/');
        normalised = (normalised.Length % 4) switch
        {
            2 => normalised + "==",
            3 => normalised + "=",
            1 => string.Empty,
            _ => normalised
        };

        if (normalised.Length == 0)
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromBase64String(normalised);
            var text = Encoding.UTF8.GetString(bytes);

            using var document = JsonDocument.Parse(text);
            json = text;
            return true;
        }
        catch (Exception exception) when (exception is FormatException or JsonException or DecoderFallbackException)
        {
            return false;
        }
    }

    private static JwtToken Failure(string error, string token) => new()
    {
        Error = error,
        SizeInBytes = Encoding.UTF8.GetByteCount(token)
    };

    private static string Prettify(JsonElement element) => JsonSerializer.Serialize(element, PrettyOptions);

    private static string ReadString(JsonElement element, string property)
        => element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;

    /// <summary><c>aud</c> may be a single string or an array; both are rendered as one line.</summary>
    private static string ReadAudience(JsonElement payload)
    {
        if (!payload.TryGetProperty(ClaimAudience, out var value))
        {
            return string.Empty;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join(", ", value.EnumerateArray().Select(item => item.ToString())),
            _ => string.Empty
        };
    }

    /// <summary>Reads a NumericDate claim, tolerating issuers that emit it as a string.</summary>
    private static DateTimeOffset? ReadUnixTime(JsonElement payload, string property)
    {
        if (!payload.TryGetProperty(property, out var value))
        {
            return null;
        }

        long? seconds = value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) => number,
            JsonValueKind.String when long.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };

        return seconds is null ? null : DateTimeOffset.FromUnixTimeSeconds(seconds.Value);
    }
}
