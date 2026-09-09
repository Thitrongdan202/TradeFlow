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

    /// <summary>Website</summary>
    public string? Website { get; set; }

    /// <summary>Logo (đường dẫn hoặc tên file, không lưu binary)</summary>
    public string? LogoPath { get; set; }

    /// <summary>Chữ ký hoặc con dấu (đường dẫn)</summary>
    public string? SignaturePath { get; set; }
}
