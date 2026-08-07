using DevhubWebApp.Interfaces;
using MudBlazor;

namespace DevhubWebApp.Services;

/// <summary>Snackbar-backed <see cref="INotificationService"/>.</summary>
public sealed class NotificationService : INotificationService
{
    private readonly ISnackbar _snackbar;

    public NotificationService(ISnackbar snackbar) => _snackbar = snackbar;

    public void Success(string message) => _snackbar.Add(message, Severity.Success);

    public void Info(string message) => _snackbar.Add(message, Severity.Info);

    public void Warning(string message) => _snackbar.Add(message, Severity.Warning);

    public void Error(string message) => _snackbar.Add(message, Severity.Error);
}
