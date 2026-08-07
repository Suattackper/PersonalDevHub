namespace DevhubWebApp.Models.Tools.Hash;

/// <summary>Auto-saved state of the hash tool. File contents are never persisted, only text.</summary>
public sealed class HashWorkspace
{
    public string Text { get; set; } = string.Empty;

    /// <summary>Digest the user wants to verify against.</summary>
    public string ExpectedHash { get; set; } = string.Empty;

    /// <summary>Render digests as Base64 instead of hexadecimal.</summary>
    public bool ShowBase64 { get; set; }

    /// <summary>Render hexadecimal digests in upper case, as some tools print them.</summary>
    public bool Uppercase { get; set; }
}
