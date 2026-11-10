using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Employees;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Dịch vụ quản lý vòng đời tài khoản TradeFlow cho nhân viên và phân quyền RBAC dựa trên nhân sự.
/// Sử dụng IServiceScopeFactory để đảm bảo cô lập DbContext tuyệt đối, tránh lỗi concurrency trong Blazor Server.
/// </summary>
public class EmployeeAccountService : IEmployeeAccountService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmployeeAccountService> _logger;

    public EmployeeAccountService(
        IServiceScopeFactory scopeFactory,
        ILogger<EmployeeAccountService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<EmployeeAccountDetailsDto?> GetAccountByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var employee = await context.Employees
            .Include(e => e.Department)
            .Include(e => e.Position)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (employee == null) return null;

        var dto = new EmployeeAccountDetailsDto
        {
            EmployeeId = employee.Id,
            EmployeeCode = employee.Code,
            EmployeeName = employee.FullName,
            DepartmentName = employee.Department?.Name,
            PositionName = employee.Position?.Name,
            UserId = employee.UserId
        };

        if (!string.IsNullOrEmpty(employee.UserId))
        {
            var user = await userManager.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == employee.UserId, cancellationToken);

            if (user != null)
            {
                dto.UserName = user.UserName;
                dto.Email = user.Email;
                dto.Status = user.Status;
                dto.MustChangePassword = user.MustChangePassword;
                dto.CreatedAt = user.CreatedAt;
                dto.CreatedBy = user.CreatedBy;
                dto.LastLoginAt = user.LastLoginAt;
                dto.PasswordChangedAt = user.PasswordChangedAt;
                dto.Role = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "Employee";
            }
        }

        return dto;
    }

    public async Task<ProvisionAccountResultDto> ProvisionAccountAsync(
        ProvisionEmployeeAccountCommand command,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        // 1. Kiểm tra quyền của người thực hiện
        var caller = await userManager.FindByNameAsync(performedBy);
        if (caller == null)
        {
            return new ProvisionAccountResultDto { Succeeded = false, ErrorMessage = "Không xác định được danh tính người thực hiện." };
        }

        var callerRoles = await userManager.GetRolesAsync(caller);
        bool isCallerAdmin = callerRoles.Contains("Administrator");
        bool isCallerManager = callerRoles.Contains("Manager");

        if (!isCallerAdmin && !isCallerManager)
        {
            return new ProvisionAccountResultDto { Succeeded = false, ErrorMessage = "Bạn không có quyền cấp tài khoản cho nhân viên." };
        }

        // Hierarchy rule: Manager không được cấp quyền Administrator
        string targetRole = string.IsNullOrWhiteSpace(command.Role) ? "Employee" : command.Role.Trim();
        if (targetRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase) && !isCallerAdmin)
        {
            return new ProvisionAccountResultDto
            {
                Succeeded = false,
                ErrorMessage = "Chỉ có Quản trị viên cấp cao (Administrator) mới có thể cấp tài khoản quyền Administrator."
            };
        }

        // 2. Kiểm tra tồn tại nhân viên
        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == command.EmployeeId, cancellationToken);
        if (employee == null)
        {
            return new ProvisionAccountResultDto { Succeeded = false, ErrorMessage = $"Không tìm thấy nhân viên ID '{command.EmployeeId}'." };
        }

        if (employee.Status == EmployeeStatus.Retired)
        {
            return new ProvisionAccountResultDto { Succeeded = false, ErrorMessage = "Không thể cấp tài khoản cho nhân viên đã thôi việc." };
        }

        // Kiểm tra xem nhân viên đã có tài khoản liên kết chưa
        if (!string.IsNullOrEmpty(employee.UserId))
        {
            var existingLinkedUser = await userManager.FindByIdAsync(employee.UserId);
            if (existingLinkedUser != null && existingLinkedUser.Status != UserStatus.Terminated)
            {
                return new ProvisionAccountResultDto
                {
                    Succeeded = false,
                    ErrorMessage = $"Nhân viên '{employee.FullName}' đã được liên kết với tài khoản '{existingLinkedUser.UserName}'."
                };
            }
        }

        // 3. Chuẩn hóa & kiểm tra tính duy nhất của Username
        string username = command.UserName.Trim().ToLowerInvariant();
        if (username.Length < 3 || !Regex.IsMatch(username, @"^[a-z0-9._-]+$"))
        {
            return new ProvisionAccountResultDto
            {
                Succeeded = false,
                ErrorMessage = "Tên đăng nhập chỉ được chứa chữ cái viết thường không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang (tối thiểu 3 ký tự)."
            };
        }

        var existingByUsername = await userManager.FindByNameAsync(username);
        if (existingByUsername != null)
        {
            return new ProvisionAccountResultDto
            {
                Succeeded = false,
                ErrorMessage = $"Tên đăng nhập '{username}' đã được sử dụng. Vui lòng chọn tên đăng nhập khác."
            };
        }

        // 4. Sinh mật khẩu tạm thời ngẫu nhiên mạnh
        string tempPassword = GenerateStrongTemporaryPassword();

        // 5. Khởi tạo ApplicationUser với cờ bắt buộc đổi mật khẩu
        string email = !string.IsNullOrWhiteSpace(employee.Email)
            ? employee.Email.Trim()
            : $"{username}@tradeflow.local";

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            FullName = employee.FullName,
            EmployeeId = employee.Id,
            Status = UserStatus.MustChangePassword,
            MustChangePassword = true,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = performedBy
        };

        // Identity CreateAsync tự động băm mật khẩu bằng PBKDF2/Argon2 one-way cryptographic hash
        // Tuyệt đối không lưu plaintext
        var createResult = await userManager.CreateAsync(user, tempPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return new ProvisionAccountResultDto { Succeeded = false, ErrorMessage = $"Lỗi khi tạo tài khoản: {errors}" };
        }

        // 6. Gán vai trò theo quy tắc: Employee luôn là vai trò nền mặc định, role được chọn là vai trò thứ hai (tối đa 2 role)
        if (targetRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
        {
            await userManager.AddToRoleAsync(user, "Administrator");
        }
        else
        {
            // Luôn gán vai trò nền Employee
            if (await roleManager.RoleExistsAsync("Employee"))
            {
                await userManager.AddToRoleAsync(user, "Employee");
            }

            // Nếu người dùng chọn một vai trò khác ngoài Employee, gán làm vai trò thứ hai
            if (!targetRole.Equals("Employee", StringComparison.OrdinalIgnoreCase) && await roleManager.RoleExistsAsync(targetRole))
            {
                await userManager.AddToRoleAsync(user, targetRole);
            }
        }

        // 7. Gắn UserId vào Employee
        employee.UserId = user.Id;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = performedBy;
        await context.SaveChangesAsync(cancellationToken);

        // 8. Ghi log kiểm toán (KHÔNG BAO GIỜ GHI MẬT KHẨU VÀO LOG)
        await auditService.LogAsync(
            AuditEventType.UserCreated,
            performedBy,
            "EmployeeAccount",
            user.UserName,
            $"Cấp tài khoản TradeFlow '{user.UserName}' cho nhân viên '{employee.FullName}' (Mã: {employee.Code}) với vai trò '{targetRole}'. Mật khẩu tạm được sinh và yêu cầu đổi mật khẩu khi đăng nhập lần đầu.");

        return new ProvisionAccountResultDto
        {
            Succeeded = true,
            UserId = user.Id,
            UserName = user.UserName,
            TemporaryPassword = tempPassword,
            Role = targetRole
        };
    }

    public async Task<ResetEmployeePasswordResultDto> ResetEmployeePasswordAsync(
        int employeeId,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee == null || string.IsNullOrEmpty(employee.UserId))
        {
            return new ResetEmployeePasswordResultDto { Succeeded = false, ErrorMessage = "Nhân viên chưa có tài khoản người dùng." };
        }

        var user = await userManager.FindByIdAsync(employee.UserId);
        if (user == null)
        {
            return new ResetEmployeePasswordResultDto { Succeeded = false, ErrorMessage = "Tài khoản liên kết không tồn tại." };
        }

        // Kiểm tra quyền của caller
        var caller = await userManager.FindByNameAsync(performedBy);
        if (caller == null)
        {
            return new ResetEmployeePasswordResultDto { Succeeded = false, ErrorMessage = "Không xác định danh tính người thực hiện." };
        }

        var callerRoles = await userManager.GetRolesAsync(caller);
        bool isCallerAdmin = callerRoles.Contains("Administrator");
        bool isCallerManager = callerRoles.Contains("Manager");

        if (!isCallerAdmin && !isCallerManager)
        {
            return new ResetEmployeePasswordResultDto { Succeeded = false, ErrorMessage = "Bạn không có quyền đặt lại mật khẩu tài khoản." };
        }

        // Manager không được reset mật khẩu của Administrator
        var targetUserRoles = await userManager.GetRolesAsync(user);
        if (targetUserRoles.Contains("Administrator") && !isCallerAdmin)
        {
            return new ResetEmployeePasswordResultDto
            {
                Succeeded = false,
                ErrorMessage = "Người quản lý (Manager) không có quyền đặt lại mật khẩu của Quản trị viên (Administrator)."
            };
        }

        // Sinh mật khẩu mới ngẫu nhiên an toàn
        string newTempPassword = GenerateStrongTemporaryPassword();

        // Đặt lại mật khẩu qua Identity token
        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, newTempPassword);
        if (!resetResult.Succeeded)
        {
            var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
            return new ResetEmployeePasswordResultDto { Succeeded = false, ErrorMessage = $"Lỗi đặt lại mật khẩu: {errors}" };
        }

        // Bắt buộc đổi mật khẩu ở lần đăng nhập tiếp theo
        user.MustChangePassword = true;
        user.Status = UserStatus.MustChangePassword;

        // Vô hiệu hóa toàn bộ phiên đăng nhập hiện tại bằng cách cập nhật SecurityStamp
        await userManager.UpdateSecurityStampAsync(user);
        await userManager.UpdateAsync(user);

        // Ghi log kiểm toán (KHÔNG GHI MẬT KHẨU VÀO LOG)
        await auditService.LogAsync(
            AuditEventType.PasswordChanged,
            performedBy,
            "EmployeeAccount",
            user.UserName,
            $"Đặt lại mật khẩu cho tài khoản '{user.UserName}' của nhân viên '{employee.FullName}'. Yêu cầu đổi mật khẩu lần đầu và thu hồi toàn bộ phiên đăng nhập hiện có.");

        return new ResetEmployeePasswordResultDto
        {
            Succeeded = true,
            TemporaryPassword = newTempPassword,
            UserName = user.UserName
        };
    }

    public async Task<bool> ToggleAccountLockAsync(int employeeId, bool isLocked, string performedBy, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee == null || string.IsNullOrEmpty(employee.UserId)) return false;

        var user = await userManager.FindByIdAsync(employee.UserId);
        if (user == null) return false;

        var caller = await userManager.FindByNameAsync(performedBy);
        var callerRoles = caller != null ? await userManager.GetRolesAsync(caller) : [];
        bool isCallerAdmin = callerRoles.Contains("Administrator");
        var targetUserRoles = await userManager.GetRolesAsync(user);

        // Hierarchy rule: Manager không được khóa tài khoản Administrator
        if (targetUserRoles.Contains("Administrator") && !isCallerAdmin)
        {
            throw new InvalidOperationException("Chỉ Quản trị viên cấp cao mới có quyền khóa tài khoản của Administrator.");
        }

        // Không được khóa tài khoản Admin duy nhất còn hoạt động
        if (isLocked && targetUserRoles.Contains("Administrator"))
        {
            var adminUsers = await userManager.GetUsersInRoleAsync("Administrator");
            int activeAdminCount = adminUsers.Count(u => u.Status == UserStatus.Active && u.Id != user.Id);
            if (activeAdminCount == 0)
            {
                throw new InvalidOperationException("Không thể khóa Quản trị viên (Administrator) đang hoạt động duy nhất trong hệ thống.");
            }
        }

        if (isLocked)
        {
            user.Status = UserStatus.Locked;
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
            // Thu hồi toàn bộ phiên đăng nhập đang hoạt động
            await userManager.UpdateSecurityStampAsync(user);
        }
        else
        {
            user.Status = user.MustChangePassword ? UserStatus.MustChangePassword : UserStatus.Active;
            user.LockoutEnd = null;
        }

        await userManager.UpdateAsync(user);

        await auditService.LogAsync(
            AuditEventType.UserUpdated,
            performedBy,
            "EmployeeAccount",
            user.UserName,
            $"{(isLocked ? "Khóa" : "Mở khóa")} tài khoản '{user.UserName}' của nhân viên '{employee.FullName}'.");

        return true;
    }

    public async Task<bool> UpdateAccountRoleAsync(int employeeId, string newRole, string performedBy, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee == null || string.IsNullOrEmpty(employee.UserId)) return false;

        var user = await userManager.FindByIdAsync(employee.UserId);
        if (user == null) return false;

        var caller = await userManager.FindByNameAsync(performedBy);
        var callerRoles = caller != null ? await userManager.GetRolesAsync(caller) : [];
        bool isCallerAdmin = callerRoles.Contains("Administrator");
        var currentRoles = await userManager.GetRolesAsync(user);

        // Hierarchy rule: Manager không được thăng cấp hoặc giáng cấp Administrator
        if (!isCallerAdmin)
        {
            if (newRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase) || currentRoles.Contains("Administrator"))
            {
                throw new InvalidOperationException("Chỉ Quản trị viên cấp cao (Administrator) mới có quyền thao tác trên vai trò Administrator.");
            }
        }

        // Không được hạ cấp tài khoản Admin duy nhất
        if (currentRoles.Contains("Administrator") && !newRole.Equals("Administrator", StringComparison.OrdinalIgnoreCase))
        {
            var adminUsers = await userManager.GetUsersInRoleAsync("Administrator");
            int activeAdminCount = adminUsers.Count(u => u.Status == UserStatus.Active && u.Id != user.Id);
            if (activeAdminCount == 0)
            {
                throw new InvalidOperationException("Không thể hạ cấp Quản trị viên (Administrator) duy nhất trong hệ thống.");
            }
        }

        // Cập nhật vai trò
        if (currentRoles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        if (await roleManager.RoleExistsAsync(newRole))
        {
            await userManager.AddToRoleAsync(user, newRole);
        }
        else
        {
            await userManager.AddToRoleAsync(user, "Employee");
            newRole = "Employee";
        }

        // Cập nhật SecurityStamp để cập nhật claim trên phiên kế tiếp
        await userManager.UpdateSecurityStampAsync(user);

        await auditService.LogAsync(
            AuditEventType.UserUpdated,
            performedBy,
            "EmployeeAccount",
            user.UserName,
            $"Thay đổi vai trò tài khoản '{user.UserName}' của nhân viên '{employee.FullName}' sang '{newRole}'.");

        return true;
    }

    public async Task<bool> DeactivateOnEmployeeTerminationAsync(int employeeId, string reason, string performedBy, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        var securityService = scope.ServiceProvider.GetRequiredService<ISecurityService>();

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee == null || string.IsNullOrEmpty(employee.UserId)) return false;

        var user = await userManager.FindByIdAsync(employee.UserId);
        if (user == null) return false;

        // Cập nhật trạng thái người dùng thành Terminated
        user.Status = UserStatus.Terminated;

        // Vô hiệu hóa toàn bộ session/cookie/token ngay lập tức
        await userManager.UpdateSecurityStampAsync(user);
        await userManager.UpdateAsync(user);

        // Thu hồi toàn bộ chữ ký số và mã cấp chứng thư nếu có thông qua ISecurityService
        try
        {
            await securityService.OffboardEmployeeAsync(user.Id, reason, performedBy, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lỗi phụ khi thu hồi tài nguyên chứng thư bảo mật cho nhân viên {UserId}", user.Id);
        }

        // Ghi log kiểm toán
        await auditService.LogAsync(
            AuditEventType.UserOffboarded,
            performedBy,
            "EmployeeAccount",
            user.UserName,
            $"Vô hiệu hóa tài khoản '{user.UserName}' do nhân viên '{employee.FullName}' nghỉ việc. Lý do: {reason}. Toàn bộ phiên làm việc đã bị thu hồi.");

        return true;
    }

    public async Task<List<string>> GetAssignableRolesAsync(string performedBy, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        var caller = await userManager.FindByNameAsync(performedBy);
        if (caller == null) return [];

        var callerRoles = await userManager.GetRolesAsync(caller);
        bool isCallerAdmin = callerRoles.Contains("Administrator");
        bool isCallerManager = callerRoles.Contains("Manager");

        var allRoles = await roleManager.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => r.Name!)
            .ToListAsync(cancellationToken);

        if (isCallerAdmin)
        {
            return allRoles;
        }

        if (isCallerManager)
        {
            // Manager không được cấp quyền Administrator
            return allRoles.Where(r => !r.Equals("Administrator", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return [];
    }

    public async Task<string> SuggestUsernameAsync(int employeeId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var employee = await context.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee == null) return "nv.user";

        // Gợi ý từ mã nhân viên: nv00001
        string baseUsername = employee.Code.ToLowerInvariant().Replace("-", "").Replace("_", "");
        if (string.IsNullOrWhiteSpace(baseUsername))
        {
            baseUsername = "nv" + employee.Id.ToString("D5");
        }

        string candidate = baseUsername;
        int counter = 1;
        while (await userManager.FindByNameAsync(candidate) != null)
        {
            candidate = $"{baseUsername}_{counter}";
            counter++;
        }

        return candidate;
    }

    private static string GenerateStrongTemporaryPassword()
    {
        // Sinh mật khẩu 12 ký tự: đủ Hoa, Thường, Số, Ký tự đặc biệt
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%^&*";

        var sb = new StringBuilder();
        sb.Append(upper[RandomNumberGenerator.GetInt32(upper.Length)]);
        sb.Append(lower[RandomNumberGenerator.GetInt32(lower.Length)]);
        sb.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
        sb.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

        string allChars = upper + lower + digits + special;
        for (int i = 0; i < 8; i++)
        {
            sb.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
        }

        // Shuffle
        char[] array = sb.ToString().ToCharArray();
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = RandomNumberGenerator.GetInt32(i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }

        return new string(array);
    }
}
