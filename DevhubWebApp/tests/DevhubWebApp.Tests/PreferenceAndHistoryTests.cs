using System.Text.Json;
using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Services;

namespace DevhubWebApp.Tests;

public sealed class PreferenceAndHistoryTests
{
    [Fact]
    public async Task Settings_SaveReloadAndResetRoundTrip()
    {
        var storage = new MemoryStorage();
        var service = new UserPreferenceService(storage);
        await service.InitializeAsync();
        Assert.Equal(AppLanguage.Vietnamese, service.Settings.Language);

        var settings = service.Settings.Clone();
        settings.Theme = AppThemeMode.Light;
        settings.Language = AppLanguage.English;
        settings.TrackHistory = false;
        await service.SaveSettingsAsync(settings);
        await service.ReloadAsync();

        Assert.Equal(AppThemeMode.Light, service.Settings.Theme);
        Assert.Equal(AppLanguage.English, service.Settings.Language);
        Assert.Equal("Settings", UiText.Get(service.Settings.Language, "Settings"));
        Assert.False(service.Settings.TrackHistory);

        await service.ResetSettingsAsync();
        Assert.Equal(AppThemeMode.Dark, service.Settings.Theme);
        Assert.Equal(AppLanguage.Vietnamese, service.Settings.Language);
        Assert.True(service.Settings.TrackHistory);
    }

    [Fact]
    public async Task History_CreatesCapsDeletesClearsAndPersists()
    {
        var storage = new MemoryStorage();
        var preferences = new UserPreferenceService(storage);
        await preferences.InitializeAsync();
        var history = new HistoryService(storage, preferences);
        await history.InitializeAsync();

        for (var index = 0; index < AppConstants.HistoryCapacity + 5; index++)
        {
            await history.RecordAsync("json", $"Action {index}", new string('x', 200));
        }

        Assert.Equal(AppConstants.HistoryCapacity, history.Entries.Count);
        Assert.Equal(121, history.Entries[0].Preview.Length);
        var id = history.Entries[0].Id;
        await history.RemoveAsync(id);
        Assert.DoesNotContain(history.Entries, entry => entry.Id == id);

        await history.ReloadAsync();
        Assert.Equal(AppConstants.HistoryCapacity - 1, history.Entries.Count);
        await history.ClearAsync();
        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task History_RestoresWorkspaceCapturedAtActionTime()
    {
        var storage = new MemoryStorage();
        var preferences = new UserPreferenceService(storage);
        await preferences.InitializeAsync();
        var history = new HistoryService(storage, preferences);
        await history.InitializeAsync();
        await storage.SetAsync(StorageKeys.Workspace("json"), new { Input = "first" });
        await history.RecordAsync("json", "Formatted");
        await storage.SetAsync(StorageKeys.Workspace("json"), new { Input = "second" });

        var restored = await history.RestoreAsync(history.Entries[0].Id);
        var workspace = await storage.GetOrDefaultAsync<JsonElement>(StorageKeys.Workspace("json"));

        Assert.True(restored);
        Assert.Equal("first", workspace.GetProperty("input").GetString());
    }

    [Fact]
    public async Task History_DoesNotRecordWhenDisabled()
    {
        var storage = new MemoryStorage();
        var preferences = new UserPreferenceService(storage);
        await preferences.InitializeAsync();
        var settings = preferences.Settings.Clone();
        settings.TrackHistory = false;
        await preferences.SaveSettingsAsync(settings);
        var history = new HistoryService(storage, preferences);
        await history.InitializeAsync();

        await history.RecordAsync("json", "Formatted");

        Assert.Empty(history.Entries);
    }

    [Fact]
    public async Task Preferences_NormalizeStructurallyValidButInvalidStoredValues()
    {
        var storage = new MemoryStorage();
        await storage.SetAsync(StorageKeys.Settings, new AppSettings
        {
            Theme = (AppThemeMode)99,
            Language = (AppLanguage)99,
            DefaultToolId = "  json  ",
            SchemaVersion = "old"
        });
        await storage.SetAsync(StorageKeys.Recent, new List<string> { "json", "", "JSON" });
        var service = new UserPreferenceService(storage);

        await service.InitializeAsync();

        Assert.Equal(AppThemeMode.Dark, service.Settings.Theme);
        Assert.Equal(AppLanguage.Vietnamese, service.Settings.Language);
        Assert.Equal("json", service.Settings.DefaultToolId);
        Assert.Equal(AppConstants.AppVersion, service.Settings.SchemaVersion);
        Assert.Single(service.Recent);
    }

    private sealed class MemoryStorage : IStorageService
    {
        private readonly Dictionary<string, string> _values = [];
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        public ValueTask<T> GetAsync<T>(string key, T fallback)
            => new(_values.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json, Options) ?? fallback : fallback);

        public ValueTask<T?> GetOrDefaultAsync<T>(string key)
            => new(_values.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json, Options) : default);

        public ValueTask SetAsync<T>(string key, T value)
        {
            _values[key] = JsonSerializer.Serialize(value, Options);
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveAsync(string key) { _values.Remove(key); return ValueTask.CompletedTask; }
        public ValueTask ClearAsync() { _values.Clear(); return ValueTask.CompletedTask; }
        public ValueTask<string> ExportAsync() => new(JsonSerializer.Serialize(_values));

        public ValueTask ImportAsync(string json)
        {
            _values.Clear();
            foreach (var item in JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? []) _values[item.Key] = item.Value;
            return ValueTask.CompletedTask;
        }
    }
}
