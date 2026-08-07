using DevhubWebApp.Models.Tools.Url;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Encoding, parsing and query-building rules of the URL toolkit.</summary>
public class UrlToolkitTests
{
    [Fact]
    public void EncodeComponent_EscapesReservedCharacters()
        => Assert.Equal("a%20b%26c%3Dd", UrlToolkit.EncodeComponent("a b&c=d"));

    [Fact]
    public void EncodeFull_KeepsUrlStructureIntact()
    {
        var encoded = UrlToolkit.EncodeFull("https://example.com/a path?x=1&y=2");

        Assert.StartsWith("https://example.com/a%20path?x=1&y=2", encoded, StringComparison.Ordinal);
    }

    [Fact]
    public void Decode_ReversesPercentEncodingAndFormPluses()
    {
        Assert.Equal("a b&c=d", UrlToolkit.Decode("a%20b%26c%3Dd"));
        Assert.Equal("hello world", UrlToolkit.Decode("hello+world"));
        Assert.Equal("chào bạn", UrlToolkit.Decode(UrlToolkit.EncodeComponent("chào bạn")));
    }

    [Fact]
    public void Decode_ReturnsMalformedInputUnchanged()
        => Assert.Equal("%zz", UrlToolkit.Decode("%zz"));

    [Fact]
    public void Parse_SplitsAnAbsoluteUrlIntoItsParts()
    {
        var parts = UrlToolkit.Parse("https://user:pw@example.com:8443/api/v1/items?page=2&q=hello%20world#section");

        Assert.True(parts.IsValid);
        Assert.Equal("https", parts.Protocol);
        Assert.Equal("example.com", parts.Host);
        Assert.Equal("8443", parts.Port);
        Assert.Equal("/api/v1/items", parts.Path);
        Assert.Equal("page=2&q=hello%20world", parts.Query);
        Assert.Equal("section", parts.Fragment);
        // An origin is scheme + host + port; credentials are deliberately not part of it.
        Assert.Equal("https://example.com:8443", parts.Origin);
        Assert.Equal("user:pw", parts.UserInfo);
    }

    [Fact]
    public void Parse_MarksDefaultPorts()
        => Assert.Equal("443 (default)", UrlToolkit.Parse("https://example.com/").Port);

    [Fact]
    public void Parse_ReportsRelativeInputAsInvalid()
    {
        var parts = UrlToolkit.Parse("/just/a/path");

        Assert.False(parts.IsValid);
        Assert.NotNull(parts.Error);
    }

    [Fact]
    public void Parse_ReturnsEmptyForBlankInput()
        => Assert.Same(UrlParts.Empty, UrlToolkit.Parse("   "));

    [Fact]
    public void ParseQuery_DecodesNamesAndValues()
    {
        var parameters = UrlToolkit.ParseQuery("?q=hello%20world&tag=a%26b");

        Assert.Collection(parameters,
            first => Assert.Equal(("q", "hello world"), (first.Name, first.Value)),
            second => Assert.Equal(("tag", "a&b"), (second.Name, second.Value)));
    }

    [Fact]
    public void ParseQuery_KeepsRepeatedNamesAndValuelessEntries()
    {
        var parameters = UrlToolkit.ParseQuery("id=1&id=2&flag");

        Assert.Equal(3, parameters.Count);
        Assert.Equal(2, parameters.Count(parameter => parameter.Name == "id"));
        Assert.Equal(string.Empty, parameters[2].Value);
    }

    [Fact]
    public void BuildQuery_EncodesAndSkipsUnnamedEntries()
    {
        var query = UrlToolkit.BuildQuery(
        [
            new QueryParameter { Name = "q", Value = "hello world" },
            new QueryParameter { Name = "tag", Value = "a&b" },
            new QueryParameter { Name = "flag" },
            new QueryParameter { Name = "  " }
        ]);

        Assert.Equal("q=hello%20world&tag=a%26b&flag", query);
    }

    [Fact]
    public void BuildQuery_AndParseQuery_RoundTrip()
    {
        const string source = "q=hello world&tag=a&b";
        var parameters = UrlToolkit.ParseQuery(UrlToolkit.BuildQuery(UrlToolkit.ParseQuery(source)));

        Assert.Equal(UrlToolkit.ParseQuery(source).Count, parameters.Count);
    }

    [Fact]
    public void BuildUrl_ReplacesOnlyTheQueryString()
    {
        var parts = UrlToolkit.Parse("https://example.com/api?old=1#frag");

        var url = UrlToolkit.BuildUrl(parts, [new QueryParameter { Name = "page", Value = "2" }]);

        Assert.Equal("https://example.com/api?page=2#frag", url);
    }

    [Fact]
    public void BuildUrl_OmitsAnEmptyQuery()
        => Assert.Equal("https://example.com/api", UrlToolkit.BuildUrl(UrlToolkit.Parse("https://example.com/api?x=1"), []));

    [Fact]
    public void SortParameters_OrdersByNameCaseInsensitively()
    {
        var sorted = UrlToolkit.SortParameters(
        [
            new QueryParameter { Name = "beta" },
            new QueryParameter { Name = "Alpha" },
            new QueryParameter { Name = "gamma" }
        ]);

        Assert.Equal(["Alpha", "beta", "gamma"], sorted.Select(parameter => parameter.Name));
    }
}
