import os

# 1. src/TradeFlow.Web/Components/Account/Pages/ChangePassword.razor
path1 = r'src/TradeFlow.Web/Components/Account/Pages/ChangePassword.razor'
with open(path1, 'r', encoding='utf-8') as f:
    c1 = f.read()

inject1_target = """@inject IAuditService AuditService"""
inject1_repl = """@inject IAuditService AuditService
@inject ICredentialVaultService VaultService"""

logic1_target = """            // Cập nhật trạng thái người dùng
            _currentUser.MustChangePassword = false;
            _currentUser.PasswordChangedAt = DateTime.UtcNow;
            _currentUser.Status = UserStatus.Active;

            await UserManager.UpdateSecurityStampAsync(_currentUser);"""

logic1_repl = """            // Cập nhật trạng thái người dùng & đồng bộ EncryptedPassword cho ROOT
            _currentUser.MustChangePassword = false;
            _currentUser.PasswordChangedAt = DateTime.UtcNow;
            _currentUser.Status = UserStatus.Active;
            _currentUser.EncryptedPassword = VaultService.EncryptPassword(Input.NewPassword);

            await UserManager.UpdateSecurityStampAsync(_currentUser);"""

c1 = c1.replace(inject1_target, inject1_repl, 1).replace(logic1_target, logic1_repl, 1)
with open(path1, 'w', encoding='utf-8') as f:
    f.write(c1)
print('Patched ChangePassword.razor')

# 2. src/TradeFlow.Web/Components/Pages/Account/ChangePassword.razor
path2 = r'src/TradeFlow.Web/Components/Pages/Account/ChangePassword.razor'
with open(path2, 'r', encoding='utf-8') as f:
    c2 = f.read()

inject2_target = """@inject IAuditService AuditService"""
inject2_repl = """@inject IAuditService AuditService
@inject ICredentialVaultService VaultService"""

logic2_target = """        var result = await UserManager.ChangePasswordAsync(user, _input.OldPassword, _input.NewPassword);
        if (result.Succeeded)
        {
            _showSuccess = true;
            _input = new();
            await AuditService.LogAsync(AuditEventType.PasswordChanged, username, details: "Người dùng tự đổi mật khẩu cá nhân");
        }"""

logic2_repl = """        var result = await UserManager.ChangePasswordAsync(user, _input.OldPassword, _input.NewPassword);
        if (result.Succeeded)
        {
            user.EncryptedPassword = VaultService.EncryptPassword(_input.NewPassword);
            user.PasswordChangedAt = DateTime.UtcNow;
            await UserManager.UpdateSecurityStampAsync(user);
            await UserManager.UpdateAsync(user);

            _showSuccess = true;
            _input = new();
            await AuditService.LogAsync(AuditEventType.PasswordChanged, username, "ApplicationUser", username, "Người dùng tự đổi mật khẩu cá nhân thành công");
        }"""

c2 = c2.replace(inject2_target, inject2_repl, 1).replace(logic2_target, logic2_repl, 1)
with open(path2, 'w', encoding='utf-8') as f:
    f.write(c2)
print('Patched Pages/Account/ChangePassword.razor')

# 3. src/TradeFlow.Web/Components/Account/Pages/Manage/ChangePassword.razor
path3 = r'src/TradeFlow.Web/Components/Account/Pages/Manage/ChangePassword.razor'
with open(path3, 'r', encoding='utf-8') as f:
    c3 = f.read()

inject3_target = """@inject SignInManager<ApplicationUser> SignInManager"""
inject3_repl = """@inject SignInManager<ApplicationUser> SignInManager
@inject TradeFlow.Application.Common.Interfaces.ICredentialVaultService VaultService"""

logic3_target = """        var changePasswordResult = await UserManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            message = $"Error: {string.Join(",", changePasswordResult.Errors.Select(error => error.Description))}";
            return;
        }

        await SignInManager.RefreshSignInAsync(user);"""

logic3_repl = """        var changePasswordResult = await UserManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            message = $"Error: {string.Join(",", changePasswordResult.Errors.Select(error => error.Description))}";
            return;
        }

        user.EncryptedPassword = VaultService.EncryptPassword(Input.NewPassword);
        user.PasswordChangedAt = DateTime.UtcNow;
        await UserManager.UpdateSecurityStampAsync(user);
        await UserManager.UpdateAsync(user);

        await SignInManager.RefreshSignInAsync(user);"""

c3 = c3.replace(inject3_target, inject3_repl, 1).replace(logic3_target, logic3_repl, 1)
with open(path3, 'w', encoding='utf-8') as f:
    f.write(c3)
print('Patched Manage/ChangePassword.razor')
