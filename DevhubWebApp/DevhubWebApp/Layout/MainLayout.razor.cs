using DevhubWebApp.Interfaces;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Layout;

/// <summary>
/// Shell state. The sidebar's open/closed choice is a user preference, so it survives reloads.
/// </summary>
public partial class MainLayout
{
    private bool _sidebarOpen = true;

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    protected override void OnInitialized() => _sidebarOpen = Preferences.Settings.SidebarExpanded;

    private async Task ToggleSidebar()
    {
        _sidebarOpen = !_sidebarOpen;

        var settings = Preferences.Settings.Clone();
        settings.SidebarExpanded = _sidebarOpen;
        await Preferences.SaveSettingsAsync(settings);
    }
}
