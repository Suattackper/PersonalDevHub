using DevhubWebApp.Interfaces;
using DevhubWebApp.JSInterop;
using DevhubWebApp.Services;
using DevhubWebApp.Storage;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace DevhubWebApp.Extensions;

/// <summary>Composition root helpers. <c>Program.cs</c> stays a three-line file because of these.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Registers MudBlazor with the DevHub snackbar defaults.</summary>
    public static IServiceCollection AddDevHubUi(this IServiceCollection services)
        => services.AddMudServices(configuration =>
        {
            configuration.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            configuration.SnackbarConfiguration.PreventDuplicates = true;
            configuration.SnackbarConfiguration.NewestOnTop = true;
            configuration.SnackbarConfiguration.ShowCloseIcon = true;
            configuration.SnackbarConfiguration.VisibleStateDuration = 2500;
            configuration.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
        });

    /// <summary>Registers application services. Singleton scope is correct for a WASM client app.</summary>
    public static IServiceCollection AddDevHubServices(this IServiceCollection services)
        => services
            .AddSingleton<IStorageService, LocalStorageService>()
            .AddSingleton<IBrowserInteropService, BrowserInteropService>()
            .AddSingleton<IKeyboardShortcutService, KeyboardShortcutService>()
            .AddSingleton<IToolDiscovery, AttributeToolDiscovery>()
            .AddSingleton<IToolRegistry, ToolRegistry>()
            .AddSingleton<IToolCatalog, ToolCatalog>()
            .AddSingleton<IUserPreferenceService, UserPreferenceService>()
            .AddSingleton<IHistoryService, HistoryService>()
            .AddSingleton<IThemeProvider, ThemeProvider>()
            .AddSingleton<IDashboardService, DashboardService>()
            .AddSingleton<IWorkspaceService, WorkspaceService>()
            .AddSingleton<IMarkdownService, MarkdownService>()
            .AddSingleton<IHtmlExportService, HtmlExportService>()
            .AddSingleton<IQrCodeService, QrCodeService>()
            .AddScoped<INotificationService, NotificationService>();
}
