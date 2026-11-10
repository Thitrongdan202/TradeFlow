const fs = require('fs');

const auditLogCode = `@page "/cai-dat/nhat-ky"
@page "/cai-dat/nhat-ky-he-thong"
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@using TradeFlow.Domain.Entities.Users
@using TradeFlow.Domain.Enums
@using TradeFlow.Infrastructure.Persistence
@attribute [Authorize(Policy = "Permission:AuditLog:View")]

@inject TradeFlowDbContext DbContext

<PageTitle>Nhật ký hệ thống &amp; Kiểm toán — TradeFlow</PageTitle>

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
            <span class="tf-breadcrumb-current">Nhật ký hoạt động</span>
        </div>
        <h1 class="tf-page-title">Nhật ký hoạt động &amp; Kiểm toán hệ thống</h1>
        <p class="tf-page-subtitle">Theo dõi, giám sát và phân loại chi tiết mọi hoạt động truy cập, phê duyệt và thay đổi dữ liệu theo tiêu chuẩn ISO/IEC 27001</p>
    </div>
    <div class="tf-page-actions">
        <button type="button" class="tf-btn tf-btn-secondary" style="display:inline-flex; align-items:center; gap:6px; font-size:13px;" @onclick="ReloadLogsAsync">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <polyline points="23 4 23 10 17 10"></polyline>
                <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
            </svg>
            Làm mới dữ liệu
        </button>
    </div>
</div>

@* Top KPI Cards: Executive Audit Overview *@
<div style="display:grid; grid-template-columns:repeat(auto-fit, minmax(220px, 1fr)); gap:1rem; margin-bottom:1.25rem;">
    <div class="tf-card" style="padding:1.1rem 1.25rem; border-left:4px solid #4f46e5; border-radius:8px;">
        <div style="font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:0.04em; color:var(--tf-text-muted, #64748b);">Tổng số sự kiện</div>
        <div style="font-size:24px; font-weight:700; color:var(--tf-text-primary, #0f172a); margin-top:4px;">
            @(_logs?.Count ?? 0)
        </div>
        <div style="font-size:12px; color:#4f46e5; margin-top:4px; font-weight:500;">Bản ghi gần nhất đã nạp</div>
    </div>

    <div class="tf-card" style="padding:1.1rem 1.25rem; border-left:4px solid #059669; border-radius:8px;">
        <div style="font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:0.04em; color:var(--tf-text-muted, #64748b);">Sự kiện hôm nay</div>
        <div style="font-size:24px; font-weight:700; color:#059669; margin-top:4px;">
            @_todayCount
        </div>
        <div style="font-size:12px; color:var(--tf-text-muted); margin-top:4px;">Từ 00:00 hôm nay</div>
    </div>

    <div class="tf-card" style="padding:1.1rem 1.25rem; border-left:4px solid #f59e0b; border-radius:8px;">
        <div style="font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:0.04em; color:var(--tf-text-muted, #64748b);">Yêu cầu chờ phê duyệt</div>
        <div style="font-size:24px; font-weight:700; color:#d97706; margin-top:4px;">
            @_pendingApprovalsCount
        </div>
        <div style="font-size:12px; color:var(--tf-text-muted); margin-top:4px;">Chấm công, phép &amp; nhân sự</div>
    </div>

    <div class="tf-card" style="padding:1.1rem 1.25rem; border-left:4px solid #ef4444; border-radius:8px;">
        <div style="font-size:12px; font-weight:600; text-transform:uppercase; letter-spacing:0.04em; color:var(--tf-text-muted, #64748b);">Cảnh báo bảo mật / Thao tác</div>
        <div style="font-size:24px; font-weight:700; color:#dc2626; margin-top:4px;">
            @_securityAlertsCount
        </div>
        <div style="font-size:12px; color:var(--tf-text-muted); margin-top:4px;">Đăng nhập lỗi, khóa tài khoản</div>
    </div>
</div>

@* Category Tabs (15 Distinct Groups) *@
<div class="tf-card" style="margin-bottom:1rem; border-radius:8px; overflow:hidden;">
    <div style="display:flex; overflow-x:auto; border-bottom:1px solid var(--tf-border, #e2e8f0); background:var(--tf-surface, #ffffff); scrollbar-width:thin;">
        @foreach (var tab in Categories)
        {
            var isCurrent = _selectedCategory == tab.Id;
            int count = GetCategoryCount(tab.Id);
            <button type="button"
                    style="padding:10px 16px; border:none; background:transparent; font-size:13px; font-weight:@(isCurrent ? "600" : "500"); color:@(isCurrent ? "#4f46e5" : "var(--tf-text-secondary, #475569)"); border-bottom:2px solid @(isCurrent ? "#4f46e5" : "transparent"); cursor:pointer; white-space:nowrap; display:inline-flex; align-items:center; gap:6px; transition:all 0.15s ease;"
                    @onclick="() => SelectCategory(tab.Id)">
                <span>@tab.Name</span>
                @if (count > 0)
                {
                    <span style="font-size:10.5px; font-weight:700; padding:1px 6px; border-radius:10px; background:@(isCurrent ? "#e0e7ff" : "#f1f5f9"); color:@(isCurrent ? "#3730a3" : "#64748b");">
                        @count
                    </span>
                }
            </button>
        }
    </div>

    @* Filters Toolbar *@
    <div class="tf-card-body" style="padding:0.9rem 1.25rem;">
        <div style="display:flex; flex-wrap:wrap; gap:0.75rem; align-items:center; justify-content:space-between;">
            <div style="display:flex; flex-wrap:wrap; gap:0.75rem; align-items:center; flex:1; max-width:850px;">
                @* Search Box *@
                <div style="position:relative; flex:1; min-width:240px;">
                    <svg style="position:absolute; left:10px; top:50%; transform:translateY(-50%); width:14px; height:14px; color:var(--tf-text-muted, #94a3b8);" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/>
                    </svg>
                    <input type="text"
                           class="tf-form-control"
                           style="padding-left:32px; font-size:13px; height:36px; border-radius:6px;"
                           placeholder="Tìm theo người thực hiện, đối tượng, mã hoặc nội dung..."
                           value="@_searchTerm"
                           @oninput="HandleSearchInput" />
                </div>

                @* Time Filter *@
                <select class="tf-form-select"
                        style="width:160px; font-size:13px; height:36px; border-radius:6px;"
                        value="@_timeFilter"
                        @onchange="e => _timeFilter = e.Value?.ToString() ?? 'all'">
                    <option value="all">Tất cả thời gian</option>
                    <option value="today">Hôm nay</option>
                    <option value="7days">7 ngày gần nhất</option>
                    <option value="30days">30 ngày gần nhất</option>
                </select>

                @* Actor / User Filter *@
                <select class="tf-form-select"
                        style="width:180px; font-size:13px; height:36px; border-radius:6px;"
                        value="@_actorFilter"
                        @onchange="e => _actorFilter = e.Value?.ToString() ?? 'all'">
                    <option value="all">Tất cả người thực hiện</option>
                    <option value="admin">Owner / Admin</option>
                    @foreach (var user in DistinctActors)
                    {
                        if (user != "admin" && user != "Administrator" && user != "Owner")
                        {
                            <option value="@user">@user</option>
                        }
                    }
                </select>
            </div>

            <div style="font-size:13px; color:var(--tf-text-muted, #64748b);">
                Hiển thị <strong>@FilteredLogs.Count</strong> / @(_logs?.Count ?? 0) sự kiện
            </div>
        </div>
    </div>
</div>

@* Audit Logs Data Table *@
<div class="tf-card">
    <div class="tf-card-body" style="padding:0;">
        @if (_logs == null)
        {
            <div style="padding:2.5rem; text-align:center; color:var(--tf-text-muted);">
                Đang tải nhật ký kiểm toán...
            </div>
        }
        else
        {
            <div class="tf-table-responsive" style="overflow-x:auto;">
                <table class="tf-table" style="width:100%; border-collapse:collapse; margin:0;">
                    <thead>
                        <tr>
                            <th style="width:160px; white-space:nowrap; padding:10px 14px;">Thời gian</th>
                            <th style="width:180px; white-space:nowrap; padding:10px 12px;">Loại sự kiện</th>
                            <th style="width:160px; white-space:nowrap; padding:10px 12px;">Người thực hiện</th>
                            <th style="width:170px; white-space:nowrap; padding:10px 12px;">Đối tượng tác động</th>
                            <th style="padding:10px 12px;">Mô tả chi tiết</th>
                            <th style="width:120px; white-space:nowrap; text-align:right; padding:10px 14px;">Địa chỉ IP</th>
                            <th style="width:80px; text-align:center; padding:10px 8px;">Chi tiết</th>
                        </tr>
                    </thead>
                    <tbody>
                        @if (!FilteredLogs.Any())
                        {
                            <tr>
                                <td colspan="7" style="padding:3rem 1rem; text-align:center; color:var(--tf-text-muted);">
                                    <div style="font-size:14px; font-weight:600; color:var(--tf-text-primary);">Không tìm thấy sự kiện nào trong nhóm này</div>
                                    <div style="font-size:12.5px; margin-top:4px;">Thử chuyển sang tab khác hoặc xóa bộ lọc tìm kiếm.</div>
                                </td>
                            </tr>
                        }
                        else
                        {
                            foreach (var log in FilteredLogs)
                            {
                                <tr style="cursor:pointer; transition:background-color 0.12s ease;"
                                    class="tf-audit-row"
                                    @onclick="() => OpenDetail(log)">
                                    <td style="color:var(--tf-text-muted); font-size:12px; white-space:nowrap; padding:9px 14px;">
                                        @log.OccurredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
                                    </td>
                                    <td style="white-space:nowrap; padding:9px 12px;">
                                        <span class="tf-badge @GetEventBadgeClass(log.EventType)" style="display:inline-flex; align-items:center; gap:4px; font-size:11.5px; font-weight:500; padding:2px 8px; border-radius:4px;">
                                            @GetEventFriendlyName(log.EventType)
                                        </span>
                                    </td>
                                    <td style="white-space:nowrap; padding:9px 12px;">
                                        @if (IsAdminActor(log.PerformedBy))
                                        {
                                            <span style="display:inline-flex; align-items:center; gap:5px; font-weight:600; font-size:12.5px; color:#0284c7;">
                                                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                                                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                                                </svg>
                                                Owner / Admin
                                            </span>
                                        }
                                        else
                                        {
                                            <span style="font-weight:500; font-size:13px; color:var(--tf-text-primary);">
                                                @(string.IsNullOrEmpty(log.PerformedBy) ? "Hệ thống" : log.PerformedBy)
                                            </span>
                                        }
                                    </td>
                                    <td style="white-space:nowrap; font-size:12.5px; color:var(--tf-text-secondary, #334155); padding:9px 12px;">
                                        @FormatTargetEntity(log.TargetEntity, log.TargetId)
                                    </td>
                                    <td style="font-size:12.5px; color:var(--tf-text-primary); line-height:1.45; padding:9px 12px; max-width:420px; overflow:hidden; text-overflow:ellipsis; white-space:nowrap;" title="@FixMojibake(log.Details)">
                                        @FixMojibake(log.Details)
                                    </td>
                                    <td style="text-align:right; font-size:11.5px; color:var(--tf-text-muted); font-family:monospace; white-space:nowrap; padding:9px 14px;">
                                        @(string.IsNullOrEmpty(log.IpAddress) ? "—" : log.IpAddress)
                                    </td>
                                    <td style="text-align:center; padding:9px 8px;">
                                        <button type="button" class="tf-btn tf-btn-ghost tf-btn-xs" style="font-size:11.5px; padding:2px 6px;" @onclick:stopPropagation="true" @onclick="() => OpenDetail(log)">
                                            Xem
                                        </button>
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

@* Event Detail Modal / Slide-out Drawer *@
@if (_selectedLog != null)
{
    <div style="position:fixed; top:0; left:0; right:0; bottom:0; background:rgba(15, 23, 42, 0.45); z-index:1050; display:flex; justify-content:center; align-items:center; padding:1.5rem;"
         @onclick="CloseDetail">
        <div class="tf-card"
             style="max-width:680px; width:100%; border-radius:12px; box-shadow:0 20px 25px -5px rgba(0,0,0,0.1), 0 8px 10px -6px rgba(0,0,0,0.1); background:var(--tf-surface, #ffffff); overflow:hidden;"
             @onclick:stopPropagation="true">
            <div class="tf-card-header" style="padding:1.25rem 1.5rem; border-bottom:1px solid var(--tf-border, #e2e8f0); display:flex; align-items:center; justify-content:space-between;">
                <div>
                    <div style="display:flex; align-items:center; gap:8px;">
                        <span class="tf-badge @GetEventBadgeClass(_selectedLog.EventType)" style="font-size:12px; font-weight:600; padding:3px 8px; border-radius:4px;">
                            @GetEventFriendlyName(_selectedLog.EventType)
                        </span>
                        <span style="font-size:12px; color:var(--tf-text-muted);">Mã log #@_selectedLog.Id</span>
                    </div>
                    <h3 style="font-size:16px; font-weight:700; margin:6px 0 0; color:var(--tf-text-primary);">Chi tiết sự kiện kiểm toán</h3>
                </div>
                <button type="button" style="background:transparent; border:none; font-size:20px; cursor:pointer; color:var(--tf-text-muted);" @onclick="CloseDetail">&times;</button>
            </div>

            <div class="tf-card-body" style="padding:1.5rem; max-height:calc(85vh - 120px); overflow-y:auto;">
                <div style="display:grid; grid-template-columns:1fr 1fr; gap:1.25rem; margin-bottom:1.25rem;">
                    <div>
                        <div style="font-size:11.5px; font-weight:600; color:var(--tf-text-muted); text-transform:uppercase;">Ai thực hiện?</div>
                        <div style="font-size:14px; font-weight:600; color:var(--tf-text-primary); margin-top:3px;">
                            @(IsAdminActor(_selectedLog.PerformedBy) ? "Owner / Admin (Toàn quyền)" : (_selectedLog.PerformedBy ?? "Hệ thống tự động"))
                        </div>
                    </div>

                    <div>
                        <div style="font-size:11.5px; font-weight:600; color:var(--tf-text-muted); text-transform:uppercase;">Thời gian ghi nhận</div>
                        <div style="font-size:14px; font-weight:600; color:var(--tf-text-primary); margin-top:3px;">
                            @_selectedLog.OccurredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss") (GMT+7)
                        </div>
                    </div>

                    <div>
                        <div style="font-size:11.5px; font-weight:600; color:var(--tf-text-muted); text-transform:uppercase;">Đối tượng bị tác động</div>
                        <div style="font-size:13.5px; font-weight:500; color:var(--tf-text-primary); margin-top:3px;">
                            @FormatTargetEntity(_selectedLog.TargetEntity, _selectedLog.TargetId)
                        </div>
                    </div>

                    <div>
                        <div style="font-size:11.5px; font-weight:600; color:var(--tf-text-muted); text-transform:uppercase;">Địa chỉ IP &amp; Kết nối</div>
                        <div style="font-size:13.5px; font-family:monospace; color:var(--tf-text-primary); margin-top:3px;">
                            @(_selectedLog.IpAddress ?? "Mạng nội bộ / Localhost")
                        </div>
                    </div>
                </div>

                <div style="margin-bottom:1.25rem;">
                    <div style="font-size:11.5px; font-weight:600; color:var(--tf-text-muted); text-transform:uppercase; margin-bottom:4px;">Mô tả hành động &amp; Lý do nghiệp vụ</div>
                    <div style="background:var(--tf-bg-alt, #f8fafc); border:1px solid var(--tf-border, #e2e8f0); border-radius:6px; padding:12px; font-size:13px; line-height:1.5; color:var(--tf-text-primary);">
                        @FixMojibake(_selectedLog.Details)
                    </div>
                </div>

                @if (!string.IsNullOrEmpty(_selectedLog.UserAgent))
                {
                    <div style="margin-bottom:1.25rem;">
                        <div style="font-size:11.5px; font-weight:600; color:var(--tf-text-muted); text-transform:uppercase; margin-bottom:4px;">Thông tin môi trường / Trình duyệt</div>
                        <div style="font-size:11.5px; font-family:monospace; color:#64748b; background:var(--tf-bg-alt, #f8fafc); padding:6px 10px; border-radius:4px; word-break:break-all;">
                            @_selectedLog.UserAgent
                        </div>
                    </div>
                }

                <div style="display:flex; align-items:center; justify-content:space-between; padding-top:1rem; border-top:1px solid var(--tf-border, #e2e8f0);">
                    <div style="font-size:12px; color:#059669; font-weight:600; display:inline-flex; align-items:center; gap:4px;">
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
                        Ghi nhận hợp lệ theo tiêu chuẩn kiểm toán
                    </div>
                    <button type="button" class="tf-btn tf-btn-secondary tf-btn-sm" @onclick="CloseDetail">Đóng</button>
                </div>
            </div>
        </div>
    </div>
}

@code {
    private List<AuditLog>? _logs;
    private string _searchTerm = "";
    private string _selectedCategory = "all";
    private string _timeFilter = "all";
    private string _actorFilter = "all";
    private AuditLog? _selectedLog;

    private int _todayCount;
    private int _pendingApprovalsCount;
    private int _securityAlertsCount;

    private record CategoryTab(string Id, string Name);

    private readonly List<CategoryTab> Categories =
    [
        new("all", "Tất cả"),
        new("auth", "Đăng nhập & Phiên"),
        new("accounts", "Tài khoản"),
        new("roles", "Vai trò & Quyền"),
        new("hr", "Nhân sự"),
        new("attendance", "Chấm công & Nghỉ phép"),
        new("sales", "Bán hàng & POS"),
        new("invoices", "Hóa đơn"),
        new("products", "Sản phẩm & Giá"),
        new("warehouses", "Kho hàng"),
        new("partners", "Đối tác & Khách"),
        new("contracts", "Hợp đồng"),
        new("documents", "Chứng từ"),
        new("approvals", "Phê duyệt & Từ chối"),
        new("security", "Bảo mật & Cảnh báo"),
        new("settings", "Cấu hình hệ thống")
    ];

    private List<string> DistinctActors => _logs?
        .Where(l => !string.IsNullOrEmpty(l.PerformedBy))
        .Select(l => l.PerformedBy!)
        .Distinct()
        .OrderBy(a => a)
        .ToList() ?? [];

    private List<AuditLog> FilteredLogs
    {
        get
        {
            if (_logs == null) return new List<AuditLog>();
            var q = _logs.AsEnumerable();

            // 1. Category Filter
            if (_selectedCategory != "all")
            {
                q = q.Where(x => MatchesCategory(x, _selectedCategory));
            }

            // 2. Time Filter
            var now = DateTime.UtcNow;
            if (_timeFilter == "today")
            {
                var todayUtc = DateTime.UtcNow.Date;
                q = q.Where(x => x.OccurredAt.Date == todayUtc || x.OccurredAt.ToLocalTime().Date == DateTime.Now.Date);
            }
            else if (_timeFilter == "7days")
            {
                var sevenDaysAgo = now.AddDays(-7);
                q = q.Where(x => x.OccurredAt >= sevenDaysAgo);
            }
            else if (_timeFilter == "30days")
            {
                var thirtyDaysAgo = now.AddDays(-30);
                q = q.Where(x => x.OccurredAt >= thirtyDaysAgo);
            }

            // 3. Actor Filter
            if (_actorFilter == "admin")
            {
                q = q.Where(x => IsAdminActor(x.PerformedBy));
            }
            else if (_actorFilter != "all")
            {
                q = q.Where(x => string.Equals(x.PerformedBy, _actorFilter, StringComparison.OrdinalIgnoreCase));
            }

            // 4. Search Filter
            if (!string.IsNullOrWhiteSpace(_searchTerm))
            {
                var term = _searchTerm.Trim();
                q = q.Where(x =>
                    (x.PerformedBy != null && x.PerformedBy.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (x.TargetEntity != null && x.TargetEntity.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (x.TargetId != null && x.TargetId.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Details != null && x.Details.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    GetEventFriendlyName(x.EventType).Contains(term, StringComparison.OrdinalIgnoreCase)
                );
            }

            return q.ToList();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadLogsAsync();
    }

    private async Task ReloadLogsAsync()
    {
        await LoadLogsAsync();
    }

    private async Task LoadLogsAsync()
    {
        _logs = await DbContext.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.OccurredAt)
            .Take(500)
            .ToListAsync();

        CalculateKpis();
    }

    private void CalculateKpis()
    {
        if (_logs == null) return;

        var today = DateTime.Now.Date;
        _todayCount = _logs.Count(l => l.OccurredAt.ToLocalTime().Date == today);

        _securityAlertsCount = _logs.Count(l =>
            l.EventType == AuditEventType.LoginFailed ||
            l.EventType == AuditEventType.UserLocked ||
            l.EventType == AuditEventType.PasswordRecoveryRequested ||
            l.EventType == AuditEventType.DocumentTampered);

        // Pending approvals from attendance & employee requests
        try
        {
            var p1 = DbContext.AttendanceAdjustmentRequests.Count(r => r.Status == AttendanceRequestStatus.Pending);
            var p2 = DbContext.LeaveRequests.Count(r => r.Status == AttendanceRequestStatus.Pending);
            var p3 = DbContext.EmployeeRequests.Count(r => r.Status == EmployeeRequestStatus.Pending);
            _pendingApprovalsCount = p1 + p2 + p3;
        }
        catch
        {
            _pendingApprovalsCount = 0;
        }
    }

    private int GetCategoryCount(string catId)
    {
        if (_logs == null) return 0;
        if (catId == "all") return _logs.Count;
        return _logs.Count(x => MatchesCategory(x, catId));
    }

    private void SelectCategory(string catId)
    {
        _selectedCategory = catId;
    }

    private void OpenDetail(AuditLog log)
    {
        _selectedLog = log;
    }

    private void CloseDetail()
    {
        _selectedLog = null;
    }

    private void HandleSearchInput(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? "";
    }

    private static bool MatchesCategory(AuditLog log, string category)
    {
        return category switch
        {
            "auth" => log.EventType == AuditEventType.Login ||
                      log.EventType == AuditEventType.LoginFailed ||
                      log.EventType == AuditEventType.Logout,

            "accounts" => (log.EventType >= AuditEventType.UserCreated && log.EventType <= AuditEventType.PasswordRecoveryRequested) ||
                          log.EventType == AuditEventType.UserOffboarded,

            "roles" => log.EventType >= AuditEventType.RoleCreated && log.EventType <= AuditEventType.PermissionRevoked,

            "hr" => (log.EventType >= AuditEventType.EmployeeCreated && log.EventType <= AuditEventType.EmployeeRequestResubmitted) ||
                    log.EventType == AuditEventType.UserOffboarded,

            "attendance" => (log.EventType >= AuditEventType.AttendanceClockIn && log.EventType <= AuditEventType.ShiftChangeRejected) ||
                            (log.TargetEntity != null && (log.TargetEntity.Contains("Attendance") || log.TargetEntity.Contains("Leave"))),

            "sales" => log.EventType >= AuditEventType.SalesOrderCreated && log.EventType <= AuditEventType.SalesOrderDeleted,

            "invoices" => (log.EventType >= AuditEventType.InvoiceCreated && log.EventType <= AuditEventType.InvoiceExportedPdf) ||
                          (log.TargetEntity != null && log.TargetEntity.Contains("Invoice")),

            "products" => (log.EventType >= AuditEventType.ProductCreated && log.EventType <= AuditEventType.ProductImageMapped) ||
                          (log.EventType >= AuditEventType.PriceListCreated && log.EventType <= AuditEventType.PriceChanged),

            "warehouses" => (log.EventType >= AuditEventType.WarehouseCreated && log.EventType <= AuditEventType.WarehouseDeleted) ||
                            (log.EventType >= AuditEventType.StocktakeCreated && log.EventType <= AuditEventType.DefectiveStockDisposed),

            "partners" => (log.EventType >= AuditEventType.CustomerCreated && log.EventType <= AuditEventType.SupplierDeleted) ||
                          (log.EventType >= AuditEventType.DebtPaymentRecorded && log.EventType <= AuditEventType.DebtExported),

            "contracts" => (log.EventType >= AuditEventType.ContractCreated && log.EventType <= AuditEventType.ContractDeleted) ||
                           (log.EventType >= AuditEventType.EmploymentContractCreated && log.EventType <= AuditEventType.EmploymentContractDeleted),

            "documents" => (log.EventType >= AuditEventType.DocumentUploaded && log.EventType <= AuditEventType.DocumentDownloaded) ||
                           (log.EventType >= AuditEventType.SignerEnrolled && log.EventType <= AuditEventType.SignerProfileUpdated),

            "approvals" => log.EventType == AuditEventType.EmployeeRequestApproved ||
                           log.EventType == AuditEventType.EmployeeRequestRejected ||
                           log.EventType == AuditEventType.AttendanceAdjustmentApproved ||
                           log.EventType == AuditEventType.AttendanceAdjustmentRejected ||
                           log.EventType == AuditEventType.LeaveRequestApproved ||
                           log.EventType == AuditEventType.LeaveRequestRejected ||
                           log.EventType == AuditEventType.ShiftChangeApproved ||
                           log.EventType == AuditEventType.ShiftChangeRejected ||
                           log.EventType == AuditEventType.PriceListApproved ||
                           log.EventType == AuditEventType.StocktakeApproved ||
                           log.EventType == AuditEventType.WarehouseVoucherApproved ||
                           (log.Details != null && (log.Details.Contains("duyệt") || log.Details.Contains("Từ chối"))),

            "security" => log.EventType == AuditEventType.LoginFailed ||
                          log.EventType == AuditEventType.UserLocked ||
                          log.EventType == AuditEventType.DocumentTampered ||
                          log.EventType == AuditEventType.SignerLockedOut ||
                          log.EventType == AuditEventType.SecuritySettingsUpdated ||
                          log.EventType == AuditEventType.SupportAccessEnabled ||
                          log.EventType == AuditEventType.SupportAccessRevoked,

            "settings" => log.EventType == AuditEventType.CompanySettingsChanged ||
                           log.EventType == AuditEventType.DatabaseBackupCreated ||
                           log.EventType == AuditEventType.DatabaseBackupRestored,

            _ => true
        };
    }

    private static bool IsAdminActor(string? actor)
    {
        if (string.IsNullOrEmpty(actor)) return false;
        return string.Equals(actor, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(actor, "Administrator", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(actor, "Owner", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetEventFriendlyName(AuditEventType eventType) => eventType switch
    {
        AuditEventType.Login => "Đăng nhập hệ thống",
        AuditEventType.LoginFailed => "Đăng nhập thất bại",
        AuditEventType.Logout => "Đăng xuất hệ thống",

        AuditEventType.UserCreated => "Cấp tài khoản mới",
        AuditEventType.UserUpdated => "Cập nhật tài khoản",
        AuditEventType.UserLocked => "Khóa tài khoản",
        AuditEventType.UserUnlocked => "Mở khóa tài khoản",
        AuditEventType.PasswordChanged => "Đổi mật khẩu",
        AuditEventType.PasswordReset => "Đặt lại mật khẩu",
        AuditEventType.PasswordViewed => "Xem mật khẩu tạm",
        AuditEventType.PasswordRecoveryRequested => "Yêu cầu khôi phục mật khẩu",
        AuditEventType.UserOffboarded => "Hoàn tất thôi việc",

        AuditEventType.RoleCreated => "Tạo vai trò mới",
        AuditEventType.RoleUpdated => "Cập nhật vai trò & quyền",
        AuditEventType.RoleDeleted => "Xóa vai trò",
        AuditEventType.PermissionAssigned => "Gán quyền người dùng",
        AuditEventType.PermissionRevoked => "Thu hồi quyền người dùng",

        AuditEventType.EmployeeCreated => "Thêm nhân viên mới",
        AuditEventType.EmployeeUpdated => "Cập nhật hồ sơ nhân viên",
        AuditEventType.EmployeeStatusChanged => "Đổi trạng thái nhân viên",
        AuditEventType.EmployeeRetired => "Thôi việc nhân viên",
        AuditEventType.EmployeeLinkedToUser => "Liên kết tài khoản người dùng",
        AuditEventType.EmployeeUnlinkedFromUser => "Hủy liên kết tài khoản",
        AuditEventType.EmployeeRequestSubmitted => "Nộp đề xuất nhân sự",
        AuditEventType.EmployeeRequestApproved => "Phê duyệt đề xuất nhân sự",
        AuditEventType.EmployeeRequestRejected => "Từ chối đề xuất nhân sự",

        AuditEventType.EmploymentContractCreated => "Tạo hợp đồng lao động",
        AuditEventType.EmploymentContractUpdated => "Cập nhật HĐ lao động",
        AuditEventType.EmploymentContractFinalized => "Chốt hợp đồng lao động",
        AuditEventType.EmploymentContractSigned => "Ký hợp đồng lao động",
        AuditEventType.EmploymentContractTerminated => "Chấm dứt HĐ lao động",

        AuditEventType.AttendanceClockIn => "Chấm công vào ca",
        AuditEventType.AttendanceClockOut => "Chấm công ra ca",
        AuditEventType.AttendanceAdjustmentSubmitted => "Nộp giải trình công",
        AuditEventType.AttendanceAdjustmentApproved => "Duyệt giải trình công",
        AuditEventType.AttendanceAdjustmentRejected => "Từ chối giải trình công",
        AuditEventType.LeaveRequestSubmitted => "Nộp đơn xin nghỉ phép",
        AuditEventType.LeaveRequestApproved => "Duyệt đơn nghỉ phép",
        AuditEventType.LeaveRequestRejected => "Từ chối đơn nghỉ phép",
        AuditEventType.LeaveBalanceAdjusted => "Điều chỉnh số dư phép",
        AuditEventType.ShiftChangeSubmitted => "Nộp đơn đổi ca",
        AuditEventType.ShiftChangeApproved => "Duyệt đơn đổi ca",
        AuditEventType.ShiftChangeRejected => "Từ chối đơn đổi ca",

        AuditEventType.SalesOrderCreated => "Tạo đơn bán hàng",
        AuditEventType.SalesOrderConfirmed => "Xác nhận đơn bán hàng",
        AuditEventType.SalesOrderUpdated => "Cập nhật đơn bán hàng",
        AuditEventType.SalesOrderCancelled => "Hủy đơn bán hàng",

        AuditEventType.InvoiceCreated => "Tạo hóa đơn",
        AuditEventType.InvoiceIssued => "Phát hành hóa đơn",
        AuditEventType.InvoiceAdjusted => "Điều chỉnh hóa đơn",
        AuditEventType.InvoiceCancelled => "Hủy hóa đơn",

        AuditEventType.QuotationCreated => "Tạo báo giá",
        AuditEventType.QuotationConverted => "Báo giá chuyển thành đơn",

        AuditEventType.ProductCreated => "Thêm sản phẩm mới",
        AuditEventType.ProductUpdated => "Cập nhật sản phẩm",
        AuditEventType.CustomerCreated => "Thêm khách hàng",
        AuditEventType.CustomerUpdated => "Cập nhật khách hàng",
        AuditEventType.SupplierCreated => "Thêm nhà cung cấp",
        AuditEventType.WarehouseCreated => "Thêm kho hàng mới",

        AuditEventType.WarehouseVoucherCreated => "Tạo phiếu xuất/nhập kho",
        AuditEventType.WarehouseVoucherApproved => "Duyệt phiếu xuất/nhập kho",

        AuditEventType.ContractCreated => "Tạo hợp đồng thương mại",
        AuditEventType.ContractSigned => "Ký hợp đồng thương mại",
        AuditEventType.ContractFinalized => "Hoàn tất hợp đồng",

        AuditEventType.DatabaseBackupCreated => "Sao lưu dữ liệu hệ thống",
        AuditEventType.DatabaseBackupRestored => "Phục hồi dữ liệu hệ thống",
        AuditEventType.CompanySettingsChanged => "Cập nhật thông tin công ty",

        AuditEventType.SupportAccessEnabled => "Kích hoạt phiên hỗ trợ",
        AuditEventType.SupportAccessRevoked => "Thu hồi phiên hỗ trợ",
        AuditEventType.SupportActionPerformed => "Thao tác hỗ trợ kỹ thuật",

        _ => eventType.ToString()
    };

    private static string GetEventBadgeClass(AuditEventType eventType) => eventType switch
    {
        AuditEventType.Login => "tf-badge-success",
        AuditEventType.LoginFailed => "tf-badge-danger",
        AuditEventType.Logout => "tf-badge-gray",

        AuditEventType.UserCreated => "tf-badge-primary",
        AuditEventType.UserUpdated => "tf-badge-primary",
        AuditEventType.UserLocked => "tf-badge-warning",
        AuditEventType.UserUnlocked => "tf-badge-success",
        AuditEventType.PasswordChanged or AuditEventType.PasswordReset => "tf-badge-primary",
        AuditEventType.UserOffboarded => "tf-badge-danger",

        AuditEventType.EmployeeCreated or AuditEventType.EmployeeUpdated => "tf-badge-primary",
        AuditEventType.EmployeeRequestApproved => "tf-badge-success",
        AuditEventType.EmployeeRequestRejected or AuditEventType.EmployeeRetired => "tf-badge-danger",

        AuditEventType.AttendanceClockIn or AuditEventType.AttendanceClockOut => "tf-badge-primary",
        AuditEventType.AttendanceAdjustmentApproved or AuditEventType.LeaveRequestApproved or AuditEventType.ShiftChangeApproved => "tf-badge-success",
        AuditEventType.AttendanceAdjustmentRejected or AuditEventType.LeaveRequestRejected or AuditEventType.ShiftChangeRejected => "tf-badge-danger",

        AuditEventType.SalesOrderCreated or AuditEventType.SalesOrderConfirmed => "tf-badge-success",
        AuditEventType.SalesOrderCancelled => "tf-badge-danger",
        AuditEventType.InvoiceIssued => "tf-badge-success",
        AuditEventType.InvoiceCancelled => "tf-badge-danger",

        AuditEventType.SupportAccessEnabled or AuditEventType.SupportAccessRevoked => "tf-badge-warning",

        _ => "tf-badge-secondary"
    };

    private static string FormatTargetEntity(string? entity, string? id)
    {
        if (string.IsNullOrEmpty(entity) && string.IsNullOrEmpty(id)) return "—";

        var formattedEntity = entity switch
        {
            "ApplicationUser" => "Tài khoản",
            "EmployeeAccount" => "Tài khoản nhân viên",
            "Employee" => "Nhân sự",
            "EmploymentContract" => "HĐ lao động",
            "AttendanceAdjustmentRequest" => "Giải trình chấm công",
            "LeaveRequest" => "Đơn xin nghỉ phép",
            "ShiftChangeRequest" => "Đơn xin đổi ca",
            "SalesOrder" => "Đơn hàng",
            "Invoice" => "Hóa đơn",
            "Quotation" => "Báo giá",
            "Product" => "Sản phẩm",
            "Customer" => "Khách hàng",
            "Supplier" => "Nhà cung cấp",
            "Warehouse" or "WarehouseVoucher" => "Kho hàng",
            "Contract" => "Hợp đồng",
            "Role" => "Vai trò",
            _ => entity ?? ""
        };

        if (string.IsNullOrEmpty(id)) return formattedEntity;
        return $"{formattedEntity} (#{id})";
    }

    private static string FixMojibake(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        if (input.Contains("NgÆ°") || input.Contains("Ã") || input.Contains("á»") || input.Contains("Ä‘"))
        {
            try
            {
                var latin1 = System.Text.Encoding.GetEncoding("ISO-8859-1");
                var bytes = latin1.GetBytes(input);
                var utf8Str = System.Text.Encoding.UTF8.GetString(bytes);
                if (!utf8Str.Contains(''))
                {
                    return utf8Str;
                }
            }
            catch { }

            if (input.Contains("NgÆ°á»"))
            {
                return "Người dùng đăng xuất";
            }
        }

        return input;
    }
}
`;

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Admin/AuditLogs/List.razor', auditLogCode, 'utf8');
console.log('Admin/AuditLogs/List.razor updated successfully!');
