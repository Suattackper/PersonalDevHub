namespace DevhubWebApp.Models.Tools.Jwt;

/// <summary>Lifetime state of a token, derived from its <c>exp</c> and <c>nbf</c> claims.</summary>
public enum JwtValidity
{
    /// <summary>The token carries no time claims at all.</summary>
    Unknown,

    /// <summary>Currently within its validity window.</summary>
    Active,

    /// <summary>Past its <c>exp</c> claim.</summary>
    Expired,

    /// <summary>Not yet valid, per its <c>nbf</c> claim.</summary>
    NotYetValid
}

/// <summary>A decoded JWT. Decoding never verifies the signature — that needs the issuer's key.</summary>
public sealed record JwtToken
{
    public static JwtToken Empty { get; } = new();

    public bool IsValid => Error is null && !string.IsNullOrEmpty(HeaderJson);

    /// <summary>Why the token could not be decoded; <c>null</c> when it could.</summary>
    public string? Error { get; init; }

    /// <summary>Pretty-printed header JSON.</summary>
    public string HeaderJson { get; init; } = string.Empty;

    /// <summary>Pretty-printed payload JSON.</summary>
    public string PayloadJson { get; init; } = string.Empty;

    /// <summary>Raw Base64Url signature segment, exactly as it appeared in the token.</summary>
    public string Signature { get; init; } = string.Empty;

    /// <summary>The <c>alg</c> header claim, e.g. <c>HS256</c>.</summary>
    public string Algorithm { get; init; } = string.Empty;

    /// <summary>The <c>typ</c> header claim, usually <c>JWT</c>.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>The <c>kid</c> header claim, when present.</summary>
    public string KeyId { get; init; } = string.Empty;

    public string Issuer { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    /// <summary><c>iat</c> claim.</summary>
    public DateTimeOffset? IssuedAt { get; init; }

    /// <summary><c>nbf</c> claim.</summary>
    public DateTimeOffset? NotBefore { get; init; }

    /// <summary><c>exp</c> claim.</summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    public JwtValidity Validity { get; init; } = JwtValidity.Unknown;

    /// <summary>Token length in bytes; browsers and proxies impose header size limits.</summary>
    public int SizeInBytes { get; init; }

    /// <summary>
    /// <c>true</c> when the signature segment is empty, which means the token is unsigned
    /// (<c>alg: none</c>) and must never be trusted.
    /// </summary>
    public bool IsUnsigned => IsValid && string.IsNullOrEmpty(Signature);

    /// <summary>Time left before expiry, or <c>null</c> when the token has no <c>exp</c> claim.</summary>
    public TimeSpan? TimeRemaining => ExpiresAt is null ? null : ExpiresAt.Value - DateTimeOffset.UtcNow;
}
