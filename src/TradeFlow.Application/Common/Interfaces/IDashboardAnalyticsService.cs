using TradeFlow.Application.Common.Models.Dashboard;

namespace TradeFlow.Application.Common.Interfaces;

public interface IDashboardAnalyticsService
{
    Task<DashboardKpiModel> GetKpisAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<DashboardChartDataModel> GetSalesChartDataAsync(string period = "30d", int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<List<DashboardBestSellingProductModel>> GetBestSellingProductsAsync(string period = "30d", int limit = 5, int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<DashboardWarehouseSummaryModel> GetWarehouseOverviewAsync(int? warehouseId = null, CancellationToken cancellationToken = default);
    Task<List<DashboardRecentActivityModel>> GetRecentActivitiesAsync(int limit = 6, CancellationToken cancellationToken = default);
}
