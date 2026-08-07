using DevhubWebApp.Interfaces;
using Microsoft.JSInterop;

namespace DevhubWebApp.JSInterop;

/// <summary>
/// <see cref="IKeyboardShortcutService"/> backed by <c>wwwroot/js/shortcuts.js</c>.
/// </summary>
public sealed class KeyboardShortcutService : IKeyboardShortcutService, IAsyncDisposable
{
    private const string ModulePath = "./js/shortcuts.js";

    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public KeyboardShortcutService(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task<IAsyncDisposable> RegisterAsync(IEnumerable<string> combos, Func<string, Task> handler)
    {
        var module = await GetModuleAsync();
        var registration = new ShortcutRegistration(module, handler);
        await registration.StartAsync(combos.ToArray());

        return registration;
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
            // Page is unloading.
        }
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
        => _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);

    /// <summary>
    /// One live registration. It owns the .NET reference JS calls back into, and unhooks the
    /// listener on disposal.
    /// </summary>
    private sealed class ShortcutRegistration : IAsyncDisposable
    {
        private readonly IJSObjectReference _module;
        private readonly Func<string, Task> _handler;
        private readonly DotNetObjectReference<ShortcutRegistration> _self;
        private int _id;

        public ShortcutRegistration(IJSObjectReference module, Func<string, Task> handler)
        {
            _module = module;
            _handler = handler;
            _self = DotNetObjectReference.Create(this);
        }

        public async Task StartAsync(string[] combos)
            => _id = await _module.InvokeAsync<int>("register", _self, combos);

        /// <summary>Invoked from JavaScript when one of the registered combos is pressed.</summary>
        [JSInvokable]
        public Task OnShortcutAsync(string combo) => _handler(combo);

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _module.InvokeVoidAsync("unregister", _id);
            }
            catch (JSDisconnectedException)
            {
                // Page is unloading; the listener dies with it.
            }

            _self.Dispose();
        }
    }
}
