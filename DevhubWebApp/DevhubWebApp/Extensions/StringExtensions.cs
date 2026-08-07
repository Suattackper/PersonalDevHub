namespace DevhubWebApp.Extensions;

/// <summary>Small string helpers shared by tools and components.</summary>
public static class StringExtensions
{
    /// <summary>Shortens a string for display, appending an ellipsis when it was cut.</summary>
    public static string Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength] + "…";
    }

    /// <summary>Collapses all whitespace runs into single spaces.</summary>
    public static string CollapseWhitespace(this string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Returns <paramref name="fallback"/> when the value is null or whitespace.</summary>
    public static string OrDefault(this string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value;
}
