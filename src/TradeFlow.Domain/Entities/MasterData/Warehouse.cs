using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

public class Warehouse : AuditableEntity<int>
{
    public Warehouse() { }

    /// <summary>Mã hệ thống tự sinh (KHO000001) - duy nhất</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Mã nội bộ/mã riêng do doanh nghiệp quản lý (tùy chọn)</summary>
    public string? BusinessCode { get; set; }

    /// <summary>Tên kho</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Địa chỉ</summary>
    public string? Address { get; set; }

    /// <summary>Người phụ trách</summary>
    public string? ManagerName { get; set; }

    /// <summary>Số điện thoại</summary>
    public string? Phone { get; set; }

    /// <summary>Ghi chú</summary>
    public string? Notes { get; set; }

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;
}