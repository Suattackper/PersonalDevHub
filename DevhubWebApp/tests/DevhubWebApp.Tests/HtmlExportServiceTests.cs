using DevhubWebApp.Services;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Guarantees of the shared HTML export writer.</summary>
public class HtmlExportServiceTests
{
    private readonly HtmlExportService _service = new();

    [Fact]
    public void BuildDocument_ProducesSelfContainedHtml()
    {
        var document = _service.BuildDocument("My notes", "<p>Body</p>");

        Assert.StartsWith("<!DOCTYPE html>", document, StringComparison.Ordinal);
        Assert.Contains("<title>My notes</title>", document, StringComparison.Ordinal);
        Assert.Contains("<p>Body</p>", document, StringComparison.Ordinal);
        Assert.Contains("<style>", document, StringComparison.Ordinal);

        // Self-contained means no network dependency of any kind.
        Assert.DoesNotContain("http://", document, StringComparison.Ordinal);
        Assert.DoesNotContain("<link", document, StringComparison.Ordinal);
        Assert.DoesNotContain("<script", document, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDocument_EncodesTheTitleButNotTheBody()
    {
        var document = _service.BuildDocument("<script>", "<p>kept</p>");

        Assert.Contains("<title>&lt;script&gt;</title>", document, StringComparison.Ordinal);
        Assert.Contains("<p>kept</p>", document, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDocument_AppendsExtraCss()
        => Assert.Contains(".custom { color: red; }", _service.BuildDocument("t", "<p/>", ".custom { color: red; }"), StringComparison.Ordinal);

    [Theory]
    [InlineData("My Notes", "md", "my-notes.md")]
    [InlineData("  spaced   out  ", "html", "spaced-out.html")]
    // Accented letters are valid in file names on every target platform, so they are kept.
    [InlineData("Ünïcødé 123", "txt", "ünïcødé-123.txt")]
    [InlineData("###", "md", "devhub.md")]
    [InlineData(null, "md", "devhub.md")]
    public void ToFileName_ProducesASafeSlug(string? title, string extension, string expected)
        => Assert.Equal(expected, _service.ToFileName(title, extension));

    [Fact]
    public void ToFileName_UsesTheGivenFallback()
        => Assert.Equal("diff.txt", _service.ToFileName(null, "txt", "diff"));

    [Fact]
    public void ToFileName_AcceptsAnExtensionWithLeadingDot()
        => Assert.Equal("notes.md", _service.ToFileName("notes", ".md"));

    [Fact]
    public void ToFileName_RejectsAMissingExtension()
        => Assert.Throws<ArgumentException>(() => _service.ToFileName("notes", "  "));
}
