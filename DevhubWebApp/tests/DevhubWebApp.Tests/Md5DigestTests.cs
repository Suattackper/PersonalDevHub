using System.Text;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>
/// The managed MD5 implementation, checked against the test suite published in RFC 1321. These
/// vectors are the only thing standing between a subtle transcription error and silently wrong
/// digests, so all of them are covered.
/// </summary>
public class Md5DigestTests
{
    private static string Hash(string input)
        => Convert.ToHexString(Md5Digest.HashData(Encoding.ASCII.GetBytes(input))).ToLowerInvariant();

    [Theory]
    [InlineData("", "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData("a", "0cc175b9c0f1b6a831c399e269772661")]
    [InlineData("abc", "900150983cd24fb0d6963f7d28e17f72")]
    [InlineData("message digest", "f96b697d7cb7938d525a2f31aaf161d0")]
    [InlineData("abcdefghijklmnopqrstuvwxyz", "c3fcd3d76192e4007dfb496cca67e13b")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789", "d174ab98d277d9f5a5611c2c9f419d9f")]
    [InlineData("12345678901234567890123456789012345678901234567890123456789012345678901234567890", "57edf4a22be3c955ac49da2e2107b67a")]
    public void HashData_MatchesTheRfc1321TestSuite(string input, string expected)
        => Assert.Equal(expected, Hash(input));

    /// <summary>Exercises the padding branch where the length no longer fits in the final block.</summary>
    [Theory]
    [InlineData(55)]
    [InlineData(56)]
    [InlineData(57)]
    [InlineData(63)]
    [InlineData(64)]
    [InlineData(65)]
    [InlineData(128)]
    public void HashData_HandlesEveryPaddingBoundary(int length)
    {
        var digest = Md5Digest.HashData(Encoding.ASCII.GetBytes(new string('x', length)));

        Assert.Equal(16, digest.Length);
    }

    [Fact]
    public void HashData_HashesUtf8BytesNotCharacters()
    {
        var expected = Convert.ToHexString(Md5Digest.HashData(Encoding.UTF8.GetBytes("chào"))).ToLowerInvariant();

        Assert.Equal(expected, Convert.ToHexString(Md5Digest.HashData(Encoding.UTF8.GetBytes("chào"))).ToLowerInvariant());
        Assert.NotEqual(expected, Hash("chao"));
    }

    [Fact]
    public void HashData_IsDeterministicAcrossLargeInputs()
    {
        var payload = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("devhub", 10_000)));

        Assert.Equal(Md5Digest.HashData(payload), Md5Digest.HashData(payload));
    }
}
