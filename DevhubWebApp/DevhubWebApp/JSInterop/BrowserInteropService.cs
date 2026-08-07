using DevhubWebApp.Interfaces;
using Microsoft.JSInterop;

namespace DevhubWebApp.JSInterop;

/// <summary>
/// <see cref="IBrowserInteropService"/> implementation backed by <c>wwwroot/js/interop.js</c>.
/// The module is imported lazily and cached for the lifetime of the application.
/// </summary>
public sealed class BrowserInteropService : IBrowserInteropService
{
    private const string ModulePath = "./js/interop.js";

    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public BrowserInteropService(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async ValueTask<bool> CopyToClipboardAsync(string text)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("copyToClipboard", text);
    }

    public async ValueTask<string> ReadClipboardAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<string>("readClipboard");
    }

    public async ValueTask DownloadTextAsync(string fileName, string content, string mimeType)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("downloadText", fileName, content, mimeType);
    }

    public async ValueTask DownloadBytesAsync(string fileName, byte[] content, string mimeType)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("downloadBytes", fileName, Convert.ToBase64String(content), mimeType);
    }

    public async ValueTask<bool> PrefersDarkSchemeAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool>("prefersDarkScheme");
    }

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
            // Page is unloading; the module is already gone.
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
}
