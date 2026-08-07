namespace DevhubWebApp.Models.Tools;

/// <summary>
/// A diagnostic to underline in the editor. Coordinates are 1-based, matching both Monaco and the
/// positions the parsers report.
/// </summary>
public sealed record EditorMarker
{
    public required string Message { get; init; }

    public required int Line { get; init; }

    public required int Column { get; init; }

    /// <summary>How many characters to underline. At least one character is always marked.</summary>
    public int Length { get; init; } = 1;
}
