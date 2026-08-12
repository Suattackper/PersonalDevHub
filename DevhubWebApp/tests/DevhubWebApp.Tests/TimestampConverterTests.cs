using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Conversion, parsing and time-zone rules of the timestamp tool.</summary>
public class TimestampConverterTests
{
    private static readonly DateTimeOffset Reference =
        new(2024, 11, 30, 9, 15, 0, TimeSpan.Zero);

    [Fact]
    public void ToUnix_AndFromUnix_RoundTripInBothUnits()
    {
        var seconds = TimestampConverter.ToUnix(Reference, TimestampUnit.Seconds);
        var milliseconds = TimestampConverter.ToUnix(Reference, TimestampUnit.Milliseconds);

        Assert.Equal(1732958100, seconds);
        Assert.Equal(1732958100000, milliseconds);
        Assert.Equal(Reference, TimestampConverter.FromUnix(seconds, TimestampUnit.Seconds));
        Assert.Equal(Reference, TimestampConverter.FromUnix(milliseconds, TimestampUnit.Milliseconds));
    }

    [Theory]
    [InlineData(1732958100, TimestampUnit.Seconds)]
    [InlineData(1732958100000, TimestampUnit.Milliseconds)]
    [InlineData(0, TimestampUnit.Seconds)]
    [InlineData(-1000, TimestampUnit.Seconds)]
    public void DetectUnit_TellsSecondsFromMilliseconds(long timestamp, TimestampUnit expected)
        => Assert.Equal(expected, TimestampConverter.DetectUnit(timestamp));

    [Fact]
    public void ParseTimestamp_AutoDetectsTheUnit()
    {
        Assert.Equal(Reference, TimestampConverter.ParseTimestamp("1732958100").Value);
        Assert.Equal(Reference, TimestampConverter.ParseTimestamp("1732958100000").Value);
    }

    [Fact]
    public void ParseTimestamp_HonoursAnExplicitUnit()
        => Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1000),
            TimestampConverter.ParseTimestamp("1000", TimestampUnit.Milliseconds).Value);

    [Fact]
    public void ParseTimestamp_AcceptsSeparatorsAndRejectsNonNumbers()
    {
        Assert.True(TimestampConverter.ParseTimestamp("1_732_958_100").IsSuccess);

        var failure = TimestampConverter.ParseTimestamp("not a number");
        Assert.False(failure.IsSuccess);
        Assert.NotNull(failure.Error);
    }

    [Fact]
    public void ParseTimestamp_ReportsOutOfRangeValues()
        => Assert.False(TimestampConverter.ParseTimestamp(long.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture)).IsSuccess);

    [Fact]
    public void ParseTimestamp_TreatsBlankInputAsNoValue()
    {
        var result = TimestampConverter.ParseTimestamp("  ");

        Assert.False(result.HasValue);
        Assert.Null(result.Error);
    }

    [Theory]
    [InlineData("2024-11-30T09:15:00Z")]
    [InlineData("2024-11-30 09:15:00")]
    [InlineData("2024-11-30T09:15:00+00:00")]
    public void ParseDate_ReadsIsoAndCommonLayouts(string input)
        => Assert.Equal(Reference, TimestampConverter.ParseDate(input).Value);

    /// <summary>A value without an offset is read as UTC, so results never depend on the host.</summary>
    [Fact]
    public void ParseDate_AssumesUtcWhenNoOffsetIsGiven()
        => Assert.Equal(TimeSpan.Zero, TimestampConverter.ParseDate("2024-11-30 09:15:00").Value.Offset);

    [Fact]
    public void ParseDate_ReportsUnreadableInput()
        => Assert.False(TimestampConverter.ParseDate("the day before yesterday").IsSuccess);

    [Fact]
    public void Formatters_ProduceTheExpectedShapes()
    {
        Assert.Equal("2024-11-30T09:15:00.000Z", TimestampConverter.ToIso8601(Reference));
        Assert.Equal("Sat, 30 Nov 2024 09:15:00 GMT", TimestampConverter.ToRfc1123(Reference));
        Assert.Equal("2024-11-30 09:15:00", TimestampConverter.ToSortable(Reference));
        Assert.Contains("30 November 2024", TimestampConverter.ToLongForm(Reference), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-30, "a few seconds ago")]
    [InlineData(-300, "5 minutes ago")]
    [InlineData(-7200, "2 hours ago")]
    [InlineData(-259200, "3 days ago")]
    [InlineData(300, "in 5 minutes")]
    [InlineData(7200, "in 2 hours")]
    public void ToRelative_DescribesTheDistanceInWords(int offsetSeconds, string expected)
    {
        var value = Reference.AddSeconds(offsetSeconds);

        Assert.Equal(expected, TimestampConverter.ToRelative(value, Reference));
    }

    [Fact]
    public void ToRelative_ScalesToMonthsAndYears()
    {
        Assert.Equal("2 months ago", TimestampConverter.ToRelative(Reference.AddDays(-70), Reference));
        Assert.Equal("in 2 years", TimestampConverter.ToRelative(Reference.AddDays(800), Reference));
    }

    /// <summary>A count of one must read "1 year ago", not "1 years ago".</summary>
    [Theory]
    [InlineData(400, "in 1 year")]
    [InlineData(-400, "1 year ago")]
    [InlineData(-40, "1 month ago")]
    [InlineData(-2, "2 days ago")]
    public void ToRelative_KeepsTheSingularForACountOfOne(int offsetDays, string expected)
        => Assert.Equal(expected, TimestampConverter.ToRelative(Reference.AddDays(offsetDays), Reference));

    [Fact]
    public void ToZone_FallsBackToUtcForAnUnknownZone()
    {
        var converted = TimestampConverter.ToZone(Reference, "Not/AZone");

        Assert.Equal(TimeSpan.Zero, converted.Offset);
        Assert.Equal(Reference.UtcDateTime, converted.UtcDateTime);
    }

    [Fact]
    public void ToZone_KeepsTheSameInstantWhateverTheOffset()
    {
        var zone = TimestampConverter.GetTimeZoneIds().First();
        var converted = TimestampConverter.ToZone(Reference, zone);

        Assert.Equal(Reference.UtcDateTime, converted.UtcDateTime);
    }

    [Fact]
    public void DescribeOffset_RendersAUtcOffset()
    {
        var described = TimestampConverter.DescribeOffset(Reference, TimeZoneInfo.Utc.Id);

        Assert.Equal("UTC+00:00", described);
    }

    /// <summary>The zone list must never be empty, even on a runtime without time-zone data.</summary>
    [Fact]
    public void GetTimeZoneIds_AlwaysOffersAtLeastUtc()
    {
        var zones = TimestampConverter.GetTimeZoneIds();

        Assert.NotEmpty(zones);
        Assert.Equal(zones.OrderBy(id => id, StringComparer.OrdinalIgnoreCase), zones);
    }
}

