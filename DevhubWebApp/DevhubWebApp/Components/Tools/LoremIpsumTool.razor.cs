using System.Globalization;
using DevhubWebApp.Constants;
using DevhubWebApp.Models.Tools.Lorem;
using DevhubWebApp.Utilities;
using MudBlazor;

namespace DevhubWebApp.Components.Tools;

/// <summary>
/// Lorem Ipsum workspace. Generation is <see cref="LoremGenerator"/>; this component owns the
/// option state and the rendered passage.
/// </summary>
public partial class LoremIpsumTool : ToolComponentBase<LoremWorkspace>
{
    private const string ShortcutGenerate = "ctrl+enter";
    private const string ShortcutDownload = "ctrl+s";

    private static readonly IReadOnlyList<LoremUnit> Units = Enum.GetValues<LoremUnit>();
    private static readonly IReadOnlyList<LoremFormat> Formats = Enum.GetValues<LoremFormat>();

    private string _output = string.Empty;

    protected override string ToolId => ToolIds.LoremIpsum;

    protected override IReadOnlyList<string> ShortcutCombos => [ShortcutGenerate, ShortcutDownload];

    private string UnitLabel => L(Workspace.Unit.ToString());

    /// <summary>Sensible upper bounds per unit: 500 words is useful, 500 paragraphs is not.</summary>
    private int MaxForUnit => Workspace.Unit switch
    {
        LoremUnit.Words => 500,
        LoremUnit.Sentences => 100,
        _ => 50
    };

    private string StatusLabel
    {
        get
        {
            if (string.IsNullOrEmpty(_output))
            {
                return string.Empty;
            }

            var (words, characters) = LoremGenerator.Measure(_output);
            return $"{words.ToString("N0", CultureInfo.InvariantCulture)} {L("words")} · {characters.ToString("N0", CultureInfo.InvariantCulture)} {L("characters")}";
        }
    }

    /// <summary>Generates on open so the tool never presents an empty preview on arrival.</summary>
    protected override Task OnWorkspaceLoadedAsync()
    {
        Generate();
        return Task.CompletedTask;
    }

    private void Generate() => _output = LoremGenerator.Generate(Workspace.ToOptions());

    private async Task GenerateAsync()
    {
        Generate();

        await SaveWorkspaceAsync();
        await RecordAsync("Generated placeholder text", $"{Workspace.Count} {Workspace.Unit}");
    }

    /// <summary>Every option change regenerates, so the preview always matches the settings.</summary>
    private async Task UpdateAsync(Action<LoremWorkspace> change)
    {
        change(Workspace);
        Workspace.Count = Math.Clamp(Workspace.Count, 1, MaxForUnit);

        Generate();
        await SaveWorkspaceAsync();
    }

    /// <summary>Turning the seed on captures the current time, so the first fixed run is arbitrary.</summary>
    private Task OnSeedToggledAsync(bool useSeed)
        => UpdateAsync(workspace => workspace.Seed = useSeed ? Environment.TickCount : null);

    private async Task DownloadAsync()
    {
        if (string.IsNullOrEmpty(_output))
        {
            return;
        }

        var (fileName, mimeType) = Workspace.Format switch
        {
            LoremFormat.Html => ("lorem.html", MimeTypes.Html),
            LoremFormat.Markdown => ("lorem.md", MimeTypes.Markdown),
            _ => ("lorem.txt", MimeTypes.PlainText)
        };

        await DownloadAsync(_output, fileName, mimeType);
        await RecordAsync("Exported placeholder text", fileName);
    }

    protected override async Task OnShortcutAsync(string combo)
    {
        switch (combo)
        {
            case ShortcutGenerate:
                await GenerateAsync();
                break;

            case ShortcutDownload:
                await DownloadAsync();
                break;

            default:
                return;
        }

        StateHasChanged();
    }

    private static string GetFormatLabel(LoremFormat format) => format switch
    {
        LoremFormat.PlainText => "Plain",
        LoremFormat.Html => "HTML",
        LoremFormat.Markdown => "Markdown",
        _ => format.ToString()
    };

    private static string GetUnitIcon(LoremUnit unit) => unit switch
    {
        LoremUnit.Words => Icons.Material.Filled.TextFields,
        LoremUnit.Sentences => Icons.Material.Filled.ShortText,
        _ => Icons.Material.Filled.Notes
    };
}
