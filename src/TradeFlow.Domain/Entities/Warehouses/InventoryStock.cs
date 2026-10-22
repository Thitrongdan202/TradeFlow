using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Số dư tồn kho thực tế theo Kho, Sản phẩm và Tình trạng phẩm chất
/// </summary>
public class InventoryStock : AuditableEntity<int>
{
    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>
    /// Phẩm chất hàng tồn: Saleable (Hàng tốt khả dụng), Usable (Hàng cũ/trưng bày), Defective (Hàng lỗi/cách ly)
    /// </summary>
    public StockCondition Condition { get; set; } = StockCondition.Saleable;

    /// <summary>
    /// Số lượng tồn kho hiện hành
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Thời điểm phát sinh giao dịch biến động kho gần nhất
    /// </summary>
    public DateTime? LastMovementAt { get; set; }
}
