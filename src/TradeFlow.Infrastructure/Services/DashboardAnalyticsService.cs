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

    public async Task<DashboardKpiModel> GetKpisAsync(int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var today = now.Date;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var prevMonthStart = startOfMonth.AddMonths(-1);
        var prevMonthEnd = startOfMonth.AddTicks(-1);

        // Revenue this month: check Invoices primary, or confirmed SalesOrders
        var invoiceMonthRevenue = await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= startOfMonth)
            .SumAsync(i => (decimal?)i.GrandTotal, cancellationToken) ?? 0m;

        var salesOrderMonthRevenue = await db.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= startOfMonth && o.Status != SalesOrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.GrandTotal, cancellationToken) ?? 0m;

        var monthRevenue = invoiceMonthRevenue > 0 ? invoiceMonthRevenue : salesOrderMonthRevenue;

        // Revenue previous month for growth comparison
        var prevInvoiceRev = await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= prevMonthStart && i.InvoiceDate <= prevMonthEnd)
            .SumAsync(i => (decimal?)i.GrandTotal, cancellationToken) ?? 0m;

        var prevSalesOrderRev = await db.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= prevMonthStart && o.OrderDate <= prevMonthEnd && o.Status != SalesOrderStatus.Cancelled)
            .SumAsync(o => (decimal?)o.GrandTotal, cancellationToken) ?? 0m;

        var prevMonthRevenue = prevInvoiceRev > 0 ? prevInvoiceRev : prevSalesOrderRev;

        decimal? growthPercent = null;
        if (prevMonthRevenue > 0)
        {
            growthPercent = Math.Round(((monthRevenue - prevMonthRevenue) / prevMonthRevenue) * 100m, 1);
        }

        // Sales Orders count
        var todayOrdersCount = await db.SalesOrders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= today, cancellationToken);

        var monthOrdersCount = await db.SalesOrders
            .AsNoTracking()
            .CountAsync(o => o.OrderDate >= startOfMonth, cancellationToken);

        var totalOrdersCount = await db.SalesOrders
            .AsNoTracking()
            .CountAsync(cancellationToken);

        // Inventory Stock counts
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

        // Active customers & products
        var activeCustomersCount = await db.Customers
            .AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        var activeProductsCount = await db.Products
            .AsNoTracking()
            .CountAsync(p => p.IsActive, cancellationToken);

        var outOfStockItemsCount = Math.Max(0, activeProductsCount - inStockItemsCount);

        return new DashboardKpiModel
        {
            MonthRevenue = monthRevenue,
            PreviousMonthRevenue = prevMonthRevenue,
            RevenueGrowthPercent = growthPercent,
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

    public async Task<DashboardChartDataModel> GetSalesChartDataAsync(string period = "30d", int? warehouseId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        var now = DateTime.UtcNow;
        DateTime startDate;
        DateTime endDate = now.Date.AddDays(1).AddTicks(-1);

        switch (period.ToLowerInvariant())
        {
            case "7d":
                startDate = now.Date.AddDays(-6);
                break;
            case "this_month":
                startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                break;
            case "30d":
            default:
                period = "30d";
                startDate = now.Date.AddDays(-29);
                break;
        }

        // Query Invoices in range
        var invoices = await db.Invoices
            .AsNoTracking()
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate)
            .Select(i => new { Date = i.InvoiceDate.Date, i.GrandTotal })
            .ToListAsync(cancellationToken);

        // Query SalesOrders in range
        var orders = await db.SalesOrders
            .AsNoTracking()
            .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate && o.Status != SalesOrderStatus.Cancelled)
            .Select(o => new { Date = o.OrderDate.Date, o.GrandTotal })
            .ToListAsync(cancellationToken);

        // Aggregate by date
        var invoiceMap = invoices
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.GrandTotal));

        var orderMap = orders
            .GroupBy(x => x.Date)
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), Revenue = g.Sum(x => x.GrandTotal) });

        var points = new List<DashboardChartPointModel>();
        var curDate = startDate.Date;
        decimal totalRevenue = 0;
        int totalOrders = 0;
        decimal maxDailyRevenue = 0;
        int maxDailyOrders = 0;

        while (curDate <= now.Date)
        {
            var invRev = invoiceMap.GetValueOrDefault(curDate, 0m);
            var ordData = orderMap.GetValueOrDefault(curDate, new { Count = 0, Revenue = 0m });

            // If invoices exist on this day, use invoice revenue; else use sales order revenue
            var dailyRevenue = invRev > 0 ? invRev : ordData.Revenue;
            var dailyOrders = ordData.Count;

            totalRevenue += dailyRevenue;
            totalOrders += dailyOrders;
            if (dailyRevenue > maxDailyRevenue) maxDailyRevenue = dailyRevenue;
            if (dailyOrders > maxDailyOrders) maxDailyOrders = dailyOrders;

            points.Add(new DashboardChartPointModel
            {
                Date = curDate,
                DateLabel = curDate.ToString("dd/MM"),
                Revenue = dailyRevenue,
                OrderCount = dailyOrders
            });

            curDate = curDate.AddDays(1);
        }

        return new DashboardChartDataModel
        {
            Period = period,
            TotalPeriodRevenue = totalRevenue,
            TotalPeriodOrders = totalOrders,
            MaxDailyRevenue = maxDailyRevenue,
            MaxDailyOrders = maxDailyOrders,
            Points = points
        };
    }

    public async Task<List<DashboardBestSellingProductModel>> GetBestSellingProductsAsync(
        string period = "30d",
        int limit = 5,
        int? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

        // Aggregate from SalesOrderItems
        var orderItemsQuery = db.SalesOrderItems
            .AsNoTracking()
            .Include(i => i.Product)
                .ThenInclude(p => p.Images)
            .Where(i => i.SalesOrder.Status != SalesOrderStatus.Cancelled);

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

        // Low stock alerts: top 5 items with lowest stock > 0 and < 15
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

        // Warehouse cards
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
                // Fallback to text
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
