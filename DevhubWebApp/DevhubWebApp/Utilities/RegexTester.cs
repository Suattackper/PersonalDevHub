using System.Globalization;
using System.Text.RegularExpressions;
using DevhubWebApp.Models.Tools.Regex;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Runs user-supplied regular expressions safely.
/// <para>
/// Every call is bounded by <see cref="MatchTimeout"/>. A pattern typed by hand can backtrack
/// catastrophically — <c>(a+)+$</c> against a long line will hang the browser tab — so a timeout is
/// not a nicety here, it is the difference between a tool and a denial of service on the user.
/// </para>
/// </summary>
public static class RegexTester
{
    /// <summary>Upper bound on a single match run.</summary>
    public static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Cap on reported matches; the UI cannot usefully render more.</summary>
    public const int MaxMatches = 1000;

    /// <summary>Runs a pattern against the input and returns its matches.</summary>
    public static RegexResult Match(string? pattern, string? input, RegexFlags? flags = null)
    {
        if (string.IsNullOrEmpty(pattern))
        {
            return RegexResult.Empty;
        }

        flags ??= RegexFlags.Default;

        if (!TryCreate(pattern, flags, out var regex, out var error))
        {
            return RegexResult.Failure(error!);
        }

        try
        {
            var matches = new List<RegexMatchInfo>();

            for (var match = regex!.Match(input ?? string.Empty); match.Success; match = match.NextMatch())
            {
                matches.Add(Describe(match, regex));

                if (!flags.Global || matches.Count >= MaxMatches)
                {
                    break;
                }

                // A zero-length match would otherwise loop forever on the same position.
                if (match.Length == 0 && match.Index >= (input?.Length ?? 0))
                {
                    break;
                }
            }

            return new RegexResult { Matches = matches };
        }
        catch (RegexMatchTimeoutException)
        {
            return RegexResult.Failure(
                $"Matching took longer than {MatchTimeout.TotalSeconds:0} seconds and was stopped. The pattern likely backtracks catastrophically.",
                timedOut: true);
        }
    }

    /// <summary>
    /// Applies a replacement. Substitutions use .NET syntax: <c>$1</c> for a numbered group and
    /// <c>${name}</c> for a named one.
    /// </summary>
    public static RegexResult Replace(string? pattern, string? input, string? replacement, RegexFlags? flags, out string output)
    {
        output = input ?? string.Empty;

        if (string.IsNullOrEmpty(pattern))
        {
            return RegexResult.Empty;
        }

        flags ??= RegexFlags.Default;

        if (!TryCreate(pattern, flags, out var regex, out var error))
        {
            return RegexResult.Failure(error!);
        }

        try
        {
            output = flags.Global
                ? regex!.Replace(input ?? string.Empty, replacement ?? string.Empty)
                : regex!.Replace(input ?? string.Empty, replacement ?? string.Empty, 1);

            return Match(pattern, input, flags);
        }
        catch (RegexMatchTimeoutException)
        {
            return RegexResult.Failure("Replacement timed out; the pattern likely backtracks catastrophically.", timedOut: true);
        }
        catch (ArgumentException exception)
        {
            return RegexResult.Failure($"Invalid replacement pattern: {exception.Message}");
        }
    }

    /// <summary>Translates the tool's flags into .NET options.</summary>
    public static RegexOptions ToOptions(RegexFlags flags)
    {
        ArgumentNullException.ThrowIfNull(flags);

        var options = RegexOptions.None;

        if (flags.IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        if (flags.Multiline)
        {
            options |= RegexOptions.Multiline;
        }

        if (flags.Singleline)
        {
            options |= RegexOptions.Singleline;
        }

        if (flags.ExplicitCapture)
        {
            options |= RegexOptions.ExplicitCapture;
        }

        if (flags.IgnoreWhitespace)
        {
            options |= RegexOptions.IgnorePatternWhitespace;
        }

        return options;
    }

    /// <summary>Compiles a pattern, reporting a syntax error as a message instead of throwing.</summary>
    private static bool TryCreate(string pattern, RegexFlags flags, out System.Text.RegularExpressions.Regex? regex, out string? error)
    {
        try
        {
            regex = new System.Text.RegularExpressions.Regex(pattern, ToOptions(flags), MatchTimeout);
            error = null;
            return true;
        }
        catch (ArgumentException exception)
        {
            regex = null;
            error = exception.Message;
            return false;
        }
    }

    private static RegexMatchInfo Describe(Match match, System.Text.RegularExpressions.Regex regex)
    {
        var groupNames = regex.GetGroupNames();

        return new RegexMatchInfo
        {
            Value = match.Value,
            Index = match.Index,
            Length = match.Length,

            // Group 0 is the whole match, which the caller already has.
            Groups = groupNames
                .Where(name => name != "0")
                .Select(name => new RegexCapture
                {
                    Name = int.TryParse(name, CultureInfo.InvariantCulture, out var number) ? $"Group {number}" : name,
                    Value = match.Groups[name].Value,
                    Index = match.Groups[name].Index,
                    Length = match.Groups[name].Length,
                    Success = match.Groups[name].Success
                })
                .ToList()
        };
    }
}
