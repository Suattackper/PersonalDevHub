using DevhubWebApp.Interfaces;
using DevhubWebApp.Constants;
using MudBlazor;

namespace DevhubWebApp.Services;

/// <summary>Snackbar-backed <see cref="INotificationService"/>.</summary>
public sealed class NotificationService : INotificationService
{
    private readonly ISnackbar _snackbar;
    private readonly IUserPreferenceService _preferences;

    public NotificationService(ISnackbar snackbar, IUserPreferenceService preferences)
    {
        _snackbar = snackbar;
        _preferences = preferences;
    }

    public void Success(string message) => _snackbar.Add(Localize(message), Severity.Success);

    public void Info(string message) => _snackbar.Add(Localize(message), Severity.Info);

    public void Warning(string message) => _snackbar.Add(Localize(message), Severity.Warning);

    public void Error(string message) => _snackbar.Add(Localize(message), Severity.Error);

    private string Localize(string message) => UiText.Get(_preferences.Settings.Language, message);
}
