using DevhubWebApp.Interfaces;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Dashboard;

/// <summary>
/// Shared plumbing for dashboard sections: each section loads its own data, owns its loading and
/// error state, and subscribes to <see cref="IDashboardService.Changed"/> itself.
/// <para>
/// That is what keeps the dashboard cheap: a favorite toggle re-renders the sections that display
/// favorites, not the page. The page holds no state and therefore never re-renders.
/// </para>
/// </summary>
public abstract class DashboardSectionBase : ComponentBase, IDisposable
{
    /// <summary><c>true</c> while the first load is in flight; sections show a skeleton meanwhile.</summary>
    protected bool IsLoading { get; private set; } = true;

    /// <summary>Message of the last failed load, or <c>null</c> when the section is healthy.</summary>
    protected string? ErrorMessage { get; private set; }

    [Inject] protected IDashboardService DashboardService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        DashboardService.Changed += OnDashboardChanged;
        await ReloadAsync();
    }

    /// <summary>Loads the section's data into its own fields. Implementations must not throw.</summary>
    protected abstract Task LoadAsync();

    /// <summary>
    /// Runs a load, translating any failure into an error state. A broken section degrades on its
    /// own instead of taking the dashboard down with it.
    /// </summary>
    protected async Task ReloadAsync()
    {
        try
        {
            ErrorMessage = null;
            await LoadAsync();
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or TaskCanceledException)
        {
            ErrorMessage = exception.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Reloads after an external change and re-renders this section only.</summary>
    private async void OnDashboardChanged()
    {
        await InvokeAsync(async () =>
        {
            await ReloadAsync();
            StateHasChanged();
        });
    }

    public void Dispose()
    {
        DashboardService.Changed -= OnDashboardChanged;
        GC.SuppressFinalize(this);
    }
}
