using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Sales;

public class Invoice : AuditableEntity<int>
{
    public string InvoiceNumber { get; set; } = string.Empty; // INV-2026-0001
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    // Snapshots: Company (Seller)
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyTaxCode { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyBankAccount { get; set; }
    public string? CompanyLogoUrl { get; set; }
    
    // Snapshots: Customer (Buyer)
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCompanyName { get; set; }
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerBankAccount { get; set; }
    public string? PaymentMethod { get; set; } = "TM/CK";
    
    public string? Notes { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public InvoiceType Type { get; set; } = InvoiceType.SalesInvoice;
    
    // Totals
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
