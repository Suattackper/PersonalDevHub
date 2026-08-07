using DevhubWebApp.Components.Shared;
using DevhubWebApp.Constants;
using DevhubWebApp.Extensions;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools.Markdown;
using DevhubWebApp.Utilities;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Markdown workspace. Rendering, analysis and export are services; this component owns only the
/// document text, the preview state and the toolbar wiring.
/// </summary>
public partial class MarkdownTool : ToolComponentBase<MarkdownWorkspace>
{
    private const string ShortcutBold = "ctrl+b";
    private const string ShortcutItalic = "ctrl+i";
    private const string ShortcutLink = "ctrl+k";
    private const string ShortcutSave = "ctrl+s";
    private const string ShortcutPreview = "ctrl+shift+p";

    /// <summary>Toolbar layout: each inner group is separated by a divider.</summary>
    private static readonly IReadOnlyList<IReadOnlyList<MarkdownAction>> FormatGroups =
    [
        [MarkdownAction.Bold, MarkdownAction.Italic, MarkdownAction.Strikethrough, MarkdownAction.InlineCode],
        [MarkdownAction.Heading1, MarkdownAction.Heading2, MarkdownAction.Heading3],
        [MarkdownAction.Link, MarkdownAction.Image, MarkdownAction.Table, MarkdownAction.CodeBlock],
        [MarkdownAction.Quote, MarkdownAction.BulletList, MarkdownAction.NumberedList, MarkdownAction.TaskList]
    ];

    private CodeEditor? _editor;
    private MarkdownStatistics _statistics = MarkdownStatistics.Empty;
    private string _content = string.Empty;
    private string _previewHtml = string.Empty;
    private bool _isRendering;
    private bool _isFullscreen;

    [Inject] private IMarkdownService Markdown { get; set; } = default!;

    [Inject] private IHtmlExportService HtmlExport { get; set; } = default!;

    protected override string ToolId => ToolIds.Markdown;

    protected override IReadOnlyList<string> ShortcutCombos =>
        [ShortcutBold, ShortcutItalic, ShortcutLink, ShortcutSave, ShortcutPreview];

    private bool IsEmpty => string.IsNullOrWhiteSpace(_content);

    private string DocumentTitle => Workspace.Title.OrDefault(FirstHeading().OrDefault("document"));

    protected override Task OnWorkspaceLoadedAsync()
    {
        _content = Workspace.Content;
        History.Reset(_content);
        Render();

        return Task.CompletedTask;
    }

    /// <summary>Typing only schedules a render; the preview never rebuilds on every keystroke.</summary>
    private async Task OnContentChangedAsync(string value)
    {
        _content = value;
        _isRendering = true;

        ScheduleAutoSave();
        await DebounceAsync(() =>
        {
            History.Push(_content);
            Render();
        });
    }

    private void Render()
    {
        _previewHtml = Markdown.ToHtml(_content);
        _statistics = Markdown.Analyse(_content);
        Workspace.Content = _content;
        _isRendering = false;
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        switch (combo)
        {
            case ShortcutBold:
                await ApplyFormatAsync(MarkdownAction.Bold);
                break;

            case ShortcutItalic:
                await ApplyFormatAsync(MarkdownAction.Italic);
                break;

            case ShortcutLink:
                await ApplyFormatAsync(MarkdownAction.Link);
                break;

            case ShortcutSave:
                await DownloadMarkdownAsync();
                break;

            case ShortcutPreview:
                await TogglePreviewAsync();
                break;

            default:
                return;
        }

        StateHasChanged();
    }

    /// <summary>Applies a toolbar action to the current selection.</summary>
    private async Task ApplyFormatAsync(MarkdownAction action)
    {
        if (_editor is null)
        {
            return;
        }

        var selection = await _editor.GetSelectedTextAsync();
        var atLineStart = await _editor.IsAtLineStartAsync();

        await _editor.ReplaceSelectionAsync(MarkdownSyntax.Build(action, selection, atLineStart));
    }

    private void Undo()
    {
        if (History.Undo() is { } previous)
        {
            _content = previous;
            Render();
        }
    }

    private void Redo()
    {
        if (History.Redo() is { } next)
        {
            _content = next;
            Render();
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
        await SaveWorkspaceAsync();
    }

    private async Task OnFileLoadedAsync(FileDropResult file)
    {
        SetContent(file.Text ?? string.Empty);
        Workspace.Title = Path.GetFileNameWithoutExtension(file.FileName);
        RememberRecentFile(file.FileName);

        await SaveWorkspaceAsync();
        Notifications.Success($"Loaded {file.FileName}.");
        await RecordAsync("Opened file", file.FileName);
    }

    /// <summary>Keeps the most recent file names, newest first and without duplicates.</summary>
    private void RememberRecentFile(string fileName)
    {
        Workspace.RecentFiles.RemoveAll(name => string.Equals(name, fileName, StringComparison.OrdinalIgnoreCase));
        Workspace.RecentFiles.Insert(0, fileName);

        if (Workspace.RecentFiles.Count > AppConstants.RecentFileCapacity)
        {
            Workspace.RecentFiles.RemoveRange(
                AppConstants.RecentFileCapacity,
                Workspace.RecentFiles.Count - AppConstants.RecentFileCapacity);
        }
    }

    private void SetContent(string value)
    {
        _content = value;
        History.Push(value);
        Render();
    }

    private Task CopyMarkdownAsync() => CopyAsync(_content, "Markdown copied.");

    private async Task CopyHtmlAsync()
    {
        await CopyAsync(_previewHtml, "HTML copied.");
        await RecordAsync("Copied HTML", _previewHtml);
    }

    private async Task DownloadMarkdownAsync()
    {
        var fileName = HtmlExport.ToFileName(DocumentTitle, "md");

        await DownloadAsync(_content, fileName, MimeTypes.Markdown);
        await RecordAsync("Downloaded Markdown", fileName);
    }

    private async Task ExportHtmlAsync()
    {
        if (IsEmpty)
        {
            return;
        }

        var fileName = HtmlExport.ToFileName(DocumentTitle, "html");
        var document = HtmlExport.BuildDocument(DocumentTitle, _previewHtml);

        await DownloadAsync(document, fileName, MimeTypes.Html);
        await RecordAsync("Exported HTML", fileName);
    }

    private async Task ClearAsync()
    {
        SetContent(string.Empty);
        await SaveWorkspaceAsync();
    }

    private Task TogglePreviewAsync() => UpdateWorkspaceAsync(workspace => workspace.ShowPreview = !workspace.ShowPreview);

    private Task ToggleWordWrapAsync() => UpdateWorkspaceAsync(workspace => workspace.WordWrap = !workspace.WordWrap);

    private Task ToggleLineNumbersAsync() => UpdateWorkspaceAsync(workspace => workspace.ShowLineNumbers = !workspace.ShowLineNumbers);

    private void ToggleFullscreen() => _isFullscreen = !_isFullscreen;

    private async Task UpdateWorkspaceAsync(Action<MarkdownWorkspace> change)
    {
        change(Workspace);
        await SaveWorkspaceAsync();
    }

    /// <summary>Falls back to the document's first ATX heading when no title was set.</summary>
    private string FirstHeading()
    {
        foreach (var line in _content.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith('#'))
            {
                return trimmed.TrimStart('#').Trim();
            }
        }

        return string.Empty;
    }

    private static string GetActionIcon(MarkdownAction action) => action switch
    {
        MarkdownAction.Bold => Icons.Material.Filled.FormatBold,
        MarkdownAction.Italic => Icons.Material.Filled.FormatItalic,
        MarkdownAction.Strikethrough => Icons.Material.Filled.StrikethroughS,
        MarkdownAction.InlineCode => Icons.Material.Filled.Code,
        MarkdownAction.Heading1 => Icons.Material.Filled.LooksOne,
        MarkdownAction.Heading2 => Icons.Material.Filled.LooksTwo,
        MarkdownAction.Heading3 => Icons.Material.Filled.Looks3,
        MarkdownAction.Link => Icons.Material.Filled.Link,
        MarkdownAction.Image => Icons.Material.Filled.Image,
        MarkdownAction.Quote => Icons.Material.Filled.FormatQuote,
        MarkdownAction.BulletList => Icons.Material.Filled.FormatListBulleted,
        MarkdownAction.NumberedList => Icons.Material.Filled.FormatListNumbered,
        MarkdownAction.TaskList => Icons.Material.Filled.Checklist,
        MarkdownAction.CodeBlock => Icons.Material.Filled.DataObject,
        MarkdownAction.Table => Icons.Material.Filled.TableChart,
        MarkdownAction.HorizontalRule => Icons.Material.Filled.HorizontalRule,
        _ => Icons.Material.Filled.TextFields
    };

    private static string GetActionTooltip(MarkdownAction action) => action switch
    {
        MarkdownAction.Bold => "Bold (Ctrl+B)",
        MarkdownAction.Italic => "Italic (Ctrl+I)",
        MarkdownAction.Strikethrough => "Strikethrough",
        MarkdownAction.InlineCode => "Inline code",
        MarkdownAction.Heading1 => "Heading 1",
        MarkdownAction.Heading2 => "Heading 2",
        MarkdownAction.Heading3 => "Heading 3",
        MarkdownAction.Link => "Link (Ctrl+K)",
        MarkdownAction.Image => "Image",
        MarkdownAction.Quote => "Quote",
        MarkdownAction.BulletList => "Bullet list",
        MarkdownAction.NumberedList => "Numbered list",
        MarkdownAction.TaskList => "Task list",
        MarkdownAction.CodeBlock => "Code block",
        MarkdownAction.Table => "Table",
        MarkdownAction.HorizontalRule => "Horizontal rule",
        _ => action.ToString()
    };
}
