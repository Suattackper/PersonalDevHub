using DevhubWebApp.Utilities;

namespace DevhubWebApp.Tests;

public sealed class HtmlEntityConverterTests
{
    [Fact]
    public void Escape_EncodesAllHtmlSensitiveCharacters()
        => Assert.Equal("&lt;tag title=&quot;x&quot;&gt;Tom &amp; Jerry&#39;s&lt;/tag&gt;", HtmlEntityConverter.Escape("<tag title=\"x\">Tom & Jerry's</tag>"));

    [Theory]
    [InlineData("")]
    [InlineData("Xin chào 🌏\nline two")]
    public void EscapeAndUnescape_RoundTripsText(string input)
        => Assert.Equal(input, HtmlEntityConverter.Unescape(HtmlEntityConverter.Escape(input)));

    [Fact]
    public void Escape_EncodesAlreadyEscapedInputWithoutLosingData()
        => Assert.Equal("&amp;lt;", HtmlEntityConverter.Escape("&lt;"));

    [Fact]
    public void Unescape_DecodesNamedAndNumericEntities()
        => Assert.Equal("<>&\"'", HtmlEntityConverter.Unescape("&lt;&gt;&amp;&quot;&#39;"));
}
