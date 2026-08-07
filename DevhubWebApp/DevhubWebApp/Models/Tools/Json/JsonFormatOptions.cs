namespace DevhubWebApp.Models.Tools.Json;

/// <summary>Transformations applied while re-writing a document.</summary>
public sealed record JsonFormatOptions
{
    /// <summary>Default: rewrite the document faithfully, changing only whitespace.</summary>
    public static JsonFormatOptions Default { get; } = new();

    /// <summary>Orders object properties alphabetically (ordinal), recursively.</summary>
    public bool SortProperties { get; init; }

    /// <summary>
    /// Keeps only the last occurrence of a repeated property name, which is what JSON parsers
    /// conventionally do. Off by default, because it silently changes the document.
    /// </summary>
    public bool RemoveDuplicateProperties { get; init; }
}
