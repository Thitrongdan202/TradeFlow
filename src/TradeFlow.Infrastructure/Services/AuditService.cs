using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Concrete implementation of IAuditService.
/// Writes audit log entries to the database.
/// IMPORTANT: Never logs plaintext passwords.
/// </summary>
public class AuditService : IAuditService
{
    private readonly Persistence.TradeFlowDbContext _dbContext;
    private readonly IUserActivityBroadcaster? _broadcaster;

    public AuditService(
        Persistence.TradeFlowDbContext dbContext,
        IUserActivityBroadcaster? broadcaster = null)
    {
        _dbContext = dbContext;
        _broadcaster = broadcaster;
    }

    public async Task LogAsync(
        AuditEventType eventType,
        string? performedBy = null,
        string? targetEntity = null,
        string? targetId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog(
            eventType,
            performedBy,
            targetEntity,
            targetId,
            details,
            ipAddress,
            userAgent);

        _dbContext.AuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast non-sensitive operational activity in real time
        if (_broadcaster != null && IsOperationalEvent(eventType))
        {
            var title = GetEventTitle(eventType);
            var desc = GetEventDescription(entry);
            var targetUrl = GetTargetUrl(targetEntity);

            _broadcaster.Broadcast(new ActivityToastNotification(
                EventId: entry.Id.ToString(),
                PerformedBy: performedBy ?? "Người dùng hệ thống",
                EventType: eventType,
                Title: title,
                Description: desc,
                TargetUrl: targetUrl,
                OccurredAt: entry.OccurredAt
            ));
        }
    }

    private static bool IsOperationalEvent(AuditEventType type)
    {
        return type switch
        {
            AuditEventType.SalesOrderCreated or
            AuditEventType.SalesOrderConfirmed or
            AuditEventType.SalesOrderUpdated or
            AuditEventType.InvoiceCreated or
            AuditEventType.InvoiceIssued or
            AuditEventType.QuotationCreated or
            AuditEventType.QuotationConverted or
            AuditEventType.ProductCreated or
            AuditEventType.CustomerCreated or
            AuditEventType.SupplierCreated or
            AuditEventType.ContractCreated or
            AuditEventType.ContractSigned or
            AuditEventType.ContractFinalized or
            AuditEventType.EmployeeRequestSubmitted or
            AuditEventType.EmployeeRequestApproved or
            AuditEventType.WarehouseCreated or
            AuditEventType.PriceListCreated or
            AuditEventType.PriceListApproved => true,
            _ => false
        };
    }

    private static string GetEventTitle(AuditEventType type)
    {
        return type switch
        {
            AuditEventType.SalesOrderCreated => "Đơn bán hàng mới",
            AuditEventType.SalesOrderConfirmed => "Đơn bán hàng đã xác nhận",
            AuditEventType.SalesOrderUpdated => "Cập nhật đơn bán hàng",
            AuditEventType.InvoiceCreated => "Hóa đơn mới được tạo",
            AuditEventType.InvoiceIssued => "Hóa đơn đã phát hành",
            AuditEventType.QuotationCreated => "Báo giá mới được tạo",
            AuditEventType.QuotationConverted => "Báo giá đã chuyển thành đơn hàng",
            AuditEventType.ProductCreated => "Sản phẩm mới trong danh mục",
            AuditEventType.CustomerCreated => "Khách hàng mới được thêm",
            AuditEventType.SupplierCreated => "Nhà cung cấp mới",
            AuditEventType.ContractCreated => "Hợp đồng kinh tế mới",
            AuditEventType.ContractSigned => "Hợp đồng đã ký điện tử",
            AuditEventType.ContractFinalized => "Hợp đồng đã hoàn tất",
            AuditEventType.EmployeeRequestSubmitted => "Đề xuất nhân sự mới",
            AuditEventType.EmployeeRequestApproved => "Đề xuất nhân sự đã duyệt",
            AuditEventType.WarehouseCreated => "Kho hàng mới được thêm",
            AuditEventType.PriceListCreated => "Bảng giá mới được tạo",
            AuditEventType.PriceListApproved => "Bảng giá đã được duyệt",
            _ => "Hoạt động hệ thống mới"
        };
    }

    private static string GetEventDescription(AuditLog entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Details))
        {
            var text = entry.Details.Trim();
            if (text.Length > 90) return text[..87] + "...";
            return text;
        }

        if (!string.IsNullOrEmpty(entry.TargetEntity) && !string.IsNullOrEmpty(entry.TargetId))
        {
            return $"{entry.TargetEntity} #{entry.TargetId}";
        }

        return "Vừa phát sinh giao dịch mới trên hệ thống.";
    }

    private static string? GetTargetUrl(string? targetEntity)
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
