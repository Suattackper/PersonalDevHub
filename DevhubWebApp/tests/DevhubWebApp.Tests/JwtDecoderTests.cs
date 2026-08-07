using System.Text;
using System.Text.Json;
using DevhubWebApp.Models.Tools.Jwt;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Decoding and lifetime rules of the JWT inspector.</summary>
public class JwtDecoderTests
{
    private static string Base64Url(string json)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string BuildToken(object header, object payload, string signature = "sig")
        => $"{Base64Url(JsonSerializer.Serialize(header))}.{Base64Url(JsonSerializer.Serialize(payload))}.{signature}";

    private static long Unix(TimeSpan offset) => DateTimeOffset.UtcNow.Add(offset).ToUnixTimeSeconds();

    [Fact]
    public void Decode_ReadsHeaderPayloadAndSignature()
    {
        var token = BuildToken(
            new { alg = "HS256", typ = "JWT", kid = "key-1" },
            new { iss = "devhub", sub = "user-42", aud = "api" });

        var result = JwtDecoder.Decode(token);

        Assert.True(result.IsValid);
        Assert.Equal("HS256", result.Algorithm);
        Assert.Equal("JWT", result.Type);
        Assert.Equal("key-1", result.KeyId);
        Assert.Equal("devhub", result.Issuer);
        Assert.Equal("user-42", result.Subject);
        Assert.Equal("api", result.Audience);
        Assert.Equal("sig", result.Signature);
        Assert.Contains("\"alg\"", result.HeaderJson, StringComparison.Ordinal);
        Assert.Contains("\"sub\"", result.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_PrettyPrintsBothSegments()
    {
        var result = JwtDecoder.Decode(BuildToken(new { alg = "none" }, new { a = 1 }));

        Assert.Contains("\n", result.HeaderJson, StringComparison.Ordinal);
        Assert.Contains("\n", result.PayloadJson, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_AcceptsATokenPastedWithItsBearerPrefix()
    {
        var token = BuildToken(new { alg = "HS256" }, new { sub = "x" });

        Assert.True(JwtDecoder.Decode($"Bearer {token}").IsValid);
    }

    [Fact]
    public void Decode_AcceptsATokenWithoutASignatureSegment()
    {
        var token = BuildToken(new { alg = "none" }, new { sub = "x" }, signature: string.Empty);
        var result = JwtDecoder.Decode(token[..^1]);

        Assert.True(result.IsValid);
        Assert.True(result.IsUnsigned);
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("only.two")]
    [InlineData("a.b.c.d")]
    public void Decode_RejectsMalformedInput(string input)
    {
        var result = JwtDecoder.Decode(input);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Decode_RejectsSegmentsThatAreNotJson()
    {
        var token = $"{Base64Url("\"just a string\"")}.{Base64Url("also not an object")}.sig";

        Assert.False(JwtDecoder.Decode(token).IsValid);
    }

    [Fact]
    public void Decode_ReturnsEmptyForBlankInput()
        => Assert.Same(JwtToken.Empty, JwtDecoder.Decode("   "));

    [Fact]
    public void Decode_ReadsNumericDateClaims()
    {
        var issued = Unix(TimeSpan.FromHours(-1));
        var expires = Unix(TimeSpan.FromHours(1));

        var result = JwtDecoder.Decode(BuildToken(new { alg = "HS256" }, new { iat = issued, exp = expires }));

        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(issued), result.IssuedAt);
        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(expires), result.ExpiresAt);
        Assert.Equal(JwtValidity.Active, result.Validity);
    }

    /// <summary>Some issuers emit NumericDate claims as strings; the tool still has to read them.</summary>
    [Fact]
    public void Decode_ReadsDateClaimsEmittedAsStrings()
    {
        var expires = Unix(TimeSpan.FromHours(1));

        var result = JwtDecoder.Decode(BuildToken(new { alg = "HS256" }, new { exp = expires.ToString(System.Globalization.CultureInfo.InvariantCulture) }));

        Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(expires), result.ExpiresAt);
    }

    [Fact]
    public void Decode_FlagsExpiredTokens()
    {
        var result = JwtDecoder.Decode(BuildToken(new { alg = "HS256" }, new { exp = Unix(TimeSpan.FromHours(-1)) }));

        Assert.Equal(JwtValidity.Expired, result.Validity);
        Assert.True(result.TimeRemaining < TimeSpan.Zero);
    }

    [Fact]
    public void Decode_FlagsTokensThatAreNotYetValid()
    {
        var result = JwtDecoder.Decode(BuildToken(
            new { alg = "HS256" },
            new { nbf = Unix(TimeSpan.FromHours(1)), exp = Unix(TimeSpan.FromHours(2)) }));

        Assert.Equal(JwtValidity.NotYetValid, result.Validity);
    }

    [Fact]
    public void Decode_ReportsUnknownValidityWithoutTimeClaims()
        => Assert.Equal(JwtValidity.Unknown, JwtDecoder.Decode(BuildToken(new { alg = "HS256" }, new { sub = "x" })).Validity);

    [Fact]
    public void Decode_JoinsAnArrayAudience()
    {
        var result = JwtDecoder.Decode(BuildToken(new { alg = "HS256" }, new { aud = new[] { "api", "web" } }));

        Assert.Equal("api, web", result.Audience);
    }

    [Fact]
    public void Decode_ReportsTokenSizeInBytes()
    {
        var token = BuildToken(new { alg = "HS256" }, new { sub = "x" });

        Assert.Equal(Encoding.UTF8.GetByteCount(token), JwtDecoder.Decode(token).SizeInBytes);
    }

    [Fact]
    public void Evaluate_PrioritisesExpiryOverNotBefore()
        => Assert.Equal(
            JwtValidity.Expired,
            JwtDecoder.Evaluate(DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(-1)));
}
