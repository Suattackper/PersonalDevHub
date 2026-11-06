using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Html;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

public partial class HtmlEscapeTool : ToolComponentBase<HtmlEscapeWorkspace>
{
    protected override string ToolId => ToolIds.HtmlEscape;
    private bool IsEmpty => string.IsNullOrEmpty(Workspace.Input);

    private Task OnInputChangedAsync(string? value)
    {
        Workspace.Input = value ?? string.Empty;
        ScheduleAutoSave();
        return Task.CompletedTask;
    }

    private async Task EscapeAsync()
    {
        Workspace.Output = HtmlEntityConverter.Escape(Workspace.Input);
        await SaveWorkspaceAsync();
        await RecordAsync("Escaped HTML", Workspace.Input);
    }

    private async Task UnescapeAsync()
    {
        Workspace.Output = HtmlEntityConverter.Unescape(Workspace.Input);
        await SaveWorkspaceAsync();
        await RecordAsync("Unescaped HTML", Workspace.Input);
    }

    private async Task ClearAsync()
    {
        Workspace.Input = string.Empty;
        Workspace.Output = string.Empty;
        await SaveWorkspaceAsync();
    }
}
