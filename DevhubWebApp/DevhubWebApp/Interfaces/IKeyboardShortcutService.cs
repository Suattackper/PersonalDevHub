namespace DevhubWebApp.Interfaces;

/// <summary>
/// Registers document-level keyboard shortcuts. Tools declare the combos they care about and get
/// back a handle; disposing it removes the listener, so shortcuts cannot leak between pages.
/// </summary>
public interface IKeyboardShortcutService
{
    /// <summary>
    /// Registers a handler for the given combos, e.g. <c>ctrl+enter</c>, <c>ctrl+s</c>.
    /// The combo that fired is passed to <paramref name="handler"/>.
    /// </summary>
    Task<IAsyncDisposable> RegisterAsync(IEnumerable<string> combos, Func<string, Task> handler);
}
