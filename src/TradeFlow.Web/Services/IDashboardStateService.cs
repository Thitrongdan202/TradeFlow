namespace TradeFlow.Web.Services;

using TradeFlow.Application.Common.Models.Dashboard;
using TradeFlow.Application.Common.Models.Notifications;
using TradeFlow.Domain.Entities.Users;

public record DashboardStatItem(string Label, string Value, string Meta, string Color);

public interface IDashboardStateService
{
    bool HasLoaded { get; }
    IReadOnlyList<DashboardStatItem> Stats { get; }
    IReadOnlyList<AuditLog> RecentActivities { get; }
    IReadOnlyList<SystemNotificationDto> Notifications { get; }
    DateTime? LastUpdatedUtc { get; }

    // Enriched Analytics Properties
    DashboardKpiModel? Kpis { get; }
    DashboardChartDataModel? ChartData { get; }
    IReadOnlyList<DashboardBestSellingProductModel> BestSellingProducts { get; }
    DashboardWarehouseSummaryModel? WarehouseSummary { get; }
    IReadOnlyList<DashboardRecentActivityModel> EnrichedActivities { get; }

    void SetState(
        IEnumerable<DashboardStatItem> stats,
        IEnumerable<AuditLog> activities,
        IEnumerable<SystemNotificationDto> notifications);

    void SetFullState(
        DashboardKpiModel kpis,
        DashboardChartDataModel chartData,
        IEnumerable<DashboardBestSellingProductModel> bestSelling,
        DashboardWarehouseSummaryModel warehouseSummary,
        IEnumerable<DashboardRecentActivityModel> enrichedActivities);

    void Invalidate();
}
