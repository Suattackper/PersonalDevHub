using DevhubWebApp.Interfaces;
using DevhubWebApp.Models;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Extensions;

/// <summary>
/// Metadata-driven helpers shared by navigation and the tool surfaces. Everything here works off
/// <see cref="ToolDescriptor"/>, so none of it grows when a tool is added.
/// </summary>
public static class ToolExtensions
{
    /// <summary>Navigates to a tool's canonical route.</summary>
    public static void GoToTool(this NavigationManager navigation, ToolDescriptor tool)
    {
        ArgumentNullException.ThrowIfNull(navigation);
        ArgumentNullException.ThrowIfNull(tool);

        navigation.NavigateTo(tool.Route);
    }

    /// <summary>Navigates to a tool's canonical route.</summary>
    public static void GoToTool(this NavigationManager navigation, ToolView view)
    {
        ArgumentNullException.ThrowIfNull(view);

        navigation.GoToTool(view.Tool);
    }

    /// <summary>Flips the favorite flag for a tool view.</summary>
    public static Task ToggleFavoriteAsync(this IUserPreferenceService preferences, ToolView view)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(view);

        return preferences.ToggleFavoriteAsync(view.Id);
    }

    /// <summary>Flips the pinned flag for a tool view.</summary>
    public static Task TogglePinAsync(this IUserPreferenceService preferences, ToolView view)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        ArgumentNullException.ThrowIfNull(view);

        return preferences.TogglePinAsync(view.Id);
    }

    /// <summary>
    /// Short usage label for cards and lists, e.g. "12 runs · 3h ago". Returns an empty string for
    /// tools that have never been opened, so callers can bind it directly.
    /// </summary>
    public static string ToUsageLabel(this ToolView view, AppLanguage language = AppLanguage.Vietnamese)
    {
        ArgumentNullException.ThrowIfNull(view);

        if (view.UsageCount == 0)
        {
            return string.Empty;
        }

        var runs = language == AppLanguage.Vietnamese
            ? $"{view.UsageCount} lượt dùng"
            : view.UsageCount == 1 ? "1 run" : $"{view.UsageCount} runs";
        return view.LastUsedUtc is null ? runs : $"{runs} · {view.LastUsedUtc.Value.ToRelativeLabel(language)}";
    }
}
