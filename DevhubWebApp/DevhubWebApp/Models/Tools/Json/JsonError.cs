namespace DevhubWebApp.Models.Tools.Json;

/// <summary>
/// A syntax error with the position the parser stopped at. Line and column are 1-based so they can
/// be shown to the user and handed to the editor without further arithmetic.
/// </summary>
public sealed record JsonError
{
    public required string Message { get; init; }

    /// <summary>1-based line number.</summary>
    public required int Line { get; init; }

    /// <summary>1-based column number.</summary>
    public required int Column { get; init; }

    /// <summary>Human-readable location, e.g. "line 4, column 12".</summary>
    public string Location => $"line {Line}, column {Column}";
}
