using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Navigation;

/// <summary>
/// Sidebar behaviour. The projections are rebuilt only when the filter text or the preference
/// state changes, so navigation does not re-query the catalogue on every render.
/// </summary>
public partial class SideBar : IDisposable
{
    private string _filter = string.Empty;
    private IReadOnlyList<ToolCategoryGroup> _groups = [];
    private IReadOnlyList<ToolView> _pinned = [];
    private int _toolCount;

    [Inject] private IToolCatalog Catalog { get; set; } = default!;

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    protected override void OnInitialized()
    {
        Preferences.Changed += OnPreferencesChanged;
        Rebuild();
    }

    private void OnFilterChanged(string value)
    {
        _filter = value;
        Rebuild();
    }

    private void OnPreferencesChanged()
    {
        Rebuild();
        StateHasChanged();
    }

    private void Rebuild()
    {
        _pinned = Catalog.Pinned();
        _groups = Catalog.ByCategory(_filter);
        _toolCount = Catalog.All().Count;
    }

    /// <summary>
    /// Groups auto-expand while filtering so matches are visible without extra clicks; otherwise
    /// only the first category starts open, which keeps the sidebar short.
    /// </summary>
    private bool IsGroupExpanded(ToolCategoryGroup group)
        => !string.IsNullOrWhiteSpace(_filter) || group.Category.Id == _groups[0].Category.Id;

    public void Dispose() => Preferences.Changed -= OnPreferencesChanged;
}
