using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Nhật ký theo dõi thao tác và biến động dữ liệu chấm công</summary>
public class AttendanceAuditLog : Entity<int>
{
    public AttendanceAuditLog() { }

    /// <summary>FK: Bản ghi chấm công liên quan (nếu có)</summary>
    public int? AttendanceRecordId { get; set; }

    /// <summary>FK: Bản ghi yêu cầu liên quan (nếu có)</summary>
    public int? RequestId { get; set; }

    /// <summary>Loại hành động (CLOCK_IN, CLOCK_OUT, REQUEST_ADJUSTMENT, APPROVE, REJECT, MANUAL_EDIT)</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>ID tài khoản thực hiện</summary>
    public string? ChangedByUserId { get; set; }

    /// <summary>Họ tên hoặc username người thực hiện</summary>
    public string? ChangedByUserName { get; set; }

    /// <summary>Thời điểm thao tác (UTC)</summary>
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Dữ liệu trước khi sửa (JSON)</summary>
    public string? OldValuesJson { get; set; }

    /// <summary>Dữ liệu sau khi sửa (JSON)</summary>
    public string? NewValuesJson { get; set; }

    /// <summary>Địa chỉ IP người thao tác</summary>
    public string? IpAddress { get; set; }

    /// <summary>User-Agent thiết bị</summary>
    public string? UserAgent { get; set; }
}
