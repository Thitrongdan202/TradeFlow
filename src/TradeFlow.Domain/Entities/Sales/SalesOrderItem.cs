using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;

namespace TradeFlow.Domain.Entities.Sales;

public class SalesOrderItem : AuditableEntity<int>
{
    public int SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;
    
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    
    // Snapshots
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    
    // Values
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; } // Price from active price list
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; } = 0; // 0, 5, 8, 10
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; } // (Qty * UnitPrice) - Discount + Tax
}
