using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Parsing, formatting and validation rules of the number base converter.</summary>
public class NumberBaseConverterTests
{
    [Theory]
    [InlineData("1010", NumberBase.Binary, 10)]
    [InlineData("777", NumberBase.Octal, 511)]
    [InlineData("255", NumberBase.Decimal, 255)]
    [InlineData("ff", NumberBase.Hexadecimal, 255)]
    [InlineData("FF", NumberBase.Hexadecimal, 255)]
    public void Parse_ReadsEachBase(string input, NumberBase numberBase, long expected)
        => Assert.Equal(expected, NumberBaseConverter.Parse(input, numberBase).Value);

    [Theory]
    [InlineData("0b1010", NumberBase.Binary, 10)]
    [InlineData("0o17", NumberBase.Octal, 15)]
    [InlineData("0xFF", NumberBase.Hexadecimal, 255)]
    public void Parse_AcceptsTheConventionalPrefix(string input, NumberBase numberBase, long expected)
        => Assert.Equal(expected, NumberBaseConverter.Parse(input, numberBase).Value);

    [Fact]
    public void Parse_AcceptsSeparatorsAndSigns()
    {
        Assert.Equal(255, NumberBaseConverter.Parse("1111_1111", NumberBase.Binary).Value);
        Assert.Equal(255, NumberBaseConverter.Parse("1111 1111", NumberBase.Binary).Value);
        Assert.Equal(-42, NumberBaseConverter.Parse("-42", NumberBase.Decimal).Value);
        Assert.Equal(42, NumberBaseConverter.Parse("+42", NumberBase.Decimal).Value);
    }

    [Theory]
    [InlineData("102", NumberBase.Binary)]
    [InlineData("99", NumberBase.Octal)]
    [InlineData("12a", NumberBase.Decimal)]
    [InlineData("gg", NumberBase.Hexadecimal)]
    public void Parse_RejectsDigitsOutsideTheBase(string input, NumberBase numberBase)
    {
        var result = NumberBaseConverter.Parse(input, numberBase);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Parse_ReportsOverflowInsteadOfThrowing()
    {
        var result = NumberBaseConverter.Parse(new string('f', 20), NumberBase.Hexadecimal);

        Assert.False(result.IsSuccess);
        Assert.Contains("too large", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_TreatsBlankInputAsNoValueRatherThanAnError()
    {
        var result = NumberBaseConverter.Parse("   ", NumberBase.Decimal);

        Assert.False(result.HasValue);
        Assert.Null(result.Error);
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [InlineData(10, NumberBase.Binary, "1010")]
    [InlineData(511, NumberBase.Octal, "777")]
    [InlineData(255, NumberBase.Decimal, "255")]
    [InlineData(255, NumberBase.Hexadecimal, "ff")]
    [InlineData(0, NumberBase.Binary, "0")]
    public void Format_RendersEachBase(long value, NumberBase numberBase, string expected)
        => Assert.Equal(expected, NumberBaseConverter.Format(value, numberBase));

    [Fact]
    public void Format_CanIncludeThePrefix()
        => Assert.Equal("0xff", NumberBaseConverter.Format(255, NumberBase.Hexadecimal, includePrefix: true));

    [Fact]
    public void Format_HandlesNegativeValuesAndTheLongBoundaries()
    {
        Assert.Equal("-1010", NumberBaseConverter.Format(-10, NumberBase.Binary));
        Assert.Equal("-ff", NumberBaseConverter.Format(-255, NumberBase.Hexadecimal));
        Assert.Equal("7fffffffffffffff", NumberBaseConverter.Format(long.MaxValue, NumberBase.Hexadecimal));
        Assert.Equal("-8000000000000000", NumberBaseConverter.Format(long.MinValue, NumberBase.Hexadecimal));
    }

    [Fact]
    public void ParseAndFormat_RoundTripAcrossBases()
    {
        var value = NumberBaseConverter.Parse("deadbeef", NumberBase.Hexadecimal).Value;

        Assert.Equal(3735928559, value);
        Assert.Equal("deadbeef", NumberBaseConverter.Format(value, NumberBase.Hexadecimal));
        Assert.Equal("33653337357", NumberBaseConverter.Format(value, NumberBase.Octal));
    }

    [Theory]
    [InlineData(65, "A")]
    [InlineData(0x1F600, "😀")]
    public void ToCharacter_RendersValidScalars(long value, string expected)
        => Assert.Equal(expected, NumberBaseConverter.ToCharacter(value));

    [Theory]
    [InlineData(-1)]
    [InlineData(0xD800)]
    [InlineData(0x110000)]
    public void ToCharacter_ReturnsEmptyForValuesThatAreNotScalars(long value)
        => Assert.Equal(string.Empty, NumberBaseConverter.ToCharacter(value));

    [Fact]
    public void ToCodePoint_FormatsWithTheUnicodeConvention()
    {
        Assert.Equal("U+0041", NumberBaseConverter.ToCodePoint(65));
        Assert.Equal(string.Empty, NumberBaseConverter.ToCodePoint(0xD800));
    }

    [Fact]
    public void TextToCodePoints_EnumeratesRunesNotUtf16Units()
    {
        Assert.Equal("U+0041 U+0042", NumberBaseConverter.TextToCodePoints("AB"));
        Assert.Equal("U+1F600", NumberBaseConverter.TextToCodePoints("😀"));
    }

    [Fact]
    public void TextToCodePoints_ReturnsEmptyForBlankInput()
        => Assert.Equal(string.Empty, NumberBaseConverter.TextToCodePoints(null));

    [Fact]
    public void GetPrefixAndName_DescribeEachBase()
    {
        Assert.Equal("0x", NumberBaseConverter.GetPrefix(NumberBase.Hexadecimal));
        Assert.Equal(string.Empty, NumberBaseConverter.GetPrefix(NumberBase.Decimal));
        Assert.Equal("Binary", NumberBaseConverter.GetName(NumberBase.Binary));
    }
}
