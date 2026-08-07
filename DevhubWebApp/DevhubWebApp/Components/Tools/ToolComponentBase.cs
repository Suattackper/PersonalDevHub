using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;
using DevhubWebApp.Utilities;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Shared plumbing for tool workspaces: the cascaded <see cref="ToolContext"/>, the common
/// services, an undo/redo history, debounced work and keyboard-shortcut registration.
/// <para>
/// Tools inherit this so each one only contains what makes it different. Nothing here knows about
/// a specific tool.
/// </para>
/// </summary>
/// <typeparam name="TWorkspace">Auto-saved state of the tool.</typeparam>
public abstract class ToolComponentBase<TWorkspace> : ComponentBase, IAsyncDisposable
    where TWorkspace : class, new()
{
    private CancellationTokenSource? _debounceCts;
    private Timer? _autoSaveTimer;
    private IAsyncDisposable? _shortcuts;

    /// <summary>Undo/redo history of the tool's main document.</summary>
    protected UndoRedoHistory<string> History { get; } = new(AppConstants.UndoStackDepth);

    /// <summary>Auto-saved workspace, hydrated before the first render completes.</summary>
    protected TWorkspace Workspace { get; private set; } = new();

    /// <summary>Cascaded by the tool host: this tool's metadata and a pre-bound history writer.</summary>
    [CascadingParameter] protected ToolContext? Context { get; set; }

    [Inject] protected IWorkspaceService WorkspaceService { get; set; } = default!;

    [Inject] protected IBrowserInteropService Interop { get; set; } = default!;

    [Inject] protected INotificationService Notifications { get; set; } = default!;

    [Inject] protected IKeyboardShortcutService Shortcuts { get; set; } = default!;

    /// <summary>Id of the tool, used for workspace storage. Must match the <c>[Tool]</c> id.</summary>
    protected abstract string ToolId { get; }

    /// <summary>Keyboard combos this tool handles, e.g. <c>ctrl+enter</c>. Empty by default.</summary>
    protected virtual IReadOnlyList<string> ShortcutCombos => Array.Empty<string>();

    protected override async Task OnInitializedAsync()
    {
        Workspace = await WorkspaceService.LoadAsync(ToolId, new TWorkspace());
        await OnWorkspaceLoadedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && ShortcutCombos.Count > 0)
        {
            _shortcuts = await Shortcuts.RegisterAsync(ShortcutCombos, OnShortcutAsync);
        }
    }

    /// <summary>Called once the workspace has been restored; the tool seeds its state here.</summary>
    protected virtual Task OnWorkspaceLoadedAsync() => Task.CompletedTask;

    /// <summary>Handles a registered shortcut. The combo that fired is passed in.</summary>
    protected virtual Task OnShortcutAsync(string combo) => Task.CompletedTask;

    /// <summary>Persists the workspace immediately.</summary>
    protected Task SaveWorkspaceAsync() => WorkspaceService.SaveAsync(ToolId, Workspace).AsTask();

    /// <summary>
    /// Persists the workspace after a quiet period. Typing must not write to storage on every
    /// keystroke, so saves coalesce into one.
    /// </summary>
    protected void ScheduleAutoSave()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new Timer(
            _ => _ = InvokeAsync(SaveWorkspaceAsync),
            null,
            TimeSpan.FromMilliseconds(AppConstants.DebounceMilliseconds * 2),
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Runs <paramref name="work"/> after the debounce interval, cancelling any pending run.
    /// Used for preview rendering and validation, which must not fire on every keystroke.
    /// </summary>
    protected async Task DebounceAsync(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);

        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _debounceCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(AppConstants.DebounceMilliseconds, _debounceCts.Token);
            work();
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
            // Superseded by a newer change; that run will produce the result.
        }
    }

    /// <summary>Records an action in the activity log, if the tool is hosted with a context.</summary>
    protected Task RecordAsync(string action, string? preview = null)
        => Context is null ? Task.CompletedTask : Context.RecordAsync(action, preview);

    /// <summary>Copies text and reports the outcome, so no tool repeats that pairing.</summary>
    protected async Task CopyAsync(string? text, string successMessage)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (await Interop.CopyToClipboardAsync(text))
        {
            Notifications.Success(successMessage);
        }
        else
        {
            Notifications.Error("Clipboard is not available in this browser context.");
        }
    }

    /// <summary>Downloads text and reports the outcome.</summary>
    protected async Task DownloadAsync(string? content, string fileName, string mimeType)
    {
        if (string.IsNullOrEmpty(content))
        {
            return;
        }

        await Interop.DownloadTextAsync(fileName, content, mimeType);
        Notifications.Success($"Saved {fileName}.");
    }

    /// <summary>Releases timers, debounce tokens and shortcut registrations, then saves.</summary>
    public virtual async ValueTask DisposeAsync()
    {
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _autoSaveTimer?.Dispose();

        if (_shortcuts is not null)
        {
            await _shortcuts.DisposeAsync();
        }

        await SaveWorkspaceAsync();
        GC.SuppressFinalize(this);
    }
}
