using System.Text.Json;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Generation, formatting and export rules of the UUID tool.</summary>
public class UuidGeneratorTests
{
    private static readonly Guid Sample = Guid.Parse("3f2a1b4c-5d6e-4f80-9a1b-2c3d4e5f6071");

    [Fact]
    public void Generate_ReturnsTheRequestedCountOfDistinctValues()
    {
        var identifiers = UuidGenerator.Generate(50);

        Assert.Equal(50, identifiers.Count);
        Assert.Equal(50, identifiers.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>Version 4 is identified by the '4' at position 14 and a variant nibble of 8, 9, a or b.</summary>
    [Fact]
    public void Generate_ProducesVersion4Identifiers()
    {
        foreach (var identifier in UuidGenerator.Generate(20))
        {
            Assert.Equal('4', identifier[14]);
            Assert.Contains(char.ToLowerInvariant(identifier[19]), "89ab");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(UuidGenerator.MaxBatchSize + 1)]
    public void Generate_RejectsCountsOutsideTheSupportedRange(int count)
        => Assert.Throws<ArgumentOutOfRangeException>(() => UuidGenerator.Generate(count));

    [Fact]
    public void Generate_AcceptsTheMaximumBatch()
        => Assert.Equal(UuidGenerator.MaxBatchSize, UuidGenerator.Generate(UuidGenerator.MaxBatchSize).Count);

    [Theory]
    [InlineData(UuidFormat.Standard, "3f2a1b4c-5d6e-4f80-9a1b-2c3d4e5f6071")]
    [InlineData(UuidFormat.Uppercase, "3F2A1B4C-5D6E-4F80-9A1B-2C3D4E5F6071")]
    [InlineData(UuidFormat.Compact, "3f2a1b4c5d6e4f809a1b2c3d4e5f6071")]
    [InlineData(UuidFormat.Braced, "{3f2a1b4c-5d6e-4f80-9a1b-2c3d4e5f6071}")]
    [InlineData(UuidFormat.Urn, "urn:uuid:3f2a1b4c-5d6e-4f80-9a1b-2c3d4e5f6071")]
    public void Format_RendersEachSupportedLayout(UuidFormat format, string expected)
        => Assert.Equal(expected, UuidGenerator.Format(Sample, format));

    [Fact]
    public void Generate_AppliesTheRequestedFormat()
        => Assert.All(UuidGenerator.Generate(5, UuidFormat.Compact), identifier => Assert.Equal(32, identifier.Length));

    [Fact]
    public void ToText_WritesOneIdentifierPerLine()
    {
        var text = UuidGenerator.ToText(["a", "b", "c"]);

        Assert.Equal("a\nb\nc", text);
    }

    [Fact]
    public void ToCsv_WritesAHeaderAndAOneBasedIndex()
    {
        var csv = UuidGenerator.ToCsv(["first", "second"]).ReplaceLineEndings("\n");

        Assert.StartsWith("index,uuid\n", csv, StringComparison.Ordinal);
        Assert.Contains("1,\"first\"\n", csv, StringComparison.Ordinal);
        Assert.Contains("2,\"second\"\n", csv, StringComparison.Ordinal);
    }

    [Fact]
    public void ToCsv_EscapesEmbeddedQuotes()
        => Assert.Contains("\"a\"\"b\"", UuidGenerator.ToCsv(["a\"b"]), StringComparison.Ordinal);

    [Fact]
    public void ToJson_ProducesAReadableArray()
    {
        var json = UuidGenerator.ToJson(["a", "b"]);

        Assert.Equal(["a", "b"], JsonSerializer.Deserialize<string[]>(json));
        Assert.Contains("\n", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Exporters_RejectNullInput()
    {
        Assert.Throws<ArgumentNullException>(() => UuidGenerator.ToText(null!));
        Assert.Throws<ArgumentNullException>(() => UuidGenerator.ToCsv(null!));
        Assert.Throws<ArgumentNullException>(() => UuidGenerator.ToJson(null!));
    }

    [Fact]
    public void GetName_DescribesEachFormat()
        => Assert.Equal("No hyphens", UuidGenerator.GetName(UuidFormat.Compact));
}
