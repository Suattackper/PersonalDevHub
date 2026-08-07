using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Export formats produced from a computed diff.</summary>
public class DiffExporterTests
{
    [Fact]
    public void ToText_WritesAUnifiedStyleHeaderAndSignedLines()
    {
        var result = DiffEngine.Compare("a\nb", "a\nc");

        var text = DiffExporter.ToText(result).ReplaceLineEndings("\n");

        Assert.StartsWith("--- left\n+++ right\n", text, StringComparison.Ordinal);
        Assert.Contains("@@ +1 -1", text, StringComparison.Ordinal);
        Assert.Contains(" a\n", text, StringComparison.Ordinal);
        Assert.Contains("-b\n", text, StringComparison.Ordinal);
        Assert.Contains("+c\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtmlFragment_MarksAddedAndRemovedRows()
    {
        var html = DiffExporter.ToHtmlFragment(DiffEngine.Compare("a\nb", "a\nc"));

        Assert.Contains("<table class=\"devhub-diff\">", html, StringComparison.Ordinal);
        Assert.Contains("class=\"removed\"", html, StringComparison.Ordinal);
        Assert.Contains("class=\"added\"", html, StringComparison.Ordinal);
    }

    /// <summary>Diffed content is untrusted text and must never become live markup in the export.</summary>
    [Fact]
    public void ToHtmlFragment_EncodesLineContent()
    {
        var html = DiffExporter.ToHtmlFragment(DiffEngine.Compare("<b>x</b>", "<script>alert(1)</script>"));

        Assert.DoesNotContain("<script>", html, StringComparison.Ordinal);
        Assert.Contains("&lt;script&gt;", html, StringComparison.Ordinal);
        Assert.Contains("&lt;b&gt;", html, StringComparison.Ordinal);
    }

    [Fact]
    public void ToHtmlFragment_RendersLineNumbersForBothSides()
    {
        var html = DiffExporter.ToHtmlFragment(DiffEngine.Compare("a", "a"));

        Assert.Contains("<td class=\"num\">1</td><td class=\"num\">1</td>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Exporters_RejectNullResults()
    {
        Assert.Throws<ArgumentNullException>(() => DiffExporter.ToText(null!));
        Assert.Throws<ArgumentNullException>(() => DiffExporter.ToHtmlFragment(null!));
    }
}
