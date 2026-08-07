using DevhubWebApp.Models;
using MudBlazor;

namespace DevhubWebApp.Interfaces;

/// <summary>Resolves the active MudBlazor theme and exposes dark-mode state to the shell.</summary>
public interface IThemeProvider
{
    MudTheme Theme { get; }

    bool IsDarkMode { get; }

    event Action? Changed;

    /// <summary>Applies a theme mode, resolving <see cref="AppThemeMode.System"/> against the OS preference.</summary>
    Task ApplyAsync(AppThemeMode mode);
}
