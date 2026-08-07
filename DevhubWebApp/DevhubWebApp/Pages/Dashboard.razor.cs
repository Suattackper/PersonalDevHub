using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Pages;

/// <summary>
/// Dashboard composition. The page owns no data: it only names which slice of the dashboard service
/// each section should load, which is why a change in one card never re-renders the others.
/// </summary>
public partial class Dashboard
{
    private static Task<IReadOnlyList<ToolView>> LoadPinnedAsync(IDashboardService service)
        => service.GetPinnedAsync();

    private static Task<IReadOnlyList<ToolView>> LoadFavoritesAsync(IDashboardService service)
        => service.GetFavoritesAsync();

    private static Task<IReadOnlyList<ToolView>> LoadRecentAsync(IDashboardService service)
        => service.GetRecentAsync(AppConstants.RecentDisplayCount);

    private static Task<IReadOnlyList<ToolView>> LoadMostUsedAsync(IDashboardService service)
        => service.GetMostUsedAsync(AppConstants.RecentDisplayCount);

    /// <summary>
    /// The page has no mutable state, so re-rendering it can never change the output. Blocking the
    /// re-render keeps a cascading parameter or navigation event from diffing every section.
    /// </summary>
    protected override bool ShouldRender() => !_hasRendered;

    private bool _hasRendered;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _hasRendered = true;
        }
    }
}
