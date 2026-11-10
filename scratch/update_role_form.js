const fs = require('fs');

const roleFormCode = `@page "/cai-dat/vai-tro/them-moi"
@page "/cai-dat/vai-tro/sua/{Id}"
@page "/cai-dat/vai-tro/sao-chep/{CloneFromId}"
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Identity
@using Microsoft.EntityFrameworkCore
@using TradeFlow.Application.Common.Interfaces
@using TradeFlow.Domain.Enums
@using TradeFlow.Infrastructure.Persistence
@attribute [Authorize(Policy = "Permission:Roles:Edit")]

@inject RoleManager<ApplicationRole> RoleManager
@inject TradeFlowDbContext DbContext
@inject NavigationManager Nav
@inject IAuditService AuditService
@inject AuthenticationStateProvider AuthStateProvider
@inject IPermissionService PermissionService

<PageTitle>@(IsEdit ? "Sửa vai trò & Phân quyền" : (IsClone ? "Sao chép vai trò" : "Thêm vai trò mới")) — TradeFlow</PageTitle>

<div class="tf-page-header">
    <div class="tf-page-header-left">
        <div class="tf-breadcrumb">
            <a href="/cai-dat" class="tf-breadcrumb-link">Cài đặt</a>
            <span class="tf-breadcrumb-sep">/</span>
            <a href="/cai-dat/vai-tro" class="tf-breadcrumb-link">Vai trò</a>
            <span class="tf-breadcrumb-sep">/</span>
            <span class="tf-breadcrumb-current">@(IsEdit ? "Cập nhật quyền" : (IsClone ? "Bản sao vai trò" : "Tạo mới"))</span>
        </div>
        <h1 class="tf-page-title">@(IsEdit ? $"Cấu hình quyền: {_role?.Name}" : (IsClone ? "Sao chép vai trò người dùng" : "Tạo vai trò & Ma trận phân quyền"))</h1>
        <p class="tf-page-subtitle">Thiết lập phạm vi thao tác chi tiết (Xem, Thêm, Sửa, Xóa, Duyệt, Nhập/Xuất) cho từng phân hệ nghiệp vụ</p>
    </div>
    <div class="tf-page-actions" style="display:flex; align-items:center; gap:8px;">
        <a href="/cai-dat/vai-tro" class="tf-btn tf-btn-secondary">Hủy bỏ</a>
        <button type="button" class="tf-btn tf-btn-primary" @onclick="SaveRole" disabled="@_isSaving" style="display:inline-flex; align-items:center; gap:6px;">
            @if (_isSaving)
            {
                <span class="tf-spinner tf-spinner-xs"></span>
                <span>Đang lưu...</span>
            }
            else
            {
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <path d="M19 21H5a2 2 0 01-2-2V5a2 2 0 012-2h11l5 5v11a2 2 0 01-2 2z"/><polyline points="17 21 17 13 7 13 7 21"/><polyline points="7 3 7 8 15 8"/>
                </svg>
                <span>Lưu thay đổi</span>
            }
        </button>
    </div>
</div>

@if (!string.IsNullOrEmpty(_errorMessage))
{
    <div class="tf-alert tf-alert-error tf-mb-4" style="padding:10px 16px; border-radius:8px; font-size:13.5px;">@_errorMessage</div>
}

@if (_role == null)
{
    <div style="padding:3rem; text-align:center; color:var(--tf-text-muted);">Đang nạp dữ liệu vai trò...</div>
}
else
{
    @* Card 1: Role Basic Information *@
    <div class="tf-card tf-mb-6">
        <div class="tf-card-header" style="padding:1rem 1.25rem; border-bottom:1px solid var(--tf-border, #e2e8f0); display:flex; justify-content:space-between; align-items:center;">
            <h2 class="tf-card-title" style="font-size:15px; font-weight:700; margin:0;">Thông tin vai trò</h2>
            @if (!IsEdit && _availableTemplateRoles.Any())
            {
                <div style="display:flex; align-items:center; gap:8px;">
                    <span style="font-size:12.5px; color:var(--tf-text-muted);">Kế thừa từ vai trò mẫu:</span>
                    <select class="tf-form-select" style="font-size:12.5px; height:32px; padding:2px 8px;" @onchange="ApplyRoleTemplate">
                        <option value="">-- Tự thiết lập từ đầu --</option>
                        @foreach (var r in _availableTemplateRoles)
                        {
                            <option value="@r.Id">@r.Name (@r.Description)</option>
                        }
                    </select>
                </div>
            }
        </div>
        <div class="tf-card-body" style="padding:1.25rem;">
            <div style="display:grid; grid-template-columns:repeat(auto-fit, minmax(280px, 1fr)); gap:1rem;">
                <div class="tf-field">
                    <label class="tf-label" style="font-size:13px; font-weight:600; margin-bottom:4px; display:block;">Tên vai trò *</label>
                    <input type="text" class="tf-form-control" @bind="_role.Name" disabled="@_role.IsSystem" placeholder="Ví dụ: Kế toán trưởng, Giám sát kho..." />
                    @if (_role.IsSystem)
                    {
                        <div style="font-size:11.5px; color:#64748b; margin-top:3px;">Vai trò hệ thống không được thay đổi tên định danh.</div>
                    }
                </div>

                <div class="tf-field">
                    <label class="tf-label" style="font-size:13px; font-weight:600; margin-bottom:4px; display:block;">Mô tả chức năng</label>
                    <input type="text" class="tf-form-control" @bind="_role.Description" placeholder="Mô tả phạm vi trách nhiệm của vai trò..." />
                </div>
            </div>

            <div style="margin-top:1rem; display:flex; align-items:center; gap:16px;">
                <label style="display:inline-flex; align-items:center; gap:8px; font-size:13.5px; cursor:pointer;">
                    <input type="checkbox" @bind="_role.IsActive" disabled="@_role.IsSystem" />
                    <span style="font-weight:500;">Kích hoạt vai trò này</span>
                </label>
                @if (_role.IsSystem)
                {
                    <span style="font-size:12px; color:#64748b;">(Vai trò hệ thống luôn ở trạng thái hoạt động)</span>
                }
                <div style="margin-left:auto; font-size:13px; color:#4f46e5; font-weight:600;">
                    Đã cấp: @TotalGrantedPermissions / @(TotalPossiblePermissions) quyền (@GetGrantedModulesCount() / @ModuleGroups.Count phân hệ)
                </div>
            </div>
        </div>
    </div>

    @* Card 2: Permissions Matrix Filter & Search *@
    <div class="tf-card tf-mb-4">
        <div class="tf-card-body" style="padding:0.75rem 1.25rem; display:flex; flex-wrap:wrap; align-items:center; justify-content:space-between; gap:12px;">
            <div style="position:relative; flex:1; max-width:400px;">
                <svg style="position:absolute; left:10px; top:50%; transform:translateY(-50%); width:14px; height:14px; color:#94a3b8;" viewBox="0 0 20 20" fill="currentColor">
                    <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/>
                </svg>
                <input type="text"
                       class="tf-form-control"
                       style="padding-left:30px; font-size:12.5px; height:34px;"
                       placeholder="Lọc tài nguyên hoặc hành động phân quyền..."
                       value="@_permFilter"
                       @oninput="e => _permFilter = e.Value?.ToString() ?? ''" />
            </div>
            <div style="display:flex; align-items:center; gap:8px;">
                <button type="button" class="tf-btn tf-btn-ghost tf-btn-sm" @onclick="SelectAllPermissions" style="font-size:12px;">
                    Chọn tất cả
                </button>
                <button type="button" class="tf-btn tf-btn-ghost tf-btn-sm" @onclick="ClearAllPermissions" style="font-size:12px;">
                    Bỏ chọn tất cả
                </button>
            </div>
        </div>
    </div>

    @* Card 3: Grouped Module Permission Accordions *@
    @foreach (var group in ModuleGroups)
    {
        var groupResources = group.Resources.Where(r => MatchesFilter(r)).ToList();
        if (!groupResources.Any() && !string.IsNullOrWhiteSpace(_permFilter)) continue;

        int groupGranted = groupResources.Sum(r => _allActions.Count(a => _matrix.TryGetValue((r, a), out var val) && val));
        int groupTotal = groupResources.Count * _allActions.Length;
        bool isAllGroupSelected = groupTotal > 0 && groupGranted == groupTotal;

        <div class="tf-card tf-mb-4" style="border: 1px solid var(--tf-border, #e2e8f0); border-radius:8px; overflow:hidden;">
            <div class="tf-card-header" style="background:var(--tf-bg-alt, #f8fafc); padding:10px 16px; display:flex; align-items:center; justify-content:space-between; border-bottom:1px solid var(--tf-border, #e2e8f0);">
                <div style="display:flex; align-items:center; gap:10px;">
                    <span style="font-weight:700; font-size:14px; color:var(--tf-text-primary);">@group.Name</span>
                    <span class="tf-badge" style="font-size:11px; padding:2px 6px; border-radius:4px; font-weight:600; background:@(groupGranted > 0 ? "#e0e7ff" : "#f1f5f9"); color:@(groupGranted > 0 ? "#4338ca" : "#64748b");">
                        @groupGranted / @groupTotal quyền
                    </span>
                </div>
                <button type="button"
                        class="tf-btn tf-btn-ghost tf-btn-xs"
                        style="font-size:11.5px; padding:2px 8px;"
                        @onclick="() => ToggleGroupPermissions(groupResources, !isAllGroupSelected)">
                    @(isAllGroupSelected ? "Bỏ chọn nhóm" : "Chọn toàn bộ nhóm")
                </button>
            </div>
            <div class="tf-card-body" style="padding:0; overflow-x:auto;">
                <table class="tf-table" style="width:100%; border-collapse:collapse; margin:0; font-size:13px;">
                    <thead>
                        <tr style="background:var(--tf-surface, #ffffff); border-bottom:1px solid var(--tf-border, #e2e8f0);">
                            <th style="min-width:200px; padding:8px 16px;">Tài nguyên hệ thống</th>
                            @foreach (var action in _allActions)
                            {
                                <th style="text-align:center; min-width:70px; padding:8px 4px; font-weight:600; font-size:12px;">
                                    @GetActionLabel(action)
                                </th>
                            }
                            <th style="text-align:center; width:90px; padding:8px 8px; font-weight:600; font-size:12px;">Tất cả</th>
                        </tr>
                    </thead>
                    <tbody>
                        @foreach (var res in groupResources)
                        {
                            int resGranted = _allActions.Count(a => _matrix.TryGetValue((res, a), out var val) && val);
                            bool resAllSelected = resGranted == _allActions.Length;

                            <tr style="border-bottom:1px solid var(--tf-border-subtle, #f1f5f9);">
                                <td style="padding:8px 16px; font-weight:500; color:var(--tf-text-primary);">
                                    <div style="display:flex; align-items:center; gap:6px;">
                                        <span>@GetResourceLabel(res)</span>
                                        <span style="font-size:11px; color:var(--tf-text-muted);">(@res.ToString())</span>
                                    </div>
                                </td>
                                @foreach (var action in _allActions)
                                {
                                    var key = (res, action);
                                    var isChecked = _matrix.TryGetValue(key, out var val) && val;
                                    var isDisabled = _role.IsSystem && _role.Name == "Administrator";

                                    <td style="text-align:center; padding:6px 4px;">
                                        <input type="checkbox"
                                               style="cursor:pointer; width:15px; height:15px; accent-color:#4f46e5;"
                                               checked="@isChecked"
                                               disabled="@isDisabled"
                                               @onchange="@(e => UpdateMatrix(res, action, (bool)(e.Value ?? false)))" />
                                    </td>
                                }
                                <td style="text-align:center; padding:6px 8px;">
                                    <button type="button"
                                            class="tf-btn tf-btn-ghost tf-btn-xs"
                                            style="font-size:11px; padding:1px 6px; color:#4f46e5;"
                                            @onclick="() => ToggleResourceAll(res, !resAllSelected)">
                                        @(resAllSelected ? "Bỏ" : "Chọn")
                                    </button>
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        </div>
    }
}

@code {
    [Parameter] public string? Id { get; set; }
    [Parameter] public string? CloneFromId { get; set; }

    private bool IsEdit => !string.IsNullOrEmpty(Id);
    private bool IsClone => !string.IsNullOrEmpty(CloneFromId);

    private ApplicationRole? _role;
    private string? _errorMessage;
    private string? _currentUser;
    private bool _isSaving;
    private string _permFilter = "";
    private List<ApplicationRole> _availableTemplateRoles = new();

    private readonly PermissionAction[] _allActions =
    [
        PermissionAction.View,
        PermissionAction.Create,
        PermissionAction.Edit,
        PermissionAction.Delete,
        PermissionAction.Approve,
        PermissionAction.Import,
        PermissionAction.Export,
        PermissionAction.OverridePrice
    ];

    private record ModuleGroupDefinition(string Name, ResourceType[] Resources);

    private readonly List<ModuleGroupDefinition> ModuleGroups =
    [
        new("1. Bán hàng & Điểm bán POS",
            [ResourceType.SalesOrders, ResourceType.Quotations, ResourceType.Invoices, ResourceType.PriceLists]),

        new("2. Chấm công & Điểm danh nhân viên",
            [ResourceType.Attendance]),

        new("3. Nhân sự & Hồ sơ nhân viên",
            [ResourceType.Employees, ResourceType.EmployeeRequests, ResourceType.Departments, ResourceType.Positions, ResourceType.Accounts]),

        new("4. Hợp đồng & Phụ lục",
            [ResourceType.Contracts, ResourceType.EmploymentContracts]),

        new("5. Kho hàng & Quản lý tồn kho",
            [ResourceType.Warehouses]),

        new("6. Sản phẩm & Danh mục hàng hóa",
            [ResourceType.Products, ResourceType.ProductCategories, ResourceType.UnitOfMeasures, ResourceType.Currencies]),

        new("7. Đối tác & Khách hàng",
            [ResourceType.Customers, ResourceType.Suppliers]),

        new("8. Hồ sơ chứng từ & Chữ ký số",
            [ResourceType.Documents, ResourceType.DigitalSignatures]),

        new("9. Quản trị hệ thống, Kiểm toán & Bảo mật",
            [ResourceType.Users, ResourceType.Roles, ResourceType.AuditLog, ResourceType.Security, ResourceType.CompanySettings, ResourceType.TechnicalSupport])
    ];

    private Dictionary<(ResourceType, PermissionAction), bool> _matrix = new();

    private int TotalGrantedPermissions => _matrix.Count(m => m.Value);
    private int TotalPossiblePermissions => Enum.GetValues<ResourceType>().Length * _allActions.Length;

    private int GetGrantedModulesCount()
    {
        return ModuleGroups.Count(g => g.Resources.Any(r => _allActions.Any(a => _matrix.TryGetValue((r, a), out var v) && v)));
    }

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        _currentUser = authState.User.Identity?.Name ?? "System";

        _availableTemplateRoles = await RoleManager.Roles.OrderBy(r => r.Name).ToListAsync();

        if (IsEdit)
        {
            _role = await RoleManager.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == Id);
            if (_role == null)
            {
                Nav.NavigateTo("/cai-dat/vai-tro");
                return;
            }

            foreach (var p in _role.Permissions.Where(p => p.IsGranted))
            {
                _matrix[(p.Resource, p.Action)] = true;
            }
        }
        else if (IsClone)
        {
            var sourceRole = await RoleManager.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == CloneFromId);
            if (sourceRole != null)
            {
                _role = new ApplicationRole
                {
                    Name = $"[Bản sao] {sourceRole.Name}",
                    Description = $"Sao chép từ {sourceRole.Name}. {sourceRole.Description}",
                    IsActive = true
                };

                foreach (var p in sourceRole.Permissions.Where(p => p.IsGranted))
                {
                    _matrix[(p.Resource, p.Action)] = true;
                }
            }
            else
            {
                _role = new ApplicationRole { IsActive = true };
            }
        }
        else
        {
            _role = new ApplicationRole { IsActive = true };
        }
    }

    private async Task ApplyRoleTemplate(ChangeEventArgs e)
    {
        var templateId = e.Value?.ToString();
        if (string.IsNullOrEmpty(templateId)) return;

        var templateRole = await RoleManager.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == templateId);
        if (templateRole != null)
        {
            _matrix.Clear();
            foreach (var p in templateRole.Permissions.Where(p => p.IsGranted))
            {
                _matrix[(p.Resource, p.Action)] = true;
            }
            StateHasChanged();
        }
    }

    private void UpdateMatrix(ResourceType r, PermissionAction a, bool val)
    {
        _matrix[(r, a)] = val;
    }

    private void ToggleResourceAll(ResourceType res, bool selectAll)
    {
        foreach (var action in _allActions)
        {
            _matrix[(res, action)] = selectAll;
        }
    }

    private void ToggleGroupPermissions(IEnumerable<ResourceType> resources, bool selectAll)
    {
        foreach (var res in resources)
        {
            foreach (var action in _allActions)
            {
                _matrix[(res, action)] = selectAll;
            }
        }
    }

    private void SelectAllPermissions()
    {
        foreach (ResourceType res in Enum.GetValues<ResourceType>())
        {
            foreach (var act in _allActions)
            {
                _matrix[(res, act)] = true;
            }
        }
    }

    private void ClearAllPermissions()
    {
        _matrix.Clear();
    }

    private bool MatchesFilter(ResourceType res)
    {
        if (string.IsNullOrWhiteSpace(_permFilter)) return true;
        var term = _permFilter.Trim();
        var label = GetResourceLabel(res);
        return label.Contains(term, StringComparison.OrdinalIgnoreCase) ||
               res.ToString().Contains(term, StringComparison.OrdinalIgnoreCase);
    }

    private async Task SaveRole()
    {
        _errorMessage = null;

        if (string.IsNullOrWhiteSpace(_role!.Name))
        {
            _errorMessage = "Tên vai trò không được để trống.";
            return;
        }

        _isSaving = true;
        try
        {
            if (IsEdit)
            {
                var existingRole = await RoleManager.FindByIdAsync(Id!);
                if (existingRole == null)
                {
                    _errorMessage = "Không tìm thấy vai trò cần cập nhật.";
                    return;
                }

                if (!existingRole.IsSystem)
                {
                    existingRole.Name = _role.Name;
                    existingRole.IsActive = _role.IsActive;
                }
                existingRole.Description = _role.Description;

                var updateResult = await RoleManager.UpdateAsync(existingRole);
                if (!updateResult.Succeeded)
                {
                    _errorMessage = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    return;
                }

                var existingPerms = await DbContext.RolePermissions.Where(p => p.RoleId == Id).ToListAsync();
                DbContext.RolePermissions.RemoveRange(existingPerms);

                var newPerms = _matrix.Where(m => m.Value).Select(m => new RolePermission
                {
                    RoleId = Id!,
                    Resource = m.Key.Item1,
                    Action = m.Key.Item2,
                    IsGranted = true,
                    GrantedBy = _currentUser
                });

                DbContext.RolePermissions.AddRange(newPerms);
                await DbContext.SaveChangesAsync();

                await AuditService.LogAsync(
                    AuditEventType.RoleUpdated,
                    _currentUser,
                    "Role",
                    existingRole.Name,
                    $"Cập nhật vai trò '{existingRole.Name}' ({newPerms.Count()} quyền được cấp)");
            }
            else
            {
                _role.CreatedBy = _currentUser;
                var result = await RoleManager.CreateAsync(_role);

                if (!result.Succeeded)
                {
                    _errorMessage = string.Join(", ", result.Errors.Select(e => e.Description));
                    return;
                }

                var newPerms = _matrix.Where(m => m.Value).Select(m => new RolePermission
                {
                    RoleId = _role.Id,
                    Resource = m.Key.Item1,
                    Action = m.Key.Item2,
                    IsGranted = true,
                    GrantedBy = _currentUser
                });

                DbContext.RolePermissions.AddRange(newPerms);
                await DbContext.SaveChangesAsync();

                await AuditService.LogAsync(
                    AuditEventType.RoleCreated,
                    _currentUser,
                    "Role",
                    _role.Name,
                    $"Tạo mới vai trò '{_role.Name}' ({newPerms.Count()} quyền được cấp)");
            }

            PermissionService.InvalidateAllPermissions();
            Nav.NavigateTo("/cai-dat/vai-tro");
        }
        catch (Exception ex)
        {
            _errorMessage = "Đã xảy ra lỗi khi lưu vai trò: " + ex.Message;
        }
        finally
        {
            _isSaving = false;
        }
    }

    private static string GetResourceLabel(ResourceType resource) => resource switch
    {
        ResourceType.Users => "Người dùng & Tài khoản",
        ResourceType.Roles => "Vai trò & Phân quyền",
        ResourceType.CompanySettings => "Cài đặt công ty & Hệ thống",
        ResourceType.AuditLog => "Nhật ký hoạt động & Kiểm toán",
        ResourceType.TechnicalSupport => "Hỗ trợ kỹ thuật",
        ResourceType.Products => "Sản phẩm & Hàng hóa",
        ResourceType.ProductCategories => "Danh mục sản phẩm",
        ResourceType.UnitOfMeasures => "Đơn vị tính",
        ResourceType.Currencies => "Loại tiền tệ",
        ResourceType.Customers => "Khách hàng",
        ResourceType.Suppliers => "Nhà cung cấp",
        ResourceType.Warehouses => "Kho hàng & Tồn kho",
        ResourceType.PriceLists => "Bảng giá sản phẩm",
        ResourceType.Quotations => "Báo giá bán hàng",
        ResourceType.SalesOrders => "Đơn bán hàng & POS",
        ResourceType.Invoices => "Hóa đơn điện tử",
        ResourceType.Documents => "Tài liệu & Chứng từ",
        ResourceType.Security => "An ninh & Bảo mật",
        ResourceType.DigitalSignatures => "Chữ ký số & Người ký",
        ResourceType.Employees => "Hồ sơ nhân viên",
        ResourceType.EmployeeRequests => "Đề xuất thêm nhân sự",
        ResourceType.Departments => "Phòng ban",
        ResourceType.Positions => "Chức danh & Vị trí",
        ResourceType.Contracts => "Hợp đồng thương mại",
        ResourceType.EmploymentContracts => "Hợp đồng lao động",
        ResourceType.Attendance => "Chấm công & Điểm danh",
        ResourceType.Accounts => "Tài khoản nhân viên",
        _ => resource.ToString()
    };

    private static string GetActionLabel(PermissionAction action) => action switch
    {
        PermissionAction.View => "Xem",
        PermissionAction.Create => "Thêm",
        PermissionAction.Edit => "Sửa",
        PermissionAction.Delete => "Xóa",
        PermissionAction.Approve => "Duyệt",
        PermissionAction.Import => "Nhập",
        PermissionAction.Export => "Xuất",
        PermissionAction.OverridePrice => "Đè giá",
        _ => action.ToString()
    };
}
`;

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Admin/Roles/RoleForm.razor', roleFormCode, 'utf8');
console.log('Admin/Roles/RoleForm.razor updated successfully!');
