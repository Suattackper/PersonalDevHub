using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using DevhubWebApp.Models.Tools.Password;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Generates secrets and measures their strength.
/// <para>
/// Every random choice comes from <see cref="RandomNumberGenerator"/>. <c>System.Random</c> is
/// seeded predictably and would make generated passwords guessable — for this tool that is not a
/// style preference, it is the whole point.
/// </para>
/// </summary>
public static class PasswordGenerator
{
    private const string Uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const string Digits = "0123456789";
    private const string Symbols = "!@#$%^&*()-_=+[]{};:,.<>?/~";

    /// <summary>Characters that are easily confused in common fonts.</summary>
    private const string SimilarCharacters = "Il1O0o";

    /// <summary>Characters that need escaping or quoting in shells, JSON and config files.</summary>
    private const string AmbiguousCharacters = "{}[]()/\\'\"`~,;:.<>";

    private const string Consonants = "bcdfghjklmnpqrstvwxz";
    private const string Vowels = "aeiouy";

    /// <summary>Guesses per second assumed for the crack-time estimate: an offline GPU attack.</summary>
    private const double GuessesPerSecond = 1e11;

    /// <summary>
    /// Word list for passphrases. Short, common, unambiguous English words — the value of a
    /// passphrase comes from the number of words, not from their obscurity.
    /// </summary>
    private static readonly string[] Words =
    [
        "amber", "anchor", "apple", "arrow", "autumn", "badge", "bamboo", "banjo", "beacon", "bench",
        "birch", "bishop", "blanket", "bloom", "bottle", "branch", "bridge", "bronze", "bucket", "bundle",
        "cabin", "cactus", "candle", "canvas", "carbon", "castle", "cedar", "chalk", "cherry", "cinder",
        "circus", "clever", "cliff", "clover", "cobalt", "comet", "copper", "coral", "cotton", "crater",
        "crimson", "crystal", "cyclone", "daisy", "dolphin", "domino", "dragon", "dust", "eagle", "ember",
        "engine", "falcon", "fern", "fiddle", "flint", "forest", "fossil", "fountain", "galaxy", "garden",
        "gecko", "ginger", "glacier", "granite", "gravel", "harbor", "harvest", "hazel", "helmet", "hollow",
        "indigo", "island", "ivory", "jasmine", "jigsaw", "junction", "kettle", "kingdom", "ladder", "lagoon",
        "lantern", "laurel", "lemon", "lighthouse", "lily", "lobster", "lumber", "magnet", "mango", "maple",
        "marble", "meadow", "mercury", "meteor", "mimosa", "mineral", "mirror", "monsoon", "mosaic", "nectar",
        "needle", "nickel", "nimbus", "noble", "nutmeg", "oasis", "octave", "olive", "onyx", "orbit",
        "orchid", "otter", "oxide", "paddle", "palace", "pancake", "papaya", "parcel", "pebble", "pelican",
        "pepper", "petal", "pigment", "pilot", "pineapple", "planet", "plaster", "plum", "pocket", "pollen",
        "poplar", "prairie", "prism", "puzzle", "quartz", "quiver", "rabbit", "radar", "raven", "ribbon",
        "river", "rocket", "rubber", "saffron", "sailor", "salmon", "sandal", "sapling", "satin", "scarlet",
        "sculpt", "seagull", "sequoia", "shadow", "shelter", "sierra", "silver", "socket", "solar", "sonnet",
        "spruce", "squash", "stellar", "stone", "summit", "sunset", "syrup", "tangent", "teapot", "thicket",
        "thistle", "thunder", "timber", "tiger", "topaz", "torch", "tulip", "tundra", "turtle", "umbrella",
        "valley", "velvet", "vessel", "vintage", "violet", "walnut", "whistle", "willow", "window", "winter",
        "wombat", "yarrow", "yellow", "zenith", "zephyr", "zigzag", "zinnia", "zodiac", "zucchini", "quill"
    ];

    /// <summary>Number of words available to passphrases; the entropy calculation uses it.</summary>
    public static int DictionarySize => Words.Length;

    /// <summary>Generates one secret according to <paramref name="options"/>.</summary>
    public static GeneratedPassword Generate(PasswordOptions? options = null)
    {
        options ??= PasswordOptions.Default;

        return options.Kind switch
        {
            PasswordKind.Passphrase => GeneratePassphrase(options),
            PasswordKind.Pronounceable => GeneratePronounceable(options),
            _ => GenerateRandom(options)
        };
    }

    /// <summary>Generates several independent secrets with the same options.</summary>
    public static IReadOnlyList<GeneratedPassword> GenerateMany(int count, PasswordOptions? options = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 100);

        return Enumerable.Range(0, count).Select(_ => Generate(options)).ToList();
    }

    /// <summary>
    /// Builds the alphabet the options select. Returns an empty string when every set is disabled,
    /// which the caller reports rather than silently substituting a default.
    /// </summary>
    public static string BuildAlphabet(PasswordOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var builder = new StringBuilder();

        if (options.IncludeUppercase)
        {
            builder.Append(Uppercase);
        }

        if (options.IncludeLowercase)
        {
            builder.Append(Lowercase);
        }

        if (options.IncludeDigits)
        {
            builder.Append(Digits);
        }

        if (options.IncludeSymbols)
        {
            builder.Append(Symbols);
        }

        var alphabet = builder.ToString();

        if (options.ExcludeSimilar)
        {
            alphabet = Remove(alphabet, SimilarCharacters);
        }

        if (options.ExcludeAmbiguous)
        {
            alphabet = Remove(alphabet, AmbiguousCharacters);
        }

        return alphabet;
    }

    /// <summary>
    /// Entropy in bits for a secret of <paramref name="length"/> symbols drawn uniformly from an
    /// alphabet of <paramref name="alphabetSize"/>: <c>length × log2(alphabetSize)</c>.
    /// </summary>
    public static double CalculateEntropy(int length, int alphabetSize)
        => length <= 0 || alphabetSize <= 1 ? 0 : Math.Round(length * Math.Log2(alphabetSize), 1);

    /// <summary>
    /// Maps entropy onto a strength band. The thresholds follow common guidance: under 28 bits is
    /// trivially crackable, 60 bits resists an offline attack, 128 bits is beyond reach.
    /// </summary>
    public static PasswordStrength Classify(double entropyBits) => entropyBits switch
    {
        < 28 => PasswordStrength.VeryWeak,
        < 45 => PasswordStrength.Weak,
        < 68 => PasswordStrength.Fair,
        < 100 => PasswordStrength.Strong,
        _ => PasswordStrength.VeryStrong
    };

    /// <summary>Display name of a strength band.</summary>
    public static string GetStrengthLabel(PasswordStrength strength) => strength switch
    {
        PasswordStrength.VeryWeak => "Very weak",
        PasswordStrength.Weak => "Weak",
        PasswordStrength.Fair => "Fair",
        PasswordStrength.Strong => "Strong",
        _ => "Very strong"
    };

    /// <summary>
    /// Rough time to exhaust the search space at <see cref="GuessesPerSecond"/>. It is an order of
    /// magnitude, not a promise — the point is to make the difference between 40 and 80 bits felt.
    /// </summary>
    public static string EstimateCrackTime(double entropyBits)
    {
        if (entropyBits <= 0)
        {
            return "instantly";
        }

        var seconds = Math.Pow(2, entropyBits - 1) / GuessesPerSecond;

        return seconds switch
        {
            < 1 => "instantly",
            < 60 => $"{seconds:0} seconds",
            < 3600 => $"{seconds / 60:0} minutes",
            < 86400 => $"{seconds / 3600:0} hours",
            < 31_536_000 => $"{seconds / 86400:0} days",
            < 3.1536e10 => $"{seconds / 31_536_000:0} years",
            _ => $"{seconds / 31_536_000:0.0e0} years"
        };
    }

    private static GeneratedPassword GenerateRandom(PasswordOptions options)
    {
        var alphabet = BuildAlphabet(options);

        if (alphabet.Length == 0 || options.Length <= 0)
        {
            return Describe(string.Empty, 0);
        }

        var builder = new StringBuilder(options.Length);

        for (var index = 0; index < options.Length; index++)
        {
            builder.Append(alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]);
        }

        return Describe(builder.ToString(), alphabet.Length);
    }

    /// <summary>
    /// Consonant-vowel syllables, optionally salted with a digit. The alphabet is effectively the
    /// syllable set, so entropy is reported against that rather than the full character space —
    /// pronounceable passwords are weaker than they look, and the meter must say so.
    /// </summary>
    private static GeneratedPassword GeneratePronounceable(PasswordOptions options)
    {
        if (options.Length <= 0)
        {
            return Describe(string.Empty, 0);
        }

        var builder = new StringBuilder(options.Length);

        while (builder.Length < options.Length)
        {
            builder.Append(Consonants[RandomNumberGenerator.GetInt32(Consonants.Length)]);

            if (builder.Length < options.Length)
            {
                builder.Append(Vowels[RandomNumberGenerator.GetInt32(Vowels.Length)]);
            }
        }

        var value = builder.ToString(0, options.Length);

        if (options.IncludeUppercase)
        {
            value = char.ToUpperInvariant(value[0]) + value[1..];
        }

        if (options.IncludeDigits && value.Length > 1)
        {
            value = value[..^1] + Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        }

        // Each position carries roughly the choice of one consonant or one vowel.
        var effectiveAlphabet = (int)Math.Round((Consonants.Length + Vowels.Length) / 2d);

        return Describe(value, effectiveAlphabet);
    }

    private static GeneratedPassword GeneratePassphrase(PasswordOptions options)
    {
        if (options.WordCount <= 0)
        {
            return Describe(string.Empty, 0);
        }

        var words = new List<string>(options.WordCount);

        for (var index = 0; index < options.WordCount; index++)
        {
            var word = Words[RandomNumberGenerator.GetInt32(Words.Length)];
            words.Add(options.IncludeUppercase ? char.ToUpperInvariant(word[0]) + word[1..] : word);
        }

        var value = string.Join(options.WordSeparator, words);

        if (options.AppendNumberToPassphrase)
        {
            value += RandomNumberGenerator.GetInt32(10, 100).ToString(CultureInfo.InvariantCulture);
        }

        // A passphrase's entropy comes from word choices, not from its character count.
        var entropy = CalculateEntropy(options.WordCount, Words.Length)
            + (options.AppendNumberToPassphrase ? Math.Log2(90) : 0);

        return new GeneratedPassword
        {
            Value = value,
            EntropyBits = Math.Round(entropy, 1),
            Strength = Classify(entropy),
            AlphabetSize = Words.Length,
            CrackTime = EstimateCrackTime(entropy)
        };
    }

    private static GeneratedPassword Describe(string value, int alphabetSize)
    {
        var entropy = CalculateEntropy(value.Length, alphabetSize);

        return new GeneratedPassword
        {
            Value = value,
            EntropyBits = entropy,
            Strength = Classify(entropy),
            AlphabetSize = alphabetSize,
            CrackTime = EstimateCrackTime(entropy)
        };
    }

    private static string Remove(string source, string unwanted)
        => new(source.Where(character => !unwanted.Contains(character, StringComparison.Ordinal)).ToArray());
}
