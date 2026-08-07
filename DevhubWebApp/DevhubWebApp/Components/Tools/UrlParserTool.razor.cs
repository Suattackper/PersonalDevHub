using System.Text.Json;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Url;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// URL parser and query editor. Parsing and rebuilding are <see cref="UrlToolkit"/>; this
/// component owns the edited parameter list and keeps the URL field in sync with it.
/// </summary>
public partial class UrlParserTool : ToolComponentBase<UrlParserWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    /// <summary>Reused across exports; building the options per call is wasteful.</summary>
    private static readonly JsonSerializerOptions ExportOptions =
        new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly List<QueryParameter> _parameters = [];
    private UrlParts _parts = UrlParts.Empty;

    protected override string ToolId => ToolIds.UrlParser;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    /// <summary>Rows of the parts table, in the order a reader expects to scan them.</summary>
    private IReadOnlyList<(string Label, string Value)> Parts =>
    [
        ("Protocol", _parts.Protocol),
        ("Origin", _parts.Origin),
        ("Host", _parts.Host),
        ("Port", _parts.Port),
        ("Path", _parts.Path),
        ("Query", _parts.Query),
        ("Fragment", _parts.Fragment),
        ("User info", _parts.UserInfo)
    ];

    protected override Task OnWorkspaceLoadedAsync()
    {
        Reparse();
        return Task.CompletedTask;
    }

    private async Task OnUrlChangedAsync(string? value)
    {
        Workspace.Url = value ?? string.Empty;

        ScheduleAutoSave();
        await DebounceAsync(Reparse);
    }

    /// <summary>Re-reads the URL field into the parts table and the editable parameter list.</summary>
    private void Reparse()
    {
        _parts = UrlToolkit.Parse(Workspace.Url);

        _parameters.Clear();
        _parameters.AddRange(_parts.Parameters);
    }

    /// <summary>
    /// Rebuilds the URL from the edited parameters. The URL field stays the single source of
    /// truth: everything the user edits flows back into it immediately.
    /// </summary>
    private async Task ApplyParametersAsync()
    {
        if (!_parts.IsValid)
        {
            return;
        }

        Workspace.Url = UrlToolkit.BuildUrl(_parts, _parameters);
        _parts = UrlToolkit.Parse(Workspace.Url);

        await SaveWorkspaceAsync();
    }

    private Task UpdateParameterAsync(int index, string? name, string? value)
    {
        if (index < 0 || index >= _parameters.Count)
        {
            return Task.CompletedTask;
        }

        _parameters[index] = new QueryParameter
        {
            Name = name ?? string.Empty,
            Value = value ?? string.Empty
        };

        return ApplyParametersAsync();
    }

    private Task AddAsync()
    {
        _parameters.Add(new QueryParameter { Name = $"param{_parameters.Count + 1}" });
        return ApplyParametersAsync();
    }

    private Task DuplicateAsync(int index)
    {
        if (index < 0 || index >= _parameters.Count)
        {
            return Task.CompletedTask;
        }

        _parameters.Insert(index + 1, _parameters[index]);
        return ApplyParametersAsync();
    }

    private Task RemoveAsync(int index)
    {
        if (index < 0 || index >= _parameters.Count)
        {
            return Task.CompletedTask;
        }

        _parameters.RemoveAt(index);
        return ApplyParametersAsync();
    }

    private Task SortAsync()
    {
        var sorted = UrlToolkit.SortParameters(_parameters);

        _parameters.Clear();
        _parameters.AddRange(sorted);

        return ApplyParametersAsync();
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        Workspace.Url = text.Trim();
        Reparse();
        await SaveWorkspaceAsync();
    }

    private async Task ClearAsync()
    {
        Workspace.Url = string.Empty;
        Reparse();
        await SaveWorkspaceAsync();
    }

    /// <summary>Exports the parsed parts, which is the form most useful to paste into a test or a ticket.</summary>
    private async Task DownloadPartsAsync()
    {
        if (!_parts.IsValid)
        {
            return;
        }

        var payload = JsonSerializer.Serialize(_parts, ExportOptions);

        await DownloadAsync(payload, "url-parts.json", MimeTypes.Json);
        await RecordAsync("Parsed URL", Workspace.Url);
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadPartsAsync();
            StateHasChanged();
        }
    }
}
