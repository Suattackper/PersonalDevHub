namespace DevhubWebApp.Models.Tools.Url;

/// <summary>Auto-saved state of the URL encode/decode tool.</summary>
public sealed class UrlWorkspace
{
    public string Input { get; set; } = string.Empty;

    /// <summary>Encode when <c>true</c>, decode when <c>false</c>.</summary>
    public bool IsEncoding { get; set; } = true;

    /// <summary>
    /// Encode the value as a single URL component (escaping separators) rather than escaping only
    /// the characters that are illegal in a whole URL.
    /// </summary>
    public bool ComponentMode { get; set; } = true;
}

/// <summary>Auto-saved state of the URL parser and query editor.</summary>
public sealed class UrlParserWorkspace
{
    public string Url { get; set; } = string.Empty;
}
