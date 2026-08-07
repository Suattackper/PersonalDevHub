namespace DevhubWebApp.Models.Tools.Hash;

/// <summary>Digest algorithms offered by the hash tool.</summary>
public enum HashAlgorithmKind
{
    /// <summary>Fast but cryptographically broken; kept for checksums and legacy interop.</summary>
    Md5,

    /// <summary>Deprecated for signatures; still common in Git and older systems.</summary>
    Sha1,

    Sha256,
    Sha384,
    Sha512
}

/// <summary>One computed digest.</summary>
public sealed record HashResult
{
    public required HashAlgorithmKind Algorithm { get; init; }

    /// <summary>Lower-case hexadecimal digest.</summary>
    public required string Hex { get; init; }

    /// <summary>Base64 form, which some APIs expect instead of hex.</summary>
    public required string Base64 { get; init; }

    /// <summary>Digest length in bits, e.g. 256 for SHA-256.</summary>
    public int Bits { get; init; }

    /// <summary>
    /// <c>true</c> when the algorithm is no longer considered safe against collisions and must not
    /// be used for signatures or password storage.
    /// </summary>
    public bool IsLegacy => Algorithm is HashAlgorithmKind.Md5 or HashAlgorithmKind.Sha1;
}

/// <summary>Outcome of comparing a computed digest against one the user supplied.</summary>
public sealed record HashVerification
{
    public static HashVerification None { get; } = new();

    /// <summary><c>true</c> when the expected value matches one of the computed digests.</summary>
    public bool IsMatch { get; init; }

    /// <summary>Algorithm the expected value matched, or was detected as.</summary>
    public HashAlgorithmKind? Algorithm { get; init; }

    /// <summary><c>true</c> once the user has entered something to compare against.</summary>
    public bool HasExpectedValue { get; init; }

    /// <summary>Why the comparison could not be made; <c>null</c> when it could.</summary>
    public string? Error { get; init; }
}
