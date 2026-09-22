using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;

namespace TradeFlow.Domain.Entities.Sales;

public class QuotationItem : AuditableEntity<int>
{
    public int QuotationId { get; set; }
    public Quotation Quotation { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    // Snapshots
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;

    // Values
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Price before VAT
    public string PriceSource { get; set; } = string.Empty;
    public decimal DiscountRate { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; } // (Quantity * UnitPrice) - DiscountAmount

    public int SortOrder { get; set; } = 1;
    public string? Notes { get; set; }
}
