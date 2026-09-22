using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Sales;

public class Quotation : AuditableEntity<int>
{
    public string Code { get; set; } = string.Empty; // BG-2026-0001
    public DateTime QuotationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiryDate { get; set; }

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Snapshots for history
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerContactPerson { get; set; }
    public string? CustomerEmail { get; set; }

    public string? SalespersonName { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    // Totals
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal GrandTotal { get; set; }

    // Conversion link to SalesOrder
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public ICollection<QuotationItem> Items { get; set; } = new List<QuotationItem>();
}
