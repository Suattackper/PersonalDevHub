namespace DevhubWebApp.Models.Tools.Url;

/// <summary>One query-string entry. Order is preserved because it is meaningful in some APIs.</summary>
public sealed record QueryParameter
{
    public required string Name { get; init; }

    public string Value { get; init; } = string.Empty;
}

/// <summary>A URL broken into its parts.</summary>
public sealed record UrlParts
{
    public static UrlParts Empty { get; } = new();

    public string Protocol { get; init; } = string.Empty;

    public string Host { get; init; } = string.Empty;

    /// <summary>Explicit or default port; empty when the URL has no authority.</summary>
    public string Port { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    /// <summary>Raw query string without the leading question mark.</summary>
    public string Query { get; init; } = string.Empty;

    /// <summary>Fragment without the leading hash.</summary>
    public string Fragment { get; init; } = string.Empty;

    /// <summary>Scheme, host and port, e.g. <c>https://example.com</c>.</summary>
    public string Origin { get; init; } = string.Empty;

    /// <summary>Credentials embedded in the URL, if any.</summary>
    public string UserInfo { get; init; } = string.Empty;

    public IReadOnlyList<QueryParameter> Parameters { get; init; } = Array.Empty<QueryParameter>();

    /// <summary><c>false</c> when the input could not be parsed as an absolute URL.</summary>
    public bool IsValid { get; init; }

    /// <summary>Why parsing failed; <c>null</c> when it succeeded.</summary>
    public string? Error { get; init; }
}
