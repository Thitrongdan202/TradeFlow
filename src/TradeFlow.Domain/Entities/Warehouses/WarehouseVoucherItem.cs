using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Chi tiết dòng hàng hóa trên chứng từ kho (Phiếu nhập / Phiếu xuất)
/// </summary>
public class WarehouseVoucherItem : AuditableEntity<int>
{
    public int WarehouseVoucherId { get; set; }
    public WarehouseVoucher? WarehouseVoucher { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    public StockCondition Condition { get; set; } = StockCondition.Saleable;

    /// <summary>Số lượng theo chứng từ / yêu cầu</summary>
    public decimal DocumentQuantity { get; set; }

    /// <summary>Số lượng thực nhập hoặc thực xuất</summary>
    public decimal ActualQuantity { get; set; }

    /// <summary>Đơn giá vốn / giá trị hạch toán</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Thành tiền = ActualQuantity * UnitPrice</summary>
    public decimal LineTotal { get; set; }

    public string? Notes { get; set; }
}
