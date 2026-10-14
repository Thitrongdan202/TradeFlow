using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;

namespace TradeFlow.Domain.Entities.Contracts;

/// <summary>
/// Chi tiết hạng mục hàng hóa / thiết bị / dịch vụ trong hợp đồng thương mại.
/// </summary>
public class ContractItem : AuditableEntity<int>
{
    public int ContractId { get; set; }
    public Contract Contract { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    /// <summary>Mã hàng hóa snapshot tại thời điểm lập hợp đồng</summary>
    public string ProductCode { get; set; } = string.Empty;

    /// <summary>Tên hàng hóa snapshot</summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>Đơn vị tính (Bộ, Cái, Chiếc...)</summary>
    public string Unit { get; set; } = "Cái";

    /// <summary>Số lượng</summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>Đơn giá thỏa thuận</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Chiết khấu cho từng dòng (nếu có)</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Thuế suất VAT (%)</summary>
    public decimal VatRate { get; set; } = 10;

    /// <summary>Tiền thuế</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>Thành tiền (sau chiết khấu + thuế hoặc theo điều kiện)</summary>
    public decimal LineTotal { get; set; }

    /// <summary>Quy cách, tiêu chuẩn kỹ thuật (kích thước, tâm xả, men sứ nano, phụ kiện đi kèm...)</summary>
    public string? Specifications { get; set; }

    /// <summary>Thứ tự hiển thị trên phụ lục / bảng danh mục hàng</summary>
    public int SortOrder { get; set; }

    public string? Note { get; set; }
}
