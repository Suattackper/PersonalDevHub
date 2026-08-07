using DevhubWebApp.Extensions;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Dashboard;

/// <summary>
/// Behaviour for <c>QuickSearchPanel.razor</c>. The term is a property rather than a field so a new
/// value re-runs the query exactly once, at assignment time.
/// </summary>
public partial class QuickSearchPanel : DashboardSectionBase
{
    private IReadOnlyList<ToolView> _results = [];
    private string _searchTerm = string.Empty;

    /// <summary>Placeholder of the search box.</summary>
    [Parameter] public string Placeholder { get; set; } = "Quick search — name, keyword, tag or category…";

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    private string SearchTerm
    {
        get => _searchTerm;
        set
        {
            _searchTerm = value;
            _ = ReloadAsync();
        }
    }

    protected override async Task LoadAsync()
        => _results = string.IsNullOrWhiteSpace(_searchTerm)
            ? []
            : await DashboardService.SearchAsync(_searchTerm);

    private Task ToggleFavoriteAsync(ToolView view) => Preferences.ToggleFavoriteAsync(view);

    private Task TogglePinAsync(ToolView view) => Preferences.TogglePinAsync(view);
}
