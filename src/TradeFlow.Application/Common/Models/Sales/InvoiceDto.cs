using System.ComponentModel.DataAnnotations;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Sales;

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty; // Mã quản lý nội bộ (Internal code)

    // Pháp lý hóa đơn điện tử (Legal E-Invoice Identifiers)
    public string FormNumber { get; set; } = "1";             // Mẫu số (KHMSHDon)
    public string InvoiceSeries { get; set; } = "1C26TFL";     // Ký hiệu (KHHDon)
    public string InvoiceNo { get; set; } = "00000001";        // Số hóa đơn 8 chữ số (SHDon)
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow; // Ngày lập
    
    public int? SalesOrderId { get; set; }
    public int? CustomerId { get; set; }
    
    // NBan
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyTaxCode { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyBankAccount { get; set; }
    public string? CompanyBankName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    
    // NMua
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCompanyName { get; set; }
    public string? CustomerTaxCode { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerBankAccount { get; set; }
    public string? CustomerBankName { get; set; }
    public string? PaymentMethod { get; set; } = "TM/CK";
    
    public string? Notes { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public InvoiceType Type { get; set; } = InvoiceType.VatInvoice;
    
    // Totals
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }

    // E-Invoice specifics
    public string? TaxAuthorityCode { get; set; } // MCCQT
    public string? QrCodeData { get; set; }        // DLQRCode

    // Signature state
    public DigitalSignatureStatus SignatureStatus { get; set; } = DigitalSignatureStatus.Unsigned;
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    
    public List<InvoiceItemDto> Items { get; set; } = new();

    // Tax breakdown summary (THTTLTSuat)
    public List<TaxBreakdownDto> TaxBreakdowns { get; set; } = new();
}

public class InvoiceItemDto
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int SortOrder { get; set; } = 1;
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

public class TaxBreakdownDto
{
    public decimal TaxRate { get; set; }
    public string TaxRateLabel => $"{TaxRate:0.##}%";
    public decimal AmountBeforeTax { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount => AmountBeforeTax + TaxAmount;
}
