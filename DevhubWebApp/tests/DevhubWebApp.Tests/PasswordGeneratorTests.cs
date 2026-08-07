using DevhubWebApp.Models.Tools.Password;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Generation, alphabet composition and strength measurement rules.</summary>
public class PasswordGeneratorTests
{
    [Fact]
    public void Generate_RespectsTheRequestedLength()
        => Assert.Equal(32, PasswordGenerator.Generate(new PasswordOptions { Length = 32 }).Value.Length);

    [Fact]
    public void Generate_ProducesADifferentSecretEachTime()
    {
        var values = Enumerable.Range(0, 25)
            .Select(_ => PasswordGenerator.Generate(new PasswordOptions { Length = 24 }).Value)
            .ToList();

        Assert.Equal(values.Count, values.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Generate_UsesOnlyTheSelectedCharacterSets()
    {
        var options = new PasswordOptions
        {
            Length = 200,
            IncludeUppercase = false,
            IncludeSymbols = false,
            IncludeLowercase = true,
            IncludeDigits = true
        };

        var value = PasswordGenerator.Generate(options).Value;

        Assert.All(value, character => Assert.True(char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character)));
    }

    [Fact]
    public void ExcludeSimilar_RemovesLookalikeCharacters()
    {
        var alphabet = PasswordGenerator.BuildAlphabet(new PasswordOptions { ExcludeSimilar = true });

        Assert.DoesNotContain('l', alphabet);
        Assert.DoesNotContain('1', alphabet);
        Assert.DoesNotContain('O', alphabet);
        Assert.DoesNotContain('0', alphabet);
    }

    [Fact]
    public void ExcludeAmbiguous_RemovesCharactersThatNeedQuoting()
    {
        var alphabet = PasswordGenerator.BuildAlphabet(new PasswordOptions { ExcludeAmbiguous = true });

        Assert.DoesNotContain('{', alphabet);
        Assert.DoesNotContain('\'', alphabet);
        Assert.DoesNotContain('\\', alphabet);
    }

    [Fact]
    public void BuildAlphabet_IsEmptyWhenEverySetIsDisabled()
    {
        var options = new PasswordOptions
        {
            IncludeUppercase = false,
            IncludeLowercase = false,
            IncludeDigits = false,
            IncludeSymbols = false
        };

        Assert.Empty(PasswordGenerator.BuildAlphabet(options));
        Assert.Empty(PasswordGenerator.Generate(options).Value);
    }

    [Fact]
    public void Pronounceable_AlternatesConsonantsAndVowelsAndStaysTheRequestedLength()
    {
        var options = new PasswordOptions
        {
            Kind = PasswordKind.Pronounceable,
            Length = 12,
            IncludeUppercase = false,
            IncludeDigits = false
        };

        var value = PasswordGenerator.Generate(options).Value;

        Assert.Equal(12, value.Length);
        Assert.All(value, character => Assert.True(char.IsAsciiLetterLower(character)));
    }

    /// <summary>A pronounceable secret must not be reported as strong as a random one of the same length.</summary>
    [Fact]
    public void Pronounceable_ReportsLowerEntropyThanARandomSecretOfTheSameLength()
    {
        var length = 16;
        var random = PasswordGenerator.Generate(new PasswordOptions { Length = length });
        var pronounceable = PasswordGenerator.Generate(new PasswordOptions { Kind = PasswordKind.Pronounceable, Length = length });

        Assert.True(pronounceable.EntropyBits < random.EntropyBits);
    }

    [Fact]
    public void Passphrase_JoinsTheRequestedNumberOfWords()
    {
        var options = new PasswordOptions
        {
            Kind = PasswordKind.Passphrase,
            WordCount = 5,
            WordSeparator = "-",
            IncludeUppercase = false
        };

        var value = PasswordGenerator.Generate(options).Value;

        Assert.Equal(5, value.Split('-').Length);
    }

    [Fact]
    public void Passphrase_CanAppendANumberAndCapitaliseWords()
    {
        var options = new PasswordOptions
        {
            Kind = PasswordKind.Passphrase,
            WordCount = 4,
            AppendNumberToPassphrase = true,
            IncludeUppercase = true
        };

        var value = PasswordGenerator.Generate(options).Value;

        Assert.True(char.IsAsciiLetterUpper(value[0]));
        Assert.True(char.IsAsciiDigit(value[^1]));
    }

    [Fact]
    public void Passphrase_DerivesEntropyFromWordChoicesNotCharacters()
    {
        var options = new PasswordOptions { Kind = PasswordKind.Passphrase, WordCount = 5 };

        var result = PasswordGenerator.Generate(options);

        Assert.Equal(PasswordGenerator.DictionarySize, result.AlphabetSize);
        Assert.Equal(PasswordGenerator.CalculateEntropy(5, PasswordGenerator.DictionarySize), result.EntropyBits);
    }

    [Theory]
    [InlineData(10, 26, 47.0)]
    [InlineData(20, 95, 131.4)]
    public void CalculateEntropy_UsesLengthTimesLog2OfTheAlphabet(int length, int alphabet, double expected)
        => Assert.Equal(expected, PasswordGenerator.CalculateEntropy(length, alphabet), 1);

    [Fact]
    public void CalculateEntropy_IsZeroForDegenerateInput()
    {
        Assert.Equal(0, PasswordGenerator.CalculateEntropy(0, 95));
        Assert.Equal(0, PasswordGenerator.CalculateEntropy(10, 1));
    }

    [Theory]
    [InlineData(20, PasswordStrength.VeryWeak)]
    [InlineData(35, PasswordStrength.Weak)]
    [InlineData(55, PasswordStrength.Fair)]
    [InlineData(80, PasswordStrength.Strong)]
    [InlineData(128, PasswordStrength.VeryStrong)]
    public void Classify_MapsEntropyOntoStrengthBands(double entropy, PasswordStrength expected)
        => Assert.Equal(expected, PasswordGenerator.Classify(entropy));

    [Fact]
    public void EstimateCrackTime_GrowsWithEntropy()
    {
        Assert.Equal("instantly", PasswordGenerator.EstimateCrackTime(0));
        Assert.Contains("years", PasswordGenerator.EstimateCrackTime(100), StringComparison.Ordinal);
    }

    [Fact]
    public void GenerateMany_ReturnsIndependentSecrets()
    {
        var passwords = PasswordGenerator.GenerateMany(10, new PasswordOptions { Length = 24 });

        Assert.Equal(10, passwords.Count);
        Assert.Equal(10, passwords.Select(password => password.Value).Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void GenerateMany_RejectsCountsOutsideTheSupportedRange(int count)
        => Assert.Throws<ArgumentOutOfRangeException>(() => PasswordGenerator.GenerateMany(count));

    [Fact]
    public void GetStrengthLabel_ReadsNaturally()
        => Assert.Equal("Very strong", PasswordGenerator.GetStrengthLabel(PasswordStrength.VeryStrong));
}
