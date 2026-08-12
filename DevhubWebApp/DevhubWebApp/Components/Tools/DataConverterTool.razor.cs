using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools.Data;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Data converter workspace. Conversion, formatting and validation are
/// <see cref="IDataConversionService"/>; this component owns the document and the format choices.
/// </summary>
public partial class DataConverterTool : ToolComponentBase<DataWorkspace>
{
    private const string ShortcutDownload = "ctrl+s";

    private static readonly IReadOnlyList<DataFormat> Formats = Enum.GetValues<DataFormat>();

    private DataResult _result = DataResult.Empty;
    private string _output = string.Empty;

    [Inject] private IDataConversionService Converter { get; set; } = default!;

    protected override string ToolId => ToolIds.DataConverter;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutDownload];

    private bool IsEmpty => string.IsNullOrWhiteSpace(Workspace.Input);

    private bool HasOutput => _output.Length > 0;

    private string ErrorDescription => _result.ErrorLine is null
        ? _result.Error ?? string.Empty
        : $"Line {_result.ErrorLine}: {_result.Error}";

    protected override Task OnWorkspaceLoadedAsync()
    {
        Convert();
        return Task.CompletedTask;
    }

    private async Task OnInputChangedAsync(string value)
    {
        Workspace.Input = value;

        ScheduleAutoSave();
        await DebounceAsync(Convert);
    }

    /// <summary>
    /// Detects the source format when asked, then converts. Detection runs before conversion so a
    /// paste of XML into a JSON-configured tool simply works.
    /// </summary>
    private void Convert()
    {
        if (IsEmpty)
        {
            _result = DataResult.Empty;
            _output = string.Empty;
            return;
        }

        if (Workspace.AutoDetect)
        {
            Workspace.From = Converter.Detect(Workspace.Input, Workspace.From);
        }

        _result = Converter.Convert(Workspace.Input, Workspace.From, Workspace.To, Workspace.Indented);
        _output = _result.IsSuccess ? _result.Output : string.Empty;
    }

    private Task OnFromChangedAsync(DataFormat format) => UpdateAsync(workspace =>
    {
        workspace.From = format;

        // Choosing a source format by hand is an explicit override of detection.
        workspace.AutoDetect = false;
    });

    private Task OnToChangedAsync(DataFormat format) => UpdateAsync(workspace => workspace.To = format);

    private Task SwapAsync() => UpdateAsync(workspace =>
    {
        (workspace.From, workspace.To) = (workspace.To, workspace.From);
        workspace.AutoDetect = false;

        // Swapping is only meaningful if the current result becomes the new input.
        if (_output.Length > 0)
        {
            workspace.Input = _output;
        }
    });

    private async Task UpdateAsync(Action<DataWorkspace> change)
    {
        change(Workspace);

        Convert();
        await SaveWorkspaceAsync();
    }

    private async Task PasteAsync()
    {
        var text = await Interop.ReadClipboardAsync();

        if (string.IsNullOrEmpty(text))
        {
            Notifications.Warning("Clipboard is empty or not readable in this browser.");
            return;
        }

        await UpdateAsync(workspace => workspace.Input = text);
    }

    private async Task OnFileLoadedAsync(FileDropResult file)
    {
        Workspace.Input = file.Text ?? string.Empty;
        Workspace.From = Converter.Detect(Workspace.Input, Workspace.From);

        Convert();
        await SaveWorkspaceAsync();

        Notifications.Success($"Loaded {file.FileName}.");
        await RecordAsync("Opened data file", file.FileName);
    }

    private Task ClearAsync() => UpdateAsync(workspace => workspace.Input = string.Empty);

    private async Task DownloadAsync()
    {
        if (!HasOutput)
        {
            return;
        }

        var fileName = $"converted.{Converter.GetExtension(Workspace.To)}";

        await DownloadAsync(_output, fileName, Converter.GetMimeType(Workspace.To));
        await RecordAsync("Converted data", $"{Workspace.From} -> {Workspace.To}");
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        if (combo == ShortcutDownload)
        {
            await DownloadAsync();
            StateHasChanged();
        }
    }

    /// <summary>Monaco language id for a format, so both panes are highlighted correctly.</summary>
    private static string GetLanguage(DataFormat format) => format switch
    {
        DataFormat.Yaml => "yaml",
        DataFormat.Xml => "xml",
        _ => "json"
    };
}
