using System;
using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Partners;

/// <summary>
/// Bản ghi nghiệp vụ thanh toán / điều chỉnh / chiết khấu công nợ đối tác (Khách hàng & Nhà cung cấp)
/// </summary>
public class PartnerPayment : AuditableEntity<int>
{
    public PartnerPayment() { }

    /// <summary>Mã chứng từ thanh toán (PT: Phiếu thu, PC: Phiếu chi, DC: Điều chỉnh, CK: Chiết khấu)</summary>
    public string PaymentCode { get; set; } = string.Empty;

    /// <summary>Loại đối tác: 1 = Customer, 2 = Supplier</summary>
    public int PartnerType { get; set; }

    /// <summary>Khóa chính đối tác tương ứng</summary>
    public int PartnerId { get; set; }

    /// <summary>Khóa ngoại tới Customer (nếu là khách hàng)</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Khóa ngoại tới Supplier (nếu là nhà cung cấp)</summary>
    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    /// <summary>Loại nghiệp vụ: Thanh toán, Điều chỉnh, Chiết khấu</summary>
    public DebtTransactionType TransactionType { get; set; } = DebtTransactionType.Payment;

    /// <summary>Ngày giờ phát sinh thanh toán (UTC)</summary>
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    /// <summary>Số tiền giao dịch (luôn là số dương)</summary>
    public decimal Amount { get; set; }

    /// <summary>Phương thức thanh toán (Tiền mặt, Chuyển khoản, Thẻ, Khác)</summary>
    public string PaymentMethod { get; set; } = "Tiền mặt";

    /// <summary>Mã chứng từ gốc liên quan (Hóa đơn, Đơn hàng, hoặc Sổ quỹ tham chiếu)</summary>
    public string? ReferenceCode { get; set; }

    /// <summary>Liên kết hóa đơn bán hàng nếu thanh toán đích danh</summary>
    public int? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    /// <summary>Liên kết đơn đặt hàng nếu thanh toán trước / tạm ứng</summary>
    public int? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>Ghi chú hoặc lý do thanh toán / đối soát</summary>
    public string? Notes { get; set; }
}
