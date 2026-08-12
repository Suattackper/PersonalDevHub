namespace DevhubWebApp.Models.Tools.Data;

/// <summary>Auto-saved state of the data converter.</summary>
public sealed class DataWorkspace
{
    public string Input { get; set; } = string.Empty;

    public DataFormat From { get; set; } = DataFormat.Json;

    public DataFormat To { get; set; } = DataFormat.Yaml;

    /// <summary>Indent the output rather than minifying it.</summary>
    public bool Indented { get; set; } = true;

    /// <summary>Detect the source format from the content whenever the input changes.</summary>
    public bool AutoDetect { get; set; } = true;
}
