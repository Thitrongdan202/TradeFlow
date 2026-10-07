using System.ComponentModel.DataAnnotations;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Employees;

public class CreateEmployeeCommand
{
    public string? Code { get; set; }
    public string? AttendanceCode { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên nhân viên")]
    [MaxLength(200, ErrorMessage = "Họ và tên tối đa 200 ký tự")]
    public string FullName { get; set; } = string.Empty;

    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; } = "Nam";
    public string? AvatarPath { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Facebook { get; set; }
    public string? MobileDevice { get; set; }

    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public string? WorkingBranch { get; set; } = "Chi nhánh trung tâm";
    public string? PayrollBranch { get; set; } = "Chi nhánh trung tâm";
    public DateTime? StartDate { get; set; } = DateTime.Today;

    public string? BankInformation { get; set; }
    public decimal DebtAndAdvance { get; set; }
    public string? Notes { get; set; }

    public string? UserId { get; set; }
}

public class UpdateEmployeeCommand
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ và tên nhân viên")]
    [MaxLength(200, ErrorMessage = "Họ và tên tối đa 200 ký tự")]
    public string FullName { get; set; } = string.Empty;

    public string? AttendanceCode { get; set; }
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
    public int? PositionId { get; set; }
    public string? WorkingBranch { get; set; }
    public string? PayrollBranch { get; set; }
    public DateTime? StartDate { get; set; }

    public string? BankInformation { get; set; }
    public decimal DebtAndAdvance { get; set; }
    public string? Notes { get; set; }

    public string? UserId { get; set; }
}

public class RetireEmployeeCommand
{
    public int Id { get; set; }
    public DateTime TerminationDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "Vui lòng nhập lý do thôi việc")]
    public string TerminationReason { get; set; } = string.Empty;
}

public class CreateEmployeeRequestCommand
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên nhân viên đề xuất")]
    [MaxLength(200, ErrorMessage = "Họ và tên tối đa 200 ký tự")]
    public string FullName { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string? AttendanceCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? NationalId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; } = "Nam";
    public string? Address { get; set; }
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public string? WorkingBranch { get; set; } = "Chi nhánh trung tâm";
    public string? PayrollBranch { get; set; } = "Chi nhánh trung tâm";
    public DateTime? StartDate { get; set; } = DateTime.Today;
    public string? BankInformation { get; set; }
    public string? Notes { get; set; }
    public string? ProposedUserId { get; set; }
}

public class ReviewEmployeeRequestCommand
{
    public int RequestId { get; set; }
    public bool Approve { get; set; }
    public string? ReviewNotes { get; set; }
}
