using System.Globalization;
using System.Text;
using DevhubWebApp.Models.Tools.Qr;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Builds the text a QR code should encode for each payload kind. Pure string construction, so the
/// escaping rules that make a scanner accept a Wi-Fi or vCard payload are testable on their own.
/// </summary>
public static class QrPayloadBuilder
{
    /// <summary>Renders a payload into the string a scanner will read.</summary>
    public static string Build(QrPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        return payload.Kind switch
        {
            QrPayloadKind.Url => BuildUrl(payload.Text),
            QrPayloadKind.Email => BuildEmail(payload),
            QrPayloadKind.Phone => Prefixed("tel:", payload.Phone),
            QrPayloadKind.Sms => BuildSms(payload),
            QrPayloadKind.WiFi => BuildWiFi(payload),
            QrPayloadKind.VCard => BuildVCard(payload),
            QrPayloadKind.Location => BuildGeo(payload),
            _ => payload.Text
        };
    }

    /// <summary>Display name of a payload kind.</summary>
    public static string GetName(QrPayloadKind kind) => kind switch
    {
        QrPayloadKind.Text => "Text",
        QrPayloadKind.Url => "URL",
        QrPayloadKind.Email => "Email",
        QrPayloadKind.Phone => "Phone",
        QrPayloadKind.Sms => "SMS",
        QrPayloadKind.WiFi => "Wi-Fi",
        QrPayloadKind.VCard => "Contact",
        QrPayloadKind.Location => "Location",
        _ => kind.ToString()
    };

    /// <summary>A bare domain is not scannable as a link, so a missing scheme is added.</summary>
    private static string BuildUrl(string? value)
    {
        var text = value?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Contains("://", StringComparison.Ordinal))
        {
            return text;
        }

        return $"https://{text}";
    }

    private static string BuildEmail(QrPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Email))
        {
            return string.Empty;
        }

        var query = new List<string>(2);

        if (!string.IsNullOrWhiteSpace(payload.Subject))
        {
            query.Add($"subject={UrlToolkit.EncodeComponent(payload.Subject)}");
        }

        if (!string.IsNullOrWhiteSpace(payload.Body))
        {
            query.Add($"body={UrlToolkit.EncodeComponent(payload.Body)}");
        }

        var suffix = query.Count == 0 ? string.Empty : $"?{string.Join('&', query)}";
        return $"mailto:{payload.Email.Trim()}{suffix}";
    }

    private static string BuildSms(QrPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Phone))
        {
            return string.Empty;
        }

        var body = string.IsNullOrWhiteSpace(payload.Body)
            ? string.Empty
            : $"?body={UrlToolkit.EncodeComponent(payload.Body)}";

        return $"SMSTO:{payload.Phone.Trim()}{body}";
    }

    /// <summary>
    /// WIFI:T:&lt;security&gt;;S:&lt;ssid&gt;;P:&lt;password&gt;;H:&lt;hidden&gt;;;
    /// Separators inside the SSID or password must be escaped or the scanner truncates the field.
    /// </summary>
    private static string BuildWiFi(QrPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Ssid))
        {
            return string.Empty;
        }

        var security = payload.Security switch
        {
            WiFiSecurity.Wep => "WEP",
            WiFiSecurity.None => "nopass",
            _ => "WPA"
        };

        var builder = new StringBuilder("WIFI:");
        builder.Append("T:").Append(security).Append(';');
        builder.Append("S:").Append(EscapeWiFi(payload.Ssid)).Append(';');

        if (payload.Security != WiFiSecurity.None && !string.IsNullOrEmpty(payload.Password))
        {
            builder.Append("P:").Append(EscapeWiFi(payload.Password)).Append(';');
        }

        if (payload.HiddenNetwork)
        {
            builder.Append("H:true;");
        }

        return builder.Append(';').ToString();
    }

    private static string BuildVCard(QrPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.FirstName) && string.IsNullOrWhiteSpace(payload.LastName))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        builder.Append("BEGIN:VCARD\nVERSION:3.0\n");
        builder.Append(CultureInfo.InvariantCulture, $"N:{payload.LastName};{payload.FirstName}\n");
        builder.Append(CultureInfo.InvariantCulture, $"FN:{$"{payload.FirstName} {payload.LastName}".Trim()}\n");

        AppendIfPresent(builder, "ORG", payload.Organisation);
        AppendIfPresent(builder, "TITLE", payload.Title);
        AppendIfPresent(builder, "TEL", payload.Phone);
        AppendIfPresent(builder, "EMAIL", payload.Email);
        AppendIfPresent(builder, "URL", payload.Website);

        return builder.Append("END:VCARD").ToString();
    }

    private static string BuildGeo(QrPayload payload)
    {
        if (string.IsNullOrWhiteSpace(payload.Latitude) || string.IsNullOrWhiteSpace(payload.Longitude))
        {
            return string.Empty;
        }

        return $"geo:{payload.Latitude.Trim()},{payload.Longitude.Trim()}";
    }

    private static string Prefixed(string prefix, string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : $"{prefix}{value.Trim()}";

    private static void AppendIfPresent(StringBuilder builder, string field, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            builder.Append(field).Append(':').Append(value.Trim()).Append('\n');
        }
    }

    private static string EscapeWiFi(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (character is '\\' or ';' or ',' or ':' or '"')
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
