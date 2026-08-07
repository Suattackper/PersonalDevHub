using System.Text.Json;
using DevhubWebApp.Interfaces;
using Microsoft.JSInterop;

namespace DevhubWebApp.Storage;

/// <summary>
/// <see cref="IStorageService"/> backed by <c>window.localStorage</c> through a dedicated JS module.
/// Values are stored as JSON; unreadable entries degrade to the caller's fallback instead of throwing,
/// so a corrupt key can never break application start-up.
/// </summary>
public sealed class LocalStorageService : IStorageService, IAsyncDisposable
{
    private const string ModulePath = "./js/storage.js";

    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private IJSObjectReference? _module;

    public LocalStorageService(IJSRuntime jsRuntime, ILogger<LocalStorageService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }

    public async ValueTask<T> GetAsync<T>(string key, T fallback)
        => await GetOrDefaultAsync<T>(key) ?? fallback;

    public async ValueTask<T?> GetOrDefaultAsync<T>(string key)
    {
        var raw = await InvokeAsync<string?>("getItem", key);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(raw, _jsonOptions);
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Discarding corrupt storage entry {Key}.", key);
            await RemoveAsync(key);
            return default;
        }
    }

    public async ValueTask SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, _jsonOptions);
        await InvokeVoidAsync("setItem", key, json);
    }

    public ValueTask RemoveAsync(string key) => InvokeVoidAsync("removeItem", key);

    public ValueTask ClearAsync() => InvokeVoidAsync("clearNamespace");

    public async ValueTask<string> ExportAsync()
        => await InvokeAsync<string?>("exportNamespace") ?? "{}";

    public ValueTask ImportAsync(string json) => InvokeVoidAsync("importNamespace", json);

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
            // The circuit is already gone during shutdown; nothing to release.
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

    private async ValueTask<TResult> InvokeAsync<TResult>(string identifier, params object?[] args)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<TResult>(identifier, args);
    }

    private async ValueTask InvokeVoidAsync(string identifier, params object?[] args)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync(identifier, args);
    }
}
