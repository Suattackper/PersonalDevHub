namespace DevhubWebApp.Models.Tools.Qr;

/// <summary>Auto-saved state of the QR generator.</summary>
public sealed class QrWorkspace
{
    public QrPayloadKind Kind { get; set; } = QrPayloadKind.Text;

    public string Text { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public string Ssid { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public WiFiSecurity Security { get; set; } = WiFiSecurity.Wpa;

    public bool HiddenNetwork { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Organisation { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Website { get; set; } = string.Empty;

    public string Latitude { get; set; } = string.Empty;

    public string Longitude { get; set; } = string.Empty;

    public QrErrorCorrection Correction { get; set; } = QrErrorCorrection.Medium;

    /// <summary>Pixels per module — the knob that sets the exported image size.</summary>
    public int PixelsPerModule { get; set; } = 10;

    /// <summary>Quiet-zone width in modules. Scanners need one; zero disables it.</summary>
    public int MarginModules { get; set; } = 4;

    public string Foreground { get; set; } = "#000000";

    public string Background { get; set; } = "#ffffff";

    /// <summary>Projects the persisted fields onto the payload builder's contract.</summary>
    public QrPayload ToPayload() => new()
    {
        Kind = Kind,
        Text = Text,
        Subject = Subject,
        Body = Body,
        Ssid = Ssid,
        Password = Password,
        Security = Security,
        HiddenNetwork = HiddenNetwork,
        FirstName = FirstName,
        LastName = LastName,
        Organisation = Organisation,
        Title = Title,
        Email = Email,
        Phone = Phone,
        Website = Website,
        Latitude = Latitude,
        Longitude = Longitude
    };
}
