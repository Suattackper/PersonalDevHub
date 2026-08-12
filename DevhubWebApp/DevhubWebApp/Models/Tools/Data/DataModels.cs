namespace DevhubWebApp.Models.Tools.Data;

/// <summary>Structured formats the converter reads and writes.</summary>
public enum DataFormat
{
    Json,
    Yaml,
    Xml
}

/// <summary>Outcome of a conversion, format or validation.</summary>
public sealed record DataResult
{
    public static DataResult Empty { get; } = new();

    /// <summary>Converted text. Empty when the operation failed.</summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>Why the input could not be handled; <c>null</c> when it could.</summary>
    public string? Error { get; init; }

    /// <summary>1-based line the error was reported at, when the parser gave one.</summary>
    public int? ErrorLine { get; init; }

    public bool IsSuccess => Error is null;

    public static DataResult Success(string output) => new() { Output = output };

    public static DataResult Failure(string error, int? line = null) => new() { Error = error, ErrorLine = line };
}
