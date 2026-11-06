using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Pages.Tools;

/// <summary>
/// Behaviour for <c>ToolHost.razor</c>: resolves the tool for the current route, records the
/// visit, and builds the <see cref="ToolContext"/> cascaded to the tool component. It contains no
/// per-tool branching — everything is driven by registry metadata.
/// </summary>
public partial class ToolHost : IDisposable
{
    private bool _showGuide;

    private void ToggleGuide() => _showGuide = !_showGuide;
    private ToolView? _view;
    private ToolContext? _context;

    /// <summary>Tool id taken from the route.</summary>
    [Parameter] public string ToolId { get; set; } = string.Empty;

    [Inject] private IToolCatalog Catalog { get; set; } = default!;

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    [Inject] private IHistoryService History { get; set; } = default!;

    protected override void OnInitialized() => Preferences.Changed += OnPreferencesChanged;

    /// <summary>Runs on every navigation between tool routes, since the host instance is reused.</summary>
    protected override async Task OnParametersSetAsync()
    {
        var isNewTool = _view?.Id != ToolId;
        if (isNewTool)
        {
            _showGuide = false;
        }
        _view = Catalog.Find(ToolId);

        if (_view is null)
        {
            _context = null;
            return;
        }

        _context = new ToolContext(_view.Tool, (action, preview) => History.RecordAsync(_view.Id, action, preview));

        if (isNewTool)
        {
            await Preferences.TrackToolOpenedAsync(_view.Id);
        }
    }

    private Task ToggleFavoriteAsync()
        => _view is null ? Task.CompletedTask : Preferences.ToggleFavoriteAsync(_view.Id);

    private Task TogglePinAsync()
        => _view is null ? Task.CompletedTask : Preferences.TogglePinAsync(_view.Id);

    /// <summary>Favorite and pin state is owned elsewhere, so the projection is refreshed on change.</summary>
    private void OnPreferencesChanged()
    {
        _view = Catalog.Find(ToolId);
        StateHasChanged();
    }

    public void Dispose() => Preferences.Changed -= OnPreferencesChanged;
}
