using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

public class Supplier : AuditableEntity<int>
{
    public Supplier() { }

    /// <summary>Mã hệ thống tự sinh (NCC000001) - duy nhất</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Mã nội bộ/mã riêng do doanh nghiệp quản lý (tùy chọn)</summary>
    public string? BusinessCode { get; set; }

    /// <summary>Tên nhà cung cấp</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tên công ty</summary>
    public string? CompanyName { get; set; }

    /// <summary>Mã số thuế</summary>
    public string? TaxCode { get; set; }

    /// <summary>Người liên hệ</summary>
    public string? ContactPerson { get; set; }

    /// <summary>Số điện thoại</summary>
    public string? Phone { get; set; }

    /// <summary>Email</summary>
    public string? Email { get; set; }

    /// <summary>Địa chỉ</summary>
    public string? Address { get; set; }

    /// <summary>Ghi chú</summary>
    public string? Notes { get; set; }

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;
}