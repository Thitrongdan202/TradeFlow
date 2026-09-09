using System.ComponentModel.DataAnnotations;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Sales;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    
    public int? SalesOrderId { get; set; }
    public int? CustomerId { get; set; }
    
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyTaxCode { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyBankAccount { get; set; }
    public string? CompanyLogoUrl { get; set; }
    
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
    
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    
    public List<InvoiceItemDto> Items { get; set; } = new();
}

public class InvoiceItemDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
}
