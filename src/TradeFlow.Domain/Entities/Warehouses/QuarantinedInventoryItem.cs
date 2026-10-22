using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Quản lý theo dõi chi tiết hàng lỗi / hàng cách ly trong kho (cách ly khỏi tồn khả dụng bán)
/// </summary>
public class QuarantinedInventoryItem : AuditableEntity<int>
{
    /// <summary>Mã theo dõi cách ly (CL-2026-0001) - duy nhất</summary>
    public string Code { get; set; } = string.Empty;

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    /// <summary>Lý do hàng bị lỗi / phải cách ly</summary>
    public string DefectReason { get; set; } = string.Empty;

    public DateTime DateIdentified { get; set; } = DateTime.UtcNow;

    /// <summary>Nguồn gốc phát hiện: "Stocktaking", "CustomerReturn", "InternalDamage", "SupplierDefect"</summary>
    public string SourceType { get; set; } = "InternalDamage";

    /// <summary>Mã tham chiếu nguồn (KK-..., KTH-..., PNK-...)</summary>
    public string? SourceReference { get; set; }

    public string? ReportedByName { get; set; }

    public QuarantineStatus Status { get; set; } = QuarantineStatus.InQuarantine;

    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByName { get; set; }

    /// <summary>Phiếu kho liên quan khi giải tỏa hàng cách ly (xuất hủy hoặc phục hồi)</summary>
    public int? ResultingVoucherId { get; set; }
    public WarehouseVoucher? ResultingVoucher { get; set; }
}
