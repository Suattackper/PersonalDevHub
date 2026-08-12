using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Mime;
using DevhubWebApp.Services;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Look-up and search rules of the MIME explorer.</summary>
public class MimeLookupServiceTests
{
    private readonly MimeLookupService _service = new();

    [Theory]
    [InlineData("png", "image/png")]
    [InlineData(".PNG", "image/png")]
    [InlineData("jpeg", "image/jpeg")]
    [InlineData("docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    public void FindByExtension_IgnoresCaseAndALeadingDot(string extension, string expected)
        => Assert.Equal(expected, _service.FindByExtension(extension)?.MimeType);

    [Fact]
    public void FindByExtension_ReturnsNullForAnUnknownExtension()
        => Assert.Null(_service.FindByExtension("notareal"));

    [Fact]
    public void FindByMimeType_MatchesExactly()
    {
        Assert.Equal(["png"], _service.FindByMimeType("image/png")?.Extensions);
        Assert.Null(_service.FindByMimeType("image/"));
    }

    [Theory]
    [InlineData("report.pdf", "application/pdf")]
    [InlineData("archive.tar.gz", "application/gzip")]
    [InlineData("noextension", "application/octet-stream")]
    [InlineData(null, "application/octet-stream")]
    public void ResolveForFileName_FallsBackToOctetStream(string? fileName, string expected)
        => Assert.Equal(expected, _service.ResolveForFileName(fileName));

    /// <summary>Typing an extension must not bury the type that owns it.</summary>
    [Fact]
    public void Search_RanksAnExactExtensionMatchFirst()
    {
        var results = _service.Search("png");

        Assert.Equal("image/png", results[0].MimeType);
    }

    [Fact]
    public void Search_RanksAnExactMimeTypeMatchFirst()
        => Assert.Equal("application/json", _service.Search("application/json")[0].MimeType);

    [Fact]
    public void Search_FindsByDescription()
        => Assert.Contains(_service.Search("vector"), entry => entry.MimeType == "image/svg+xml");

    [Fact]
    public void Search_NarrowsToACategory()
    {
        var results = _service.Search(null, MimeCategory.Font);

        Assert.NotEmpty(results);
        Assert.All(results, entry => Assert.Equal(MimeCategory.Font, entry.Category));
    }

    [Fact]
    public void Search_ReturnsEverythingForABlankTerm()
        => Assert.Equal(_service.All.Count, _service.Search("   ").Count);

    [Fact]
    public void Search_ReturnsNothingForAnUnknownTerm()
        => Assert.Empty(_service.Search("zzzz-not-a-format"));

    [Fact]
    public void Catalogue_HasNoDuplicateMediaTypes()
    {
        var duplicates = MimeCatalog.All
            .GroupBy(entry => entry.MimeType, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key);

        Assert.Empty(duplicates);
    }

    [Fact]
    public void Catalogue_EntriesAreCompleteAndOrdered()
    {
        Assert.All(MimeCatalog.All, entry =>
        {
            Assert.NotEmpty(entry.Extensions);
            Assert.False(string.IsNullOrWhiteSpace(entry.Description));
            Assert.Equal(entry.Extensions[0], entry.PrimaryExtension);
            Assert.All(entry.Extensions, extension => Assert.DoesNotContain('.', extension));
        });

        Assert.Equal(MimeCatalog.All.OrderBy(entry => entry.MimeType, StringComparer.OrdinalIgnoreCase), MimeCatalog.All);
    }

    [Fact]
    public void Categories_CoverEverySupportedGroup()
    {
        Assert.Contains(MimeCategory.Image, _service.Categories);
        Assert.Contains(MimeCategory.Audio, _service.Categories);
        Assert.Contains(MimeCategory.Video, _service.Categories);
        Assert.Contains(MimeCategory.Document, _service.Categories);
        Assert.Contains(MimeCategory.Archive, _service.Categories);
        Assert.Contains(MimeCategory.Font, _service.Categories);
        Assert.Contains(MimeCategory.Code, _service.Categories);
        Assert.Contains(MimeCategory.Executable, _service.Categories);
    }
}
