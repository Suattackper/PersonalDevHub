using DevhubWebApp.Constants;
using DevhubWebApp.Models;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Components.Dashboard;

/// <summary>Behaviour for <c>HistoryPanel.razor</c>.</summary>
public partial class HistoryPanel : DashboardSectionBase
{
    private IReadOnlyList<HistoryEntry> _entries = [];

    /// <summary>How many entries to show before linking to the full history page.</summary>
    [Parameter] public int Count { get; set; } = AppConstants.RecentDisplayCount;

    protected override async Task LoadAsync() => _entries = await DashboardService.GetRecentHistoryAsync(Count);
}
