using System.Text.Json;
using DevhubWebApp.Extensions;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace DevhubWebApp.Pages;

public partial class Settings
{
    private AppSettings _settings = new();

    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;
    [Inject] private IHistoryService History { get; set; } = default!;
    [Inject] private IStorageService Storage { get; set; } = default!;
    [Inject] private IThemeProvider Theme { get; set; } = default!;
    [Inject] private IToolCatalog Catalog { get; set; } = default!;
    [Inject] private IBrowserInteropService Interop { get; set; } = default!;
    [Inject] private INotificationService Notifications { get; set; } = default!;
    [Inject] private IDialogService Dialogs { get; set; } = default!;

    protected override void OnInitialized() => _settings = Preferences.Settings.Clone();

    private async Task SaveAsync(Action<AppSettings> update)
    {
        update(_settings);
        await Preferences.SaveSettingsAsync(_settings);
    }

    private async Task SetThemeAsync(AppThemeMode value) { await SaveAsync(s => s.Theme = value); await Theme.ApplyAsync(value); }
    private Task SetLanguageAsync(AppLanguage value) => SaveAsync(s => s.Language = value);
    private Task SetDefaultToolAsync(string value) => SaveAsync(s => s.DefaultToolId = value ?? string.Empty);
    private Task SetAutoSaveAsync(bool value) => SaveAsync(s => s.AutoSaveWorkspace = value);
    private Task SetTrackHistoryAsync(bool value) => SaveAsync(s => s.TrackHistory = value);

    private async Task ExportAsync()
    {
        await Interop.DownloadTextAsync("devhub-backup.json", await Storage.ExportAsync(), "application/json");
        Notifications.Success("Backup exported.");
    }

    private async Task ImportAsync(FileDropResult file)
    {
        try
        {
            using var document = JsonDocument.Parse(file.Text ?? string.Empty);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                document.RootElement.EnumerateObject().Any(property => !property.Name.StartsWith("devhub.", StringComparison.Ordinal) || property.Value.ValueKind != JsonValueKind.String))
            {
                throw new JsonException("The file is not a DevHub backup.");
            }
            await Storage.ImportAsync(file.Text!);
            await Preferences.ReloadAsync();
            await History.ReloadAsync();
            _settings = Preferences.Settings.Clone();
            await Theme.ApplyAsync(_settings.Theme);
            Notifications.Success("Backup imported.");
        }
        catch (Exception exception) when (exception is JsonException or JSException)
        {
            Notifications.Error($"Could not import backup: {exception.Message}");
        }
    }

    private async Task ResetAllAsync()
    {
        if (!await Dialogs.ConfirmAsync("Reset all local data?", "Settings, history, favorites, pins and saved tool inputs will be removed.", "Reset all"))
        {
            return;
        }
        await Storage.ClearAsync();
        await Preferences.ReloadAsync();
        await History.ReloadAsync();
        _settings = Preferences.Settings.Clone();
        await Theme.ApplyAsync(_settings.Theme);
        Notifications.Success("Local data reset.");
    }
}
