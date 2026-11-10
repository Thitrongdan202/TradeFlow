import os

path = r'src/TradeFlow.Web/Components/Pages/Admin/Users/List.razor'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

# 1. Inject ICredentialVaultService
inject_target = """@inject ISecurityService SecurityService
@inject AuthenticationStateProvider AuthStateProvider"""

inject_repl = """@inject ISecurityService SecurityService
@inject ICredentialVaultService VaultService
@inject AuthenticationStateProvider AuthStateProvider"""

# 2. Add header action button
header_action_target = """    <div class="tf-page-actions">
        <AuthorizeView Policy="Permission:Users:Create">"""

header_action_repl = """    <div class="tf-page-actions" style="display:flex;gap:8px;flex-wrap:wrap;">
        <a href="/cai-dat/nguoi-dung/bao-mat" class="tf-btn tf-btn-secondary" style="display:flex;align-items:center;gap:6px;">
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
            </svg>
            <span>Bảo mật &amp; Quản trị mật khẩu ROOT</span>
        </a>
        <AuthorizeView Policy="Permission:Users:Create">"""

# 3. Add table columns: Mật khẩu hiện tại & Đổi MK cuối
th_target = """                        <th>Tên đăng nhập</th>
                        <th>Họ và tên</th>
                        <th>Vai trò</th>
                        <th style="text-align:center;">Trạng thái</th>
                        <th>Đăng nhập cuối</th>
                        <th style="text-align:right;">Thao tác</th>"""

th_repl = """                        <th>Tên đăng nhập</th>
                        <th>Họ và tên</th>
                        <th>Vai trò</th>
                        <th>Mật khẩu hiện tại</th>
                        <th style="text-align:center;">Trạng thái</th>
                        <th>Đổi MK cuối</th>
                        <th style="text-align:right;">Thao tác</th>"""

# 4. Add td columns in row
td_target = """                            <td>@entry.User.FullName</td>
                            <td>
                                @if (entry.Roles.Any())
                                {
                                    foreach (var role in entry.Roles)
                                    {
                                        <TfBadge Variant="primary" Text="@role" Class="tw-mr-1 tw-mb-0.5" />
                                    }
                                }
                                else
                                {
                                    <span class="tw-text-slate-400 tw-text-sm">—</span>
                                }
                            </td>
                            <td style="text-align:center;">
                                @if (entry.User.Status == UserStatus.Active)
                                {
                                    <TfBadge Variant="success" Dot="true" Text="Hoạt động" />
                                }
                                else if (entry.User.Status == UserStatus.Locked)
                                {
                                    <TfBadge Variant="danger" Dot="true" Text="Bị khóa" />
                                }
                                else
                                {
                                    <TfBadge Variant="gray" Dot="true" Text="Vô hiệu hóa" />
                                }
                            </td>
                            <td class="tw-text-slate-500 tw-text-xs">
                                @(entry.User.LastLoginAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Chưa đăng nhập")
                            </td>"""

td_repl = """                            <td>@entry.User.FullName</td>
                            <td>
                                @if (entry.Roles.Any())
                                {
                                    foreach (var role in entry.Roles)
                                    {
                                        <TfBadge Variant="primary" Text="@role" Class="tw-mr-1 tw-mb-0.5" />
                                    }
                                }
                                else
                                {
                                    <span class="tw-text-slate-400 tw-text-sm">—</span>
                                }
                            </td>
                            <td>
                                <div style="display:flex;align-items:center;gap:6px;">
                                    @if (_revealedPasswordUserIds.Contains(entry.User.Id))
                                    {
                                        var plainPass = VaultService.DecryptPassword(entry.User.EncryptedPassword) ?? "Chưa lưu";
                                        <span style="font-family:monospace;font-size:12px;font-weight:700;color:var(--tf-text-primary);background:rgba(0,0,0,0.05);padding:2px 6px;border-radius:4px;border:1px solid rgba(0,0,0,0.1);">
                                            @plainPass
                                        </span>
                                        <button type="button"
                                                class="tf-btn tf-btn-ghost tf-btn-sm"
                                                style="padding:2px 4px;"
                                                @onclick="() => ToggleRevealPassword(entry.User.Id)"
                                                title="Ẩn mật khẩu">
                                            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                                <path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/>
                                                <line x1="1" y1="1" x2="23" y2="23"/>
                                            </svg>
                                        </button>
                                    }
                                    else
                                    {
                                        <span style="letter-spacing:2px;color:var(--tf-text-muted);font-size:13px;">••••••••</span>
                                        @if (_isCurrentUserAdmin)
                                        {
                                            <button type="button"
                                                    class="tf-btn tf-btn-ghost tf-btn-sm"
                                                    style="padding:2px 4px;color:var(--tf-primary);"
                                                    @onclick="() => ToggleRevealPassword(entry.User.Id)"
                                                    title="Nhấn con mắt để xem mật khẩu hiện tại">
                                                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                                    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>
                                                </svg>
                                            </button>
                                        }
                                    }
                                </div>
                            </td>
                            <td style="text-align:center;">
                                @if (entry.User.Status == UserStatus.Active)
                                {
                                    <TfBadge Variant="success" Dot="true" Text="Hoạt động" />
                                }
                                else if (entry.User.Status == UserStatus.Locked)
                                {
                                    <TfBadge Variant="danger" Dot="true" Text="Bị khóa" />
                                }
                                else
                                {
                                    <TfBadge Variant="gray" Dot="true" Text="Vô hiệu hóa" />
                                }
                            </td>
                            <td class="tw-text-slate-500 tw-text-xs">
                                @(entry.User.PasswordChangedAt?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Chưa đổi")
                            </td>"""

# 5. Add Bảo mật link to actions
action_target = """                                        <a href="/cai-dat/nguoi-dung/sua/@entry.User.Id"
                                           class="tf-btn tf-btn-ghost tf-btn-sm" title="Sửa">
                                            Sửa
                                        </a>"""

action_repl = """                                        <a href="/cai-dat/nguoi-dung/@entry.User.Id/bao-mat"
                                           class="tf-btn tf-btn-ghost tf-btn-sm tw-text-indigo-600 hover:tw-bg-indigo-50" title="Bảo mật & Đặt lại mật khẩu ROOT">
                                            Bảo mật
                                        </a>
                                        <a href="/cai-dat/nguoi-dung/sua/@entry.User.Id"
                                           class="tf-btn tf-btn-ghost tf-btn-sm" title="Sửa">
                                            Sửa
                                        </a>"""

# 6. Add code fields & methods
code_fields_target = """    private string? _currentUsername;
    private (string Username, string TempPassword)? _resetPasswordInfo;"""

code_fields_repl = """    private string? _currentUsername;
    private bool _isCurrentUserAdmin;
    private HashSet<string> _revealedPasswordUserIds = new();
    private (string Username, string TempPassword)? _resetPasswordInfo;

    private void ToggleRevealPassword(string userId)
    {
        if (_revealedPasswordUserIds.Contains(userId))
            _revealedPasswordUserIds.Remove(userId);
        else
            _revealedPasswordUserIds.Add(userId);
    }"""

init_target = """    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _currentUsername = authState.User.Identity?.Name;
        await LoadUsersAsync();
    }"""

init_repl = """    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _currentUsername = authState.User.Identity?.Name;
        _isCurrentUserAdmin = _currentUsername == "admin" || authState.User.IsInRole("Administrator");
        await LoadUsersAsync();
    }"""

c = c.replace(inject_target, inject_repl, 1).replace(header_action_target, header_action_repl, 1).replace(th_target, th_repl, 1).replace(td_target, td_repl, 1).replace(action_target, action_repl, 1).replace(code_fields_target, code_fields_repl, 1).replace(init_target, init_repl, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('SUCCESS')
