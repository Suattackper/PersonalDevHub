using DevhubWebApp.Models.Tools.Json;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>
/// Behaviour of the JSON formatter. These cover the guarantees the tool advertises: valid input is
/// rewritten faithfully, invalid input reports a usable position, and the optional transforms only
/// do what they claim.
/// </summary>
public class JsonFormatterTests
{
    [Fact]
    public void Pretty_IndentsNestedStructures()
    {
        var result = JsonFormatter.Pretty("""{"a":1,"b":[1,2]}""");

        Assert.True(result.IsSuccess);
        Assert.Equal(
            """
            {
              "a": 1,
              "b": [
                1,
                2
              ]
            }
            """.ReplaceLineEndings("\n"),
            result.Output.ReplaceLineEndings("\n"));
    }

    [Fact]
    public void Minify_RemovesInsignificantWhitespace()
    {
        var result = JsonFormatter.Minify("{\n  \"a\" : 1,\n  \"b\" : \"x y\"\n}");

        Assert.True(result.IsSuccess);
        Assert.Equal("""{"a":1,"b":"x y"}""", result.Output);
    }

    [Fact]
    public void Minify_PreservesNumberPrecisionAndNonAsciiText()
    {
        const string source = """{"big":12345678901234567890,"decimal":1.7976931348623157E+308,"text":"chào bạn","slash":"a/b"}""";

        var result = JsonFormatter.Minify(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(source, result.Output);
    }

    /// <summary>
    /// Characters outside the BMP come back as escape sequences: no built-in .NET encoder emits
    /// them raw. The round-trip is lossless, which is the guarantee that actually matters.
    /// </summary>
    [Fact]
    public void Minify_EmitsAstralCharactersAsLosslessEscapes()
    {
        var result = JsonFormatter.Minify("""{"text":"😀"}""");

        Assert.True(result.IsSuccess);
        Assert.Equal("{\"text\":\"\\uD83D\\uDE00\"}", result.Output);
        Assert.Equal("😀", System.Text.Json.JsonDocument.Parse(result.Output).RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public void Pretty_KeepsEscapesIntact()
    {
        var result = JsonFormatter.Minify("""{"quote":"say \"hi\"","tab":"a\tb"}""");

        Assert.True(result.IsSuccess);
        Assert.Equal("""{"quote":"say \"hi\"","tab":"a\tb"}""", result.Output);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Pretty_TreatsBlankInputAsEmptyOutput(string? input)
    {
        var result = JsonFormatter.Pretty(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(string.Empty, result.Output);
    }

    [Fact]
    public void Pretty_ReportsOneBasedPositionOfSyntaxError()
    {
        var result = JsonFormatter.Pretty("{\n  \"a\": 1,\n  \"b\" 2\n}");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal(3, result.Error!.Line);
        Assert.True(result.Error.Column > 1);
        Assert.DoesNotContain("LineNumber:", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_RejectsTrailingCommasAndComments()
    {
        Assert.NotNull(JsonFormatter.Validate("""{"a":1,}"""));
        Assert.NotNull(JsonFormatter.Validate("""{"a":1} // trailing"""));
    }

    [Fact]
    public void Validate_AcceptsValidDocumentAndBlankInput()
    {
        Assert.Null(JsonFormatter.Validate("""{"a":[1,{"b":null}]}"""));
        Assert.Null(JsonFormatter.Validate("   "));
    }

    [Fact]
    public void SortProperties_OrdersRecursivelyWithoutTouchingArrays()
    {
        var options = new JsonFormatOptions { SortProperties = true };

        var result = JsonFormatter.Minify("""{"b":1,"a":{"d":2,"c":[3,1,2]}}""", options);

        Assert.True(result.IsSuccess);
        Assert.Equal("""{"a":{"c":[3,1,2],"d":2},"b":1}""", result.Output);
    }

    [Fact]
    public void RemoveDuplicateProperties_KeepsLastOccurrenceAndCountsRemovals()
    {
        var options = new JsonFormatOptions { RemoveDuplicateProperties = true };

        var result = JsonFormatter.Minify("""{"a":1,"b":2,"a":3}""", options);

        Assert.True(result.IsSuccess);
        Assert.Equal("""{"a":3,"b":2}""", result.Output);
        Assert.Equal(1, result.RemovedDuplicates);
    }

    [Fact]
    public void RemoveDuplicateProperties_IsOffByDefault()
    {
        var result = JsonFormatter.Minify("""{"a":1,"a":2}""");

        Assert.True(result.IsSuccess);
        Assert.Equal("""{"a":1,"a":2}""", result.Output);
        Assert.Equal(0, result.RemovedDuplicates);
    }

    [Fact]
    public void CountDuplicateProperties_CountsNestedOccurrences()
    {
        const string source = """{"a":1,"a":2,"child":{"b":1,"b":2,"b":3},"list":[{"c":1,"c":2}]}""";

        Assert.Equal(4, JsonFormatter.CountDuplicateProperties(source));
    }

    [Fact]
    public void CountDuplicateProperties_ReturnsZeroForInvalidInput()
        => Assert.Equal(0, JsonFormatter.CountDuplicateProperties("{ nope"));

    [Fact]
    public void Inspect_ReportsValidityDuplicatesAndTreeInOnePass()
    {
        var inspection = JsonFormatter.Inspect("""{"a":1,"a":2,"child":{"b":[1,2]}}""");

        Assert.True(inspection.IsValid);
        Assert.Equal(1, inspection.DuplicateCount);
        Assert.NotNull(inspection.Tree);
        Assert.Equal("root", inspection.Tree!.Name);

        // The tree mirrors the document as written, duplicates included — it is a view of the
        // input, not of the de-duplicated output.
        Assert.Equal(3, inspection.Tree.Children.Count);
    }

    [Fact]
    public void Inspect_SkipsTheTreeWhenNotRequested()
    {
        var inspection = JsonFormatter.Inspect("""{"a":1}""", buildTree: false);

        Assert.True(inspection.IsValid);
        Assert.Null(inspection.Tree);
    }

    [Fact]
    public void Inspect_ReportsTheErrorAndNoTreeForInvalidInput()
    {
        var inspection = JsonFormatter.Inspect("{\"a\":}");

        Assert.False(inspection.IsValid);
        Assert.Null(inspection.Tree);
        Assert.Equal(0, inspection.DuplicateCount);
    }

    [Fact]
    public void Inspect_TreatsBlankInputAsEmpty()
        => Assert.Same(JsonInspection.Empty, JsonFormatter.Inspect("  "));

    [Fact]
    public void Minify_HandlesLargeDocumentsWithoutLosingData()
    {
        var items = Enumerable.Range(0, 20_000)
            .Select(index => $$"""{"id":{{index}},"name":"item-{{index}}"}""");
        var source = $"[{string.Join(',', items)}]";

        var result = JsonFormatter.Minify(source);

        Assert.True(result.IsSuccess);
        Assert.Equal(source, result.Output);
    }
}
