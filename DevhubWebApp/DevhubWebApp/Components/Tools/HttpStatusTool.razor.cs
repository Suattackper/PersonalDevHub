using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Http;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// HTTP status explorer. Look-up, ranking and example building are <see cref="IHttpStatusService"/>;
/// this component owns the filter state, the selection and the saved lists.
/// </summary>
public partial class HttpStatusTool : ToolComponentBase<HttpStatusWorkspace>
{
    private const int RecentCapacity = 10;

    private IReadOnlyList<HttpStatusEntry> _results = [];
    private IReadOnlyList<HttpStatusEntry> _related = [];
    private HttpStatusEntry? _selected;
    private string _example = string.Empty;

    [Inject] private IHttpStatusService Statuses { get; set; } = default!;

    protected override string ToolId => ToolIds.HttpStatus;

    private bool IsFavorite => _selected is not null && Workspace.Favorites.Contains(_selected.Code);

    protected override Task OnWorkspaceLoadedAsync()
    {
        Refresh();
        Select(Statuses.Find(Workspace.SelectedCode));

        return Task.CompletedTask;
    }

    private Task OnSearchChangedAsync(string value)
    {
        Workspace.Search = value;

        Refresh();
        ScheduleAutoSave();

        return Task.CompletedTask;
    }

    private async Task OnClassChangedAsync(HttpStatusClass? statusClass)
    {
        Workspace.Class = statusClass;

        Refresh();
        await SaveWorkspaceAsync();
    }

    private void Refresh() => _results = Statuses.Search(Workspace.Search, Workspace.Class);

    /// <summary>Sets the selection and everything derived from it in one place.</summary>
    private void Select(HttpStatusEntry? entry)
    {
        _selected = entry;
        _related = entry is null ? [] : Statuses.GetRelated(entry);
        _example = entry is null ? string.Empty : Statuses.BuildExample(entry);
        Workspace.SelectedCode = entry?.Code ?? 0;
    }

    private async Task SelectAsync(HttpStatusEntry entry)
    {
        Select(entry);
        RememberRecent(entry.Code);

        await SaveWorkspaceAsync();
        await RecordAsync("Looked up HTTP status", entry.Title);
    }

    private async Task SelectByCodeAsync(int code)
    {
        if (Statuses.Find(code) is { } entry)
        {
            await SelectAsync(entry);
        }
    }

    /// <summary>Keeps the newest look-ups, without duplicates.</summary>
    private void RememberRecent(int code)
    {
        Workspace.Recent.Remove(code);
        Workspace.Recent.Insert(0, code);

        if (Workspace.Recent.Count > RecentCapacity)
        {
            Workspace.Recent.RemoveRange(RecentCapacity, Workspace.Recent.Count - RecentCapacity);
        }
    }

    private async Task ToggleFavoriteAsync()
    {
        if (_selected is null)
        {
            return;
        }

        if (!Workspace.Favorites.Remove(_selected.Code))
        {
            Workspace.Favorites.Insert(0, _selected.Code);
        }

        await SaveWorkspaceAsync();
    }

    private static string GetClassIcon(HttpStatusClass statusClass) => statusClass switch
    {
        HttpStatusClass.Informational => Icons.Material.Filled.Info,
        HttpStatusClass.Success => Icons.Material.Filled.CheckCircle,
        HttpStatusClass.Redirection => Icons.Material.Filled.CallSplit,
        HttpStatusClass.ClientError => Icons.Material.Filled.ReportProblem,
        _ => Icons.Material.Filled.Dangerous
    };

    private static Color GetClassColor(HttpStatusClass statusClass) => statusClass switch
    {
        HttpStatusClass.Informational => Color.Info,
        HttpStatusClass.Success => Color.Success,
        HttpStatusClass.Redirection => Color.Secondary,
        HttpStatusClass.ClientError => Color.Warning,
        _ => Color.Error
    };
}
