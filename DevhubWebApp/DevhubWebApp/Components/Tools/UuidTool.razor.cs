using System.Globalization;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Uuid;
using DevhubWebApp.Utilities;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// UUID workspace. Generation and export are <see cref="UuidGenerator"/>; this component owns the
/// current batch, the view mode and the searchable local history.
/// </summary>
public partial class UuidTool : ToolComponentBase<UuidWorkspace>
{
    private const string ShortcutGenerate = "ctrl+enter";
    private const string ShortcutCopy = "ctrl+c";

    /// <summary>How many identifiers the history keeps before dropping the oldest.</summary>
    private const int HistoryCapacity = 200;

    private static readonly IReadOnlyList<UuidFormat> Formats = Enum.GetValues<UuidFormat>();

    /// <summary>Concrete list so the virtualised table can bind to it directly.</summary>
    private List<string> _identifiers = [];
    private string _joined = string.Empty;
    private string _historySearch = string.Empty;

    /// <summary>Export shapes offered by the download menu.</summary>
    private enum ExportKind
    {
        Text,
        Csv,
        Json
    }

    protected override string ToolId => ToolIds.Uuid;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutGenerate, ShortcutCopy];

    private bool HasResults => _identifiers.Count > 0;

    private IReadOnlyList<string> FilteredHistory => string.IsNullOrWhiteSpace(_historySearch)
        ? Workspace.History
        : Workspace.History
            .Where(identifier => identifier.Contains(_historySearch, StringComparison.OrdinalIgnoreCase))
            .ToList();

    private async Task GenerateAsync()
    {
        _identifiers = UuidGenerator.Generate(Workspace.Quantity, Workspace.Format).ToList();
        _joined = UuidGenerator.ToText(_identifiers);

        RememberBatch();
        await SaveWorkspaceAsync();

        if (Workspace.AutoCopy)
        {
            await CopyAsync(_joined, $"{_identifiers.Count} identifier(s) copied.");
        }

        await RecordAsync("Generated UUIDs", $"{_identifiers.Count} × {UuidGenerator.GetName(Workspace.Format)}");
    }

    /// <summary>Keeps the newest identifiers, dropping the oldest once the cap is reached.</summary>
    private void RememberBatch()
    {
        Workspace.History.InsertRange(0, _identifiers);

        if (Workspace.History.Count > HistoryCapacity)
        {
            Workspace.History.RemoveRange(HistoryCapacity, Workspace.History.Count - HistoryCapacity);
        }
    }

    private Task OnQuantityChangedAsync(int value)
        => UpdateAsync(workspace => workspace.Quantity = Math.Clamp(value, 1, UuidGenerator.MaxBatchSize));

    private Task OnFormatChangedAsync(UuidFormat format) => UpdateAsync(workspace => workspace.Format = format);

    private async Task UpdateAsync(Action<UuidWorkspace> change)
    {
        change(Workspace);
        await SaveWorkspaceAsync();
    }

    private async Task ClearHistoryAsync()
    {
        Workspace.History.Clear();
        await SaveWorkspaceAsync();
    }

    private async Task DownloadAsync(ExportKind kind)
    {
        if (!HasResults)
        {
            return;
        }

        var (content, fileName, mimeType) = kind switch
        {
            ExportKind.Csv => (UuidGenerator.ToCsv(_identifiers), "uuids.csv", MimeTypes.Csv),
            ExportKind.Json => (UuidGenerator.ToJson(_identifiers), "uuids.json", MimeTypes.Json),
            _ => (UuidGenerator.ToText(_identifiers), "uuids.txt", MimeTypes.PlainText)
        };

        await DownloadAsync(content, fileName, mimeType);
        await RecordAsync("Exported UUIDs", $"{_identifiers.Count.ToString(CultureInfo.InvariantCulture)} → {fileName}");
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        switch (combo)
        {
            case ShortcutGenerate:
                await GenerateAsync();
                break;

            case ShortcutCopy when HasResults:
                await CopyAsync(_joined, $"{_identifiers.Count} identifier(s) copied.");
                break;

            default:
                return;
        }

        StateHasChanged();
    }
}
