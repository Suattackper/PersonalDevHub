using DevhubWebApp.Components.Shared;
using MudBlazor;

namespace DevhubWebApp.Extensions;

/// <summary>Turns "ask before doing something destructive" into a single awaited call.</summary>
public static class DialogServiceExtensions
{
    /// <summary>
    /// Shows the shared confirmation dialog and returns <c>true</c> only when the user confirmed.
    /// Dismissing the dialog, pressing escape or clicking the backdrop all count as "no".
    /// </summary>
    public static async Task<bool> ConfirmAsync(
        this IDialogService dialogService,
        string title,
        string message,
        string confirmText = "Confirm",
        Color color = Color.Error,
        string icon = Icons.Material.Filled.WarningAmber)
    {
        ArgumentNullException.ThrowIfNull(dialogService);

        var parameters = new DialogParameters<ConfirmDialog>
        {
            { dialog => dialog.Title, title },
            { dialog => dialog.Message, message },
            { dialog => dialog.ConfirmText, confirmText },
            { dialog => dialog.Color, color },
            { dialog => dialog.Icon, icon }
        };

        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.ExtraSmall,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var reference = await dialogService.ShowAsync<ConfirmDialog>(title, parameters, options);
        var result = await reference.Result;

        return result is { Canceled: false, Data: true };
    }
}
