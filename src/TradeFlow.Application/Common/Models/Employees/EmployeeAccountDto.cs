using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Employees;

/// <summary>
/// Chi tiết tài khoản TradeFlow được gắn với nhân viên.
/// </summary>
public class EmployeeAccountDetailsDto
{
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }

    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? Role { get; set; }
    public UserStatus? Status { get; set; }
    public bool MustChangePassword { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? PasswordChangedAt { get; set; }

    public bool HasAccount => !string.IsNullOrEmpty(UserId);
    public bool IsLocked => Status == UserStatus.Locked;
    public bool IsTerminated => Status == UserStatus.Terminated;
    public bool IsDisabled => Status == UserStatus.Disabled;
    public bool IsActive => Status == UserStatus.Active;
}

/// <summary>
/// Yêu cầu cấp tài khoản đăng nhập cho nhân viên.
/// </summary>
public class ProvisionEmployeeAccountCommand
{
    public int EmployeeId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = "Employee";
    public string? Notes { get; set; }
}

/// <summary>
/// Kết quả cấp tài khoản ban đầu chứa mật khẩu tạm một lần duy nhất.
/// </summary>
public class ProvisionAccountResultDto
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    /// <summary>
    /// Mật khẩu tạm thời một lần duy nhất. Không lưu plaintext trong DB, không log.
    /// </summary>
    public string? TemporaryPassword { get; set; }
    public string? Role { get; set; }
}

/// <summary>
/// Kết quả đặt lại mật khẩu chứa mật khẩu tạm một lần duy nhất.
/// </summary>
public class ResetEmployeePasswordResultDto
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }
    public string? UserName { get; set; }
    /// <summary>
    /// Mật khẩu tạm thời một lần duy nhất. Không lưu plaintext trong DB, không log.
    /// </summary>
    public string? TemporaryPassword { get; set; }
}

/// <summary>
/// Lệnh cập nhật vai trò tài khoản nhân viên.
/// </summary>
public class UpdateEmployeeRoleCommand
{
    public int EmployeeId { get; set; }
    public string NewRole { get; set; } = string.Empty;
}
