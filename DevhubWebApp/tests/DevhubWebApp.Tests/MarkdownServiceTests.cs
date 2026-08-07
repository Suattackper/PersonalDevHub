using DevhubWebApp.Services;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Rendering and analysis guarantees of the Markdown service.</summary>
public class MarkdownServiceTests
{
    private readonly MarkdownService _service = new();

    [Fact]
    public void ToHtml_RendersCommonMarkConstructs()
    {
        var html = _service.ToHtml("# Title\n\nSome **bold** and `code`.\n\n- one\n- two");

        Assert.Contains("<h1", html, StringComparison.Ordinal);
        Assert.Contains("<strong>bold</strong>", html, StringComparison.Ordinal);
        Assert.Contains("<code>code</code>", html, StringComparison.Ordinal);
        Assert.Contains("<li>one</li>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtml_RendersAdvancedExtensions()
    {
        var html = _service.ToHtml("| a | b |\n|---|---|\n| 1 | 2 |");

        Assert.Contains("<table", html, StringComparison.Ordinal);
        Assert.Contains("<td>1</td>", html, StringComparison.Ordinal);
    }

    /// <summary>
    /// The preview renders text the user may have pasted from anywhere, so embedded HTML must come
    /// out as visible text rather than live markup.
    /// </summary>
    [Fact]
    public void ToHtml_NeutralisesEmbeddedHtml()
    {
        var html = _service.ToHtml("Hello <script>alert('x')</script> world");

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ToHtml_ReturnsEmptyForBlankInput(string? input)
        => Assert.Equal(string.Empty, _service.ToHtml(input));

    [Fact]
    public void ToPlainText_StripsMarkup()
    {
        var text = _service.ToPlainText("# Title\n\n**bold** [link](https://example.com)");

        Assert.DoesNotContain("#", text, StringComparison.Ordinal);
        Assert.DoesNotContain("**", text, StringComparison.Ordinal);
        Assert.Contains("Title", text, StringComparison.Ordinal);
        Assert.Contains("bold", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Analyse_CountsWordsCharactersAndLines()
    {
        const string source = "# Title\n\nOne two three.";

        var statistics = _service.Analyse(source);

        Assert.Equal(4, statistics.Words);
        Assert.Equal(source.Length, statistics.Characters);
        Assert.Equal(source.Count(character => !char.IsWhiteSpace(character)), statistics.CharactersWithoutSpaces);
        Assert.Equal(3, statistics.Lines);
    }

    [Fact]
    public void Analyse_RoundsReadingTimeUpToWholeMinutes()
    {
        var words = string.Join(' ', Enumerable.Repeat("word", 450));

        var statistics = _service.Analyse(words);

        Assert.Equal(450, statistics.Words);
        Assert.Equal(TimeSpan.FromMinutes(2.25), statistics.ReadingTime);
        Assert.Equal("3 min", statistics.ReadingTimeLabel);
    }

    [Fact]
    public void Analyse_ReportsShortDocumentsAsUnderOneMinute()
    {
        var statistics = _service.Analyse("a few words only");

        Assert.Equal("< 1 min", statistics.ReadingTimeLabel);
    }

    [Fact]
    public void Analyse_ReturnsEmptyStatisticsForBlankInput()
        => Assert.Equal(0, _service.Analyse("   ").Words);
}
