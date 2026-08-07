namespace DevhubWebApp.Interfaces;

/// <summary>
/// Persistence abstraction over the browser storage engine. Consumers depend on this
/// contract only, so the backing store (localStorage today, IndexedDB later) can change
/// without touching services or components.
/// </summary>
public interface IStorageService
{
    /// <summary>Reads and deserializes a value, or returns <paramref name="fallback"/> when absent or corrupt.</summary>
    ValueTask<T> GetAsync<T>(string key, T fallback);

    /// <summary>Reads and deserializes a value, or <c>null</c> when absent or corrupt.</summary>
    ValueTask<T?> GetOrDefaultAsync<T>(string key);

    ValueTask SetAsync<T>(string key, T value);

    ValueTask RemoveAsync(string key);

    /// <summary>Removes every DevHub-owned key. Other origin data is left untouched.</summary>
    ValueTask ClearAsync();

    /// <summary>Raw JSON of all DevHub keys, used by settings export.</summary>
    ValueTask<string> ExportAsync();

    /// <summary>Restores keys from a document produced by <see cref="ExportAsync"/>.</summary>
    ValueTask ImportAsync(string json);
}
