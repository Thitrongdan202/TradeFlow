using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Attendance;

/// <summary>Ca làm việc trong hệ thống TradeFlow</summary>
public class WorkShift : AuditableEntity<int>
{
    public WorkShift() { }

    /// <summary>Mã ca (VD: CA-HC, CA-SANG, CA-CHIEU, CA-DEM)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên ca làm việc</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Phân loại ca làm việc</summary>
    public WorkShiftType ShiftType { get; set; } = WorkShiftType.StandardOffice;

    /// <summary>Giờ bắt đầu ca làm</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Giờ kết thúc ca làm</summary>
    public TimeSpan EndTime { get; set; }

    /// <summary>Thời gian nghỉ giữa ca (phút, ví dụ nghỉ trưa 90 phút)</summary>
    public int BreakMinutes { get; set; } = 60;

    /// <summary>Khoảng thời gian ân hạn đi muộn không bị tính vi phạm (phút, VD: 15 phút)</summary>
    public int GracePeriodMinutes { get; set; } = 15;

    /// <summary>Thời gian làm việc linh hoạt (phút)</summary>
    public int FlexibleMinutes { get; set; } = 0;

    /// <summary>Ca làm qua đêm (kết thúc vào ngày hôm sau)</summary>
    public bool IsOvernight { get; set; } = false;

    /// <summary>Trạng thái hoạt động</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Mã màu đại diện trên lịch (Hex code, VD: #2563eb)</summary>
    public string Color { get; set; } = "#2563eb";

    /// <summary>Phạm vi áp dụng (Khối văn phòng, Khối bán hàng, Kho vận, v.v.)</summary>
    public string? AppliesTo { get; set; }

    /// <summary>Ghi chú ca làm</summary>
    public string? Notes { get; set; }

    // Navigation
    public ICollection<EmployeeShiftAssignment> ShiftAssignments { get; set; } = [];
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = [];
}
