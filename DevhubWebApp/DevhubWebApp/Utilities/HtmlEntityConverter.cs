using System.Net;

namespace DevhubWebApp.Utilities;

/// <summary>Encodes and decodes HTML entities without interpreting markup.</summary>
public static class HtmlEntityConverter
{
    public static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }

    public static string Unescape(string? value)
        => string.IsNullOrEmpty(value) ? string.Empty : WebUtility.HtmlDecode(value);
}
