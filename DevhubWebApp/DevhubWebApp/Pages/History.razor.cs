using DevhubWebApp.Extensions;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Pages;

public partial class History : IDisposable
{
    private IReadOnlyList<HistoryEntry> _entries = [];
    private string _search = string.Empty;

    [Inject] private IHistoryService HistoryService { get; set; } = default!;
    [Inject] private IToolCatalog Catalog { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;
    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    private IReadOnlyList<HistoryEntry> FilteredEntries => string.IsNullOrWhiteSpace(_search)
        ? _entries
        : _entries.Where(entry =>
            entry.Action.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            entry.Preview.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            (Catalog.Find(entry.ToolId)?.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

    protected override void OnInitialized()
    {
        Load();
        HistoryService.Changed += OnChanged;
    }

    private void Load() => _entries = HistoryService.Entries.ToList();
    private Task OnSearchChanged(string value) { _search = value; return Task.CompletedTask; }
    private Task RemoveAsync(string id) => HistoryService.RemoveAsync(id);

    private async Task Restore(HistoryEntry entry)
    {
        var tool = Catalog.Find(entry.ToolId);
        if (tool is not null)
        {
            await HistoryService.RestoreAsync(entry.Id);
            Navigation.NavigateTo(tool.Route);
        }
    }

    private async Task ClearAllAsync()
    {
        if (await Dialogs.ConfirmAsync("Clear history?", "All recorded tool activity will be removed.", "Clear history"))
        {
            await HistoryService.ClearAsync();
        }
    }

    private void OnChanged()
    {
        Load();
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose() => HistoryService.Changed -= OnChanged;
}
