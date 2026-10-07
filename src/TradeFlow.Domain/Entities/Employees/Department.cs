using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Employees;

/// <summary>Phòng ban</summary>
public class Department : AuditableEntity<int>
{
    public Department() { }

    /// <summary>Mã phòng ban (duy nhất, VD: KHO, KINHDOANH)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên phòng ban (VD: Kho vận, Kinh doanh)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Mô tả phòng ban</summary>
    public string? Description { get; set; }

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Employee> Employees { get; set; } = [];
}
