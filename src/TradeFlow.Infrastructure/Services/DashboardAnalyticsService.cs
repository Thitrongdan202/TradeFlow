using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Dashboard;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class DashboardAnalyticsService : IDashboardAnalyticsService
{
    private readonly IDbContextFactory<TradeFlowDbContext> _dbFactory;
    private readonly IFileStorageService _fileStorage;

    public DashboardAnalyticsService(
        IDbContextFactory<TradeFlowDbContext> dbFactory,
        IFileStorageService fileStorage)
    {
        _dbFactory = dbFactory;
        _fileStorage = fileStorage;
    }

    public Task<DashboardKpiModel> GetKpisAsync(int? warehouseId, CancellationToken cancellationToken = default)
    {
        return GetKpisAsync("30d", null, null, warehouseId, cancellationToken);
    }

    public async Task<DashboardKpiModel> GetKpisAsync(
        string period = "30d",
        DateTime? customStart = null,
        DateTime? customEnd = null,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var range = ResolvePeriodRange(period, customStart, customEnd);
        var utcStart = range.UtcStart;
        var utcEnd = range.UtcEnd;
        var prevUtcStart = range.PrevUtcStart;
        var prevUtcEnd = range.PrevUtcEnd;

        // 1. Current Period Revenue
        var invPeriodRev = await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= utcStart && i.InvoiceDate <= utcEnd)
            .SumAsync(i => (decimal?)i.GrandTotal, cancellationToken) ?? 0m;

        var ordPeriodRev = await db.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= utcStart && o.OrderDate <= utcEnd && o.Status != SalesOrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.GrandTotal, cancellationToken) ?? 0m;

        var periodRevenue = invPeriodRev > 0 ? invPeriodRev : ordPeriodRev;

        // 2. Previous Period Revenue for growth comparison
        var prevInvRev = await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= prevUtcStart && i.InvoiceDate <= prevUtcEnd)
            .SumAsync(i => (decimal?)i.GrandTotal, cancellationToken) ?? 0m;

        var prevOrdRev = await db.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= prevUtcStart && o.OrderDate <= prevUtcEnd && o.Status != SalesOrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.GrandTotal, cancellationToken) ?? 0m;

        var prevPeriodRevenue = prevInvRev > 0 ? prevInvRev : prevOrdRev;

        decimal? growthPercent = null;
        if (prevPeriodRevenue > 0)
        {
            growthPercent = Math.Round(((periodRevenue - prevPeriodRevenue) / prevPeriodRevenue) * 100m, 1);
        }

        // 3. Current Period Orders Count
        var periodOrdersCount = await db.SalesOrders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= utcStart && o.OrderDate <= utcEnd && o.Status != SalesOrderStatus.Cancelled, cancellationToken);

        // 4. Current Period Items Sold
        var periodItemsSold = await db.SalesOrderItems
            .AsNoTracking()
            .Where(item => item.SalesOrder.OrderDate >= utcStart && item.SalesOrder.OrderDate <= utcEnd && item.SalesOrder.Status != SalesOrderStatus.Cancelled)
            .SumAsync(item => (decimal?)item.Quantity, cancellationToken) ?? 0m;

        // 5. Standard Baseline Metrics (for subtext context)
        var vnNow = DateTime.UtcNow.AddHours(7);
        var todayVnStart = DateTime.SpecifyKind(vnNow.Date.AddHours(-7), DateTimeKind.Utc);
        var thisMonthVnStart = DateTime.SpecifyKind(new DateTime(vnNow.Year, vnNow.Month, 1, 0, 0, 0).AddHours(-7), DateTimeKind.Utc);

        var todayOrdersCount = await db.SalesOrders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= todayVnStart && o.Status != SalesOrderStatus.Cancelled, cancellationToken);

        var monthOrdersCount = await db.SalesOrders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= thisMonthVnStart && o.Status != SalesOrderStatus.Cancelled, cancellationToken);

        var totalOrdersCount = await db.SalesOrders
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // 6. Inventory Stock counts
        var stockQuery = db.InventoryStocks.AsNoTracking();
        if (warehouseId.HasValue)
        {
            stockQuery = stockQuery.Where(s => s.WarehouseId == warehouseId.Value);
        }

        var inStockItemsCount = await stockQuery
            .Where(s => s.Condition == StockCondition.Saleable && s.Quantity > 0)
            .Select(s => s.ProductId)
            .Distinct()
            .CountAsync(cancellationToken);

        var lowStockItemsCount = await stockQuery
            .Where(s => s.Condition == StockCondition.Saleable && s.Quantity > 0 && s.Quantity < 15)
            .Select(s => s.ProductId)
            .Distinct()
            .CountAsync(cancellationToken);

        // 7. Active customers & products
        var activeCustomersCount = await db.Customers
            .AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        var activeProductsCount = await db.Products
            .AsNoTracking()
            .CountAsync(p => p.IsActive, cancellationToken);

        var outOfStockItemsCount = Math.Max(0, activeProductsCount - inStockItemsCount);

        return new DashboardKpiModel
        {
            PeriodRevenue = periodRevenue,
            PreviousPeriodRevenue = prevPeriodRevenue,
            RevenueGrowthPercent = growthPercent,
            PeriodRevenueLabel = range.PeriodLabel,
            PreviousPeriodLabel = range.PrevPeriodLabel,
            PeriodOrdersCount = periodOrdersCount,
            PeriodOrdersLabel = range.OrdersLabel,
            PeriodItemsSoldCount = periodItemsSold,

            MonthRevenue = periodRevenue,
            PreviousMonthRevenue = prevPeriodRevenue,
            TodayOrdersCount = todayOrdersCount,
            MonthOrdersCount = monthOrdersCount,
            TotalOrdersCount = totalOrdersCount,
            InStockItemsCount = inStockItemsCount,
            LowStockItemsCount = lowStockItemsCount,
            OutOfStockItemsCount = outOfStockItemsCount,
            ActiveCustomersCount = activeCustomersCount,
            ActiveProductsCount = activeProductsCount
        };
    }

    public async Task<DashboardChartDataModel> GetSalesChartDataAsync(
        string period = "30d",
        DateTime? customStart = null,
        DateTime? customEnd = null,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var range = ResolvePeriodRange(period, customStart, customEnd);
        var utcStart = range.UtcStart;
        var utcEnd = range.UtcEnd;

        if (range.IsHourly)
        {
            // Query Orders with Items in this 24-hour window
            var orders = await db.SalesOrders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= utcStart && o.OrderDate <= utcEnd && o.Status != SalesOrderStatus.Cancelled)
                .ToListAsync(cancellationToken);

            // Query Invoices with Items in this 24-hour window
            var invoices = await db.Invoices
                .AsNoTracking()
                .Include(i => i.Items)
                .Where(i => i.InvoiceDate >= utcStart && i.InvoiceDate <= utcEnd)
                .ToListAsync(cancellationToken);

            var points = new List<DashboardChartPointModel>();
            decimal totalRevenue = 0;
            int totalOrders = 0;
            decimal totalItemsSold = 0;
            decimal maxHourlyRevenue = 0;
            int maxHourlyOrders = 0;

            for (int h = 0; h < 24; h++)
            {
                var ordersInHour = orders.Where(o => o.OrderDate.AddHours(7).Hour == h).ToList();
                var invoicesInHour = invoices.Where(i => i.InvoiceDate.AddHours(7).Hour == h).ToList();

                var invHRev = invoicesInHour.Sum(i => i.GrandTotal);
                var ordHRev = ordersInHour.Sum(o => o.GrandTotal);
                var hourlyRev = invHRev > 0 ? invHRev : ordHRev;
                var hourlyOrders = Math.Max(ordersInHour.Count, invoicesInHour.Count);

                var itemsFromOrders = ordersInHour.SelectMany(o => o.Items).Sum(x => x.Quantity);
                var itemsFromInvoices = invoicesInHour.SelectMany(i => i.Items).Sum(x => x.Quantity);
                var hourlyItems = itemsFromOrders > 0 ? itemsFromOrders : itemsFromInvoices;

                var productsInHour = ordersInHour.SelectMany(o => o.Items)
                    .GroupBy(x => new { x.ProductId, x.ProductCode, x.ProductName })
                    .Select(g => new DashboardHourlyProductModel
                    {
                        ProductId = g.Key.ProductId,
                        ProductCode = g.Key.ProductCode,
                        ProductName = g.Key.ProductName,
                        Quantity = g.Sum(x => x.Quantity),
                        TotalAmount = g.Sum(x => x.LineTotal)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(5)
                    .ToList();

                if (productsInHour.Count == 0 && invoicesInHour.Count > 0)
                {
                    productsInHour = invoicesInHour.SelectMany(i => i.Items)
                        .GroupBy(x => new { x.ProductId, x.ProductCode, x.ProductName })
                        .Select(g => new DashboardHourlyProductModel
                        {
                            ProductId = g.Key.ProductId ?? 0,
                            ProductCode = g.Key.ProductCode,
                            ProductName = g.Key.ProductName,
                            Quantity = g.Sum(x => x.Quantity),
                            TotalAmount = g.Sum(x => x.LineTotal)
                        })
                        .OrderByDescending(x => x.TotalAmount)
                        .Take(5)
                        .ToList();
                }

                totalRevenue += hourlyRev;
                totalOrders += hourlyOrders;
                totalItemsSold += hourlyItems;

                if (hourlyRev > maxHourlyRevenue) maxHourlyRevenue = hourlyRev;
                if (hourlyOrders > maxHourlyOrders) maxHourlyOrders = hourlyOrders;

                points.Add(new DashboardChartPointModel
                {
                    Date = range.VnStart.Date.AddHours(h),
                    DateLabel = $"{h:D2}:00",
                    FullDateLabel = $"{h:D2}:00 – {h:D2}:59, {range.VnStart:dd/MM/yyyy}",
                    Revenue = hourlyRev,
                    OrderCount = hourlyOrders,
                    ItemsSold = hourlyItems,
                    Hour = h,
                    IsPeak = false,
                    ProductsSold = productsInHour
                });
            }

            string peakHourLabel = "Chưa có giờ cao điểm";
            decimal peakRev = 0;
            if (maxHourlyRevenue > 0)
            {
                var peakPt = points.OrderByDescending(p => p.Revenue).First();
                peakPt.IsPeak = true;
                peakHourLabel = $"{peakPt.Hour:D2}:00 – {peakPt.Hour + 1:D2}:00";
                peakRev = peakPt.Revenue;
            }

            string peakOrderHourLabel = string.Empty;
            int peakHourlyOrders = 0;
            if (maxHourlyOrders > 0)
            {
                var peakOrdPt = points.OrderByDescending(p => p.OrderCount).First();
                peakOrderHourLabel = $"{peakOrdPt.Hour:D2}:00 – {peakOrdPt.Hour + 1:D2}:00";
                peakHourlyOrders = peakOrdPt.OrderCount;
            }

            return new DashboardChartDataModel
            {
                Period = period,
                IsHourly = true,
                PeriodTitle = range.PeriodLabel,
                PeakHourLabel = peakHourLabel,
                PeakHourRevenue = peakRev,
                PeakOrderHourLabel = peakOrderHourLabel,
                PeakHourlyOrders = peakHourlyOrders,
                TotalPeriodRevenue = totalRevenue,
                TotalPeriodOrders = totalOrders,
                TotalPeriodItemsSold = totalItemsSold,
                MaxDailyRevenue = maxHourlyRevenue,
                MaxDailyOrders = maxHourlyOrders,
                Points = points
            };
        }
        else
        {
            // Multi-day aggregation
            var orders = await db.SalesOrders
                .AsNoTracking()
                .Include(o => o.Items)
                .Where(o => o.OrderDate >= utcStart && o.OrderDate <= utcEnd && o.Status != SalesOrderStatus.Cancelled)
                .ToListAsync(cancellationToken);

            var invoices = await db.Invoices
                .AsNoTracking()
                .Include(i => i.Items)
                .Where(i => i.InvoiceDate >= utcStart && i.InvoiceDate <= utcEnd)
                .ToListAsync(cancellationToken);

            var points = new List<DashboardChartPointModel>();
            var curDate = range.VnStart.Date;
            var finalDate = range.VnEnd.Date;
            bool is7Days = (finalDate - curDate).Days == 6;

            decimal totalRevenue = 0;
            int totalOrders = 0;
            decimal totalItemsSold = 0;
            decimal maxDailyRevenue = 0;
            int maxDailyOrders = 0;

            while (curDate <= finalDate)
            {
                var ordersInDay = orders.Where(o => o.OrderDate.AddHours(7).Date == curDate).ToList();
                var invoicesInDay = invoices.Where(i => i.InvoiceDate.AddHours(7).Date == curDate).ToList();

                var invDRev = invoicesInDay.Sum(i => i.GrandTotal);
                var ordDRev = ordersInDay.Sum(o => o.GrandTotal);
                var dailyRevenue = invDRev > 0 ? invDRev : ordDRev;
                var dailyOrders = Math.Max(ordersInDay.Count, invoicesInDay.Count);

                var itemsFromOrders = ordersInDay.SelectMany(o => o.Items).Sum(x => x.Quantity);
                var itemsFromInvoices = invoicesInDay.SelectMany(i => i.Items).Sum(x => x.Quantity);
                var dailyItems = itemsFromOrders > 0 ? itemsFromOrders : itemsFromInvoices;

                var productsInDay = ordersInDay.SelectMany(o => o.Items)
                    .GroupBy(x => new { x.ProductId, x.ProductCode, x.ProductName })
                    .Select(g => new DashboardHourlyProductModel
                    {
                        ProductId = g.Key.ProductId,
                        ProductCode = g.Key.ProductCode,
                        ProductName = g.Key.ProductName,
                        Quantity = g.Sum(x => x.Quantity),
                        TotalAmount = g.Sum(x => x.LineTotal)
                    })
                    .OrderByDescending(x => x.TotalAmount)
                    .Take(5)
                    .ToList();

                totalRevenue += dailyRevenue;
                totalOrders += dailyOrders;
                totalItemsSold += dailyItems;

                if (dailyRevenue > maxDailyRevenue) maxDailyRevenue = dailyRevenue;
                if (dailyOrders > maxDailyOrders) maxDailyOrders = dailyOrders;

                string dateLabel = is7Days 
                    ? $"{GetVnDayOfWeek(curDate)} · {curDate:dd/MM}"
                    : curDate.ToString("dd/MM");

                points.Add(new DashboardChartPointModel
                {
                    Date = curDate,
                    DateLabel = dateLabel,
                    FullDateLabel = curDate.ToString("dddd, dd/MM/yyyy", new CultureInfo("vi-VN")),
                    Revenue = dailyRevenue,
                    OrderCount = dailyOrders,
                    ItemsSold = dailyItems,
                    Hour = 0,
                    IsPeak = false,
                    IsLowest = false,
                    ProductsSold = productsInDay
                });

                curDate = curDate.AddDays(1);
            }

            // Identify peak and lowest revenue days if any sales occurred
            if (maxDailyRevenue > 0)
            {
                var peakPt = points.OrderByDescending(p => p.Revenue).FirstOrDefault();
                if (peakPt != null) peakPt.IsPeak = true;

                var nonZeroPoints = points.Where(p => p.Revenue > 0).ToList();
                if (nonZeroPoints.Count > 1)
                {
                    var lowestPt = nonZeroPoints.OrderBy(p => p.Revenue).FirstOrDefault();
                    if (lowestPt != null && lowestPt != peakPt) lowestPt.IsLowest = true;
                }
            }

            return new DashboardChartDataModel
            {
                Period = period,
                IsHourly = false,
                PeriodTitle = range.PeriodLabel,
                TotalPeriodRevenue = totalRevenue,
                TotalPeriodOrders = totalOrders,
                TotalPeriodItemsSold = totalItemsSold,
                MaxDailyRevenue = maxDailyRevenue,
                MaxDailyOrders = maxDailyOrders,
                Points = points
            };
        }
    }

    public async Task<List<DashboardBestSellingProductModel>> GetBestSellingProductsAsync(
        string period = "30d",
        int limit = 5,
        int? warehouseId = null,
        DateTime? customStart = null,
        DateTime? customEnd = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var range = ResolvePeriodRange(period, customStart, customEnd);
        var utcStart = range.UtcStart;
        var utcEnd = range.UtcEnd;

        // Aggregate from SalesOrderItems
        var orderItemsQuery = db.SalesOrderItems
            .AsNoTracking()
            .Include(i => i.Product)
                .ThenInclude(p => p.Images)
            .Where(i => i.SalesOrder.Status != SalesOrderStatus.Cancelled &&
                        i.SalesOrder.OrderDate >= utcStart &&
                        i.SalesOrder.OrderDate <= utcEnd);

        var grouped = await orderItemsQuery
            .GroupBy(i => new
            {
                i.ProductId,
                i.Product.Code,
                i.Product.Name,
                i.UnitName
            })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.Code,
                g.Key.Name,
                g.Key.UnitName,
                QuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.LineTotal)
            })
            .OrderByDescending(x => x.TotalRevenue)
            .ThenByDescending(x => x.QuantitySold)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var result = new List<DashboardBestSellingProductModel>();

        if (grouped.Any())
        {
            var productIds = grouped.Select(x => x.ProductId).ToList();
            var primaryImages = await db.ProductImages
                .AsNoTracking()
                .Where(img => productIds.Contains(img.ProductId) && img.IsActive && img.IsPrimary)
                .ToDictionaryAsync(img => img.ProductId, img => img.StorageReference, cancellationToken);

            var maxRev = grouped.First().TotalRevenue;
            if (maxRev <= 0) maxRev = 1;

            int rank = 1;
            foreach (var item in grouped)
            {
                string? imgUrl = null;
                if (primaryImages.TryGetValue(item.ProductId, out var storageRef) && !string.IsNullOrEmpty(storageRef))
                {
                    imgUrl = _fileStorage.GetPublicUrl(storageRef);
                }

                result.Add(new DashboardBestSellingProductModel
                {
                    ProductId = item.ProductId,
                    ProductCode = item.Code,
                    ProductName = item.Name,
                    ImageUrl = imgUrl,
                    UnitName = string.IsNullOrEmpty(item.UnitName) ? "Cái" : item.UnitName,
                    QuantitySold = item.QuantitySold,
                    TotalRevenue = item.TotalRevenue,
                    Rank = rank,
                    PercentageOfTop = Math.Min(100.0, Math.Round((double)(item.TotalRevenue / maxRev) * 100.0, 1))
                });
                rank++;
            }
        }
        else
        {
            // Graceful fallback to prominent products in the catalog
            var prominentProducts = await db.Products
                .AsNoTracking()
                .Include(p => p.Unit)
                .Include(p => p.Images)
                .Where(p => p.IsActive)
                .OrderBy(p => p.Code)
                .Take(limit)
                .ToListAsync(cancellationToken);

            int rank = 1;
            foreach (var p in prominentProducts)
            {
                var primaryImg = p.Images.FirstOrDefault(i => i.IsActive && i.IsPrimary)
                              ?? p.Images.FirstOrDefault(i => i.IsActive);
                string? imgUrl = primaryImg != null ? _fileStorage.GetPublicUrl(primaryImg.StorageReference) : null;

                result.Add(new DashboardBestSellingProductModel
                {
                    ProductId = p.Id,
                    ProductCode = p.Code,
                    ProductName = p.Name,
                    ImageUrl = imgUrl,
                    UnitName = p.Unit?.Name ?? "Cái",
                    QuantitySold = 0,
                    TotalRevenue = 0,
                    Rank = rank,
                    PercentageOfTop = 0
                });
                rank++;
            }
        }

        return result;
    }

    public async Task<DashboardWarehouseSummaryModel> GetWarehouseOverviewAsync(int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var warehouses = await db.Warehouses
            .AsNoTracking()
            .Where(w => w.IsActive)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        var stockQuery = db.InventoryStocks
            .AsNoTracking()
            .Include(s => s.Product)
                .ThenInclude(p => p.Unit)
            .Include(s => s.Warehouse)
            .AsQueryable();

        if (warehouseId.HasValue)
        {
            stockQuery = stockQuery.Where(s => s.WarehouseId == warehouseId.Value);
        }

        var allStock = await stockQuery.ToListAsync(cancellationToken);

        var inStockCount = allStock
            .Where(s => s.Condition == StockCondition.Saleable && s.Quantity > 0)
            .Select(s => s.ProductId)
            .Distinct()
            .Count();

        var lowStockCount = allStock
            .Where(s => s.Condition == StockCondition.Saleable && s.Quantity > 0 && s.Quantity < 15)
            .Select(s => s.ProductId)
            .Distinct()
            .Count();

        var totalActiveProducts = await db.Products
            .AsNoTracking()
            .CountAsync(p => p.IsActive, cancellationToken);

        var outOfStockCount = Math.Max(0, totalActiveProducts - inStockCount);

        var lowStockAlerts = allStock
            .Where(s => s.Condition == StockCondition.Saleable && s.Quantity < 15 && s.Product != null)
            .OrderBy(s => s.Quantity)
            .Take(5)
            .Select(s => new DashboardLowStockAlertModel
            {
                ProductId = s.ProductId,
                ProductCode = s.Product!.Code,
                ProductName = s.Product.Name,
                UnitName = s.Product.Unit?.Name ?? "Cái",
                CurrentQuantity = s.Quantity,
                Threshold = 15,
                WarehouseName = s.Warehouse?.Name ?? "Kho chính",
                Condition = s.Condition
            })
            .ToList();

        var warehouseCards = new List<DashboardWarehouseCardModel>();
        foreach (var wh in warehouses)
        {
            var whStock = allStock.Where(s => s.WarehouseId == wh.Id).ToList();
            warehouseCards.Add(new DashboardWarehouseCardModel
            {
                Id = wh.Id,
                Code = wh.Code,
                Name = wh.Name,
                ManagerName = wh.ManagerName,
                Address = wh.Address,
                InStockCount = whStock.Where(s => s.Quantity > 0).Select(s => s.ProductId).Distinct().Count(),
                LowStockCount = whStock.Where(s => s.Quantity > 0 && s.Quantity < 15).Select(s => s.ProductId).Distinct().Count()
            });
        }

        return new DashboardWarehouseSummaryModel
        {
            ActiveWarehouseCount = warehouses.Count,
            TotalWarehouseCount = warehouses.Count,
            InStockCount = inStockCount,
            LowStockCount = lowStockCount,
            OutOfStockCount = outOfStockCount,
            TotalStockValue = 0,
            Warehouses = warehouseCards,
            LowStockAlerts = lowStockAlerts
        };
    }

    public async Task<List<DashboardRecentActivityModel>> GetRecentActivitiesAsync(int limit = 6, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var logs = await db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var result = new List<DashboardRecentActivityModel>();
        foreach (var log in logs)
        {
            result.Add(new DashboardRecentActivityModel
            {
                Id = log.Id,
                EventType = log.EventType,
                Title = FormatAuditEventTitle(log),
                Description = FormatAuditEventDescription(log),
                PerformedBy = log.PerformedBy ?? "Hệ thống",
                OccurredAt = log.OccurredAt,
                TargetEntity = log.TargetEntity,
                TargetId = log.TargetId,
                TargetUrl = ResolveTargetUrl(log.TargetEntity)
            });
        }

        return result;
    }

    // =========================================================================
    // PERIOD & TIMEZONE HELPERS (Asia/Ho_Chi_Minh: UTC+7)
    // =========================================================================

    private record PeriodRangeResult(
        DateTime VnStart,
        DateTime VnEnd,
        DateTime UtcStart,
        DateTime UtcEnd,
        DateTime PrevVnStart,
        DateTime PrevVnEnd,
        DateTime PrevUtcStart,
        DateTime PrevUtcEnd,
        string PeriodLabel,
        string PrevPeriodLabel,
        string OrdersLabel,
        bool IsHourly
    );

    private static PeriodRangeResult ResolvePeriodRange(string period, DateTime? customStart, DateTime? customEnd)
    {
        var vnNow = DateTime.UtcNow.AddHours(7);
        DateTime vnStart, vnEnd, prevVnStart, prevVnEnd;
        string periodLabel, prevPeriodLabel, ordersLabel;
        bool isHourly = false;

        switch (period.ToLowerInvariant())
        {
            case "yesterday":
                isHourly = true;
                var vnYest = vnNow.Date.AddDays(-1);
                vnStart = vnYest;
                vnEnd = vnYest.AddDays(1).AddTicks(-1);
                var vnDayBefore = vnYest.AddDays(-1);
                prevVnStart = vnDayBefore;
                prevVnEnd = vnDayBefore.AddDays(1).AddTicks(-1);
                periodLabel = "Doanh thu hôm qua";
                prevPeriodLabel = "so với hôm kia";
                ordersLabel = "đơn hôm qua";
                break;

            case "today":
                isHourly = true;
                vnStart = vnNow.Date;
                vnEnd = vnNow.Date.AddDays(1).AddTicks(-1);
                var prevDay = vnNow.Date.AddDays(-1);
                prevVnStart = prevDay;
                prevVnEnd = prevDay.AddDays(1).AddTicks(-1);
                periodLabel = "Doanh thu hôm nay";
                prevPeriodLabel = "so với hôm qua";
                ordersLabel = "đơn hôm nay";
                break;

            case "7d":
                vnStart = vnNow.Date.AddDays(-6);
                vnEnd = vnNow.Date.AddDays(1).AddTicks(-1);
                prevVnStart = vnStart.AddDays(-7);
                prevVnEnd = vnStart.AddTicks(-1);
                periodLabel = "Doanh thu 7 ngày qua";
                prevPeriodLabel = "so với 7 ngày trước";
                ordersLabel = "đơn trong 7 ngày";
                break;

            case "this_month":
                vnStart = new DateTime(vnNow.Year, vnNow.Month, 1, 0, 0, 0);
                int daysThisMonth = DateTime.DaysInMonth(vnNow.Year, vnNow.Month);
                vnEnd = new DateTime(vnNow.Year, vnNow.Month, daysThisMonth, 23, 59, 59, 999);
                var lastMonthDate = vnStart.AddMonths(-1);
                int daysInLastMonth = DateTime.DaysInMonth(lastMonthDate.Year, lastMonthDate.Month);
                prevVnStart = lastMonthDate;
                prevVnEnd = new DateTime(lastMonthDate.Year, lastMonthDate.Month, daysInLastMonth, 23, 59, 59, 999);
                periodLabel = "Doanh thu tháng này";
                prevPeriodLabel = "so với tháng trước";
                ordersLabel = "đơn tháng này";
                break;

            case "last_month":
                var lmDate = new DateTime(vnNow.Year, vnNow.Month, 1, 0, 0, 0).AddMonths(-1);
                int daysLm = DateTime.DaysInMonth(lmDate.Year, lmDate.Month);
                vnStart = lmDate;
                vnEnd = new DateTime(lmDate.Year, lmDate.Month, daysLm, 23, 59, 59, 999);
                var lmbDate = lmDate.AddMonths(-1);
                int daysLmb = DateTime.DaysInMonth(lmbDate.Year, lmbDate.Month);
                prevVnStart = lmbDate;
                prevVnEnd = new DateTime(lmbDate.Year, lmbDate.Month, daysLmb, 23, 59, 59, 999);
                periodLabel = "Doanh thu tháng trước";
                prevPeriodLabel = "so với tháng trước đó";
                ordersLabel = "đơn tháng trước";
                break;

            case "custom":
                if (customStart.HasValue && customEnd.HasValue)
                {
                    vnStart = customStart.Value.Date;
                    vnEnd = customEnd.Value.Date.AddDays(1).AddTicks(-1);
                    int durationDays = Math.Max(1, (customEnd.Value.Date - customStart.Value.Date).Days + 1);
                    prevVnStart = vnStart.AddDays(-durationDays);
                    prevVnEnd = vnStart.AddTicks(-1);
                    isHourly = (customStart.Value.Date == customEnd.Value.Date);

                    if (isHourly)
                    {
                        periodLabel = $"Doanh thu ngày {vnStart:dd/MM/yyyy}";
                        prevPeriodLabel = "so với ngày trước";
                        ordersLabel = $"đơn ngày {vnStart:dd/MM}";
                    }
                    else
                    {
                        periodLabel = $"Doanh thu ({vnStart:dd/MM} - {customEnd.Value:dd/MM})";
                        prevPeriodLabel = "so với kỳ trước";
                        ordersLabel = "đơn trong kỳ";
                    }
                }
                else
                {
                    vnStart = vnNow.Date.AddDays(-29);
                    vnEnd = vnNow.Date.AddDays(1).AddTicks(-1);
                    prevVnStart = vnStart.AddDays(-30);
                    prevVnEnd = vnStart.AddTicks(-1);
                    periodLabel = "Doanh thu 30 ngày qua";
                    prevPeriodLabel = "so với 30 ngày trước";
                    ordersLabel = "đơn trong 30 ngày";
                }
                break;

            case "30d":
            default:
                vnStart = vnNow.Date.AddDays(-29);
                vnEnd = vnNow.Date.AddDays(1).AddTicks(-1);
                prevVnStart = vnStart.AddDays(-30);
                prevVnEnd = vnStart.AddTicks(-1);
                periodLabel = "Doanh thu 30 ngày qua";
                prevPeriodLabel = "so với 30 ngày trước";
                ordersLabel = "đơn trong 30 ngày";
                break;
        }

        var utcStart = DateTime.SpecifyKind(vnStart.AddHours(-7), DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(vnEnd.AddHours(-7), DateTimeKind.Utc);
        var prevUtcStart = DateTime.SpecifyKind(prevVnStart.AddHours(-7), DateTimeKind.Utc);
        var prevUtcEnd = DateTime.SpecifyKind(prevVnEnd.AddHours(-7), DateTimeKind.Utc);

        return new PeriodRangeResult(
            vnStart, vnEnd, utcStart, utcEnd,
            prevVnStart, prevVnEnd, prevUtcStart, prevUtcEnd,
            periodLabel, prevPeriodLabel, ordersLabel, isHourly);
    }

    private static string GetVnDayOfWeek(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        DayOfWeek.Sunday => "CN",
        _ => ""
    };

    private static string FormatAuditEventTitle(AuditLog log)
    {
        return log.EventType switch
        {
            AuditEventType.Login => "Đăng nhập hệ thống",
            AuditEventType.Logout => "Đăng xuất hệ thống",
            AuditEventType.UserCreated => "Thêm người dùng mới",
            AuditEventType.UserUpdated => "Cập nhật tài khoản",
            AuditEventType.ProductCreated => "Thêm sản phẩm mới",
            AuditEventType.ProductUpdated => "Cập nhật sản phẩm",
            AuditEventType.SalesOrderCreated => "Tạo đơn bán hàng",
            AuditEventType.SalesOrderConfirmed => "Xác nhận đơn bán hàng",
            AuditEventType.SalesOrderUpdated => "Cập nhật đơn bán hàng",
            AuditEventType.InvoiceCreated => "Tạo hóa đơn bán hàng",
            AuditEventType.InvoiceIssued => "Phát hành hóa đơn",
            AuditEventType.InvoiceExportedPdf => "Xuất PDF hóa đơn",
            AuditEventType.InvoiceAdjusted => "Điều chỉnh hóa đơn",
            AuditEventType.InvoiceReplaced => "Thay thế hóa đơn",
            AuditEventType.InvoiceCancelled => "Hủy hóa đơn bán hàng",
            AuditEventType.InvoiceDeleted => "Xóa hóa đơn bán hàng",
            AuditEventType.QuotationCreated => "Tạo báo giá mới",
            AuditEventType.QuotationConverted => "Chuyển báo giá thành đơn hàng",
            AuditEventType.QuotationExportedPdf => "Xuất PDF báo giá",
            AuditEventType.CustomerCreated => "Thêm khách hàng mới",
            AuditEventType.CustomerUpdated => "Cập nhật khách hàng",
            AuditEventType.SupplierCreated => "Thêm nhà cung cấp",
            AuditEventType.WarehouseCreated => "Thêm kho hàng mới",
            AuditEventType.WarehouseUpdated => "Cập nhật kho hàng",
            AuditEventType.ContractCreated => "Tạo hợp đồng kinh tế",
            AuditEventType.ContractSigned => "Ký điện tử hợp đồng",
            AuditEventType.ContractFinalized => "Hoàn tất hợp đồng",
            AuditEventType.ContractExportedPdf => "Xuất PDF hợp đồng",
            AuditEventType.EmployeeCreated => "Thêm nhân viên mới",
            AuditEventType.EmployeeUpdated => "Cập nhật thông tin nhân viên",
            AuditEventType.EmployeeStatusChanged => "Cập nhật trạng thái nhân sự",
            AuditEventType.EmployeeRetired => "Xử lý thôi việc",
            AuditEventType.EmployeeLinkedToUser => "Liên kết tài khoản người dùng",
            AuditEventType.EmployeeUnlinkedFromUser => "Hủy liên kết tài khoản",
            AuditEventType.EmployeeRequestSubmitted => "Gửi đề xuất nhân sự",
            AuditEventType.EmployeeRequestApproved => "Phê duyệt đề xuất nhân sự",
            AuditEventType.EmployeeRequestRejected => "Từ chối đề xuất nhân sự",
            AuditEventType.DebtPaymentRecorded => "Ghi nhận thanh toán công nợ",
            AuditEventType.DebtAdjustmentRecorded => "Điều chỉnh công nợ",
            AuditEventType.DebtDiscountRecorded => "Ghi nhận chiết khấu thanh toán",
            AuditEventType.DebtExported => "Xuất báo cáo công nợ",
            AuditEventType.DocumentUploaded => "Tải chứng từ / tài liệu",
            AuditEventType.DocumentDownloaded => "Tải chứng từ về máy",
            AuditEventType.DocumentDeleted => "Xóa chứng từ",
            AuditEventType.PriceListCreated => "Tạo bảng giá mới",
            AuditEventType.PriceListApproved => "Phê duyệt bảng giá",
            AuditEventType.DatabaseBackupCreated => "Tạo bản sao lưu hệ thống",
            AuditEventType.DatabaseBackupRestored => "Khôi phục dữ liệu hệ thống",
            _ => !string.IsNullOrEmpty(log.TargetEntity) ? $"{log.EventType} ({log.TargetEntity})" : log.EventType.ToString()
        };
    }

    private static string FormatAuditEventDescription(AuditLog log)
    {
        if (string.IsNullOrWhiteSpace(log.Details))
        {
            if (!string.IsNullOrEmpty(log.TargetEntity) && !string.IsNullOrEmpty(log.TargetId))
            {
                return $"{log.TargetEntity} #{log.TargetId}";
            }
            return string.Empty;
        }

        var text = log.Details.Trim();

        if ((text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("[") && text.EndsWith("]")))
        {
            try
            {
                using var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("Summary", out var sum) && sum.ValueKind == JsonValueKind.String)
                        return sum.GetString()!;
                    if (root.TryGetProperty("Message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        return msg.GetString()!;
                    if (root.TryGetProperty("Note", out var note) && note.ValueKind == JsonValueKind.String)
                        return note.GetString()!;
                    if (root.TryGetProperty("Description", out var desc) && desc.ValueKind == JsonValueKind.String)
                        return desc.GetString()!;

                    var summaries = new List<string>();
                    foreach (var prop in root.EnumerateObject())
                    {
                        var propName = prop.Name;
                        if (propName.EndsWith("Id", StringComparison.OrdinalIgnoreCase) ||
                            propName.Equals("Timestamp", StringComparison.OrdinalIgnoreCase) ||
                            propName.Equals("Scope", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        if (prop.Value.ValueKind == JsonValueKind.String)
                        {
                            summaries.Add($"{propName}: {prop.Value.GetString()}");
                        }
                        else if (prop.Value.ValueKind == JsonValueKind.Number)
                        {
                            summaries.Add($"{propName}: {prop.Value}");
                        }
                    }

                    if (summaries.Count > 0)
                    {
                        return string.Join(" • ", summaries);
                    }
                }
            }
            catch
            {
                // Fallback
            }
        }

        var cleaned = text;
        cleaned = cleaned.Replace("Manual invoice created", "Tạo hóa đơn thủ công");
        cleaned = cleaned.Replace("Invoice created from SO", "Tạo hóa đơn từ đơn bán hàng");
        cleaned = cleaned.Replace("Draft invoice updated", "Cập nhật hóa đơn nháp");
        cleaned = cleaned.Replace("Invoice issued", "Phát hành hóa đơn thành công");
        cleaned = cleaned.Replace("Draft invoice deleted", "Xóa hóa đơn nháp");
        cleaned = cleaned.Replace("PDF exported", "Xuất tệp PDF thành công");
        cleaned = cleaned.Replace("Draft order created", "Tạo đơn hàng nháp mới");
        cleaned = cleaned.Replace("Draft order updated", "Cập nhật đơn hàng nháp");
        cleaned = cleaned.Replace("Order confirmed", "Xác nhận đơn hàng thành công");
        cleaned = cleaned.Replace("Order cancelled", "Hủy đơn hàng");

        return cleaned;
    }

    private static string? ResolveTargetUrl(string? targetEntity)
    {
        if (string.IsNullOrEmpty(targetEntity)) return null;

        return targetEntity.ToLowerInvariant() switch
        {
            "salesorder" => "/ban-hang/don-ban-hang",
            "invoice" => "/ban-hang/hoa-don",
            "quotation" => "/ban-hang/bao-gia",
            "product" => "/danh-muc/san-pham",
            "customer" => "/doi-tac/khach-hang",
            "supplier" => "/doi-tac/nha-cung-cap",
            "contract" => "/hop-dong",
            "warehouse" or "warehousevoucher" => "/kho/phieu-kho",
            "employeerequest" or "employee" => "/nhan-vien/duyet-ho-so",
            "pricelist" => "/danh-muc/bang-gia",
            _ => null
        };
    }
}
