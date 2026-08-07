namespace DevhubWebApp.Models.Tools.Qr;

/// <summary>Kinds of payload the generator can encode.</summary>
public enum QrPayloadKind
{
    Text,
    Url,
    Email,
    Phone,
    Sms,
    WiFi,
    VCard,
    Location
}

/// <summary>Wi-Fi authentication modes supported by the WIFI payload format.</summary>
public enum WiFiSecurity
{
    /// <summary>Open network with no passphrase.</summary>
    None,

    Wep,
    Wpa
}

/// <summary>
/// Error correction level: higher levels survive more damage but hold less data.
/// </summary>
public enum QrErrorCorrection
{
    /// <summary>Recovers about 7% of the code.</summary>
    Low,

    /// <summary>About 15%. The usual default.</summary>
    Medium,

    /// <summary>About 25%.</summary>
    Quartile,

    /// <summary>About 30%. Needed when a logo covers part of the code.</summary>
    High
}

/// <summary>Fields the structured payload builders read. Unused fields are simply ignored.</summary>
public sealed record QrPayload
{
    public QrPayloadKind Kind { get; init; } = QrPayloadKind.Text;

    public string Text { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public string Ssid { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public WiFiSecurity Security { get; init; } = WiFiSecurity.Wpa;

    public bool HiddenNetwork { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Organisation { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Phone { get; init; } = string.Empty;

    public string Website { get; init; } = string.Empty;

    public string Latitude { get; init; } = string.Empty;

    public string Longitude { get; init; } = string.Empty;
}
