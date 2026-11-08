using TradeFlow.Application.Common.Models.Employees;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ quản lý vòng đời tài khoản người dùng gắn với nhân sự (Employee Account Provisioning & RBAC).
/// </summary>
public interface IEmployeeAccountService
{
    /// <summary>
    /// Lấy thông tin tài khoản TradeFlow được liên kết với nhân viên.
    /// </summary>
    Task<EmployeeAccountDetailsDto?> GetAccountByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cấp tài khoản đăng nhập mới cho nhân viên. Sinh mật khẩu tạm một lần duy nhất được hash an toàn.
    /// </summary>
    Task<ProvisionAccountResultDto> ProvisionAccountAsync(ProvisionEmployeeAccountCommand command, string performedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đặt lại mật khẩu tài khoản nhân viên. Sinh mật khẩu tạm mới một lần duy nhất và bắt buộc đổi khi đăng nhập.
    /// </summary>
    Task<ResetEmployeePasswordResultDto> ResetEmployeePasswordAsync(int employeeId, string performedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Khóa hoặc mở khóa tài khoản nhân viên.
    /// </summary>
    Task<bool> ToggleAccountLockAsync(int employeeId, bool isLocked, string performedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật vai trò / phân quyền của tài khoản nhân viên. Tuân thủ phân cấp quyền (Manager không thể cấp Administrator).
    /// </summary>
    Task<bool> UpdateAccountRoleAsync(int employeeId, string newRole, string performedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tự động vô hiệu hóa và thu hồi toàn bộ quyền/phiên đăng nhập khi nhân viên thôi việc.
    /// </summary>
    Task<bool> DeactivateOnEmployeeTerminationAsync(int employeeId, string reason, string performedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách các vai trò mà người thực hiện được phép cấp cho tài khoản khác.
    /// </summary>
    Task<List<string>> GetAssignableRolesAsync(string performedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gợi ý tên đăng nhập hợp lệ và chưa tồn tại dựa trên mã hoặc thông tin nhân viên.
    /// </summary>
    Task<string> SuggestUsernameAsync(int employeeId, CancellationToken cancellationToken = default);
}
