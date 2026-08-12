using DevhubWebApp.Models.Tools.Cron;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Parsing, validation, description and scheduling rules of the cron builder.</summary>
public class CronParserTests
{
    private static readonly DateTimeOffset Reference = new(2024, 11, 30, 9, 15, 0, TimeSpan.Zero);

    [Fact]
    public void Parse_ExpandsEveryFieldOfALinuxExpression()
    {
        var expression = CronParser.Parse("*/15 9-17 * * 1-5");

        Assert.True(expression.IsValid);
        Assert.Equal(CronDialect.Linux, expression.Dialect);
        Assert.Equal([0, 15, 30, 45], expression.Minutes);
        Assert.Equal([9, 10, 11, 12, 13, 14, 15, 16, 17], expression.Hours);
        Assert.Equal([1, 2, 3, 4, 5], expression.DaysOfWeek);
        Assert.Equal(31, expression.DaysOfMonth.Count);
        Assert.Equal(12, expression.Months.Count);
    }

    [Fact]
    public void Parse_InfersTheDialectFromTheFieldCount()
    {
        Assert.Equal(CronDialect.Linux, CronParser.Parse("0 9 * * *").Dialect);
        Assert.Equal(CronDialect.Quartz, CronParser.Parse("0 0 9 * * ?").Dialect);
        Assert.Equal(CronDialect.Quartz, CronParser.Parse("0 0 9 * * ? 2025").Dialect);
    }

    [Fact]
    public void Parse_ReadsTheQuartzSecondsField()
    {
        var expression = CronParser.Parse("*/30 0 9 * * ?");

        Assert.True(expression.IsValid);
        Assert.Equal([0, 30], expression.Seconds);
    }

    [Fact]
    public void Parse_AcceptsMonthAndDayNames()
    {
        var expression = CronParser.Parse("0 9 * JAN-MAR MON,FRI");

        Assert.Equal([1, 2, 3], expression.Months);
        Assert.Equal([1, 5], expression.DaysOfWeek);
    }

    [Fact]
    public void Parse_TreatsSevenAndZeroAsSundayInLinux()
        => Assert.Equal([0], CronParser.Parse("0 9 * * 7").DaysOfWeek);

    /// <summary>Quartz numbers Sunday as 1; the parser normalises to the .NET 0-6 convention.</summary>
    [Fact]
    public void Parse_NormalisesQuartzDayNumbering()
        => Assert.Equal([1], CronParser.Parse("0 0 9 ? * 2").DaysOfWeek);

    [Theory]
    [InlineData("0 9 * *", "5 fields")]
    [InlineData("60 9 * * *", "0-59")]
    [InlineData("0 25 * * *", "0-23")]
    [InlineData("0 9 * * 1-", "not a number")]
    [InlineData("0 9 * * 5-1", "reversed range")]
    [InlineData("*/0 9 * * *", "invalid step")]
    public void Parse_ReportsInvalidExpressionsWithAReason(string expression, string expectedFragment)
    {
        var parsed = CronParser.Parse(expression);

        Assert.False(parsed.IsValid);
        Assert.Contains(expectedFragment, parsed.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_ReturnsEmptyForBlankInput()
        => Assert.Same(CronExpression.Empty, CronParser.Parse("   "));

    [Fact]
    public void IsValid_MirrorsParse()
    {
        Assert.True(CronParser.IsValid("0 9 * * *"));
        Assert.False(CronParser.IsValid("nonsense"));
    }

    [Fact]
    public void GetNextOccurrences_ReturnsTheRequestedNumberInOrder()
    {
        var occurrences = CronParser.GetNextOccurrences(CronParser.Parse("0 9 * * *"), Reference, 3);

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateTimeOffset(2024, 12, 1, 9, 0, 0, TimeSpan.Zero), occurrences[0]);
        Assert.Equal(new DateTimeOffset(2024, 12, 2, 9, 0, 0, TimeSpan.Zero), occurrences[1]);
        Assert.True(occurrences.SequenceEqual(occurrences.OrderBy(value => value)));
    }

    [Fact]
    public void GetNextOccurrences_NeverReturnsTheStartingInstant()
    {
        var occurrences = CronParser.GetNextOccurrences(CronParser.Parse("15 9 * * *"), Reference, 1);

        Assert.True(occurrences[0] > Reference);
    }

    [Fact]
    public void GetNextOccurrences_HonoursWeekdayRestrictions()
    {
        var occurrences = CronParser.GetNextOccurrences(CronParser.Parse("0 9 * * 1-5"), Reference, 3);

        Assert.All(occurrences, value => Assert.InRange((int)value.DayOfWeek, 1, 5));
    }

    /// <summary>Cron unions the two day fields when both are restricted — a classic trap.</summary>
    [Fact]
    public void GetNextOccurrences_UnionsBothDayFieldsWhenBothAreRestricted()
    {
        var expression = CronParser.Parse("0 9 1 * MON");

        Assert.True(expression.DayUnion);

        var occurrences = CronParser.GetNextOccurrences(expression, Reference, 5);

        Assert.All(occurrences, value => Assert.True(value.Day == 1 || value.DayOfWeek == DayOfWeek.Monday));
        Assert.Contains(occurrences, value => value.DayOfWeek == DayOfWeek.Monday && value.Day != 1);
    }

    [Fact]
    public void GetNextOccurrences_TerminatesForAnUnsatisfiableSchedule()
    {
        // 30 February never occurs; the search must give up rather than loop.
        var occurrences = CronParser.GetNextOccurrences(CronParser.Parse("0 9 30 2 *"), Reference, 3);

        Assert.Empty(occurrences);
    }

    [Fact]
    public void GetNextOccurrences_StepsBySecondsForQuartzSecondSchedules()
    {
        var occurrences = CronParser.GetNextOccurrences(CronParser.Parse("*/30 * * * * ?"), Reference, 3);

        Assert.Equal(3, occurrences.Count);
        Assert.All(occurrences, value => Assert.Contains(value.Second, new[] { 0, 30 }));
    }

    [Fact]
    public void GetNextOccurrences_ReturnsNothingForAnInvalidExpression()
        => Assert.Empty(CronParser.GetNextOccurrences(CronParser.Parse("bad"), Reference, 3));

    [Fact]
    public void Matches_ChecksEveryField()
    {
        var expression = CronParser.Parse("15 9 30 11 *");

        Assert.True(CronParser.Matches(expression, Reference));
        Assert.False(CronParser.Matches(expression, Reference.AddMinutes(1)));
        Assert.False(CronParser.Matches(expression, Reference.AddMonths(1)));
    }

    [Theory]
    [InlineData("* * * * *", "Every minute")]
    [InlineData("0 9 * * *", "At 09:00")]
    [InlineData("0 9 * * 1-5", "Monday")]
    [InlineData("0 0 1 1 *", "January")]
    public void Describe_ProducesReadableEnglish(string expression, string expectedFragment)
        => Assert.Contains(expectedFragment, CronParser.Describe(CronParser.Parse(expression)), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Describe_SaysEveryDayWhenNeitherDayFieldIsRestricted()
        => Assert.Contains("every day", CronParser.Describe(CronParser.Parse("0 9 * * *")), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void Describe_ReportsTheErrorForAnInvalidExpression()
        => Assert.Contains("fields", CronParser.Describe(CronParser.Parse("0 9 * *")), StringComparison.OrdinalIgnoreCase);

    [Fact]
    public void GetNextOccurrences_RejectsANonPositiveCount()
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => CronParser.GetNextOccurrences(CronParser.Parse("0 9 * * *"), Reference, 0));
}
