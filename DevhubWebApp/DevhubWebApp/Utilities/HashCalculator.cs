using System.Security.Cryptography;
using System.Text;
using DevhubWebApp.Models.Tools.Hash;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Digest computation, detection and verification. Pure and side-effect free: it takes bytes and
/// returns values, which is what makes the whole tool testable without a browser.
/// </summary>
public static class HashCalculator
{
    /// <summary>Algorithms in the order the UI presents them: strongest first, legacy last.</summary>
    public static IReadOnlyList<HashAlgorithmKind> All { get; } =
    [
        HashAlgorithmKind.Sha256,
        HashAlgorithmKind.Sha512,
        HashAlgorithmKind.Sha384,
        HashAlgorithmKind.Sha1,
        HashAlgorithmKind.Md5
    ];

    /// <summary>Hex digest length per algorithm, used for detection.</summary>
    private static readonly Dictionary<int, HashAlgorithmKind> ByHexLength = new()
    {
        [32] = HashAlgorithmKind.Md5,
        [40] = HashAlgorithmKind.Sha1,
        [64] = HashAlgorithmKind.Sha256,
        [96] = HashAlgorithmKind.Sha384,
        [128] = HashAlgorithmKind.Sha512
    };

    /// <summary>Display name, e.g. <c>SHA-256</c>.</summary>
    public static string GetName(HashAlgorithmKind algorithm) => algorithm switch
    {
        HashAlgorithmKind.Md5 => "MD5",
        HashAlgorithmKind.Sha1 => "SHA-1",
        HashAlgorithmKind.Sha256 => "SHA-256",
        HashAlgorithmKind.Sha384 => "SHA-384",
        HashAlgorithmKind.Sha512 => "SHA-512",
        _ => algorithm.ToString().ToUpperInvariant()
    };

    /// <summary>Computes one digest over UTF-8 encoded text.</summary>
    public static HashResult ComputeText(HashAlgorithmKind algorithm, string? text)
        => Compute(algorithm, Encoding.UTF8.GetBytes(text ?? string.Empty));

    /// <summary>Computes one digest over raw bytes.</summary>
    public static HashResult Compute(HashAlgorithmKind algorithm, byte[]? bytes)
    {
        var payload = bytes ?? [];
        var digest = HashBytes(algorithm, payload);

        return new HashResult
        {
            Algorithm = algorithm,
            Hex = Convert.ToHexString(digest).ToLowerInvariant(),
            Base64 = Convert.ToBase64String(digest),
            Bits = digest.Length * 8
        };
    }

    /// <summary>Computes every supported digest in one pass over the input.</summary>
    public static IReadOnlyList<HashResult> ComputeAll(byte[]? bytes)
    {
        var payload = bytes ?? [];
        return All.Select(algorithm => Compute(algorithm, payload)).ToList();
    }

    /// <summary>Computes every supported digest over UTF-8 encoded text.</summary>
    public static IReadOnlyList<HashResult> ComputeAllText(string? text)
        => ComputeAll(Encoding.UTF8.GetBytes(text ?? string.Empty));

    /// <summary>
    /// Infers the algorithm from a hex digest's length. Length is the only signal a bare digest
    /// carries, so a value of an unknown length is reported as undetectable rather than guessed.
    /// </summary>
    public static HashAlgorithmKind? Detect(string? hash)
    {
        var normalised = Normalise(hash);

        if (normalised.Length == 0 || !IsHex(normalised))
        {
            return null;
        }

        return ByHexLength.TryGetValue(normalised.Length, out var algorithm) ? algorithm : null;
    }

    /// <summary>
    /// Checks an expected digest against the computed set. Comparison is case-insensitive and
    /// tolerant of the spaces and colons that tools like <c>certutil</c> and <c>sha256sum</c> emit.
    /// </summary>
    public static HashVerification Verify(IReadOnlyList<HashResult> computed, string? expected)
    {
        ArgumentNullException.ThrowIfNull(computed);

        var normalised = Normalise(expected);

        if (normalised.Length == 0)
        {
            return HashVerification.None;
        }

        var detected = Detect(normalised);

        if (detected is null)
        {
            return new HashVerification
            {
                HasExpectedValue = true,
                Error = "This does not look like a hex digest of a supported algorithm."
            };
        }

        var candidate = computed.FirstOrDefault(result => result.Algorithm == detected);

        return new HashVerification
        {
            HasExpectedValue = true,
            Algorithm = detected,
            IsMatch = candidate is not null && FixedTimeEquals(candidate.Hex, normalised)
        };
    }

    /// <summary>
    /// Constant-time comparison. A hash check is a security check, and timing-dependent string
    /// equality is exactly the pattern that leaks information in one.
    /// </summary>
    public static bool FixedTimeEquals(string? left, string? right)
    {
        if (left is null || right is null || left.Length != right.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left.ToLowerInvariant()),
            Encoding.ASCII.GetBytes(right.ToLowerInvariant()));
    }

    /// <summary>
    /// MD5 and SHA-1 are offered deliberately: users need them to match checksums produced by
    /// legacy systems. Both are surfaced in the UI as legacy, and neither is used by DevHub itself
    /// for anything security-related.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5350:Do Not Use Weak Cryptographic Algorithms",
        Justification = "SHA-1 is exposed as a tool output for legacy interop, not used to protect data.")]
    private static byte[] HashBytes(HashAlgorithmKind algorithm, byte[] payload) => algorithm switch
    {
        // System.Security.Cryptography.MD5 is unsupported on the browser runtime; see Md5Digest.
        HashAlgorithmKind.Md5 => Md5Digest.HashData(payload),
        HashAlgorithmKind.Sha1 => SHA1.HashData(payload),
        HashAlgorithmKind.Sha256 => SHA256.HashData(payload),
        HashAlgorithmKind.Sha384 => SHA384.HashData(payload),
        HashAlgorithmKind.Sha512 => SHA512.HashData(payload),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm), algorithm, "Unsupported hash algorithm.")
    };

    /// <summary>Strips the separators digest tools print, leaving bare hex.</summary>
    private static string Normalise(string? hash)
        => string.IsNullOrWhiteSpace(hash)
            ? string.Empty
            : new string(hash.Where(character => !char.IsWhiteSpace(character) && character != ':' && character != '-').ToArray())
                .ToLowerInvariant();

    private static bool IsHex(string value) => value.All(Uri.IsHexDigit);
}
