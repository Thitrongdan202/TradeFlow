using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Employees;

public class EmployeeDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? AttendanceCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? AvatarPath { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Facebook { get; set; }
    public string? MobileDevice { get; set; }

    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? PositionId { get; set; }
    public string? PositionName { get; set; }
    public string? WorkingBranch { get; set; }
    public string? PayrollBranch { get; set; }
    public DateTime? StartDate { get; set; }
    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }

    public string? BankInformation { get; set; }
    public decimal DebtAndAdvance { get; set; }
    public string? Notes { get; set; }

    public string? UserId { get; set; }
    public string? UserName { get; set; }

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
