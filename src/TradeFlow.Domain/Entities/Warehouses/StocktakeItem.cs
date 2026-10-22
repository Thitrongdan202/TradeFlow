using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Warehouses;

/// <summary>
/// Chi tiết kiểm đếm từng mặt hàng trong đợt kiểm kê
/// </summary>
public class StocktakeItem : AuditableEntity<int>
{
    public int StocktakeSessionId { get; set; }
    public StocktakeSession? StocktakeSession { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    /// <summary>Số lượng tồn sổ sách tại thời điểm chốt (Cutoff Book Stock)</summary>
    public decimal BookQuantity { get; set; }

    /// <summary>Số lượng thực tế kiểm đếm được</summary>
    public decimal CountedQuantity { get; set; }

    /// <summary>Chênh lệch = CountedQuantity - BookQuantity (Dương: thừa, Âm: thiếu, 0: khớp)</summary>
    public decimal VarianceQuantity { get; set; }

    /// <summary>Đánh giá phẩm chất hàng thực tế đếm được</summary>
    public StockCondition Condition { get; set; } = StockCondition.Saleable;

    /// <summary>Đơn giá vốn ước tính để định giá trị chênh lệch</summary>
    public decimal UnitCost { get; set; }

    /// <summary>Giá trị chênh lệch = VarianceQuantity * UnitCost</summary>
    public decimal VarianceValue { get; set; }

    /// <summary>Lý do giải trình chênh lệch</summary>
    public string? DiscrepancyReason { get; set; }

    public string? Notes { get; set; }
}
