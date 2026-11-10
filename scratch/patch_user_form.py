import os

path = r'src/TradeFlow.Web/Components/Pages/Admin/Users/UserForm.razor'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

# 1. Inject VaultService
inject_target = """@inject AuthenticationStateProvider AuthStateProvider
@inject IPermissionService PermissionService"""

inject_repl = """@inject AuthenticationStateProvider AuthStateProvider
@inject IPermissionService PermissionService
@inject ICredentialVaultService VaultService"""

# 2. Add ROOT protection & EncryptedPassword update
save_target = """            if (IsEdit)
            {
                var existingUser = await UserManager.FindByIdAsync(Id!);
                if (existingUser == null) { _errorMessage = "Không tìm thấy người dùng."; return; }"""

save_repl = """            var currentAdmin = _currentUsername != null ? await UserManager.FindByNameAsync(_currentUsername) : null;
            var isCurrentAdmin = _currentUsername == "admin" || (currentAdmin != null && (await UserManager.IsInRoleAsync(currentAdmin, "Administrator")));

            if (!_isCurrentUser && _selectedRoles.Contains("Administrator") && !isCurrentAdmin)
            {
                _errorMessage = "Bảo vệ ROOT: Chỉ Quản trị viên ROOT mới có quyền cấp vai trò Administrator.";
                return;
            }

            if (IsEdit)
            {
                var existingUser = await UserManager.FindByIdAsync(Id!);
                if (existingUser == null) { _errorMessage = "Không tìm thấy người dùng."; return; }

                var targetRoles = await UserManager.GetRolesAsync(existingUser);
                if ((existingUser.UserName == "admin" || targetRoles.Contains("Administrator")) && !isCurrentAdmin)
                {
                    _errorMessage = "Bảo vệ ROOT: Không được phép chỉnh sửa hoặc quản lý tài khoản Quản trị viên (ROOT).";
                    return;
                }"""

pass_target = """                // Password reset (optional on edit)
                if (!string.IsNullOrWhiteSpace(_password))
                {
                    var token = await UserManager.GeneratePasswordResetTokenAsync(existingUser);
                    var passResult = await UserManager.ResetPasswordAsync(existingUser, token, _password);
                    if (!passResult.Succeeded)
                    {
                        _errorMessage = string.Join("; ", passResult.Errors.Select(e => e.Description));
                        return;
                    }
                }"""

pass_repl = """                // Password reset (optional on edit)
                if (!string.IsNullOrWhiteSpace(_password))
                {
                    var token = await UserManager.GeneratePasswordResetTokenAsync(existingUser);
                    var passResult = await UserManager.ResetPasswordAsync(existingUser, token, _password);
                    if (!passResult.Succeeded)
                    {
                        _errorMessage = string.Join("; ", passResult.Errors.Select(e => e.Description));
                        return;
                    }
                    existingUser.EncryptedPassword = VaultService.EncryptPassword(_password);
                    existingUser.PasswordChangedAt = DateTime.UtcNow;
                    await UserManager.UpdateSecurityStampAsync(existingUser);
                    await UserManager.UpdateAsync(existingUser);
                }"""

create_target = """                _user.UserName = _username.Trim().ToLower();
                _user.Email = _user.Email!.Trim().ToLower();
                _user.EmailConfirmed = true;
                _user.CreatedBy = _currentUsername;

                var result = await UserManager.CreateAsync(_user, _password!);"""

create_repl = """                _user.UserName = _username.Trim().ToLower();
                _user.Email = _user.Email!.Trim().ToLower();
                _user.EmailConfirmed = true;
                _user.CreatedBy = _currentUsername;
                _user.EncryptedPassword = VaultService.EncryptPassword(_password!);
                _user.PasswordChangedAt = DateTime.UtcNow;

                var result = await UserManager.CreateAsync(_user, _password!);"""

c = c.replace(inject_target, inject_repl, 1).replace(save_target, save_repl, 1).replace(pass_target, pass_repl, 1).replace(create_target, create_repl, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('SUCCESS')
