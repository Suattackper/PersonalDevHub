using System.Text;
using DevhubWebApp.Models.Tools.Hash;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>
/// Digest, detection and verification rules. Expected values are the published vectors for the
/// input "abc", so a regression in the wiring shows up immediately.
/// </summary>
public class HashCalculatorTests
{
    [Theory]
    [InlineData(HashAlgorithmKind.Md5, "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData(HashAlgorithmKind.Sha1, "a9993e364706816aba3e25717850c26c9cd0d89d")]
    [InlineData(HashAlgorithmKind.Sha256, "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    public void ComputeText_MatchesTheKnownVectorsForAbc(HashAlgorithmKind algorithm, string expected)
        => Assert.Equal(expected, HashCalculator.ComputeText(algorithm, "abc").Hex);

    [Fact]
    public void ComputeText_ProducesTheExpectedDigestLengths()
    {
        Assert.Equal(384, HashCalculator.ComputeText(HashAlgorithmKind.Sha384, "abc").Bits);
        Assert.Equal(512, HashCalculator.ComputeText(HashAlgorithmKind.Sha512, "abc").Bits);
    }

    [Fact]
    public void Compute_AlsoReportsTheDigestInBase64()
    {
        var result = HashCalculator.ComputeText(HashAlgorithmKind.Sha256, "abc");

        Assert.Equal(Convert.ToBase64String(Convert.FromHexString(result.Hex)), result.Base64);
    }

    [Fact]
    public void ComputeText_UsesUtf8ForNonAsciiInput()
    {
        var viaText = HashCalculator.ComputeText(HashAlgorithmKind.Sha256, "chào");
        var viaBytes = HashCalculator.Compute(HashAlgorithmKind.Sha256, Encoding.UTF8.GetBytes("chào"));

        Assert.Equal(viaBytes.Hex, viaText.Hex);
    }

    [Fact]
    public void Compute_HandlesEmptyAndNullInputIdentically()
    {
        const string emptySha256 = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855";

        Assert.Equal(emptySha256, HashCalculator.ComputeText(HashAlgorithmKind.Sha256, null).Hex);
        Assert.Equal(emptySha256, HashCalculator.Compute(HashAlgorithmKind.Sha256, null).Hex);
    }

    [Fact]
    public void ComputeAll_ReturnsEveryAlgorithmOnce()
    {
        var results = HashCalculator.ComputeAllText("abc");

        Assert.Equal(HashCalculator.All.Count, results.Count);
        Assert.Equal(HashCalculator.All, results.Select(result => result.Algorithm));
    }

    [Fact]
    public void IsLegacy_FlagsMd5AndSha1Only()
    {
        var results = HashCalculator.ComputeAllText("abc").ToDictionary(result => result.Algorithm);

        Assert.True(results[HashAlgorithmKind.Md5].IsLegacy);
        Assert.True(results[HashAlgorithmKind.Sha1].IsLegacy);
        Assert.False(results[HashAlgorithmKind.Sha256].IsLegacy);
    }

    [Theory]
    [InlineData("900150983cd24fb0d6963f7d28e17f72", HashAlgorithmKind.Md5)]
    [InlineData("a9993e364706816aba3e25717850c26c9cd0d89d", HashAlgorithmKind.Sha1)]
    [InlineData("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", HashAlgorithmKind.Sha256)]
    public void Detect_InfersTheAlgorithmFromDigestLength(string hash, HashAlgorithmKind expected)
        => Assert.Equal(expected, HashCalculator.Detect(hash));

    [Fact]
    public void Detect_IgnoresSeparatorsAndCase()
        => Assert.Equal(HashAlgorithmKind.Md5, HashCalculator.Detect("90:01:50:98:3C:D2:4F:B0 D6 96 3F 7D 28 E1 7F 72"));

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("abcdef")]
    public void Detect_ReturnsNullForUnrecognisedInput(string hash)
        => Assert.Null(HashCalculator.Detect(hash));

    [Fact]
    public void Verify_MatchesTheDetectedAlgorithm()
    {
        var computed = HashCalculator.ComputeAllText("abc");

        var verification = HashCalculator.Verify(computed, "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD");

        Assert.True(verification.IsMatch);
        Assert.Equal(HashAlgorithmKind.Sha256, verification.Algorithm);
        Assert.True(verification.HasExpectedValue);
    }

    [Fact]
    public void Verify_ReportsAMismatchOfTheSameLength()
    {
        var computed = HashCalculator.ComputeAllText("abc");

        var verification = HashCalculator.Verify(computed, new string('a', 64));

        Assert.False(verification.IsMatch);
        Assert.Equal(HashAlgorithmKind.Sha256, verification.Algorithm);
        Assert.Null(verification.Error);
    }

    [Fact]
    public void Verify_ExplainsAnUnrecognisableExpectedValue()
    {
        var verification = HashCalculator.Verify(HashCalculator.ComputeAllText("abc"), "zzzz");

        Assert.False(verification.IsMatch);
        Assert.NotNull(verification.Error);
    }

    [Fact]
    public void Verify_ReturnsNoneWhenNothingWasEntered()
        => Assert.Same(HashVerification.None, HashCalculator.Verify(HashCalculator.ComputeAllText("abc"), "   "));

    [Fact]
    public void FixedTimeEquals_ComparesCaseInsensitivelyAndRejectsLengthMismatches()
    {
        Assert.True(HashCalculator.FixedTimeEquals("ABCD", "abcd"));
        Assert.False(HashCalculator.FixedTimeEquals("abcd", "abce"));
        Assert.False(HashCalculator.FixedTimeEquals("abcd", "abc"));
        Assert.False(HashCalculator.FixedTimeEquals(null, "abc"));
    }

    [Fact]
    public void GetName_UsesTheConventionalSpelling()
    {
        Assert.Equal("SHA-256", HashCalculator.GetName(HashAlgorithmKind.Sha256));
        Assert.Equal("MD5", HashCalculator.GetName(HashAlgorithmKind.Md5));
    }
}
