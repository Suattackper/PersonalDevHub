namespace DevhubWebApp.Models.Tools.Http;

/// <summary>The five status classes defined by RFC 9110.</summary>
public enum HttpStatusClass
{
    /// <summary>1xx — the request was received and processing continues.</summary>
    Informational,

    /// <summary>2xx — the request succeeded.</summary>
    Success,

    /// <summary>3xx — further action is needed to complete the request.</summary>
    Redirection,

    /// <summary>4xx — the request is faulty.</summary>
    ClientError,

    /// <summary>5xx — the server failed to fulfil a valid request.</summary>
    ServerError
}

/// <summary>One status code and everything the explorer knows about it.</summary>
public sealed record HttpStatusEntry
{
    public required int Code { get; init; }

    /// <summary>Reason phrase, e.g. <c>Not Found</c>.</summary>
    public required string Name { get; init; }

    public required HttpStatusClass Class { get; init; }

    /// <summary>What the code means, in one or two sentences.</summary>
    public required string Description { get; init; }

    /// <summary>Situations where this code is the right answer.</summary>
    public IReadOnlyList<string> UseCases { get; init; } = Array.Empty<string>();

    /// <summary>Codes worth comparing against this one.</summary>
    public IReadOnlyList<int> Related { get; init; } = Array.Empty<int>();

    /// <summary>Headers that matter for this code, e.g. <c>Location</c> for a redirect.</summary>
    public IReadOnlyList<string> Headers { get; init; } = Array.Empty<string>();

    /// <summary><c>404 Not Found</c>.</summary>
    public string Title => $"{Code} {Name}";

    /// <summary>Case-insensitive match across code, name and description.</summary>
    public bool Matches(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return true;
        }

        var needle = term.Trim();

        return Code.ToString(System.Globalization.CultureInfo.InvariantCulture).StartsWith(needle, StringComparison.Ordinal)
            || Name.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || Description.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || UseCases.Any(useCase => useCase.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}
