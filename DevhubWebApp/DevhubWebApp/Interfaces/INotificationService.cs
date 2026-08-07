namespace DevhubWebApp.Interfaces;

/// <summary>Toast notifications. Wraps MudBlazor's snackbar so components never take a UI dependency.</summary>
public interface INotificationService
{
    void Success(string message);

    void Info(string message);

    void Warning(string message);

    void Error(string message);
}
