using DevhubWebApp.Extensions;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Navigation;

/// <summary>Top bar behaviour: metadata-driven global search and theme switching.</summary>
public partial class TopBar : IDisposable
{
    [Parameter] public EventCallback OnToggleSidebar { get; set; }

    [Inject] private IToolCatalog Catalog { get; set; } = default!;

    [Inject] private IThemeProvider ThemeProvider { get; set; } = default!;

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override void OnInitialized()
    {
        ThemeProvider.Changed += OnThemeChanged;
        Preferences.Changed += OnThemeChanged;
    }

    private Task<IEnumerable<ToolView>> SearchToolsAsync(string? term, CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<ToolView>>(Catalog.Search(term));

    private void OnToolSelected(ToolView? view)
    {
        if (view is not null)
        {
            Navigation.GoToTool(view);
        }
    }

    private async Task ToggleThemeAsync()
    {
        var settings = Preferences.Settings.Clone();
        settings.Theme = ThemeProvider.IsDarkMode ? AppThemeMode.Light : AppThemeMode.Dark;

        await Preferences.SaveSettingsAsync(settings);
        await ThemeProvider.ApplyAsync(settings.Theme);
    }

    private void OnThemeChanged() => StateHasChanged();

    public void Dispose()
    {
        ThemeProvider.Changed -= OnThemeChanged;
        Preferences.Changed -= OnThemeChanged;
    }
}
