using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Dashboard;

public class DashboardKpiModel
{
    public decimal MonthRevenue { get; set; }
    public decimal PreviousMonthRevenue { get; set; }
    public decimal? RevenueGrowthPercent { get; set; }
    public int TodayOrdersCount { get; set; }
    public int MonthOrdersCount { get; set; }
    public int TotalOrdersCount { get; set; }
    public int InStockItemsCount { get; set; }
    public int LowStockItemsCount { get; set; }
    public int OutOfStockItemsCount { get; set; }
    public int ActiveCustomersCount { get; set; }
    public int ActiveProductsCount { get; set; }
}

public class DashboardChartPointModel
{
    public DateTime Date { get; set; }
    public string DateLabel { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int OrderCount { get; set; }
}

public class DashboardChartDataModel
{
    public string Period { get; set; } = "30d";
    public decimal TotalPeriodRevenue { get; set; }
    public int TotalPeriodOrders { get; set; }
    public decimal MaxDailyRevenue { get; set; }
    public int MaxDailyOrders { get; set; }
    public List<DashboardChartPointModel> Points { get; set; } = [];
}

public class DashboardBestSellingProductModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public int Rank { get; set; }
    public double PercentageOfTop { get; set; }
}

public class DashboardWarehouseCardModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ManagerName { get; set; }
    public string? Address { get; set; }
    public int InStockCount { get; set; }
    public int LowStockCount { get; set; }
}

public class DashboardLowStockAlertModel
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal Threshold { get; set; } = 15;
    public string WarehouseName { get; set; } = string.Empty;
    public StockCondition Condition { get; set; } = StockCondition.Saleable;
}

public class DashboardWarehouseSummaryModel
{
    public int ActiveWarehouseCount { get; set; }
    public int TotalWarehouseCount { get; set; }
    public int InStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
    public decimal TotalStockValue { get; set; }
    public List<DashboardWarehouseCardModel> Warehouses { get; set; } = [];
    public List<DashboardLowStockAlertModel> LowStockAlerts { get; set; } = [];
}

public class DashboardRecentActivityModel
{
    public long Id { get; set; }
    public AuditEventType EventType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? TargetEntity { get; set; }
    public string? TargetId { get; set; }
    public string? TargetUrl { get; set; }
}
