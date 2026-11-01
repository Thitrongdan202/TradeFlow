namespace TradeFlow.Web.Services;

using TradeFlow.Application.Common.Models.Dashboard;
using TradeFlow.Application.Common.Models.Notifications;
using TradeFlow.Domain.Entities.Users;

public class DashboardStateService : IDashboardStateService
{
    private readonly List<DashboardStatItem> _stats = [];
    private readonly List<AuditLog> _recentActivities = [];
    private readonly List<SystemNotificationDto> _notifications = [];
    private readonly List<DashboardBestSellingProductModel> _bestSellingProducts = [];
    private readonly List<DashboardRecentActivityModel> _enrichedActivities = [];

    public bool HasLoaded { get; private set; }
    public IReadOnlyList<DashboardStatItem> Stats => _stats;
    public IReadOnlyList<AuditLog> RecentActivities => _recentActivities;
    public IReadOnlyList<SystemNotificationDto> Notifications => _notifications;
    public DateTime? LastUpdatedUtc { get; private set; }

    public DashboardKpiModel? Kpis { get; private set; }
    public DashboardChartDataModel? ChartData { get; private set; }
    public IReadOnlyList<DashboardBestSellingProductModel> BestSellingProducts => _bestSellingProducts;
    public DashboardWarehouseSummaryModel? WarehouseSummary { get; private set; }
    public IReadOnlyList<DashboardRecentActivityModel> EnrichedActivities => _enrichedActivities;

    public void SetState(
        IEnumerable<DashboardStatItem> stats,
        IEnumerable<AuditLog> activities,
        IEnumerable<SystemNotificationDto> notifications)
    {
        _stats.Clear();
        _stats.AddRange(stats);

        _recentActivities.Clear();
        _recentActivities.AddRange(activities);

        _notifications.Clear();
        _notifications.AddRange(notifications);

        LastUpdatedUtc = DateTime.UtcNow;
        HasLoaded = true;
    }

    public void SetFullState(
        DashboardKpiModel kpis,
        DashboardChartDataModel chartData,
        IEnumerable<DashboardBestSellingProductModel> bestSelling,
        DashboardWarehouseSummaryModel warehouseSummary,
        IEnumerable<DashboardRecentActivityModel> enrichedActivities)
    {
        Kpis = kpis;
        ChartData = chartData;

        _bestSellingProducts.Clear();
        _bestSellingProducts.AddRange(bestSelling);

        WarehouseSummary = warehouseSummary;

        _enrichedActivities.Clear();
        _enrichedActivities.AddRange(enrichedActivities);

        LastUpdatedUtc = DateTime.UtcNow;
        HasLoaded = true;
    }

    public void Invalidate()
    {
        HasLoaded = false;
        _stats.Clear();
        _recentActivities.Clear();
        _notifications.Clear();
        Kpis = null;
        ChartData = null;
        _bestSellingProducts.Clear();
        WarehouseSummary = null;
        _enrichedActivities.Clear();
    }
}
