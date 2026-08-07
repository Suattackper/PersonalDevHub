namespace DevhubWebApp.Models.Tools;

/// <summary>
/// Cascaded to every tool component by the tool host. It carries the tool's own metadata and a
/// pre-bound history writer, so a tool never has to know — or repeat — its own id.
/// </summary>
public sealed class ToolContext
{
    private readonly Func<string, string?, Task> _record;

    public ToolContext(ToolDescriptor tool, Func<string, string?, Task> record)
    {
        Tool = tool;
        _record = record;
    }

    /// <summary>Metadata of the tool currently rendered.</summary>
    public ToolDescriptor Tool { get; }

    /// <summary>
    /// Records an action in the activity log, e.g. <c>RecordAsync("Formatted", json)</c>.
    /// Honours the user's history setting; callers do not need to check it.
    /// </summary>
    public Task RecordAsync(string action, string? preview = null) => _record(action, preview);
}
