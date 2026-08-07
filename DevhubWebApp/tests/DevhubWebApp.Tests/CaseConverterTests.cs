using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Tokenising and casing rules of the case converter.</summary>
public class CaseConverterTests
{
    [Theory]
    [InlineData("hello world", new[] { "hello", "world" })]
    [InlineData("helloWorld", new[] { "hello", "World" })]
    [InlineData("HelloWorld", new[] { "Hello", "World" })]
    [InlineData("hello_world-again", new[] { "hello", "world", "again" })]
    [InlineData("  spaced   out  ", new[] { "spaced", "out" })]
    [InlineData("value2Name", new[] { "value", "2", "Name" })]
    public void Tokenize_SplitsOnSeparatorsAndCamelBoundaries(string input, string[] expected)
        => Assert.Equal(expected, CaseConverter.Tokenize(input));

    /// <summary>Acronym runs must not be shattered into single letters.</summary>
    [Fact]
    public void Tokenize_KeepsAcronymsTogether()
        => Assert.Equal(["HTTP", "Response", "Code"], CaseConverter.Tokenize("HTTPResponseCode"));

    [Fact]
    public void Tokenize_ReturnsEmptyForBlankInput()
        => Assert.Empty(CaseConverter.Tokenize("   "));

    [Theory]
    [InlineData(TextCase.Camel, "helloWorldAgain")]
    [InlineData(TextCase.Pascal, "HelloWorldAgain")]
    [InlineData(TextCase.Snake, "hello_world_again")]
    [InlineData(TextCase.Kebab, "hello-world-again")]
    [InlineData(TextCase.Constant, "HELLO_WORLD_AGAIN")]
    [InlineData(TextCase.Title, "Hello World Again")]
    public void Convert_ProducesEachNamingStyle(TextCase style, string expected)
        => Assert.Equal(expected, CaseConverter.Convert("hello world again", style));

    [Fact]
    public void Convert_RoundTripsBetweenStyles()
    {
        const string source = "parseHTTPResponseCode";

        Assert.Equal("parse_http_response_code", CaseConverter.Convert(source, TextCase.Snake));
        Assert.Equal("ParseHttpResponseCode", CaseConverter.Convert(source, TextCase.Pascal));
        Assert.Equal("parse-http-response-code", CaseConverter.Convert(source, TextCase.Kebab));
    }

    [Theory]
    [InlineData(TextCase.Lower, "hello world! it's here.")]
    [InlineData(TextCase.Upper, "HELLO WORLD! IT'S HERE.")]
    public void Convert_AppliesWholeTextStylesWithoutTokenising(TextCase style, string expected)
        => Assert.Equal(expected, CaseConverter.Convert("Hello World! It's here.", style));

    [Fact]
    public void SentenceCase_CapitalisesTheFirstLetterOfEachLine()
    {
        var result = CaseConverter.Convert("HELLO world.\n  second LINE", TextCase.Sentence);

        Assert.Equal("Hello world.\n  Second line", result);
    }

    [Fact]
    public void ToggleCase_FlipsEveryLetter()
        => Assert.Equal("hELLO wORLD", CaseConverter.Convert("Hello World", TextCase.Toggle));

    [Fact]
    public void Convert_ProcessesEachLineIndependently()
    {
        var result = CaseConverter.Convert("first line\nsecond line", TextCase.Snake);

        Assert.Equal("first_line\nsecond_line", result);
    }

    [Fact]
    public void Convert_ReturnsEmptyForBlankInput()
        => Assert.Equal(string.Empty, CaseConverter.Convert(null, TextCase.Camel));

    [Fact]
    public void TrimLines_RemovesSurroundingWhitespacePerLine()
        => Assert.Equal("one\ntwo", CaseConverter.TrimLines("  one  \n\ttwo\t"));

    [Fact]
    public void RemoveEmptyLines_DropsWhitespaceOnlyLines()
        => Assert.Equal("one\ntwo", CaseConverter.RemoveEmptyLines("one\n\n   \ntwo"));

    [Fact]
    public void CollapseSpaces_ReducesRunsWithinEachLine()
        => Assert.Equal("one two\nthree four", CaseConverter.CollapseSpaces("one    two\nthree \t four"));

    [Fact]
    public void TextUtilities_NormaliseLineEndings()
        => Assert.Equal("one\ntwo", CaseConverter.TrimLines("one\r\ntwo"));

    [Fact]
    public void GetName_ShowsTheStyleInItsOwnNotation()
    {
        Assert.Equal("snake_case", CaseConverter.GetName(TextCase.Snake));
        Assert.Equal("CONSTANT_CASE", CaseConverter.GetName(TextCase.Constant));
    }

    [Fact]
    public void All_ExposesEveryStyle()
        => Assert.Equal(Enum.GetValues<TextCase>().Length, CaseConverter.All.Count);
}
