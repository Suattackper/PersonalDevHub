using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;
using MudBlazor.Utilities;

namespace DevhubWebApp.Components.Shared;

/// <summary>
/// Behaviour for <c>FileDropZone.razor</c>. Enforces the size limit, reads the payload once and
/// surfaces failures as notifications rather than exceptions.
/// </summary>
public partial class FileDropZone
{
    private bool _isDragging;
    private bool _isBusy;
    private string? _fileName;

    /// <summary>Headline shown inside the drop area.</summary>
    [Parameter] public string Title { get; set; } = "Drop a file here";

    /// <summary>Secondary hint, e.g. the accepted formats.</summary>
    [Parameter] public string Hint { get; set; } = "or click to browse";

    /// <summary>Icon shown in the idle state.</summary>
    [Parameter] public string Icon { get; set; } = Icons.Material.Outlined.CloudUpload;

    /// <summary>HTML <c>accept</c> filter, e.g. <c>.json,application/json</c>.</summary>
    [Parameter] public string? Accept { get; set; }

    /// <summary>Maximum accepted size in bytes. Larger files are rejected with a notification.</summary>
    [Parameter] public long MaxFileSize { get; set; } = 5 * 1024 * 1024;

    /// <summary>Reads the file as bytes instead of UTF-8 text.</summary>
    [Parameter] public bool ReadAsBytes { get; set; }

    /// <summary>Compact variant for use inside a toolbar or a card.</summary>
    [Parameter] public bool Dense { get; set; }

    /// <summary>Raised with the file content once it has been read.</summary>
    [Parameter] public EventCallback<FileDropResult> OnFileLoaded { get; set; }

    /// <summary>Extra CSS classes appended to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private INotificationService Notifications { get; set; } = default!;

    private string CssClass => new CssBuilder("devhub-dropzone")
        .AddClass("devhub-dropzone--active", _isDragging)
        .AddClass("devhub-dropzone--dense", Dense)
        .AddClass(Class)
        .Build();

    private void OnDragEnter() => _isDragging = true;

    private void OnDragLeave() => _isDragging = false;

    private async Task OnFileSelectedAsync(InputFileChangeEventArgs args)
    {
        _isDragging = false;
        var file = args.File;

        if (file.Size > MaxFileSize)
        {
            Notifications.Error($"“{file.Name}” exceeds the {MaxFileSize / 1024 / 1024} MB limit.");
            return;
        }

        _isBusy = true;
        _fileName = file.Name;

        try
        {
            await using var stream = file.OpenReadStream(MaxFileSize);
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);

            var bytes = buffer.ToArray();
            var result = new FileDropResult
            {
                FileName = file.Name,
                ContentType = file.ContentType,
                Size = file.Size,
                Bytes = ReadAsBytes ? bytes : null,
                Text = ReadAsBytes ? null : System.Text.Encoding.UTF8.GetString(bytes)
            };

            await OnFileLoaded.InvokeAsync(result);
        }
        catch (IOException exception)
        {
            _fileName = null;
            Notifications.Error($"Could not read “{file.Name}”: {exception.Message}");
        }
        finally
        {
            _isBusy = false;
        }
    }
}
