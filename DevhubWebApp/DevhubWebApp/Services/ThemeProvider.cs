using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Themes;
using MudBlazor;

namespace DevhubWebApp.Services;

/// <summary>
/// Resolves the effective color scheme. <see cref="AppThemeMode.System"/> is answered by the
/// browser media query; every other mode is explicit. Dark is the fallback, by product design.
/// </summary>
public sealed class ThemeProvider : IThemeProvider
{
    private readonly IBrowserInteropService _interop;

    public ThemeProvider(IBrowserInteropService interop) => _interop = interop;

    public MudTheme Theme { get; } = DevHubTheme.Create();

    public bool IsDarkMode { get; private set; } = true;

    public event Action? Changed;

    public async Task ApplyAsync(AppThemeMode mode)
    {
        var isDark = mode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            _ => await _interop.PrefersDarkSchemeAsync()
        };

        if (isDark == IsDarkMode)
        {
            return;
        }

        IsDarkMode = isDark;
        Changed?.Invoke();
    }
}
