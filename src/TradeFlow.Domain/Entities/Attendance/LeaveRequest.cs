using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Đơn xin nghỉ phép của nhân viên</summary>
public class LeaveRequest : AuditableEntity<int>
{
    public LeaveRequest() { }

    /// <summary>FK: Nhân viên xin nghỉ</summary>
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>Loại nghỉ phép</summary>
    public LeaveType LeaveType { get; set; } = LeaveType.AnnualLeave;

    /// <summary>Ngày bắt đầu nghỉ</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Ngày kết thúc nghỉ</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Tổng số ngày nghỉ đề xuất (hỗ trợ nghỉ nửa ngày: 0.5 ngày)</summary>
    public decimal TotalDays { get; set; } = 1.0m;

    /// <summary>Lý do xin nghỉ</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Tệp đính kèm (giấy khám bệnh viện, giấy mời, v.v.)</summary>
    public string? AttachmentPath { get; set; }

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
