using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Thẻ kho / Sổ chi tiết biến động xuất nhập tồn kho (Immutable audit ledger)
/// </summary>
public class InventoryMovement : AuditableEntity<int>
{
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;

    public InventoryMovementType MovementType { get; set; }

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public StockCondition Condition { get; set; } = StockCondition.Saleable;

    /// <summary>
    /// Số lượng biến động (luôn dương)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Chiều biến động: "IN" (Nhập/tăng) hoặc "OUT" (Xuất/giảm)
    /// </summary>
    public string Direction { get; set; } = "IN";

    /// <summary>
    /// Số tồn trước khi phát sinh giao dịch
    /// </summary>
    public decimal PreviousBalance { get; set; }

    /// <summary>
    /// Số tồn sau khi ghi nhận giao dịch
    /// </summary>
    public decimal NewBalance { get; set; }

    /// <summary>
    /// Đơn giá vốn / giá trị xuất nhập tại thời điểm giao dịch
    /// </summary>
    public decimal UnitCost { get; set; }

    /// <summary>
    /// ID chứng từ kho liên kết (nếu có)
    /// </summary>
    public int? VoucherId { get; set; }
    public WarehouseVoucher? Voucher { get; set; }

    /// <summary>
    /// Số phiếu kho (PNK-..., PXK-...)
    /// </summary>
    public string? VoucherNumber { get; set; }

    /// <summary>
    /// Mã đối tượng nghiệp vụ gốc: Mã đợt kiểm kê, Mã đơn trả hàng, Mã đơn bán hàng, Mã hóa đơn...
    /// </summary>
    public string? SourceReference { get; set; }

    public string? Notes { get; set; }

    public string? CreatedByUserId { get; set; }
    public string? CreatedByName { get; set; }
}
