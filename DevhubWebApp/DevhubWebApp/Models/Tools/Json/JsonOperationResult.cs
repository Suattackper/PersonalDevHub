namespace DevhubWebApp.Models.Tools.Json;

/// <summary>
/// Outcome of a formatter operation. Failures are values, not exceptions: invalid input is the
/// normal case for this tool, and the UI needs the position, not a stack trace.
/// </summary>
public sealed record JsonOperationResult
{
    public bool IsSuccess => Error is null;

    /// <summary>Rewritten document. Empty when the operation failed.</summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>Syntax error, or <c>null</c> when the document parsed.</summary>
    public JsonError? Error { get; init; }

    /// <summary>How many duplicate properties were dropped during the rewrite.</summary>
    public int RemovedDuplicates { get; init; }

    public static JsonOperationResult Success(string output, int removedDuplicates = 0)
        => new() { Output = output, RemovedDuplicates = removedDuplicates };

    public static JsonOperationResult Failure(JsonError error) => new() { Error = error };
}
