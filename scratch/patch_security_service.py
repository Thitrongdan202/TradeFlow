import os

path = r'src/TradeFlow.Infrastructure/Services/Security/SecurityService.cs'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

# 1. Update constructor and fields
fields_target = """    private readonly ISigningProvider _signingProvider;
    private readonly IAuditService _auditService;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        TradeFlowDbContext context,
        UserManager<ApplicationUser> userManager,
        ISigningProvider signingProvider,
        IAuditService auditService,
        ILogger<SecurityService> logger)
    {
        _context = context;
        _userManager = userManager;
        _signingProvider = signingProvider;
        _auditService = auditService;
        _logger = logger;
    }"""

fields_repl = """    private readonly ISigningProvider _signingProvider;
    private readonly IAuditService _auditService;
    private readonly ICredentialVaultService _vaultService;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(
        TradeFlowDbContext context,
        UserManager<ApplicationUser> userManager,
        ISigningProvider signingProvider,
        IAuditService auditService,
        ICredentialVaultService vaultService,
        ILogger<SecurityService> logger)
    {
        _context = context;
        _userManager = userManager;
        _signingProvider = signingProvider;
        _auditService = auditService;
        _vaultService = vaultService;
        _logger = logger;
    }"""

# 2. Update AdminResetPasswordAsync and add new methods
reset_target = """        await _userManager.UpdateSecurityStampAsync(user);

        // Audit log strictly WITHOUT logging the password value
        await _auditService.LogAsync(
            AuditEventType.PasswordReset,
            performedBy ?? "System",
            "ApplicationUser",
            user.UserName,
            $"Quản trị viên đã đặt lại mật khẩu tạm thời cho người dùng {user.UserName}. Yêu cầu đổi mật khẩu sau khi đăng nhập.");

        return new PasswordResetResult
        {
            Succeeded = true,
            TempPassword = tempPassword,
            UserName = user.UserName
        };
    }"""

reset_repl = """        user.EncryptedPassword = _vaultService.EncryptPassword(tempPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        user.MustChangePassword = true;
        await _userManager.UpdateSecurityStampAsync(user);
        await _userManager.UpdateAsync(user);

        // Audit log strictly WITHOUT logging the password value
        await _auditService.LogAsync(
            AuditEventType.TemporaryPasswordIssued,
            performedBy ?? "System",
            "ApplicationUser",
            user.UserName,
            $"Quản trị viên đã đặt lại mật khẩu tạm thời cho người dùng '{user.UserName}'. Yêu cầu đổi mật khẩu ở lần đăng nhập tiếp theo.");

        return new PasswordResetResult
        {
            Succeeded = true,
            TempPassword = tempPassword,
            UserName = user.UserName
        };
    }

    public async Task<PasswordResetResult> AdminResetPasswordCustomAsync(
        string userId,
        string? newPassword,
        bool isTemporary,
        bool revokeSessions,
        string? reason,
        string performedByAdmin,
        string? adminVerifyPassword = null,
        CancellationToken cancellationToken = default)
    {
        var adminUser = await _userManager.FindByNameAsync(performedByAdmin);
        if (adminUser == null)
        {
            return new PasswordResetResult { Succeeded = false, ErrorMessage = "Không xác định được danh tính Quản trị viên thực hiện." };
        }

        var adminRoles = await _userManager.GetRolesAsync(adminUser);
        if (!adminRoles.Contains("Administrator") && adminUser.UserName != "admin")
        {
            return new PasswordResetResult { Succeeded = false, ErrorMessage = "Từ chối quyền truy cập: Chỉ Quản trị viên ROOT mới có quyền quản trị thông tin xác thực." };
        }

        // Nếu có truyền mật khẩu xác thực lại của Admin
        if (!string.IsNullOrEmpty(adminVerifyPassword))
        {
            var isPasswordValid = await _userManager.CheckPasswordAsync(adminUser, adminVerifyPassword);
            if (!isPasswordValid)
            {
                await _auditService.LogAsync(
                    AuditEventType.LoginFailed,
                    performedByAdmin,
                    "ApplicationUser",
                    userId,
                    $"Xác thực mật khẩu ROOT thất bại khi cố gắng đặt lại mật khẩu cho người dùng ID {userId}.");

                return new PasswordResetResult { Succeeded = false, ErrorMessage = "Xác thực danh tính ROOT thất bại: Mật khẩu xác nhận của Quản trị viên không chính xác." };
            }
        }

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null)
        {
            return new PasswordResetResult { Succeeded = false, ErrorMessage = "Không tìm thấy tài khoản người dùng mục tiêu." };
        }

        // Bảo vệ tài khoản ROOT Admin: Không ai ngoài 'admin' được reset mật khẩu của ROOT 'admin'
        if (targetUser.UserName == "admin" && performedByAdmin != "admin")
        {
            return new PasswordResetResult { Succeeded = false, ErrorMessage = "Bảo vệ hệ thống: Không thể can thiệp tài khoản ROOT tối cao." };
        }

        string finalPassword;
        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            finalPassword = newPassword.Trim();
            if (finalPassword.Length < 8 ||
                !finalPassword.Any(char.IsUpper) ||
                !finalPassword.Any(char.IsLower) ||
                !finalPassword.Any(char.IsDigit))
            {
                return new PasswordResetResult { Succeeded = false, ErrorMessage = "Mật khẩu không đáp ứng chính sách bảo mật: Cần tối thiểu 8 ký tự, bao gồm chữ hoa, chữ thường và chữ số." };
            }
        }
        else
        {
            // Sinh mật khẩu mạnh ngẫu nhiên
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%^&*";

            var sb = new StringBuilder("TF#");
            sb.Append(upper[RandomNumberGenerator.GetInt32(upper.Length)]);
            sb.Append(lower[RandomNumberGenerator.GetInt32(lower.Length)]);
            sb.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            sb.Append(special[RandomNumberGenerator.GetInt32(special.Length)]);

            string allChars = upper + lower + digits + special;
            for (int i = 0; i < 5; i++)
            {
                sb.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }
            finalPassword = sb.ToString();
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(targetUser);
        var resetRes = await _userManager.ResetPasswordAsync(targetUser, token, finalPassword);
        if (!resetRes.Succeeded)
        {
            return new PasswordResetResult
            {
                Succeeded = false,
                ErrorMessage = string.Join("; ", resetRes.Errors.Select(e => e.Description))
            };
        }

        targetUser.EncryptedPassword = _vaultService.EncryptPassword(finalPassword);
        targetUser.PasswordChangedAt = DateTime.UtcNow;
        targetUser.MustChangePassword = isTemporary;

        if (revokeSessions)
        {
            await _userManager.UpdateSecurityStampAsync(targetUser);
        }

        await _userManager.UpdateAsync(targetUser);

        // Audit log strictly WITHOUT saving the password value
        var eventType = isTemporary ? AuditEventType.TemporaryPasswordIssued : AuditEventType.PasswordReset;
        var details = $"Quản trị viên '{performedByAdmin}' đã {(isTemporary ? "cấp mật khẩu tạm thời" : "đặt lại mật khẩu")} cho tài khoản '{targetUser.UserName}'. Lý do: {reason ?? "Quản trị mật khẩu ROOT"}. Thu hồi phiên: {(revokeSessions ? "Có" : "Không")}.";

        await _auditService.LogAsync(
            eventType,
            performedByAdmin,
            "ApplicationUser",
            targetUser.UserName,
            details);

        return new PasswordResetResult
        {
            Succeeded = true,
            TempPassword = finalPassword,
            UserName = targetUser.UserName
        };
    }

    public async Task<bool> RevokeAllUserSessionsAsync(string userId, string? reason, string performedBy, CancellationToken cancellationToken = default)
    {
        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null) return false;

        // Bảo vệ ROOT
        if (targetUser.UserName == "admin" && performedBy != "admin") return false;

        await _userManager.UpdateSecurityStampAsync(targetUser);

        await _auditService.LogAsync(
            AuditEventType.SessionRevoked,
            performedBy,
            "ApplicationUser",
            targetUser.UserName,
            $"Quản trị viên đã thu hồi tất cả phiên đăng nhập và đăng xuất tài khoản '{targetUser.UserName}' khỏi toàn bộ thiết bị. Lý do: {reason ?? "Yêu cầu an ninh"}");

        return true;
    }

    public async Task<bool> SetUserStatusAsync(string userId, UserStatus status, string? reason, string performedBy, CancellationToken cancellationToken = default)
    {
        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null) return false;

        // Bảo vệ ROOT
        if (targetUser.UserName == "admin") return false;

        targetUser.Status = status;

        if (status == UserStatus.Locked)
        {
            await _userManager.SetLockoutEndDateAsync(targetUser, DateTimeOffset.UtcNow.AddYears(100));
            await _userManager.UpdateSecurityStampAsync(targetUser);
            await _userManager.UpdateAsync(targetUser);

            await _auditService.LogAsync(
                AuditEventType.UserLocked,
                performedBy,
                "ApplicationUser",
                targetUser.UserName,
                $"Quản trị viên đã khóa tài khoản '{targetUser.UserName}'. Lý do: {reason ?? "Chính sách bảo mật"}");
        }
        else if (status == UserStatus.Active)
        {
            await _userManager.SetLockoutEndDateAsync(targetUser, null);
            await _userManager.ResetAccessFailedCountAsync(targetUser);
            await _userManager.UpdateAsync(targetUser);

            await _auditService.LogAsync(
                AuditEventType.UserUnlocked,
                performedBy,
                "ApplicationUser",
                targetUser.UserName,
                $"Quản trị viên đã mở khóa tài khoản '{targetUser.UserName}'. Lý do: {reason ?? "Khôi phục truy cập"}");
        }
        else if (status == UserStatus.Disabled)
        {
            await _userManager.UpdateSecurityStampAsync(targetUser);
            await _userManager.UpdateAsync(targetUser);

            await _auditService.LogAsync(
                AuditEventType.AccountDisabled,
                performedBy,
                "ApplicationUser",
                targetUser.UserName,
                $"Quản trị viên đã vô hiệu hóa tài khoản '{targetUser.UserName}'. Lý do: {reason ?? "Đình chỉ tài khoản"}");
        }

        return true;
    }

    public async Task<string?> GetDecryptedUserPasswordAsync(string userId, string performedBy, CancellationToken cancellationToken = default)
    {
        var adminUser = await _userManager.FindByNameAsync(performedBy);
        if (adminUser == null) return null;

        var roles = await _userManager.GetRolesAsync(adminUser);
        if (!roles.Contains("Administrator") && adminUser.UserName != "admin")
        {
            return null; // Không phải ROOT -> từ chối
        }

        var targetUser = await _userManager.FindByIdAsync(userId);
        if (targetUser == null || string.IsNullOrEmpty(targetUser.EncryptedPassword))
        {
            return null;
        }

        return _vaultService.DecryptPassword(targetUser.EncryptedPassword);
    }"""

c = c.replace(fields_target, fields_repl, 1).replace(reset_target, reset_repl, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('SUCCESS')
