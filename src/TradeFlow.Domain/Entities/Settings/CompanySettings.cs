using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Settings;

/// <summary>
/// Thông tin công ty - lưu trong PostgreSQL, không hard-code trong UI.
/// </summary>
public class CompanySettings : AuditableEntity<int>
{
    protected CompanySettings() { }

    public CompanySettings(string companyName)
    {
        Id = 1; // Singleton row
        CompanyName = companyName;
    }

    /// <summary>Tên công ty</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Tên giao dịch</summary>
    public string? TradingName { get; set; }

    /// <summary>Mã số thuế</summary>
    public string? TaxCode { get; set; }

    /// <summary>Địa chỉ</summary>
    public string? Address { get; set; }

    /// <summary>Điện thoại</summary>
    public string? Phone { get; set; }

    /// <summary>Email</summary>
    public string? Email { get; set; }
    public string? BankAccount { get; set; }

    /// <summary>Ngân hàng</summary>
    public string? BankName { get; set; }

    /// <summary>Tên chủ tài khoản (Chủ tài khoản ngân hàng nhận tiền)</summary>
    public string? BankAccountHolder { get; set; }

    /// <summary>Website</summary>
    public string? Website { get; set; }

    /// <summary>Logo (đường dẫn hoặc tên file, không lưu binary)</summary>
    public string? LogoPath { get; set; }

    /// <summary>Chữ ký hoặc con dấu (đường dẫn)</summary>
    public string? SignaturePath { get; set; }

    /// <summary>Đường dẫn hình ảnh mã QR thanh toán</summary>
    public string? OrderQrCodePath { get; set; }

    /// <summary>Ghi chú đơn hàng mặc định (Ghi chú KH)</summary>
    public string? DefaultOrderNote { get; set; }

    /// <summary>Ghi chú thuế VAT mặc định (VD: Đơn giá trên chưa bao gồm thuế GTGT (8%).)</summary>
    public string? DefaultVatNote { get; set; }

    /// <summary>Hotline / Zalo liên hệ hiển thị trên đơn đặt hàng</summary>
    public string? OrderHotline { get; set; }

    /// <summary>Ghi chú chân trang 1 (Kiểm tra hàng hóa)</summary>
    public string? OrderFooterNote1 { get; set; }

    /// <summary>Ghi chú chân trang 2 (Chính sách đổi trả)</summary>
    public string? OrderFooterNote2 { get; set; }
}
