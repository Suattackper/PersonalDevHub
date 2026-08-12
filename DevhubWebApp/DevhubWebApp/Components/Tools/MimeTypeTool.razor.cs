using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Mime;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// MIME explorer. Look-up and ranking are <see cref="IMimeLookupService"/>; this component owns the
/// filter state, the selection and the saved lists.
/// </summary>
public partial class MimeTypeTool : ToolComponentBase<MimeWorkspace>
{
    private const int RecentCapacity = 10;

    private IReadOnlyList<MimeEntry> _results = [];
    private MimeEntry? _selected;

    [Inject] private IMimeLookupService Mime { get; set; } = default!;

    protected override string ToolId => ToolIds.MimeType;

    private bool IsFavorite => _selected is not null
        && Workspace.Favorites.Contains(_selected.MimeType, StringComparer.OrdinalIgnoreCase);

    /// <summary>The header a developer usually wants to paste after looking a type up.</summary>
    private string HeaderSnippet => _selected is null
        ? string.Empty
        : $"Content-Type: {_selected.MimeType}";

    protected override Task OnWorkspaceLoadedAsync()
    {
        Refresh();
        _selected = Mime.FindByMimeType(Workspace.SelectedMimeType);

        return Task.CompletedTask;
    }

    private async Task OnSearchChangedAsync(string value)
    {
        Workspace.Search = value;

        Refresh();
        ScheduleAutoSave();
        await Task.CompletedTask;
    }

    private async Task OnCategoryChangedAsync(MimeCategory? category)
    {
        Workspace.Category = category;

        Refresh();
        await SaveWorkspaceAsync();
    }

    private void Refresh() => _results = Mime.Search(Workspace.Search, Workspace.Category);

    private async Task SelectAsync(MimeEntry entry)
    {
        _selected = entry;
        Workspace.SelectedMimeType = entry.MimeType;

        RememberRecent(entry.MimeType);
        await SaveWorkspaceAsync();
        await RecordAsync("Looked up MIME type", entry.MimeType);
    }

    private async Task SelectByTypeAsync(string mimeType)
    {
        if (Mime.FindByMimeType(mimeType) is { } entry)
        {
            await SelectAsync(entry);
        }
    }

    /// <summary>Keeps the newest look-ups, without duplicates.</summary>
    private void RememberRecent(string mimeType)
    {
        Workspace.Recent.RemoveAll(entry => string.Equals(entry, mimeType, StringComparison.OrdinalIgnoreCase));
        Workspace.Recent.Insert(0, mimeType);

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

        var mimeType = _selected.MimeType;

        if (Workspace.Favorites.RemoveAll(entry => string.Equals(entry, mimeType, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            Workspace.Favorites.Insert(0, mimeType);
        }

        await SaveWorkspaceAsync();
    }

    private static string GetCategoryIcon(MimeCategory category) => category switch
    {
        MimeCategory.Image => Icons.Material.Filled.Image,
        MimeCategory.Audio => Icons.Material.Filled.AudioFile,
        MimeCategory.Video => Icons.Material.Filled.VideoFile,
        MimeCategory.Document => Icons.Material.Filled.Description,
        MimeCategory.Archive => Icons.Material.Filled.FolderZip,
        MimeCategory.Font => Icons.Material.Filled.FontDownload,
        MimeCategory.Code => Icons.Material.Filled.Code,
        MimeCategory.Executable => Icons.Material.Filled.Terminal,
        _ => Icons.Material.Filled.DataObject
    };
}
