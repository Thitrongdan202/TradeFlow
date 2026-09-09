using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;

namespace TradeFlow.Domain.Entities.Sales;

public class InvoiceItem : AuditableEntity<int>
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;
    
    public int? ProductId { get; set; }
    public Product? Product { get; set; }
    
    // Snapshots
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    
    // Values
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
