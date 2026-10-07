using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Employees;

/// <summary>Yêu cầu thêm nhân viên mới (cần duyệt)</summary>
public class EmployeeRequest : AuditableEntity<int>
{
    public EmployeeRequest() { }

    // === Thông tin nhân viên đề xuất ===
    public string FullName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? AttendanceCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public int? PositionId { get; set; }
    public Position? Position { get; set; }
    public string? WorkingBranch { get; set; }
    public string? PayrollBranch { get; set; }
    public DateTime? StartDate { get; set; }
    public string? BankInformation { get; set; }
    public string? Notes { get; set; }
    public string? ProposedUserId { get; set; }

    // === Thông tin phê duyệt ===
    public EmployeeRequestStatus Status { get; set; } = EmployeeRequestStatus.Pending;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ReviewNotes { get; set; }

    /// <summary>ID nhân viên được tạo sau khi duyệt thành công</summary>
    public int? CreatedEmployeeId { get; set; }
    public Employee? CreatedEmployee { get; set; }
}
