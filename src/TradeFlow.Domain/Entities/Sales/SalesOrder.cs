using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Sales;

public class SalesOrder : AuditableEntity<int>
{
    public string Code { get; set; } = string.Empty; // SO-2026-0001
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveryDate { get; set; }
    
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    
    // Snapshots for history
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhone { get; set; }
    
    public string? Notes { get; set; }
    public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;
    
    // Totals
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    
    public ICollection<SalesOrderItem> Items { get; set; } = new List<SalesOrderItem>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
