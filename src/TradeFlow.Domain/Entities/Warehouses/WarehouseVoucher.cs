using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Chứng từ kho chính thức theo chế độ kế toán (Phiếu nhập kho Mẫu 01-VT, Phiếu xuất kho Mẫu 02-VT...)
/// </summary>
public class WarehouseVoucher : AuditableEntity<int>
{
    public WarehouseVoucherType VoucherType { get; set; } = WarehouseVoucherType.InboundReceipt;

    /// <summary>Số chứng từ (PNK-2026-0001, PXK-2026-0001...) - duy nhất</summary>
    public string VoucherNumber { get; set; } = string.Empty;

    public DateTime VoucherDate { get; set; } = DateTime.UtcNow;

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    /// <summary>Lý do nhập/xuất kho (ví dụ: "Thừa phát hiện khi kiểm kê", "Thiếu phát hiện khi kiểm kê", "Nhập hàng khách trả lại"...)</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Họ tên người giao hàng / người nhận hàng / đơn vị đối tác</summary>
    public string? PartnerName { get; set; }

    /// <summary>Địa chỉ / bộ phận của đối tác hoặc người giao nhận</summary>
    public string? PartnerAddress { get; set; }

    /// <summary>Phân hệ nguồn: "Stocktaking", "Returns", "Quarantine", "Sales", "Warehouse"</summary>
    public string SourceModule { get; set; } = "Warehouse";

    /// <summary>ID bản ghi nguồn</summary>
    public int? SourceRecordId { get; set; }

    /// <summary>Mã tham chiếu nguồn (KK-2026-0001, KTH-2026-0001, SO-2026-0001...)</summary>
    public string? SourceReference { get; set; }

    public WarehouseVoucherStatus Status { get; set; } = WarehouseVoucherStatus.Draft;

    public decimal TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    // Thông tin người lập & phê duyệt
    public string? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
    public string? ApprovedByUserId { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }

    // Ký nhận bàn giao
    public string? WarehouseKeeperName { get; set; }
    public string? DelivererOrReceiverName { get; set; }

    public ICollection<WarehouseVoucherItem> Items { get; set; } = new List<WarehouseVoucherItem>();
    public ICollection<InventoryMovement> Movements { get; set; } = new List<InventoryMovement>();
}
