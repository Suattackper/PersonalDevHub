using DevhubWebApp.Extensions;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Dashboard;

/// <summary>
/// Behaviour for <c>ToolSection.razor</c>. The caller supplies a loader delegate, so the section
/// stays agnostic about which slice of the catalogue it shows.
/// </summary>
public partial class ToolSection : DashboardSectionBase
{
    private IReadOnlyList<ToolView> _views = [];

    /// <summary>Section heading.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>Heading icon.</summary>
    [Parameter] public string Icon { get; set; } = Icons.Material.Filled.Widgets;

    /// <summary>Supplies the tools to render. Invoked on first load and after every change.</summary>
    [Parameter, EditorRequired] public Func<IDashboardService, Task<IReadOnlyList<ToolView>>> Loader { get; set; } = default!;

    /// <summary>Headline shown when the loader returns nothing.</summary>
    [Parameter] public string EmptyTitle { get; set; } = "Nothing here yet";

    /// <summary>Guidance shown when the loader returns nothing.</summary>
    [Parameter] public string? EmptyDescription { get; set; }

    /// <summary>Icon of the empty state.</summary>
    [Parameter] public string EmptyIcon { get; set; } = Icons.Material.Outlined.Inbox;

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    protected override async Task LoadAsync() => _views = await Loader(DashboardService);

    private Task ToggleFavoriteAsync(ToolView view) => Preferences.ToggleFavoriteAsync(view);

    private Task TogglePinAsync(ToolView view) => Preferences.TogglePinAsync(view);
}
