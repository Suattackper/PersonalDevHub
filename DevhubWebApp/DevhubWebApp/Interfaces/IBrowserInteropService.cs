namespace DevhubWebApp.Interfaces;

/// <summary>
/// Thin, typed facade over the browser APIs DevHub needs. Every JS call in the application
/// goes through this contract so interop stays in one testable place.
/// </summary>
public interface IBrowserInteropService : IAsyncDisposable
{
    ValueTask<bool> CopyToClipboardAsync(string text);

    ValueTask<string> ReadClipboardAsync();

    /// <summary>Triggers a client-side download of UTF-8 text content.</summary>
    ValueTask DownloadTextAsync(string fileName, string content, string mimeType);

    /// <summary>Triggers a client-side download of binary content, e.g. a generated PNG.</summary>
    ValueTask DownloadBytesAsync(string fileName, byte[] content, string mimeType);

    /// <summary><c>true</c> when the OS/browser reports a dark color scheme preference.</summary>
    ValueTask<bool> PrefersDarkSchemeAsync();
}
