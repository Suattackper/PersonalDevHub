namespace DevhubWebApp.Models.Tools.Http;

/// <summary>Auto-saved state of the HTTP status explorer.</summary>
public sealed class HttpStatusWorkspace
{
    public string Search { get; set; } = string.Empty;

    /// <summary>Selected status class, or <c>null</c> for all of them.</summary>
    public HttpStatusClass? Class { get; set; }

    /// <summary>Code currently shown in the detail panel; zero means nothing selected.</summary>
    public int SelectedCode { get; set; }

    /// <summary>Codes the user kept, newest first.</summary>
    public List<int> Favorites { get; set; } = [];

    /// <summary>Recently viewed codes, newest first.</summary>
    public List<int> Recent { get; set; } = [];
}
