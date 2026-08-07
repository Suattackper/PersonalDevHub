namespace DevhubWebApp.Models.Tools.Regex;

/// <summary>Options the tester exposes, mapped onto .NET regex options.</summary>
public sealed record RegexFlags
{
    public static RegexFlags Default { get; } = new();

    /// <summary>Case-insensitive matching.</summary>
    public bool IgnoreCase { get; init; }

    /// <summary><c>^</c> and <c>$</c> match at line boundaries.</summary>
    public bool Multiline { get; init; }

    /// <summary><c>.</c> also matches newlines (.NET calls this Singleline).</summary>
    public bool Singleline { get; init; }

    /// <summary>Find every match rather than stopping at the first.</summary>
    public bool Global { get; init; } = true;

    /// <summary>Only named groups capture; bare parentheses become non-capturing.</summary>
    public bool ExplicitCapture { get; init; }

    /// <summary>Whitespace in the pattern is ignored and <c>#</c> starts a comment.</summary>
    public bool IgnoreWhitespace { get; init; }
}

/// <summary>One capture within a match.</summary>
public sealed record RegexCapture
{
    /// <summary>Group name, or the group number as text when the group is unnamed.</summary>
    public required string Name { get; init; }

    public required string Value { get; init; }

    public int Index { get; init; }

    public int Length { get; init; }

    /// <summary><c>false</c> when the group did not participate in this match.</summary>
    public bool Success { get; init; }
}

/// <summary>One match, with its capture groups.</summary>
public sealed record RegexMatchInfo
{
    public required string Value { get; init; }

    public int Index { get; init; }

    public int Length { get; init; }

    public IReadOnlyList<RegexCapture> Groups { get; init; } = Array.Empty<RegexCapture>();
}

/// <summary>Outcome of running a pattern against the test text.</summary>
public sealed record RegexResult
{
    public static RegexResult Empty { get; } = new();

    public IReadOnlyList<RegexMatchInfo> Matches { get; init; } = Array.Empty<RegexMatchInfo>();

    /// <summary>Why the pattern could not be run; <c>null</c> when it ran.</summary>
    public string? Error { get; init; }

    /// <summary><c>true</c> when matching was abandoned because it took too long.</summary>
    public bool TimedOut { get; init; }

    public bool IsSuccess => Error is null;

    public int MatchCount => Matches.Count;

    public static RegexResult Failure(string error, bool timedOut = false)
        => new() { Error = error, TimedOut = timedOut };
}

/// <summary>A ready-made pattern offered in the tool's library.</summary>
public sealed record RegexTemplate
{
    public required string Name { get; init; }

    public required string Pattern { get; init; }

    public required string Description { get; init; }

    /// <summary>Sample text that the pattern matches, used to preview the template.</summary>
    public string Sample { get; init; } = string.Empty;
}

/// <summary>One row of the syntax cheatsheet.</summary>
public sealed record RegexCheatsheetEntry
{
    public required string Token { get; init; }

    public required string Meaning { get; init; }

    public required string Category { get; init; }
}
