using DevhubWebApp.Constants;
using DevhubWebApp.Extensions;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Pages;

/// <summary>
/// Interaction state for the design-system gallery. Nothing here is business logic — the fields
/// exist purely so each component can be exercised in its different states.
/// </summary>
public partial class Playground
{
    private const string SampleJson = """{ "tool": "DevHub", "offline": true, "tools": 22 }""";

    private ToolView? _sampleTool;

    private string _searchTerm = string.Empty;
    private bool _prettySelected = true;
    private bool _darkPreview = true;
    private bool _autoSavePreview = true;
    private bool _overlayVisible;

    [Inject] private INotificationService Notifications { get; set; } = default!;

    [Inject] private IDialogService Dialogs { get; set; } = default!;

    [Inject] private IToolCatalog Catalog { get; set; } = default!;

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    /// <summary>The card demo binds to a real catalogue entry so it exercises real metadata.</summary>
    protected override void OnInitialized() => _sampleTool = Catalog.Find(ToolIds.Json);

    private async Task ToggleFavoriteAsync(ToolView view)
    {
        await Preferences.ToggleFavoriteAsync(view);
        _sampleTool = Catalog.Find(view.Id);
    }

    private async Task TogglePinAsync(ToolView view)
    {
        await Preferences.TogglePinAsync(view);
        _sampleTool = Catalog.Find(view.Id);
    }

    private async Task ShowOverlayAsync()
    {
        _overlayVisible = true;
        await Task.Delay(TimeSpan.FromSeconds(2));
        _overlayVisible = false;
    }

    private void OnFileLoaded(FileDropResult result)
        => Notifications.Success($"Loaded {result.FileName} ({result.Size:N0} bytes).");

    private async Task ConfirmClearAsync()
    {
        var confirmed = await Dialogs.ConfirmAsync(
            "Clear local data?",
            "History, favorites and pinned tools will be removed from this browser. This cannot be undone.",
            confirmText: "Clear everything");

        if (confirmed)
        {
            Notifications.Warning("Confirmed — the settings module will wire this to storage.");
        }
    }
}
