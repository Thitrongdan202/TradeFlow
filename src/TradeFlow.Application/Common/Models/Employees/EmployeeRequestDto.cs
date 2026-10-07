using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Employees;

public class EmployeeRequestDto
{
    public int Id { get; set; }
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
    public string? DepartmentName { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public string? WorkingBranch { get; set; }
    public string? PayrollBranch { get; set; }
    public DateTime? StartDate { get; set; }
    public string? BankInformation { get; set; }
    public string? Notes { get; set; }
    public string? ProposedUserId { get; set; }

    public EmployeeRequestStatus Status { get; set; } = EmployeeRequestStatus.Pending;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ReviewNotes { get; set; }
    public int? CreatedEmployeeId { get; set; }
    public DateTime CreatedAt { get; set; }
}
