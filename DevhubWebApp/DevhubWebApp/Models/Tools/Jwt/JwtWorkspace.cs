namespace DevhubWebApp.Models.Tools.Jwt;

/// <summary>
/// Auto-saved state of the JWT decoder. The token is persisted like any other tool input; it never
/// leaves the browser, and clearing the workspace removes it.
/// </summary>
public sealed class JwtWorkspace
{
    public string Token { get; set; } = string.Empty;
}
