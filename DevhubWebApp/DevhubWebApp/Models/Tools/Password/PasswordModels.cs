namespace DevhubWebApp.Models.Tools.Password;

/// <summary>Kind of secret to produce.</summary>
public enum PasswordKind
{
    /// <summary>Uniformly random characters from the selected sets.</summary>
    Random,

    /// <summary>Alternating consonant/vowel syllables — weaker, but typable from memory.</summary>
    Pronounceable,

    /// <summary>Several dictionary words joined by a separator.</summary>
    Passphrase
}

/// <summary>Qualitative strength band derived from entropy.</summary>
public enum PasswordStrength
{
    VeryWeak,
    Weak,
    Fair,
    Strong,
    VeryStrong
}

/// <summary>Everything that shapes a generated secret.</summary>
public sealed record PasswordOptions
{
    public static PasswordOptions Default { get; } = new();

    public PasswordKind Kind { get; init; } = PasswordKind.Random;

    /// <summary>Character count for random and pronounceable secrets.</summary>
    public int Length { get; init; } = 20;

    public bool IncludeUppercase { get; init; } = true;

    public bool IncludeLowercase { get; init; } = true;

    public bool IncludeDigits { get; init; } = true;

    public bool IncludeSymbols { get; init; } = true;

    /// <summary>Drops characters that look alike in most fonts: I, l, 1, O, 0.</summary>
    public bool ExcludeSimilar { get; init; }

    /// <summary>Drops brackets and quotes that are awkward in shells and config files.</summary>
    public bool ExcludeAmbiguous { get; init; }

    /// <summary>Word count for passphrases.</summary>
    public int WordCount { get; init; } = 5;

    /// <summary>Separator placed between passphrase words.</summary>
    public string WordSeparator { get; init; } = "-";

    /// <summary>Appends a digit to a passphrase, for policies that demand one.</summary>
    public bool AppendNumberToPassphrase { get; init; }
}

/// <summary>A generated secret together with its measured strength.</summary>
public sealed record GeneratedPassword
{
    public required string Value { get; init; }

    /// <summary>Shannon entropy in bits, given the alphabet the secret was drawn from.</summary>
    public double EntropyBits { get; init; }

    public PasswordStrength Strength { get; init; }

    /// <summary>Size of the alphabet or dictionary the secret was drawn from.</summary>
    public int AlphabetSize { get; init; }

    /// <summary>Human-readable crack-time estimate at a fixed guess rate.</summary>
    public required string CrackTime { get; init; }
}
