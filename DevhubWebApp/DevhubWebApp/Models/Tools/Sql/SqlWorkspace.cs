namespace DevhubWebApp.Models.Tools.Sql;

/// <summary>Auto-saved SQL formatter input and options.</summary>
public sealed class SqlWorkspace
{
    public string Input { get; set; } = string.Empty;

    public bool UppercaseKeywords { get; set; } = true;
}
