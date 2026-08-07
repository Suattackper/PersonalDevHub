namespace DevhubWebApp.Models.Tools.Base64;

/// <summary>Direction the toolkit is currently working in.</summary>
public enum Base64Mode
{
    Encode,
    Decode
}

/// <summary>Auto-saved state of the Base64 toolkit.</summary>
public sealed class Base64Workspace
{
    public Base64Mode Mode { get; set; } = Base64Mode.Encode;

    public string Input { get; set; } = string.Empty;

    /// <summary>Emit a <c>data:</c> URI rather than bare Base64 when encoding.</summary>
    public bool AsDataUri { get; set; }
}
