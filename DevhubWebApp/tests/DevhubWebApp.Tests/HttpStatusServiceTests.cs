using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Http;
using DevhubWebApp.Services;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Look-up, search and example-building rules of the HTTP status explorer.</summary>
public class HttpStatusServiceTests
{
    private readonly HttpStatusService _service = new();

    [Theory]
    [InlineData(200, "OK", HttpStatusClass.Success)]
    [InlineData(404, "Not Found", HttpStatusClass.ClientError)]
    [InlineData(500, "Internal Server Error", HttpStatusClass.ServerError)]
    [InlineData(301, "Moved Permanently", HttpStatusClass.Redirection)]
    [InlineData(101, "Switching Protocols", HttpStatusClass.Informational)]
    public void Find_ReturnsTheEntryWithItsClass(int code, string name, HttpStatusClass expected)
    {
        var entry = _service.Find(code);

        Assert.NotNull(entry);
        Assert.Equal(name, entry!.Name);
        Assert.Equal(expected, entry.Class);
        Assert.Equal($"{code} {name}", entry.Title);
    }

    [Fact]
    public void Find_ReturnsNullForAnUncataloguedCode()
        => Assert.Null(_service.Find(799));

    [Theory]
    [InlineData(100, HttpStatusClass.Informational)]
    [InlineData(299, HttpStatusClass.Success)]
    [InlineData(399, HttpStatusClass.Redirection)]
    [InlineData(499, HttpStatusClass.ClientError)]
    [InlineData(599, HttpStatusClass.ServerError)]
    public void ClassOf_MapsEveryRange(int code, HttpStatusClass expected)
        => Assert.Equal(expected, HttpStatusCatalog.ClassOf(code));

    [Theory]
    [InlineData(99)]
    [InlineData(600)]
    public void ClassOf_ReturnsNullOutsideTheValidRange(int code)
        => Assert.Null(HttpStatusCatalog.ClassOf(code));

    /// <summary>Typing a code must show that code, not the first one containing those digits.</summary>
    [Fact]
    public void Search_RanksAnExactCodeFirst()
        => Assert.Equal(404, _service.Search("404")[0].Code);

    [Fact]
    public void Search_MatchesNamesDescriptionsAndUseCases()
    {
        Assert.Contains(_service.Search("not found"), entry => entry.Code == 404);
        Assert.Contains(_service.Search("rate limited"), entry => entry.Code == 429);
        Assert.Contains(_service.Search("optimistic concurrency"), entry => entry.Code == 412);
    }

    [Fact]
    public void Search_MatchesACodePrefix()
    {
        var results = _service.Search("40");

        Assert.NotEmpty(results);
        Assert.All(results, entry => Assert.StartsWith("40", entry.Code.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
    }

    [Fact]
    public void Search_NarrowsToAClass()
    {
        var results = _service.Search(null, HttpStatusClass.ServerError);

        Assert.NotEmpty(results);
        Assert.All(results, entry => Assert.Equal(HttpStatusClass.ServerError, entry.Class));
    }

    [Fact]
    public void GetRelated_ResolvesEveryReferencedCode()
    {
        var related = _service.GetRelated(_service.Find(401)!);

        Assert.Contains(related, entry => entry.Code == 403);
    }

    /// <summary>A dangling reference would render an empty chip, so the catalogue must be consistent.</summary>
    [Fact]
    public void Catalogue_HasNoDanglingRelatedCodes()
    {
        foreach (var entry in HttpStatusCatalog.All)
        {
            Assert.Equal(entry.Related.Count, _service.GetRelated(entry).Count);
        }
    }

    [Fact]
    public void Catalogue_HasNoDuplicateCodesAndIsOrdered()
    {
        Assert.Equal(HttpStatusCatalog.All.Select(entry => entry.Code).Distinct().Count(), HttpStatusCatalog.All.Count);
        Assert.Equal(HttpStatusCatalog.All.OrderBy(entry => entry.Code), HttpStatusCatalog.All);
    }

    [Fact]
    public void BuildExample_StartsWithTheStatusLineAndIncludesRelevantHeaders()
    {
        var example = _service.BuildExample(_service.Find(301)!);

        Assert.StartsWith("HTTP/1.1 301 Moved Permanently", example, StringComparison.Ordinal);
        Assert.Contains("Location:", example, StringComparison.Ordinal);
    }

    /// <summary>204 and 304 must not carry a body; emitting one would be wrong to copy into a test.</summary>
    [Theory]
    [InlineData(204)]
    [InlineData(304)]
    [InlineData(100)]
    public void BuildExample_OmitsTheBodyWhereTheSpecForbidsIt(int code)
    {
        var example = _service.BuildExample(_service.Find(code)!);

        Assert.DoesNotContain("Content-Type: application/json", example, StringComparison.Ordinal);
        Assert.DoesNotContain("\"status\"", example, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildExample_IncludesAProblemBodyForErrors()
    {
        var example = _service.BuildExample(_service.Find(422)!);

        Assert.Contains("Content-Type: application/json", example, StringComparison.Ordinal);
        Assert.Contains("\"status\": 422", example, StringComparison.Ordinal);
    }

    [Fact]
    public void DescribeClass_LabelsEachRange()
        => Assert.Equal("4xx client error", HttpStatusCatalog.DescribeClass(HttpStatusClass.ClientError));
}
