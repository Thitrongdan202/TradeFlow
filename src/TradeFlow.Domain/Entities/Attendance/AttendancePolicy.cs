using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Chính sách và quy tắc chấm công toàn doanh nghiệp</summary>
public class AttendancePolicy : AuditableEntity<int>
{
    public AttendancePolicy() { }

    /// <summary>Tên chính sách (VD: Quy chế chấm công Công ty 2026)</summary>
    public string PolicyName { get; set; } = "Quy chế chấm công mặc định";

    /// <summary>Khoảng thời gian ân hạn đi muộn không phạt (phút)</summary>
    public int LateGraceMinutes { get; set; } = 15;

    /// <summary>Thời gian về sớm cho phép (phút)</summary>
    public int EarlyLeaveToleranceMinutes { get; set; } = 10;

    /// <summary>Ngưỡng đi muộn tính nửa ngày công (phút, VD: muộn trên 120 phút)</summary>
    public int HalfDayLateMinutes { get; set; } = 120;

    /// <summary>Cho phép nhân viên tự gửi yêu cầu giải trình / điều chỉnh công</summary>
    public bool AllowSelfAdjustment { get; set; } = true;

    /// <summary>Yêu cầu Quản lý trực tiếp phê duyệt trước khi HR xác nhận</summary>
    public bool RequireManagerApproval { get; set; } = true;

    /// <summary>Yêu cầu Phòng Nhân sự (HR) phê duyệt cuối cùng</summary>
    public bool RequireHrApproval { get; set; } = true;

    /// <summary>Giờ tự động chốt ra ca nếu nhân viên quên chấm ra (VD: 23:59:00)</summary>
    public TimeSpan? AutoClockOutTime { get; set; }

    /// <summary>Danh sách EmployeeId được miễn trừ chấm công (cách nhau dấu phẩy, VD: 1,5,10)</summary>
    public string? ExemptEmployeeIds { get; set; }

    /// <summary>Chính sách đang có hiệu lực áp dụng</summary>
    public bool IsActive { get; set; } = true;
}
