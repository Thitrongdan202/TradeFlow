using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Yêu cầu giải trình / điều chỉnh giờ công</summary>
public class AttendanceAdjustmentRequest : AuditableEntity<int>
{
    public AttendanceAdjustmentRequest() { }

    /// <summary>FK: Nhân viên gửi yêu cầu</summary>
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    /// <summary>FK: Bản ghi chấm công cần điều chỉnh (nếu có)</summary>
    public int? AttendanceRecordId { get; set; }
    public AttendanceRecord? AttendanceRecord { get; set; }

    /// <summary>Loại điều chỉnh</summary>
    public AttendanceAdjustmentType RequestType { get; set; } = AttendanceAdjustmentType.MissedClockIn;

    /// <summary>Ngày cần điều chỉnh công</summary>
    public DateTime TargetDate { get; set; }

    /// <summary>Giờ vào đề xuất (UTC)</summary>
    public DateTime? ProposedClockIn { get; set; }

    /// <summary>Giờ ra đề xuất (UTC)</summary>
    public DateTime? ProposedClockOut { get; set; }

    /// <summary>Lý do giải trình</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Đường dẫn tệp tài liệu / ảnh minh chứng (nếu có)</summary>
    public string? AttachmentPath { get; set; }

    /// <summary>Trạng thái xử lý yêu cầu</summary>
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
