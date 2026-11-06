using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Sql;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

public partial class SqlTool : ToolComponentBase<SqlWorkspace>
{
    private string _output = string.Empty;
    private string _error = string.Empty;

    protected override string ToolId => ToolIds.Sql;
    private bool IsEmpty => string.IsNullOrWhiteSpace(Workspace.Input);

    protected override Task OnWorkspaceLoadedAsync()
    {
        if (IsEmpty)
        {
            _error = string.Empty;
            _output = string.Empty;
            return Task.CompletedTask;
        }

        _error = SqlFormatter.Validate(Workspace.Input);
        _output = string.IsNullOrEmpty(_error)
            ? SqlFormatter.Format(Workspace.Input, Workspace.UppercaseKeywords)
            : string.Empty;

        return Task.CompletedTask;
    }

    private Task OnInputChangedAsync(string? value)
    {
        Workspace.Input = value ?? string.Empty;
        _error = string.Empty;
        ScheduleAutoSave();
        return Task.CompletedTask;
    }

    private async Task SetKeywordCaseAsync(bool value)
    {
        Workspace.UppercaseKeywords = value;
        await SaveWorkspaceAsync();
        if (!IsEmpty)
        {
            await FormatAsync();
        }
    }

    private async Task FormatAsync()
    {
        _error = SqlFormatter.Validate(Workspace.Input);
        if (!string.IsNullOrEmpty(_error))
        {
            _output = string.Empty;
            return;
        }
        _output = SqlFormatter.Format(Workspace.Input, Workspace.UppercaseKeywords);
        await SaveWorkspaceAsync();
        await RecordAsync("Formatted SQL", Workspace.Input);
    }

    private async Task ClearAsync()
    {
        Workspace.Input = string.Empty;
        _output = string.Empty;
        _error = string.Empty;
        await SaveWorkspaceAsync();
    }
}
