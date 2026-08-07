using System.Globalization;
using DevhubWebApp.Constants;
using DevhubWebApp.Extensions;
using DevhubWebApp.Models.Dashboard;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DevhubWebApp.Components.Dashboard;

/// <summary>
/// Behaviour for <c>StatisticsPanel.razor</c>. The tiles are built as data so the markup stays a
/// single loop rather than six near-identical blocks.
/// </summary>
public partial class StatisticsPanel : DashboardSectionBase
{
    private IReadOnlyList<StatTile> _tiles = [];

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    protected override async Task LoadAsync()
    {
        var statistics = await DashboardService.GetStatisticsAsync();
        _tiles = BuildTiles(statistics);
    }

    private static IReadOnlyList<StatTile> BuildTiles(DashboardStatistics statistics) =>
    [
        new()
        {
            Label = "Tools",
            Value = Format(statistics.ToolCount),
            Hint = $"in {statistics.CategoryCount} categories",
            Icon = Icons.Material.Filled.Widgets,
            Color = Color.Primary
        },
        new()
        {
            Label = "Usage",
            Value = Format(statistics.UsageCount),
            Hint = $"across {statistics.ToolsUsed} tools",
            Icon = Icons.Material.Filled.Insights,
            Color = Color.Tertiary
        },
        new()
        {
            Label = "Favorites",
            Value = Format(statistics.FavoriteCount),
            Hint = statistics.FavoriteCount == 0 ? "star a tool to add one" : "starred on this device",
            Icon = Icons.Material.Filled.Star,
            Color = Color.Warning
        },
        new()
        {
            Label = "Pinned",
            Value = Format(statistics.PinnedCount),
            Hint = "shown in the sidebar",
            Icon = Icons.Material.Filled.PushPin,
            Color = Color.Info
        },
        new()
        {
            Label = "Recent",
            Value = Format(statistics.RecentCount),
            Hint = statistics.LastActivityUtc?.ToRelativeLabel() ?? "no activity yet",
            Icon = Icons.Material.Filled.Schedule,
            Color = Color.Secondary
        },
        new()
        {
            Label = "History",
            Value = Format(statistics.HistoryCount),
            Hint = "recorded actions",
            Icon = Icons.Material.Filled.ManageSearch,
            Color = Color.Success,
            Route = AppRoutes.History
        }
    ];

    private static string Format(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private void Navigate(string? route)
    {
        if (!string.IsNullOrWhiteSpace(route))
        {
            Navigation.NavigateTo(route);
        }
    }

    /// <summary>One counter tile. Private to the panel — nothing else needs this shape.</summary>
    private sealed record StatTile
    {
        public required string Label { get; init; }

        public required string Value { get; init; }

        public string? Hint { get; init; }

        public required string Icon { get; init; }

        public Color Color { get; init; } = Color.Primary;

        /// <summary>Optional navigation target; tiles without one are not interactive.</summary>
        public string? Route { get; init; }
    }
}
