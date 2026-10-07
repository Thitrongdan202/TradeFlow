using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Employees;

/// <summary>Chức danh / Vị trí công việc</summary>
public class Position : AuditableEntity<int>
{
    public Position() { }

    /// <summary>Mã chức danh (duy nhất, VD: LEADER, STAFF, MANAGER)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên chức danh (VD: Trưởng nhóm, Nhân viên, Quản lý)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả chức danh</summary>
    public string? Description { get; set; }

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Employee> Employees { get; set; } = [];
}
