using DevhubWebApp.Models.Tools.Data;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// Conversion, formatting and validation across JSON, YAML and XML. Behind an interface so the
/// tool never depends on the parsers used underneath.
/// </summary>
public interface IDataConversionService
{
    /// <summary>Converts between any two supported formats. Same-format input is reformatted.</summary>
    DataResult Convert(string? input, DataFormat from, DataFormat to, bool indented = true);

    /// <summary>Re-formats a document in its own format, indented or minified.</summary>
    DataResult Format(string? input, DataFormat format, bool indented);

    /// <summary>Checks that a document parses, returning the reason when it does not.</summary>
    DataResult Validate(string? input, DataFormat format);

    /// <summary>Guesses the format from the content, so a paste needs no manual selection.</summary>
    DataFormat Detect(string? input, DataFormat fallback = DataFormat.Json);

    /// <summary>Conventional file extension for a format.</summary>
    string GetExtension(DataFormat format);

    /// <summary>Media type for a format.</summary>
    string GetMimeType(DataFormat format);
}
