namespace DevhubWebApp.Models.Tools.Password;

/// <summary>
/// Auto-saved options of the password generator. Generated secrets are deliberately **not**
/// persisted — writing them to storage would outlive the moment they were needed.
/// </summary>
public sealed class PasswordWorkspace
{
    public PasswordKind Kind { get; set; } = PasswordKind.Random;

    public int Length { get; set; } = 20;

    public bool IncludeUppercase { get; set; } = true;

    public bool IncludeLowercase { get; set; } = true;

    public bool IncludeDigits { get; set; } = true;

    public bool IncludeSymbols { get; set; } = true;

    public bool ExcludeSimilar { get; set; }

    public bool ExcludeAmbiguous { get; set; }

    public int WordCount { get; set; } = 5;

    public string WordSeparator { get; set; } = "-";

    public bool AppendNumber { get; set; }

    /// <summary>How many secrets a single generation produces.</summary>
    public int BatchSize { get; set; } = 1;

    /// <summary>Projects the persisted options onto the generator's contract.</summary>
    public PasswordOptions ToOptions() => new()
    {
        Kind = Kind,
        Length = Length,
        IncludeUppercase = IncludeUppercase,
        IncludeLowercase = IncludeLowercase,
        IncludeDigits = IncludeDigits,
        IncludeSymbols = IncludeSymbols,
        ExcludeSimilar = ExcludeSimilar,
        ExcludeAmbiguous = ExcludeAmbiguous,
        WordCount = WordCount,
        WordSeparator = WordSeparator,
        AppendNumberToPassphrase = AppendNumber
    };
}
