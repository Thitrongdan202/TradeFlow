using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.Employees;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Phân công ca làm việc cho nhân viên</summary>
public class EmployeeShiftAssignment : AuditableEntity<int>
{
    public EmployeeShiftAssignment() { }

    /// <summary>FK: Nhân viên được phân ca</summary>
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>FK: Ca làm việc được gán</summary>
    public int WorkShiftId { get; set; }
    public WorkShift WorkShift { get; set; } = null!;

    /// <summary>Thứ trong tuần áp dụng (null = áp dụng tất cả ngày làm việc)</summary>
    public DayOfWeek? DayOfWeek { get; set; }

    /// <summary>Ngày cụ thể áp dụng (áp dụng cho ca đặc thù/thay ca ngày cụ thể)</summary>
    public DateTime? SpecificDate { get; set; }

    /// <summary>Thời điểm bắt đầu có hiệu lực</summary>
    public DateTime EffectiveFrom { get; set; }

    /// <summary>Thời điểm hết hiệu lực (null = vô thời hạn)</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Trạng thái kích hoạt</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Người phân công ca làm</summary>
    public string? AssignedBy { get; set; }

    /// <summary>Ghi chú phân ca</summary>
    public string? Notes { get; set; }
}
