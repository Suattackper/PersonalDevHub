using BlazorMonaco.Editor;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;
using MudBlazor.Utilities;

namespace DevhubWebApp.Components.Shared;

/// <summary>
/// Behaviour for <c>CodeEditor.razor</c>: a thin, reusable facade over Monaco.
/// <para>
/// Content flows one way in (<see cref="Value"/> is pushed to the model only when it differs from
/// what the editor already holds, so the caret is never reset while typing) and one way out
/// (<see cref="ValueChanged"/>). Markers are declarative: the caller hands over the current
/// diagnostics and the component reconciles them.
/// </para>
/// </summary>
public partial class CodeEditor : IAsyncDisposable
{
    /// <summary>Marker owner id. Monaco replaces the whole set belonging to one owner at a time.</summary>
    private const string MarkerOwner = "devhub";

    private readonly string _elementId = $"devhub-editor-{Guid.NewGuid():N}";

    private StandaloneCodeEditor? _editor;
    private string _lastKnownValue = string.Empty;
    private bool _isReady;

    /// <summary>
    /// Set while a value we pushed ourselves is echoing back as a model-change event. Reading the
    /// document back over JS interop would marshal the whole text a second time — megabytes, for a
    /// value we already hold.
    /// </summary>
    private bool _expectingEcho;

    /// <summary>Editor content.</summary>
    [Parameter] public string Value { get; set; } = string.Empty;

    /// <summary>Raised on every model change, debounced by the caller if needed.</summary>
    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    /// <summary>Monaco language id, e.g. <c>json</c>, <c>sql</c>, <c>markdown</c>.</summary>
    [Parameter] public string Language { get; set; } = "plaintext";

    /// <summary>Disables editing while keeping syntax highlighting.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Shows the line-number gutter.</summary>
    [Parameter] public bool ShowLineNumbers { get; set; } = true;

    /// <summary>Wraps long lines instead of scrolling horizontally.</summary>
    [Parameter] public bool WordWrap { get; set; }

    /// <summary>Shows the minimap. Off by default: it costs rendering time on large documents.</summary>
    [Parameter] public bool ShowMinimap { get; set; }

    /// <summary>Diagnostics rendered as squiggles in the gutter and the text.</summary>
    [Parameter] public IReadOnlyList<EditorMarker> Markers { get; set; } = Array.Empty<EditorMarker>();

    /// <summary>Raised after the user pastes, so tools can offer "format on paste".</summary>
    [Parameter] public EventCallback OnPaste { get; set; }

    /// <summary>Raised once the editor is ready and its API can be called.</summary>
    [Parameter] public EventCallback OnReady { get; set; }

    /// <summary>Extra CSS classes appended to the container.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IThemeProvider ThemeProvider { get; set; } = default!;

    /// <summary>Required by Monaco's static marker API, which is not exposed on the editor instance.</summary>
    [Inject] private Microsoft.JSInterop.IJSRuntime JsRuntime { get; set; } = default!;

    private string CssClass => new CssBuilder("devhub-editor").AddClass(Class).Build();

    private string MonacoTheme => ThemeProvider.IsDarkMode ? "vs-dark" : "vs";

    protected override void OnInitialized() => ThemeProvider.Changed += OnThemeChanged;

    /// <summary>
    /// Pushes external changes (formatting, undo, file upload) into the model. Comparing against
    /// the last known value first is what prevents a feedback loop with <see cref="ValueChanged"/>.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (!_isReady || _editor is null)
        {
            return;
        }

        if (!string.Equals(Value, _lastKnownValue, StringComparison.Ordinal))
        {
            _lastKnownValue = Value;
            _expectingEcho = true;
            await _editor.SetValue(Value);
        }

        await ApplyOptionsAsync();
        await ApplyMarkersAsync();
    }

    private StandaloneEditorConstructionOptions BuildOptions(StandaloneCodeEditor editor) => new()
    {
        Value = Value,
        Language = Language,
        Theme = MonacoTheme,
        ReadOnly = ReadOnly,
        AutomaticLayout = true,
        LineNumbers = ShowLineNumbers ? "on" : "off",
        WordWrap = WordWrap ? "on" : "off",
        Minimap = new EditorMinimapOptions { Enabled = ShowMinimap },
        ScrollBeyondLastLine = false,
        RenderLineHighlight = "line",
        FontSize = 13,
        TabSize = 2,
        FormatOnPaste = false,
        Scrollbar = new EditorScrollbarOptions { UseShadows = false }
    };

    private async Task OnEditorInitAsync()
    {
        _isReady = true;

        // The construction options captured whatever Value held on the first render. When the
        // caller loads its content asynchronously — a restored workspace, an uploaded file — the
        // model is still empty by the time Monaco reports ready, so it is pushed once here.
        if (!string.IsNullOrEmpty(Value) && !string.Equals(Value, await _editor!.GetValue(), StringComparison.Ordinal))
        {
            _expectingEcho = true;
            await _editor.SetValue(Value);
        }

        _lastKnownValue = Value;

        await ApplyOptionsAsync();
        await ApplyMarkersAsync();
        await OnReady.InvokeAsync();
    }

    private async Task OnContentChangedAsync(ModelContentChangedEvent args)
    {
        if (_editor is null)
        {
            return;
        }

        if (_expectingEcho)
        {
            _expectingEcho = false;
            return;
        }

        _lastKnownValue = await _editor.GetValue();
        await ValueChanged.InvokeAsync(_lastKnownValue);
    }

    private Task OnPasteAsync(PasteEvent args) => OnPaste.InvokeAsync();

    private async Task ApplyOptionsAsync()
    {
        if (_editor is null)
        {
            return;
        }

        await _editor.UpdateOptions(new EditorUpdateOptions
        {
            Theme = MonacoTheme,
            ReadOnly = ReadOnly,
            LineNumbers = ShowLineNumbers ? "on" : "off",
            WordWrap = WordWrap ? "on" : "off",
            Minimap = new EditorMinimapOptions { Enabled = ShowMinimap }
        });
    }

    /// <summary>Replaces the diagnostic set; an empty list clears the squiggles.</summary>
    private async Task ApplyMarkersAsync()
    {
        if (_editor is null)
        {
            return;
        }

        var model = await _editor.GetModel();
        if (model is null)
        {
            return;
        }

        var markers = Markers.Select(marker => new MarkerData
        {
            Message = marker.Message,
            Severity = BlazorMonaco.MarkerSeverity.Error,
            StartLineNumber = marker.Line,
            StartColumn = marker.Column,
            EndLineNumber = marker.Line,
            EndColumn = marker.Column + Math.Max(marker.Length, 1)
        }).ToList();

        await BlazorMonaco.Editor.Global.SetModelMarkers(JsRuntime, model, MarkerOwner, markers);
    }

    /// <summary>Text currently selected, or an empty string when the selection is collapsed.</summary>
    public async Task<string> GetSelectedTextAsync()
    {
        if (_editor is null)
        {
            return string.Empty;
        }

        var selection = await _editor.GetSelection();
        var model = await _editor.GetModel();

        if (selection is null || model is null)
        {
            return string.Empty;
        }

        return await model.GetValueInRange(new BlazorMonaco.Range
        {
            StartLineNumber = selection.StartLineNumber,
            StartColumn = selection.StartColumn,
            EndLineNumber = selection.EndLineNumber,
            EndColumn = selection.EndColumn
        }, EndOfLinePreference.TextDefined);
    }

    /// <summary><c>true</c> when the caret sits at the first column of its line.</summary>
    public async Task<bool> IsAtLineStartAsync()
    {
        if (_editor is null)
        {
            return true;
        }

        var selection = await _editor.GetSelection();
        return selection is null || selection.StartColumn == 1;
    }

    /// <summary>
    /// Replaces the current selection, keeping the change in Monaco's own undo stack so Ctrl+Z
    /// behaves exactly as it does for typed text.
    /// </summary>
    public async Task ReplaceSelectionAsync(string text)
    {
        if (_editor is null)
        {
            return;
        }

        var selection = await _editor.GetSelection();
        if (selection is null)
        {
            return;
        }

        await _editor.ExecuteEdits(MarkerOwner,
        [
            new IdentifiedSingleEditOperation
            {
                Range = new BlazorMonaco.Range
                {
                    StartLineNumber = selection.StartLineNumber,
                    StartColumn = selection.StartColumn,
                    EndLineNumber = selection.EndLineNumber,
                    EndColumn = selection.EndColumn
                },
                Text = text,
                ForceMoveMarkers = true
            }
        ], (List<BlazorMonaco.Selection>?)null);

        await _editor.Focus();
    }

    /// <summary>Scrolls to a position and places the caret there, used when jumping to an error.</summary>
    public async Task RevealPositionAsync(int line, int column)
    {
        if (_editor is null)
        {
            return;
        }

        await _editor.RevealLineInCenter(line);
        await _editor.SetPosition(new BlazorMonaco.Position { LineNumber = line, Column = column }, MarkerOwner);
        await _editor.Focus();
    }

    private void OnThemeChanged() => _ = InvokeAsync(async () =>
    {
        await ApplyOptionsAsync();
        StateHasChanged();
    });

    public ValueTask DisposeAsync()
    {
        ThemeProvider.Changed -= OnThemeChanged;
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
