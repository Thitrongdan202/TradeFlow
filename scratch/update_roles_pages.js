const fs = require('fs');

const rolesListCode = `@page "/cai-dat/vai-tro"
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Identity
@using Microsoft.EntityFrameworkCore
@using TradeFlow.Application.Common.Interfaces
@using TradeFlow.Domain.Enums
@using TradeFlow.Infrastructure.Persistence
@attribute [Authorize(Policy = "Permission:Roles:View")]

@inject RoleManager<ApplicationRole> RoleManager
@inject UserManager<ApplicationUser> UserManager
@inject TradeFlowDbContext DbContext
@inject IAuditService AuditService
@inject AuthenticationStateProvider AuthStateProvider

<PageTitle>Quản lý vai trò &amp; Phân quyền — TradeFlow</PageTitle>

<div class="tf-page-header">
    <div class="tf-page-header-left">
        <div class="tf-breadcrumb">
            <a href="/cai-dat" class="tf-breadcrumb-link">
                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                    <polyline points="15 18 9 12 15 6"></polyline>
                </svg>
                Cài đặt
            </a>
            <span class="tf-breadcrumb-sep">/</span>
            <span class="tf-breadcrumb-current">Vai trò &amp; Phân quyền</span>
        </div>
        <h1 class="tf-page-title">Vai trò &amp; Ma trận phân quyền hệ thống</h1>
        <p class="tf-page-subtitle">Quản lý quyền hạn chi tiết cho từng nhóm người dùng theo kiến trúc RBAC đa cấp</p>
    </div>
    <div class="tf-page-actions">
        <AuthorizeView Policy="Permission:Roles:Create">
            <a href="/cai-dat/vai-tro/them-moi" class="tf-btn tf-btn-primary" style="display:inline-flex; align-items:center; gap:6px;">
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
                </svg>
                Tạo vai trò mới
            </a>
        </AuthorizeView>
    </div>
</div>

@if (!string.IsNullOrEmpty(_actionMessage))
{
    <div class="tf-alert @(_actionSuccess ? "tf-alert-success" : "tf-alert-error") tf-mb-4" style="padding:10px 16px; border-radius:8px; font-size:13.5px; display:flex; align-items:center; justify-content:space-between;">
        <span>@_actionMessage</span>
        <button type="button" @onclick="() => _actionMessage = null" style="background:transparent; border:none; cursor:pointer; color:inherit; font-size:16px;">&times;</button>
    </div>
}

@* Search & Filter Toolbar *@
<div class="tf-card" style="margin-bottom: 1.25rem;">
    <div class="tf-card-body" style="padding: 1rem 1.25rem;">
        <div style="display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; justify-content: space-between;">
            <div style="position: relative; flex: 1; max-width: 480px;">
                <svg style="position: absolute; left: 10px; top: 50%; transform: translateY(-50%;); width: 15px; height: 15px; color: var(--tf-text-muted, #94a3b8);" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/>
                </svg>
                <input type="text"
                       class="tf-form-control"
                       style="padding-left: 32px; font-size: 13px; height: 36px; border-radius: 6px;"
                       placeholder="Tìm kiếm vai trò theo tên hoặc mô tả..."
                       value="@_searchTerm"
                       @oninput="HandleSearchInput" />
            </div>
            <div style="font-size: 13px; color: var(--tf-text-muted, #64748b);">
                Tổng cộng: <strong>@FilteredRoles.Count</strong> vai trò
            </div>
        </div>
    </div>
</div>

<div class="tf-card">
    <div class="tf-card-body" style="padding:0;">
        @if (_roles == null)
        {
            <div style="padding:2.5rem; text-align:center; color:var(--tf-text-muted);">
                Đang tải danh sách vai trò và phân quyền...
            </div>
        }
        else
        {
            <div class="tf-table-responsive" style="overflow-x:auto;">
                <table class="tf-table" style="width:100%; border-collapse:collapse;">
                    <thead>
                        <tr>
                            <th style="min-width:180px;">Tên vai trò</th>
                            <th style="min-width:220px;">Mô tả</th>
                            <th style="text-align:center; width:130px;">Tài khoản</th>
                            <th style="text-align:center; width:160px;">Module truy cập</th>
                            <th style="text-align:center; width:120px;">Trạng thái</th>
                            <th style="text-align:right; width:220px;">Thao tác</th>
                        </tr>
                    </thead>
                    <tbody>
                        @if (!FilteredRoles.Any())
                        {
                            <tr>
                                <td colspan="6">
                                    <div class="tf-empty" style="padding:3rem 1rem; text-align:center;">
                                        <div class="tf-empty-title" style="font-weight:600; color:var(--tf-text-primary);">Không tìm thấy vai trò nào</div>
                                        <p style="font-size:12.5px; color:var(--tf-text-muted);">Thử thay đổi từ khóa tìm kiếm hoặc bấm Thêm vai trò mới.</p>
                                    </div>
                                </td>
                            </tr>
                        }
                        else
                        {
                            foreach (var entry in FilteredRoles)
                            {
                                <tr>
                                    <td class="tf-font-medium">
                                        <div style="display:flex; align-items:center; gap:8px;">
                                            <span style="font-weight:600; color:var(--tf-text-primary);">@entry.Role.Name</span>
                                            @if (entry.Role.IsSystem)
                                            {
                                                <span class="tf-badge tf-badge-primary" style="font-size:10.5px; font-weight:600; padding:2px 6px; border-radius:4px; background:#e0e7ff; color:#3730a3;">Hệ thống</span>
                                            }
                                        </div>
                                    </td>
                                    <td style="color:var(--tf-text-secondary, #475569); font-size:13px;">
                                        @entry.Role.Description
                                    </td>
                                    <td style="text-align:center;">
                                        @if (entry.UserCount > 0)
                                        {
                                            <span style="display:inline-flex; align-items:center; gap:4px; font-weight:600; color:#4f46e5; font-size:12.5px;">
                                                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                                                    <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/>
                                                    <circle cx="9" cy="7" r="4"/>
                                                </svg>
                                                @entry.UserCount user
                                            </span>
                                        }
                                        else
                                        {
                                            <span style="color:var(--tf-text-muted); font-size:12px;">0 user</span>
                                        }
                                    </td>
                                    <td style="text-align:center;">
                                        @if (entry.Role.Name == "Administrator")
                                        {
                                            <span class="tf-badge" style="background:#dcfce7; color:#15803d; font-size:11.5px; font-weight:600; padding:3px 8px; border-radius:4px;">
                                                Toàn quyền (26/26)
                                            </span>
                                        }
                                        else
                                        {
                                            <span class="tf-badge" style="background:#f1f5f9; color:#334155; font-size:11.5px; font-weight:600; padding:3px 8px; border-radius:4px;">
                                                @entry.GrantedModuleCount / 26 module
                                            </span>
                                        }
                                    </td>
                                    <td style="text-align:center;">
                                        <span class="tf-badge" style="display:inline-flex; align-items:center; gap:5px; font-size:12px; font-weight:500; padding:2px 8px; border-radius:4px; @(entry.Role.IsActive ? "background:#ecfdf5; color:#059669;" : "background:#f1f5f9; color:#64748b;")">
                                            <span style="width:6px; height:6px; border-radius:50%; background:currentColor;"></span>
                                            @(entry.Role.IsActive ? "Hoạt động" : "Vô hiệu")
                                        </span>
                                    </td>
                                    <td style="text-align:right;">
                                        <div style="display:inline-flex; align-items:center; gap:6px; justify-content:flex-end;">
                                            <AuthorizeView Policy="Permission:Roles:Edit">
                                                <a href="/cai-dat/vai-tro/sua/@entry.Role.Id" class="tf-btn tf-btn-ghost tf-btn-sm" style="font-size:12px; padding:4px 8px;" title="Chỉnh sửa vai trò &amp; quyền">
                                                    Sửa quyền
                                                </a>
                                                <a href="/cai-dat/vai-tro/sao-chep/@entry.Role.Id" class="tf-btn tf-btn-ghost tf-btn-sm" style="font-size:12px; padding:4px 8px;" title="Tạo bản sao từ vai trò này">
                                                    Sao chép
                                                </a>
                                                @if (!entry.Role.IsSystem && entry.UserCount == 0)
                                                {
                                                    <button type="button"
                                                            class="tf-btn tf-btn-ghost tf-btn-sm"
                                                            style="color:#dc2626; font-size:12px; padding:4px 8px;"
                                                            disabled="@(_deletingRoleId == entry.Role.Id)"
                                                            @onclick="() => DeleteRole(entry.Role)"
                                                            title="Xóa vai trò tùy chỉnh này">
                                                        @if (_deletingRoleId == entry.Role.Id)
                                                        {
                                                            <span>Đang xóa...</span>
                                                        }
                                                        else
                                                        {
                                                            <span>Xóa</span>
                                                        }
                                                    </button>
                                                }
                                                else if (!entry.Role.IsSystem && entry.UserCount > 0)
                                                {
                                                    <span style="font-size:11px; color:#94a3b8; padding:0 4px;" title="Cần chuyển user sang vai trò khác trước khi xóa">
                                                        Đang dùng
                                                    </span>
                                                }
                                            </AuthorizeView>
                                        </div>
                                    </td>
                                </tr>
                            }
                        }
                    </tbody>
                </table>
            </div>
        }
    </div>
</div>

@code {
    private record RoleEntry(ApplicationRole Role, int UserCount, int GrantedModuleCount, int GrantedPermsCount);
    private List<RoleEntry>? _roles;
    private string _searchTerm = "";
    private string? _actionMessage;
    private bool _actionSuccess;
    private string? _currentUsername;
    private string? _deletingRoleId;

    private List<RoleEntry> FilteredRoles
    {
        get
        {
            if (_roles == null) return new List<RoleEntry>();
            if (string.IsNullOrWhiteSpace(_searchTerm)) return _roles;
            var term = _searchTerm.Trim();
            return _roles.Where(r => 
                (r.Role.Name != null && r.Role.Name.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (r.Role.Description != null && r.Role.Description.Contains(term, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _currentUsername = authState.User.Identity?.Name;
        await LoadRolesAsync();
    }

    private void HandleSearchInput(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? "";
    }

    private async Task LoadRolesAsync()
    {
        var roles = await RoleManager.Roles
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .ToListAsync();

        var allPerms = await DbContext.RolePermissions
            .Where(p => p.IsGranted)
            .Select(p => new { p.RoleId, p.Resource })
            .ToListAsync();

        var entries = new List<RoleEntry>();
        foreach (var r in roles)
        {
            var users = await UserManager.GetUsersInRoleAsync(r.Name!);
            var rolePerms = allPerms.Where(p => p.RoleId == r.Id).ToList();
            int moduleCount = r.Name == "Administrator" ? 26 : rolePerms.Select(p => p.Resource).Distinct().Count();
            int permCount = rolePerms.Count;

            entries.Add(new RoleEntry(r, users.Count, moduleCount, permCount));
        }
        _roles = entries;
    }

    private async Task DeleteRole(ApplicationRole role)
    {
        _actionMessage = null;
        if (role.IsSystem)
        {
            _actionMessage = "Không thể xóa vai trò hệ thống.";
            _actionSuccess = false;
            return;
        }

        _deletingRoleId = role.Id;
        try
        {
            var perms = await DbContext.RolePermissions.Where(p => p.RoleId == role.Id).ToListAsync();
            DbContext.RolePermissions.RemoveRange(perms);
            await DbContext.SaveChangesAsync();

            var result = await RoleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                _actionMessage = string.Join("; ", result.Errors.Select(e => e.Description));
                _actionSuccess = false;
                return;
            }

            await AuditService.LogAsync(AuditEventType.RoleDeleted, _currentUsername, "Role", role.Name, $"Đã xóa vai trò tùy chỉnh: {role.Name}");
            _actionMessage = $"Đã xóa vai trò \"{role.Name}\" thành công.";
            _actionSuccess = true;
            await LoadRolesAsync();
        }
        catch (Exception ex)
        {
            _actionMessage = "Lỗi khi xóa vai trò: " + ex.Message;
            _actionSuccess = false;
        }
        finally
        {
            _deletingRoleId = null;
        }
    }
}
`;

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Admin/Roles/List.razor', rolesListCode, 'utf8');
console.log('Admin/Roles/List.razor updated successfully!');
