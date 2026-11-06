using DevhubWebApp.Extensions;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Shared;

/// <summary>
/// Behaviour for <c>ToolCard.razor</c>. The card renders the state it is handed and reports intent
/// through callbacks; it never writes to storage itself.
/// </summary>
public partial class ToolCard
{
    /// <summary>Tool metadata joined with this browser's favorite, pin and usage state.</summary>
    [Parameter, EditorRequired] public ToolView View { get; set; } = default!;

    /// <summary>Shows the favorite toggle.</summary>
    [Parameter] public bool ShowFavorite { get; set; } = true;

    /// <summary>Shows the pin toggle.</summary>
    [Parameter] public bool ShowPin { get; set; } = true;

    /// <summary>Overrides the footer note. Defaults to the tool's usage label.</summary>
    [Parameter] public string? Meta { get; set; }

    /// <summary>Raised when the favorite toggle is activated.</summary>
    [Parameter] public EventCallback<ToolView> OnToggleFavorite { get; set; }

    /// <summary>Raised when the pin toggle is activated.</summary>
    [Parameter] public EventCallback<ToolView> OnTogglePin { get; set; }

    /// <summary>Extra CSS classes appended to the card.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private string FooterMeta => Meta ?? View.ToUsageLabel(Preferences.Settings.Language);

    /// <summary>The icon tint follows the tool's declared accent color.</summary>
    private string IconStyle => $"background-color: var(--mud-palette-{View.Tool.Color.ToDescriptionString()}-hover);";

    private void NavigateToTool() => Navigation.GoToTool(View);

    private Task ToggleFavoriteAsync() => OnToggleFavorite.InvokeAsync(View);

    private Task TogglePinAsync() => OnTogglePin.InvokeAsync(View);
}
