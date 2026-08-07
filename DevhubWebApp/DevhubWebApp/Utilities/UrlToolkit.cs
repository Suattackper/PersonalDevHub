using System.Text;
using DevhubWebApp.Models.Tools.Url;

namespace DevhubWebApp.Utilities;

/// <summary>
/// URL encoding, parsing and query-string manipulation. Pure static helpers, so every rule is
/// directly testable and no component needs to know the escaping subtleties.
/// </summary>
public static class UrlToolkit
{
    /// <summary>
    /// Percent-encodes a value for use inside a URL component. Reserved characters such as
    /// <c>&amp;</c>, <c>=</c> and <c>/</c> are escaped — this is what a query value needs.
    /// </summary>
    public static string EncodeComponent(string? value)
        => string.IsNullOrEmpty(value) ? string.Empty : Uri.EscapeDataString(value);

    /// <summary>Characters that can never appear literally in a URL, whatever the component.</summary>
    private static readonly Dictionary<char, string> IllegalCharacters = new()
    {
        [' '] = "%20",
        ['"'] = "%22",
        ['<'] = "%3C",
        ['>'] = "%3E",
        ['\\'] = "%5C",
        ['^'] = "%5E",
        ['`'] = "%60",
        ['{'] = "%7B",
        ['|'] = "%7C",
        ['}'] = "%7D"
    };

    /// <summary>
    /// Escapes only characters that are illegal in a URL, leaving its structure intact. Use this on
    /// a whole URL; <see cref="EncodeComponent"/> would destroy the separators.
    /// <para>
    /// Absolute URLs are normalised through <see cref="Uri"/>. Anything else is escaped
    /// character by character — the framework's whole-URI escaper is obsolete precisely because it
    /// mangles inputs it cannot parse.
    /// </para>
    /// </summary>
    public static string EncodeFull(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            return uri.AbsoluteUri;
        }

        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            builder.Append(IllegalCharacters.TryGetValue(character, out var escaped) ? escaped : character);
        }

        return builder.ToString();
    }

    /// <summary>Reverses percent-encoding. Returns the input unchanged when it is not valid.</summary>
    public static string Decode(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        try
        {
            // A '+' means a space in form-encoded payloads, which is where most pasted values come from.
            return Uri.UnescapeDataString(value.Replace('+', ' '));
        }
        catch (UriFormatException)
        {
            return value;
        }
    }

    /// <summary>Splits an absolute URL into its parts, including parsed query parameters.</summary>
    public static UrlParts Parse(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return UrlParts.Empty;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return UrlParts.Empty with
            {
                IsValid = false,
                Error = "Enter an absolute URL, including the scheme (for example https://example.com/path)."
            };
        }

        var query = uri.Query.TrimStart('?');

        return new UrlParts
        {
            IsValid = true,
            Protocol = uri.Scheme,
            Host = uri.Host,
            Port = uri.IsDefaultPort ? $"{uri.Port} (default)" : uri.Port.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Path = uri.AbsolutePath,
            Query = query,
            Fragment = uri.Fragment.TrimStart('#'),
            Origin = $"{uri.Scheme}://{uri.Authority}",
            UserInfo = uri.UserInfo,
            Parameters = ParseQuery(query)
        };
    }

    /// <summary>
    /// Parses a query string. Repeated names are kept as separate entries, and a name without
    /// <c>=</c> yields an empty value rather than being dropped.
    /// </summary>
    public static IReadOnlyList<QueryParameter> ParseQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<QueryParameter>();
        }

        return query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair =>
            {
                var separator = pair.IndexOf('=', StringComparison.Ordinal);

                return separator < 0
                    ? new QueryParameter { Name = Decode(pair) }
                    : new QueryParameter
                    {
                        Name = Decode(pair[..separator]),
                        Value = Decode(pair[(separator + 1)..])
                    };
            })
            .ToList();
    }

    /// <summary>Rebuilds a query string, percent-encoding both names and values.</summary>
    public static string BuildQuery(IEnumerable<QueryParameter>? parameters)
    {
        if (parameters is null)
        {
            return string.Empty;
        }

        var pairs = parameters
            .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Name))
            .Select(parameter => string.IsNullOrEmpty(parameter.Value)
                ? EncodeComponent(parameter.Name)
                : $"{EncodeComponent(parameter.Name)}={EncodeComponent(parameter.Value)}");

        return string.Join('&', pairs);
    }

    /// <summary>Replaces a URL's query string, keeping every other part untouched.</summary>
    public static string BuildUrl(UrlParts parts, IEnumerable<QueryParameter>? parameters)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var builder = new StringBuilder(parts.Origin);
        builder.Append(parts.Path);

        var query = BuildQuery(parameters);
        if (!string.IsNullOrEmpty(query))
        {
            builder.Append('?').Append(query);
        }

        if (!string.IsNullOrEmpty(parts.Fragment))
        {
            builder.Append('#').Append(parts.Fragment);
        }

        return builder.ToString();
    }

    /// <summary>Sorts parameters by name, keeping the original order of equal names.</summary>
    public static IReadOnlyList<QueryParameter> SortParameters(IEnumerable<QueryParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return parameters.OrderBy(parameter => parameter.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }
}
