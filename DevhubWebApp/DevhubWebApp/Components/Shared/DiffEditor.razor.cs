using BlazorMonaco.Editor;
using DevhubWebApp.Interfaces;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Shared;

/// <summary>
/// Behaviour for <c>DiffEditor.razor</c>: a reusable facade over Monaco's diff view.
/// <para>
/// Both sides flow one way in. Models are created once and their text is replaced in place, so
/// switching options never discards the user's scroll position or selection.
/// </para>
/// </summary>
public partial class DiffEditor : IAsyncDisposable
{
    private readonly string _elementId = $"devhub-diff-{Guid.NewGuid():N}";

    private StandaloneDiffEditor? _editor;
    private TextModel? _originalModel;
    private TextModel? _modifiedModel;
    private string _lastOriginal = string.Empty;
    private string _lastModified = string.Empty;
    private bool _isReady;

    /// <summary>Left-hand text.</summary>
    [Parameter] public string Original { get; set; } = string.Empty;

    /// <summary>Right-hand text.</summary>
    [Parameter] public string Modified { get; set; } = string.Empty;

    /// <summary>Monaco language id used for highlighting both sides.</summary>
    [Parameter] public string Language { get; set; } = "plaintext";

    /// <summary><c>true</c> renders the two documents side by side, <c>false</c> inline.</summary>
    [Parameter] public bool SideBySide { get; set; } = true;

    /// <summary>Hides whitespace-only differences in the rendered diff.</summary>
    [Parameter] public bool IgnoreTrimWhitespace { get; set; }

    /// <summary>Wraps long lines instead of scrolling horizontally.</summary>
    [Parameter] public bool WordWrap { get; set; }

    /// <summary>Shows the line-number gutters.</summary>
    [Parameter] public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Allows editing the right-hand side.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Extra CSS classes appended to the container.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IThemeProvider ThemeProvider { get; set; } = default!;

    [Inject] private Microsoft.JSInterop.IJSRuntime JsRuntime { get; set; } = default!;

    private string MonacoTheme => ThemeProvider.IsDarkMode ? "vs-dark" : "vs";

    protected override void OnInitialized() => ThemeProvider.Changed += OnThemeChanged;

    protected override async Task OnParametersSetAsync()
    {
        if (!_isReady)
        {
            return;
        }

        await SyncModelsAsync();
        await ApplyOptionsAsync();
    }

    private StandaloneDiffEditorConstructionOptions BuildOptions(StandaloneDiffEditor editor) => new()
    {
        Theme = MonacoTheme,
        AutomaticLayout = true,
        RenderSideBySide = SideBySide,
        IgnoreTrimWhitespace = IgnoreTrimWhitespace,
        ReadOnly = ReadOnly,
        OriginalEditable = false,
        LineNumbers = ShowLineNumbers ? "on" : "off",
        WordWrap = WordWrap ? "on" : "off",
        ScrollBeyondLastLine = false,
        FontSize = 13,
        EnableSplitViewResizing = true
    };

    private async Task OnEditorInitAsync()
    {
        _originalModel = await Global.CreateModel(JsRuntime, Original, Language);
        _modifiedModel = await Global.CreateModel(JsRuntime, Modified, Language);

        await _editor!.SetModel(new DiffEditorModel { Original = _originalModel, Modified = _modifiedModel });

        _lastOriginal = Original;
        _lastModified = Modified;
        _isReady = true;
    }

    /// <summary>Pushes changed text into the existing models rather than recreating them.</summary>
    private async Task SyncModelsAsync()
    {
        if (_originalModel is not null && !string.Equals(Original, _lastOriginal, StringComparison.Ordinal))
        {
            _lastOriginal = Original;
            await _originalModel.SetValue(Original);
        }

        if (_modifiedModel is not null && !string.Equals(Modified, _lastModified, StringComparison.Ordinal))
        {
            _lastModified = Modified;
            await _modifiedModel.SetValue(Modified);
        }
    }

    private async Task ApplyOptionsAsync()
    {
        if (_editor is null)
        {
            return;
        }

        // The diff editor has no per-instance theme option; Monaco's theme is global.
        await Global.SetTheme(JsRuntime, MonacoTheme);

        await _editor.UpdateOptions(new DiffEditorOptions
        {
            RenderSideBySide = SideBySide,
            IgnoreTrimWhitespace = IgnoreTrimWhitespace,
            ReadOnly = ReadOnly,
            LineNumbers = ShowLineNumbers ? "on" : "off",
            WordWrap = WordWrap ? "on" : "off"
        });
    }

    /// <summary>Opens Monaco's find widget on the modified side.</summary>
    public async Task FocusSearchAsync()
    {
        if (_editor is null)
        {
            return;
        }

        await _editor.ModifiedEditor.Trigger("devhub", "actions.find");
    }

    private void OnThemeChanged() => _ = InvokeAsync(async () =>
    {
        await ApplyOptionsAsync();
        StateHasChanged();
    });

    public async ValueTask DisposeAsync()
    {
        ThemeProvider.Changed -= OnThemeChanged;

        // Diff models are created explicitly, so they must be released explicitly.
        foreach (var model in new[] { _originalModel, _modifiedModel })
        {
            if (model is null)
            {
                continue;
            }

            try
            {
                await model.DisposeModel();
            }
            catch (Microsoft.JSInterop.JSDisconnectedException)
            {
                // Page is unloading; Monaco is already gone.
            }
        }

        GC.SuppressFinalize(this);
    }
}
