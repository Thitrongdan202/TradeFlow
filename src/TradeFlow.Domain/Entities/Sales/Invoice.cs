using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Sales;

public class Invoice : AuditableEntity<int>
{
    /// <summary>Mã tham chiếu nội bộ (Internal tracking, ví dụ: INV-2026-0001)</summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>Mẫu số hóa đơn (Form No. / KHMSHDon, ví dụ: 1 cho HĐ GTGT, 2 cho HĐ bán hàng)</summary>
    public string FormNumber { get; set; } = "1";

    /// <summary>Ký hiệu hóa đơn (Serial No. / KHHDon, ví dụ: 1C26TFL)</summary>
    public string InvoiceSeries { get; set; } = "1C26TFL";

    /// <summary>Số hóa đơn pháp lý (8 chữ số, ví dụ: 00000001)</summary>
    public string InvoiceNo { get; set; } = "00000001";

    /// <summary>Ngày lập hóa đơn (NLap)</summary>
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }
    
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    
    // Snapshots: Company (Seller - NBan)
    public string CompanyName { get; set; } = string.Empty;
    public string? CompanyTaxCode { get; set; }
    public string? CompanyAddress { get; set; }
    public string? CompanyPhone { get; set; }
    public string? CompanyEmail { get; set; }
    public string? CompanyBankAccount { get; set; }
    public string? CompanyBankName { get; set; }
    public string? CompanyLogoUrl { get; set; }
    
    // Snapshots: Customer (Buyer - NMua)
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
    
    // Totals (TToan)
    public decimal SubTotal { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalTax { get; set; }
    public decimal GrandTotal { get; set; }
    
    // E-Invoice specifics (MCCQT, DLQRCode, DSCKS)
    public string? TaxAuthorityCode { get; set; } // Mã của cơ quan thuế (MCCQT)
    public string? QrCodeData { get; set; }        // Dữ liệu tra cứu QR (DLQRCode)

    // Digital Signature abstraction (DSCKS)
    public DigitalSignatureStatus SignatureStatus { get; set; } = DigitalSignatureStatus.Unsigned;
    public string? SignedBy { get; set; }
    public DateTime? SignedAt { get; set; }
    public string? SignatureValue { get; set; }
    public string? CertificateSubject { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
}
