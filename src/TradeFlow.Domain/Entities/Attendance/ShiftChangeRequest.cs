using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Yêu cầu đổi ca làm việc</summary>
public class ShiftChangeRequest : AuditableEntity<int>
{
    public ShiftChangeRequest() { }

    /// <summary>FK: Nhân viên đề nghị đổi ca</summary>
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Ngày áp dụng đổi ca</summary>
    public DateTime TargetDate { get; set; }

    /// <summary>FK: Ca làm việc hiện tại</summary>
    public int CurrentWorkShiftId { get; set; }
    public WorkShift CurrentWorkShift { get; set; } = null!;

    /// <summary>FK: Ca làm việc mong muốn đổi sang</summary>
    public int ProposedWorkShiftId { get; set; }
    public WorkShift ProposedWorkShift { get; set; } = null!;

    /// <summary>FK: Nhân viên đổi ca chéo cùng (nếu có)</summary>
    public int? SubstituteEmployeeId { get; set; }
    public Employee? SubstituteEmployee { get; set; }

    /// <summary>Lý do xin đổi ca</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Trạng thái duyệt</summary>
    public AttendanceRequestStatus Status { get; set; } = AttendanceRequestStatus.Pending;

    /// <summary>ID người phê duyệt</summary>
    public string? ReviewedByUserId { get; set; }

    /// <summary>Họ tên người phê duyệt</summary>
    public string? ReviewedByUserName { get; set; }

    /// <summary>Thời điểm phê duyệt (UTC)</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Ghi chú phản hồi của người phê duyệt</summary>
    public string? ReviewNotes { get; set; }
}
