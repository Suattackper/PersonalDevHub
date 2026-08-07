using DevhubWebApp.Models.Tools.Diff;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Behaviour of the line diff engine and its comparison options.</summary>
public class DiffEngineTests
{
    [Fact]
    public void Compare_ReportsIdenticalDocumentsAsUnchanged()
    {
        var result = DiffEngine.Compare("a\nb\nc", "a\nb\nc");

        Assert.True(result.Statistics.AreIdentical);
        Assert.Equal(3, result.Statistics.Unchanged);
        Assert.All(result.Lines, line => Assert.Equal(DiffLineKind.Unchanged, line.Kind));
    }

    [Fact]
    public void Compare_DetectsAnInsertedLineWithoutShiftingTheRest()
    {
        var result = DiffEngine.Compare("a\nc", "a\nb\nc");

        Assert.Equal(1, result.Statistics.Added);
        Assert.Equal(0, result.Statistics.Removed);
        Assert.Equal(2, result.Statistics.Unchanged);

        var added = Assert.Single(result.Lines, line => line.Kind == DiffLineKind.Added);
        Assert.Equal("b", added.Text);
        Assert.Equal(2, added.RightLineNumber);
        Assert.Null(added.LeftLineNumber);
    }

    [Fact]
    public void Compare_DetectsARemovedLine()
    {
        var result = DiffEngine.Compare("a\nb\nc", "a\nc");

        var removed = Assert.Single(result.Lines, line => line.Kind == DiffLineKind.Removed);
        Assert.Equal("b", removed.Text);
        Assert.Equal(2, removed.LeftLineNumber);
        Assert.Null(removed.RightLineNumber);
    }

    [Fact]
    public void Compare_TreatsAModifiedLineAsARemovalAndAnAddition()
    {
        var result = DiffEngine.Compare("a\nb", "a\nB!");

        Assert.Equal(1, result.Statistics.Added);
        Assert.Equal(1, result.Statistics.Removed);
        Assert.Equal(1, result.Statistics.Unchanged);
    }

    [Fact]
    public void IgnoreWhitespace_CollapsesRunsAndTrims()
    {
        var options = new DiffOptions { IgnoreWhitespace = true };

        Assert.True(DiffEngine.Compare("  hello   world ", "hello world", options).Statistics.AreIdentical);
        Assert.False(DiffEngine.Compare("  hello   world ", "hello world").Statistics.AreIdentical);
    }

    [Fact]
    public void IgnoreCase_MakesComparisonCaseInsensitive()
    {
        var options = new DiffOptions { IgnoreCase = true };

        Assert.True(DiffEngine.Compare("Hello", "hello", options).Statistics.AreIdentical);
        Assert.False(DiffEngine.Compare("Hello", "hello").Statistics.AreIdentical);
    }

    [Fact]
    public void IgnoreBlankLines_DropsEmptyLinesFromBothSides()
    {
        var options = new DiffOptions { IgnoreBlankLines = true };

        var result = DiffEngine.Compare("a\n\n\nb", "a\nb", options);

        Assert.True(result.Statistics.AreIdentical);
        Assert.Equal(2, result.Statistics.TotalLines);
    }

    [Fact]
    public void Compare_HandlesEmptySides()
    {
        Assert.Equal(2, DiffEngine.Compare(null, "a\nb").Statistics.Added);
        Assert.Equal(2, DiffEngine.Compare("a\nb", null).Statistics.Removed);
        Assert.Equal(DiffResult.Empty, DiffEngine.Compare(null, null));
    }

    [Fact]
    public void Compare_NormalisesLineEndings()
        => Assert.True(DiffEngine.Compare("a\r\nb", "a\nb").Statistics.AreIdentical);

    [Fact]
    public void ChangePercentage_ReflectsTheShareOfChangedLines()
    {
        var statistics = DiffEngine.Compare("a\nb\nc\nd", "a\nb\nc\nD").Statistics;

        Assert.Equal(2, statistics.Changed);
        Assert.Equal(5, statistics.TotalLines);
        Assert.Equal(40d, statistics.ChangePercentage);
    }

    /// <summary>
    /// Above the exact-diff limit the engine switches to a linear comparison. It must still find
    /// every difference — only the alignment quality degrades.
    /// </summary>
    [Fact]
    public void Compare_FallsBackToLinearComparisonForVeryLargeInputs()
    {
        var lines = Enumerable.Range(0, DiffEngine.MaxLinesForExactDiff + 100).Select(index => $"line {index}").ToList();
        var left = string.Join('\n', lines);

        lines[10] = "changed";
        var right = string.Join('\n', lines);

        var statistics = DiffEngine.Compare(left, right).Statistics;

        Assert.Equal(1, statistics.Added);
        Assert.Equal(1, statistics.Removed);
    }
}
