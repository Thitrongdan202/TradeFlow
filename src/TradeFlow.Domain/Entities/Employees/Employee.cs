using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Employees;

/// <summary>Nhân viên</summary>
public class Employee : AuditableEntity<int>
{
    public Employee() { }

    // === Thông tin định danh ===

    /// <summary>Mã nhân viên (NV00001 - duy nhất)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Mã chấm công</summary>
    public string? AttendanceCode { get; set; }

    /// <summary>Họ và tên nhân viên</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Số CMND/CCCD</summary>
    public string? NationalId { get; set; }

    /// <summary>Ngày sinh</summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>Giới tính (Nam / Nữ / Khác)</summary>
    public string? Gender { get; set; }

    /// <summary>Ảnh đại diện (đường dẫn file)</summary>
    public string? AvatarPath { get; set; }

    // === Thông tin liên hệ ===

    /// <summary>Số điện thoại liên hệ</summary>
    public string? Phone { get; set; }

    /// <summary>Email liên hệ</summary>
    public string? Email { get; set; }

    /// <summary>Địa chỉ thường trú / tạm trú</summary>
    public string? Address { get; set; }

    /// <summary>Trang Facebook / Zalo</summary>
    public string? Facebook { get; set; }

    /// <summary>Thiết bị di động / Ghi chú thiết bị</summary>
    public string? MobileDevice { get; set; }

    // === Thông tin công việc ===

    /// <summary>Phòng ban</summary>
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    /// <summary>Chức danh</summary>
    public int? PositionId { get; set; }
    public Position? Position { get; set; }

    /// <summary>Chi nhánh làm việc</summary>
    public string? WorkingBranch { get; set; }

    /// <summary>Chi nhánh trả lương</summary>
    public string? PayrollBranch { get; set; }

    /// <summary>Ngày bắt đầu làm việc</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Trạng thái nhân viên (Đang làm việc / Đã nghỉ)</summary>
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;

    /// <summary>Ngày thôi việc (nếu có)</summary>
    public DateTime? TerminationDate { get; set; }

    /// <summary>Lý do thôi việc</summary>
    public string? TerminationReason { get; set; }

    // === Thông tin tài chính & ngân hàng ===

    /// <summary>Thông tin tài khoản ngân hàng (VD: 24367896789, TPBank, NGUYEN TRONG NGUYEN)</summary>
    public string? BankInformation { get; set; }

    /// <summary>Nợ và tạm ứng</summary>
    public decimal DebtAndAdvance { get; set; } = 0;

    /// <summary>Ghi chú</summary>
    public string? Notes { get; set; }

    // === Liên kết tài khoản TradeFlow ===

    /// <summary>ID tài khoản người dùng đăng nhập hệ thống (tùy chọn)</summary>
    public string? UserId { get; set; }
}
