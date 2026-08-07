using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Regex;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Matching, flag translation and replacement behaviour of the regex tester.</summary>
public class RegexTesterTests
{
    [Fact]
    public void Match_FindsEveryOccurrenceWithTheGlobalFlag()
    {
        var result = RegexTester.Match(@"\d+", "a1 b22 c333");

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.MatchCount);
        Assert.Equal(["1", "22", "333"], result.Matches.Select(match => match.Value));
    }

    [Fact]
    public void Match_StopsAtTheFirstResultWithoutTheGlobalFlag()
    {
        var result = RegexTester.Match(@"\d+", "a1 b22", new RegexFlags { Global = false });

        Assert.Equal(1, result.MatchCount);
    }

    [Fact]
    public void Match_ReportsPositionsAndLengths()
    {
        var match = Assert.Single(RegexTester.Match("b22", "a1 b22").Matches);

        Assert.Equal(3, match.Index);
        Assert.Equal(3, match.Length);
    }

    [Fact]
    public void Match_ExposesNumberedAndNamedGroups()
    {
        var result = RegexTester.Match(@"(?<key>\w+)=(\w+)", "host=local");

        var match = Assert.Single(result.Matches);
        Assert.Contains(match.Groups, group => group.Name == "key" && group.Value == "host");
        Assert.Contains(match.Groups, group => group.Name == "Group 1" && group.Value == "local");
        Assert.DoesNotContain(match.Groups, group => group.Name == "0");
    }

    [Fact]
    public void Match_MarksGroupsThatDidNotParticipate()
    {
        var result = RegexTester.Match(@"(a)|(b)", "b");

        var match = Assert.Single(result.Matches);
        Assert.Contains(match.Groups, group => group.Name == "Group 1" && !group.Success);
    }

    [Fact]
    public void Match_ReportsInvalidPatternsWithoutThrowing()
    {
        var result = RegexTester.Match("(unclosed", "text");

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public void Match_ReturnsEmptyForABlankPattern()
        => Assert.Same(RegexResult.Empty, RegexTester.Match("", "text"));

    [Fact]
    public void Match_HandlesAZeroWidthPatternWithoutLooping()
    {
        var result = RegexTester.Match(@"^", "one\ntwo", new RegexFlags { Multiline = true });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.MatchCount);
    }

    /// <summary>
    /// The safety guarantee of the whole tool: a catastrophically backtracking pattern is stopped
    /// instead of freezing the browser.
    /// </summary>
    [Fact]
    public void Match_StopsCatastrophicBacktracking()
    {
        var result = RegexTester.Match(@"^(a+)+$", new string('a', 40) + "!");

        Assert.False(result.IsSuccess);
        Assert.True(result.TimedOut);
    }

    [Fact]
    public void IgnoreCase_AndMultiline_AndSingleline_ChangeMatching()
    {
        Assert.Equal(1, RegexTester.Match("ABC", "abc", new RegexFlags { IgnoreCase = true }).MatchCount);
        Assert.Equal(0, RegexTester.Match("ABC", "abc").MatchCount);

        Assert.Equal(2, RegexTester.Match("^x", "x\nx", new RegexFlags { Multiline = true }).MatchCount);
        Assert.Equal(1, RegexTester.Match("^x", "x\nx").MatchCount);

        Assert.Equal(1, RegexTester.Match("a.b", "a\nb", new RegexFlags { Singleline = true }).MatchCount);
        Assert.Equal(0, RegexTester.Match("a.b", "a\nb").MatchCount);
    }

    [Fact]
    public void ExplicitCapture_MakesBareParenthesesNonCapturing()
    {
        var result = RegexTester.Match(@"(\w+)@(?<domain>\w+)", "user@host", new RegexFlags { ExplicitCapture = true });

        var match = Assert.Single(result.Matches);
        Assert.Single(match.Groups);
        Assert.Equal("domain", match.Groups[0].Name);
    }

    [Fact]
    public void IgnoreWhitespace_AllowsCommentedPatterns()
    {
        var flags = new RegexFlags { IgnoreWhitespace = true };

        Assert.Equal(1, RegexTester.Match("\\d+ # a number", "42", flags).MatchCount);
    }

    [Fact]
    public void Replace_SubstitutesEveryMatchAndReportsThem()
    {
        var result = RegexTester.Replace(@"\d+", "a1 b22", "#", RegexFlags.Default, out var output);

        Assert.True(result.IsSuccess);
        Assert.Equal("a# b#", output);
        Assert.Equal(2, result.MatchCount);
    }

    [Fact]
    public void Replace_SubstitutesOnlyTheFirstMatchWithoutTheGlobalFlag()
    {
        RegexTester.Replace(@"\d+", "a1 b22", "#", new RegexFlags { Global = false }, out var output);

        Assert.Equal("a# b22", output);
    }

    /// <summary>
    /// .NET numbers unnamed groups first and named groups afterwards, so in
    /// <c>(?&lt;key&gt;\w+)=(\w+)</c> the *unnamed* group is $1 — a long-standing gotcha that the
    /// tool inherits and that this test pins down.
    /// </summary>
    [Fact]
    public void Replace_SupportsNumberedAndNamedSubstitutions()
    {
        RegexTester.Replace(@"(?<key>\w+)=(\w+)", "host=local", "${key} is $1", RegexFlags.Default, out var output);

        Assert.Equal("host is local", output);
    }

    [Fact]
    public void Replace_LeavesInputUnchangedWhenThePatternIsInvalid()
    {
        var result = RegexTester.Replace("(unclosed", "text", "x", RegexFlags.Default, out var output);

        Assert.False(result.IsSuccess);
        Assert.Equal("text", output);
    }

    [Fact]
    public void ToOptions_MapsEveryFlag()
    {
        var options = RegexTester.ToOptions(new RegexFlags
        {
            IgnoreCase = true,
            Multiline = true,
            Singleline = true,
            ExplicitCapture = true,
            IgnoreWhitespace = true
        });

        Assert.True(options.HasFlag(System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        Assert.True(options.HasFlag(System.Text.RegularExpressions.RegexOptions.Multiline));
        Assert.True(options.HasFlag(System.Text.RegularExpressions.RegexOptions.Singleline));
        Assert.True(options.HasFlag(System.Text.RegularExpressions.RegexOptions.ExplicitCapture));
        Assert.True(options.HasFlag(System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace));
    }

    /// <summary>Every shipped template must compile and actually match its own sample.</summary>
    [Fact]
    public void Templates_CompileAndMatchTheirSamples()
    {
        Assert.NotEmpty(RegexLibrary.Templates);

        foreach (var template in RegexLibrary.Templates)
        {
            var result = RegexTester.Match(template.Pattern, template.Sample, new RegexFlags { Multiline = true });

            Assert.True(result.IsSuccess, $"{template.Name} failed to compile: {result.Error}");
            Assert.True(result.MatchCount > 0, $"{template.Name} did not match its own sample.");
        }
    }

    [Fact]
    public void Cheatsheet_IsGroupedAndNonEmpty()
    {
        Assert.NotEmpty(RegexLibrary.Cheatsheet);
        Assert.All(RegexLibrary.Cheatsheet, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Category)));
    }
}
