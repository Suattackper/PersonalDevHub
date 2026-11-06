using System.Globalization;
using System.Text.Json;
using DevhubWebApp.Components.Shared;
using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools;
using DevhubWebApp.Models.Tools.Json;
using DevhubWebApp.Utilities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Binds <see cref="JsonFormatter"/> to the workspace UI. It owns interaction state only —
/// document text, options, undo history — and delegates every transformation to the formatter.
/// </summary>
public partial class JsonTool : IAsyncDisposable
{
    private const string ShortcutFormat = "ctrl+enter";
    private const string ShortcutMinify = "ctrl+shift+m";
    private const string ShortcutDownload = "ctrl+s";
    private const string ShortcutCopy = "ctrl+c";
    private const string DownloadFileName = "devhub.json";

    /// <summary>Above this size a rewrite is slow enough that the user needs a progress indicator.</summary>
    private const int LargeDocumentThreshold = 200_000;

    /// <summary>Above this size the duplicate scan and the tree are skipped; validation still runs.</summary>
    private const int StructureAnalysisLimit = 512_000;

    /// <summary>Largest document written to the auto-saved workspace.</summary>
    private const int MaxPersistedContent = 256_000;

    private readonly UndoRedoHistory<string> _history = new(AppConstants.UndoStackDepth);

    private IAsyncDisposable? _shortcuts;
    private CodeEditor? _editor;
    private JsonWorkspace _workspace = new();
    private IReadOnlyList<TreeItemData<JsonTreeNode>> _treeItems = [];
    private IReadOnlyList<EditorMarker> _markers = [];
    private JsonTreeNode? _tree;
    private JsonError? _error;
    private CancellationTokenSource? _validationCts;
    private Timer? _autoSaveTimer;
    private string _content = string.Empty;
    private string _statusLabel = string.Empty;
    private int _duplicateCount;
    private bool _isBusy;
    private bool _isStructureAnalysisPaused;

    /// <summary>Cascaded by the tool host: this tool's metadata plus a pre-bound history writer.</summary>
    [CascadingParameter] private ToolContext? Context { get; set; }

    [Inject] private IWorkspaceService Workspace { get; set; } = default!;

    [Inject] private IBrowserInteropService Interop { get; set; } = default!;

    [Inject] private INotificationService Notifications { get; set; } = default!;
    [Inject] private IUserPreferenceService Preferences { get; set; } = default!;

    [Inject] private IKeyboardShortcutService Shortcuts { get; set; } = default!;

    private bool IsEmpty => string.IsNullOrWhiteSpace(_content);
    private string L(string text) => UiText.Get(Preferences.Settings.Language, text);

    private string TreePlaceholderTitle => (IsEmpty, _isStructureAnalysisPaused, _error) switch
    {
        (true, _, _) => "Nothing to show yet",
        (_, true, _) => "Document too large for the tree",
        (_, _, not null) => "Document does not parse",
        _ => "No structure to show"
    };

    private string TreePlaceholderDescription => (IsEmpty, _isStructureAnalysisPaused, _error) switch
    {
        (true, _, _) => "Paste, type or drop a JSON file to see its structure.",
        (_, true, _) => $"The tree and duplicate scan are paused above {StructureAnalysisLimit / 1000} KB so formatting stays responsive.",
        (_, _, not null) => "Fix the error to see the structure.",
        _ => string.Empty
    };

    private string DuplicateChipLabel
        => _duplicateCount > 0 ? $"Drop {_duplicateCount} duplicate keys" : "Drop duplicate keys";

    protected override async Task OnInitializedAsync()
    {
        _workspace = await Workspace.LoadAsync(ToolIds.Json, new JsonWorkspace());
        _content = _workspace.Content;
        _history.Reset(_content);

        Analyse();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _shortcuts = await Shortcuts.RegisterAsync(
                [ShortcutFormat, ShortcutDownload, ShortcutCopy, ShortcutMinify],
                HandleShortcutAsync);
        }
    }

    /// <summary>Routes a shortcut to the same handlers the toolbar buttons use.</summary>
    private async Task HandleShortcutAsync(string combo)
    {
        switch (combo)
        {
            case ShortcutFormat:
                await PrettyAsync();
                break;

            case ShortcutMinify:
                await MinifyAsync();
                break;

            case ShortcutDownload:
                await DownloadAsync();
                break;

            case ShortcutCopy:
                await CopyAsync();
                break;

            default:
                return;
        }

        StateHasChanged();
    }

    private async Task CopyAsync()
    {
        if (IsEmpty)
        {
            return;
        }

        if (await Interop.CopyToClipboardAsync(_content))
        {
            Notifications.Success("Copied to clipboard.");
        }
    }

    private async Task DownloadAsync()
    {
        if (IsEmpty)
        {
            return;
        }

        await Interop.DownloadTextAsync(DownloadFileName, _content, MimeTypes.Json);
        Notifications.Success($"Saved {DownloadFileName}.");
    }

    /// <summary>
    /// Typing must not reformat, so a keystroke only schedules validation. The debounce is what
    /// keeps a large document responsive while the user edits it.
    /// </summary>
    private async Task OnContentChangedAsync(string value)
    {
        _content = value;
        await ScheduleValidationAsync();
        ScheduleAutoSave();
    }

    private async Task ScheduleValidationAsync()
    {
        _validationCts?.Cancel();
        _validationCts?.Dispose();
        _validationCts = new CancellationTokenSource();
        var token = _validationCts.Token;

        try
        {
            await Task.Delay(AppConstants.DebounceMilliseconds, token);
            Analyse();
            StateHasChanged();
        }
        catch (TaskCanceledException)
        {
            // Superseded by a newer keystroke; the newer run will produce the diagnostics.
        }
    }

    /// <summary>
    /// Re-derives diagnostics, duplicates and the tree from a single parse.
    /// <para>
    /// Past <see cref="StructureAnalysisLimit"/> the structural pass is skipped and only validation
    /// runs: walking a multi-megabyte document on every debounce tick costs seconds on WebAssembly,
    /// and a tree of that size is not useful anyway. Formatting keeps working at full size.
    /// </para>
    /// </summary>
    private void Analyse()
    {
        _isStructureAnalysisPaused = _content.Length > StructureAnalysisLimit;

        if (_isStructureAnalysisPaused)
        {
            _error = JsonFormatter.Validate(_content);
            _duplicateCount = 0;
            _tree = null;
        }
        else
        {
            var inspection = JsonFormatter.Inspect(_content, buildTree: _workspace.ShowTree);
            _error = inspection.Error;
            _duplicateCount = inspection.DuplicateCount;
            _tree = inspection.Tree;
        }

        _markers = _error is null
            ? []
            : [new EditorMarker { Message = _error.Message, Line = _error.Line, Column = _error.Column }];

        _treeItems = _tree is null ? [] : [ToTreeItem(_tree, expanded: true)];
        _statusLabel = BuildStatusLabel();
    }

    private string BuildStatusLabel()
    {
        if (IsEmpty)
        {
            return string.Empty;
        }

        var lines = _content.AsSpan().Count('\n') + 1;
        var size = _content.Length;
        var label = Preferences.Settings.Language == AppLanguage.Vietnamese
            ? $"{lines.ToString("N0", CultureInfo.InvariantCulture)} dòng · {size.ToString("N0", CultureInfo.InvariantCulture)} ký tự"
            : $"{lines.ToString("N0", CultureInfo.InvariantCulture)} lines · {size.ToString("N0", CultureInfo.InvariantCulture)} characters";

        if (_isStructureAnalysisPaused)
        {
            return Preferences.Settings.Language == AppLanguage.Vietnamese
                ? $"{label} · tạm dừng phân tích cấu trúc trên {StructureAnalysisLimit / 1000} KB"
                : $"{label} · structure analysis paused above {StructureAnalysisLimit / 1000} KB";
        }

        return _duplicateCount > 0
            ? Preferences.Settings.Language == AppLanguage.Vietnamese ? $"{label} · {_duplicateCount} khóa trùng" : $"{label} · {_duplicateCount} duplicate keys"
            : label;
    }

    private Task PrettyAsync() => ApplyAsync(JsonFormatter.Pretty, "Formatted");

    private Task MinifyAsync() => ApplyAsync(JsonFormatter.Minify, "Minified");

    /// <summary>
    /// Runs a transformation, commits it to the undo history and records it in the activity log.
    /// A failed transformation leaves the document untouched.
    /// </summary>
    private async Task ApplyAsync(Func<string?, JsonFormatOptions?, JsonOperationResult> operation, string action)
    {
        var options = new JsonFormatOptions
        {
            SortProperties = _workspace.SortProperties,
            RemoveDuplicateProperties = _workspace.RemoveDuplicateProperties
        };

        // WebAssembly is single-threaded: a megabyte-scale rewrite blocks the UI thread. Showing
        // the overlay and yielding first means the user sees progress instead of a frozen page.
        var isLarge = _content.Length >= LargeDocumentThreshold;

        if (isLarge)
        {
            _isBusy = true;
            StateHasChanged();
            await Task.Delay(1);
        }

        var result = operation(_content, options);

        _isBusy = false;

        if (!result.IsSuccess)
        {
            _error = result.Error;
            _markers = [new EditorMarker { Message = _error!.Message, Line = _error.Line, Column = _error.Column }];
            Notifications.Error($"Invalid JSON at {_error.Location}.");
            return;
        }

        SetFormattedContent(result.Output, result.RemovedDuplicates);

        var suffix = result.RemovedDuplicates > 0 ? $" · {result.RemovedDuplicates} duplicate keys removed" : string.Empty;
        Notifications.Success($"{action}{suffix}.");

        if (Context is not null)
        {
            await Context.RecordAsync(action, result.Output);
        }

        await SaveWorkspaceAsync();
    }

    /// <summary>Replaces the document and records a single undo step.</summary>
    private void SetContent(string value)
    {
        _content = value;
        _history.Push(value);
        Analyse();
    }

    /// <summary>
    /// Commits formatter output. The text was produced by a successful parse, so re-validating it
    /// would mean parsing megabytes again to learn what we already know.
    /// </summary>
    private void SetFormattedContent(string value, int removedDuplicates)
    {
        _content = value;
        _history.Push(value);

        _error = null;
        _markers = [];
        _isStructureAnalysisPaused = _content.Length > StructureAnalysisLimit;

        if (_isStructureAnalysisPaused || !_workspace.ShowTree)
        {
            _tree = null;
            _treeItems = [];
            _duplicateCount = _workspace.RemoveDuplicateProperties ? 0 : _duplicateCount;
        }
        else
        {
            var inspection = JsonFormatter.Inspect(_content, buildTree: true);
            _duplicateCount = inspection.DuplicateCount;
            _tree = inspection.Tree;
            _treeItems = _tree is null ? [] : [ToTreeItem(_tree, expanded: true)];
        }

        if (removedDuplicates > 0)
        {
            _duplicateCount = 0;
        }

        _statusLabel = BuildStatusLabel();
    }

    private void Undo()
    {
        if (_history.Undo() is { } previous)
        {
            _content = previous;
            Analyse();
        }
    }

    private void Redo()
    {
        if (_history.Redo() is { } next)
        {
            _content = next;
            Analyse();
        }
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        SetContent(text);
        MaybeFormatOnPaste();
        await SaveWorkspaceAsync();
    }

    /// <summary>Fired by Monaco after an in-editor paste.</summary>
    private async Task OnPasteAsync()
    {
        MaybeFormatOnPaste();
        await SaveWorkspaceAsync();
        StateHasChanged();
    }

    /// <summary>Reformats right after a paste when the setting is on. Invalid input is left alone.</summary>
    private void MaybeFormatOnPaste()
    {
        if (!_workspace.FormatOnPaste || IsEmpty)
        {
            return;
        }

        var result = JsonFormatter.Pretty(_content, new JsonFormatOptions());

        if (result.IsSuccess)
        {
            SetContent(result.Output);
        }
        else
        {
            // A paste of invalid JSON is normal; the inline diagnostics already explain it.
            Analyse();
        }
    }

    private async Task OnFileLoadedAsync(FileDropResult file)
    {
        SetContent(file.Text ?? string.Empty);
        MaybeFormatOnPaste();
        await SaveWorkspaceAsync();

        Notifications.Success($"Loaded {file.FileName}.");

        if (Context is not null)
        {
            await Context.RecordAsync("Opened file", file.FileName);
        }
    }

    private async Task GoToErrorAsync()
    {
        if (_error is not null && _editor is not null)
        {
            await _editor.RevealPositionAsync(_error.Line, _error.Column);
        }
    }

    private async Task ClearAsync()
    {
        SetContent(string.Empty);
        await SaveWorkspaceAsync();
    }

    private Task ToggleSortAsync() => UpdateWorkspaceAsync(workspace => workspace.SortProperties = !workspace.SortProperties);

    private Task ToggleDuplicatesAsync() => UpdateWorkspaceAsync(workspace => workspace.RemoveDuplicateProperties = !workspace.RemoveDuplicateProperties);

    private Task ToggleWordWrapAsync() => UpdateWorkspaceAsync(workspace => workspace.WordWrap = !workspace.WordWrap);

    private Task ToggleLineNumbersAsync() => UpdateWorkspaceAsync(workspace => workspace.ShowLineNumbers = !workspace.ShowLineNumbers);

    private Task ToggleFormatOnPasteAsync() => UpdateWorkspaceAsync(workspace => workspace.FormatOnPaste = !workspace.FormatOnPaste);

    private Task ToggleTreeAsync() => UpdateWorkspaceAsync(workspace => workspace.ShowTree = !workspace.ShowTree);

    private async Task UpdateWorkspaceAsync(Action<JsonWorkspace> change)
    {
        change(_workspace);
        await SaveWorkspaceAsync();
    }

    /// <summary>Auto-save is debounced so typing does not write to storage on every keystroke.</summary>
    private void ScheduleAutoSave()
    {
        _autoSaveTimer?.Dispose();
        _autoSaveTimer = new Timer(
            _ => _ = InvokeAsync(SaveWorkspaceAsync),
            null,
            TimeSpan.FromMilliseconds(AppConstants.DebounceMilliseconds * 2),
            Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    /// Persists options always, and the document only while it is small enough to be worth it.
    /// A multi-megabyte string would be JSON-escaped and written synchronously on every save, and
    /// would risk blowing the origin's ~5 MB localStorage quota for a single tool.
    /// </summary>
    private async Task SaveWorkspaceAsync()
    {
        _workspace.Content = _content.Length <= MaxPersistedContent ? _content : string.Empty;
        await Workspace.SaveAsync(ToolIds.Json, _workspace);
    }

    /// <summary>
    /// Projects one node without its children. Materialising the whole document would create a
    /// component per node — thousands of them for a large file, which stalls rendering; children
    /// are loaded by <see cref="LoadChildrenAsync"/> when a branch is actually opened.
    /// </summary>
    private static TreeItemData<JsonTreeNode> ToTreeItem(JsonTreeNode node, bool expanded = false) => new()
    {
        Value = node,
        Text = node.Name,
        Expanded = expanded,
        Expandable = node.HasChildren,
        Children = expanded ? node.Children.Select(child => ToTreeItem(child)).ToList() : null
    };

    /// <summary>Supplies a branch's children the first time it is expanded.</summary>
    private static Task<IReadOnlyCollection<TreeItemData<JsonTreeNode>>> LoadChildrenAsync(JsonTreeNode node)
        => Task.FromResult<IReadOnlyCollection<TreeItemData<JsonTreeNode>>>(
            node.Children.Select(child => ToTreeItem(child)).ToList());

    private static string GetNodeIcon(JsonTreeNode node) => node.Kind switch
    {
        JsonValueKind.Object => Icons.Material.Filled.DataObject,
        JsonValueKind.Array => Icons.Material.Filled.DataArray,
        JsonValueKind.String => Icons.Material.Filled.Abc,
        JsonValueKind.Number => Icons.Material.Filled.Numbers,
        JsonValueKind.True or JsonValueKind.False => Icons.Material.Filled.ToggleOn,
        JsonValueKind.Null => Icons.Material.Filled.Block,
        _ => Icons.Material.Filled.MoreHoriz
    };

    public async ValueTask DisposeAsync()
    {
        _validationCts?.Cancel();
        _validationCts?.Dispose();
        _autoSaveTimer?.Dispose();

        if (_shortcuts is not null)
        {
            await _shortcuts.DisposeAsync();
        }

        await SaveWorkspaceAsync();
        GC.SuppressFinalize(this);
    }
}
