using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Bản ghi chấm công theo ngày của nhân viên</summary>
public class AttendanceRecord : AuditableEntity<int>
{
    public AttendanceRecord() { }

    /// <summary>FK: Nhân viên chấm công</summary>
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>FK: Ca làm việc được phân công</summary>
    public int? WorkShiftId { get; set; }
    public WorkShift? WorkShift { get; set; }

    /// <summary>Ngày làm việc nghiệp vụ (chuẩn hóa về 00:00:00 theo giờ VN)</summary>
    public DateTime WorkDate { get; set; }

    /// <summary>Thời điểm chấm công vào ca (giờ server UTC)</summary>
    public DateTime? ClockInTime { get; set; }

    /// <summary>Thời điểm chấm công ra ca (giờ server UTC)</summary>
    public DateTime? ClockOutTime { get; set; }

    /// <summary>Kênh/nguồn chấm công vào (Web, Mobile, POS, Face, Manual, v.v.)</summary>
    public string? ClockInSource { get; set; } = "Web";

    /// <summary>Kênh/nguồn chấm công ra</summary>
    public string? ClockOutSource { get; set; } = "Web";

    /// <summary>Thông tin thiết bị khi chấm công vào</summary>
    public string? ClockInDevice { get; set; }

    /// <summary>Thông tin thiết bị khi chấm công ra</summary>
    public string? ClockOutDevice { get; set; }

    /// <summary>Địa chỉ IP mạng khi chấm công vào</summary>
    public string? ClockInIp { get; set; }

    /// <summary>Địa chỉ IP mạng khi chấm công ra</summary>
    public string? ClockOutIp { get; set; }

    /// <summary>Trạng thái chấm công</summary>
    public AttendanceStatus Status { get; set; } = AttendanceStatus.NotClockedIn;

    /// <summary>Tổng thời gian làm việc thực tế (phút, đã trừ giờ nghỉ)</summary>
    public int WorkingMinutes { get; set; } = 0;

    /// <summary>Số phút đi muộn so với quy định ca</summary>
    public int LateMinutes { get; set; } = 0;

    /// <summary>Số phút về sớm so với quy định ca</summary>
    public int EarlyLeaveMinutes { get; set; } = 0;

    /// <summary>Số phút làm thêm giờ (OT) được phê duyệt</summary>
    public int OvertimeMinutes { get; set; } = 0;

    /// <summary>Đánh dấu ca làm đêm (kết thúc sang ngày sau)</summary>
    public bool IsOvernight { get; set; } = false;

    /// <summary>Được điều chỉnh thủ công bởi Quản lý/HR</summary>
    public bool IsManuallyAdjusted { get; set; } = false;

    /// <summary>Ghi chú hoặc lý do</summary>
    public string? Notes { get; set; }

    // Navigation
    public ICollection<AttendanceAdjustmentRequest> AdjustmentRequests { get; set; } = [];
}
