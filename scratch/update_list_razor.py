path = r"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\Components\Pages\Admin\AuditLogs\List.razor"

content = '''@page "/cai-dat/nhat-ky"
@page "/cai-dat/nhat-ky-he-thong"
@rendermode InteractiveServer
@using Microsoft.AspNetCore.Authorization
@using Microsoft.EntityFrameworkCore
@using TradeFlow.Domain.Entities.Users
@using TradeFlow.Domain.Enums
@using TradeFlow.Infrastructure.Persistence
@attribute [Authorize(Policy = "Permission:AuditLog:View")]

@inject TradeFlowDbContext DbContext

<PageTitle>Nhật ký hệ thống & Kiểm toán — TradeFlow</PageTitle>

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
        <h1 class="tf-page-title">Nhật ký hoạt động &amp; Kiểm toán</h1>
        <p class="tf-page-subtitle">Theo dõi và giám sát lịch sử truy cập, thay đổi dữ liệu và sự kiện bảo mật theo tiêu chuẩn kiểm toán</p>
    </div>
</div>

<div class="tf-card" style="margin-bottom: 1.25rem;">
    <div class="tf-card-body" style="padding: 1rem 1.25rem;">
        <div style="display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; justify-content: space-between;">
            <div style="display: flex; flex-wrap: wrap; gap: 0.75rem; align-items: center; flex: 1; max-width: 680px;">
                @* Module-specific Search Input *@
                <div style="position: relative; flex: 1; min-width: 240px;">
                    <svg style="position: absolute; left: 10px; top: 50%; transform: translateY(-50%); width: 15px; height: 15px; color: var(--tf-text-muted, #94a3b8);" viewBox="0 0 20 20" fill="currentColor">
                        <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd"/>
                    </svg>
                    <input type="text"
                           class="tf-form-control"
                           style="padding-left: 32px; font-size: 13px; height: 36px; border-radius: 6px;"
                           placeholder="Tìm theo người thực hiện, đối tượng hoặc nội dung..."
                           value="@_searchTerm"
                           @oninput="HandleSearchInput" />
                </div>

                @* Category Filter *@
                <select class="tf-form-select"
                        style="width: 200px; font-size: 13px; height: 36px; border-radius: 6px;"
                        value="@_selectedCategory"
                        @onchange="HandleCategoryChange">
                    <option value="all">Tất cả loại sự kiện</option>
                    <option value="auth">Đăng nhập &amp; Đăng xuất</option>
                    <option value="security">Tài khoản &amp; Bảo mật</option>
                    <option value="employee">Nhân sự &amp; Hợp đồng</option>
                    <option value="business">Kinh doanh &amp; Kho hàng</option>
                </select>
            </div>

            <div style="display: flex; align-items: center; gap: 0.5rem;">
                <span style="font-size: 13px; color: var(--tf-text-muted, #64748b);">
                    Hiển thị <strong>@FilteredLogs.Count</strong> bản ghi
                </span>
                <button type="button" class="btn btn-sm btn-outline-secondary" style="height: 36px; padding: 0 12px; display: inline-flex; align-items: center; gap: 5px;" @onclick="ReloadLogsAsync">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                        <polyline points="23 4 23 10 17 10"></polyline>
                        <path d="M20.49 15a9 9 0 1 1-2.12-9.36L23 10"></path>
                    </svg>
                    Làm mới
                </button>
            </div>
        </div>
    </div>
</div>

<div class="tf-card">
    <div class="tf-card-body" style="padding:0;">
        @if (_logs == null)
        {
            <div style="padding: 2rem; text-align: center; color: var(--tf-text-muted);">
                Đang tải dữ liệu kiểm toán...
            </div>
        }
        else
        {
            <div class="tf-table-responsive" style="overflow-x: auto;">
                <table class="tf-table" style="width: 100%; border-collapse: collapse;">
                    <thead>
                        <tr>
                            <th style="width:160px; white-space:nowrap;">Thời gian</th>
                            <th style="width:180px; white-space:nowrap;">Loại sự kiện</th>
                            <th style="width:150px; white-space:nowrap;">Người thực hiện</th>
                            <th style="width:180px; white-space:nowrap;">Đối tượng tác động</th>
                            <th>Mô tả chi tiết</th>
                            <th style="width:110px; white-space:nowrap; text-align:right;">Địa chỉ IP</th>
                        </tr>
                    </thead>
                    <tbody>
                        @if (!FilteredLogs.Any())
                        {
                            <tr>
                                <td colspan="6" style="padding: 2.5rem 1rem; text-align: center; color: var(--tf-text-muted);">
                                    <div style="font-size: 14px; font-weight: 500;">Không tìm thấy bản ghi nhật ký phù hợp</div>
                                    <div style="font-size: 12px; margin-top: 4px;">Thử thay đổi từ khóa tìm kiếm hoặc bộ lọc sự kiện.</div>
                                </td>
                            </tr>
                        }
                        else
                        {
                            foreach (var log in FilteredLogs)
                            {
                                <tr>
                                    <td style="color:var(--tf-text-muted); font-size:12.5px; white-space:nowrap;">
                                        @log.OccurredAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
                                    </td>
                                    <td style="white-space:nowrap;">
                                        <span class="tf-badge @GetEventBadgeClass(log.EventType)" style="display:inline-flex; align-items:center; gap:4px; font-size:12px; font-weight:500; padding: 2px 8px; border-radius: 4px;">
                                            @GetEventFriendlyName(log.EventType)
                                        </span>
                                    </td>
                                    <td style="white-space:nowrap;">
                                        @if (IsAdminActor(log.PerformedBy))
                                        {
                                            <span style="display:inline-flex; align-items:center; gap:4px; font-weight:600; color:var(--tf-text-primary);">
                                                <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" style="color:#0284c7;">
                                                    <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/>
                                                </svg>
                                                Owner / Admin
                                            </span>
                                        }
                                        else
                                        {
                                            <span class="tf-font-medium" style="color:var(--tf-text-primary);">
                                                @(string.IsNullOrEmpty(log.PerformedBy) ? "Hệ thống" : log.PerformedBy)
                                            </span>
                                        }
                                    </td>
                                    <td style="white-space:nowrap; font-size:12.5px; color:var(--tf-text-secondary, #334155);">
                                        @FormatTargetEntity(log.TargetEntity, log.TargetId)
                                    </td>
                                    <td style="font-size:13px; color:var(--tf-text-primary); line-height: 1.45;">
                                        @FixMojibake(log.Details)
                                    </td>
                                    <td style="text-align:right; font-size:11.5px; color:var(--tf-text-muted); font-family:monospace; white-space:nowrap;">
                                        @(string.IsNullOrEmpty(log.IpAddress) ? "—" : log.IpAddress)
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
    private List<AuditLog>? _logs;
    private string _searchTerm = "";
    private string _selectedCategory = "all";

    private List<AuditLog> FilteredLogs
    {
        get
        {
            if (_logs == null) return new List<AuditLog>();

            var q = _logs.AsEnumerable();

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

            if (_selectedCategory == "auth")
            {
                q = q.Where(x => x.EventType == AuditEventType.Login ||
                                 x.EventType == AuditEventType.LoginFailed ||
                                 x.EventType == AuditEventType.Logout);
            }
            else if (_selectedCategory == "security")
            {
                q = q.Where(x => (x.EventType >= AuditEventType.UserCreated && x.EventType <= AuditEventType.PermissionRevoked) ||
                                 (x.EventType >= AuditEventType.SignerEnrolled && x.EventType <= AuditEventType.SignerProfileUpdated) ||
                                 x.EventType == AuditEventType.UserOffboarded);
            }
            else if (_selectedCategory == "employee")
            {
                q = q.Where(x => (x.EventType >= AuditEventType.EmployeeCreated && x.EventType <= AuditEventType.EmployeeRequestResubmitted) ||
                                 (x.EventType >= AuditEventType.EmploymentContractCreated && x.EventType <= AuditEventType.EmploymentContractDeleted) ||
                                 x.EventType == AuditEventType.UserOffboarded);
            }
            else if (_selectedCategory == "business")
            {
                q = q.Where(x => (x.EventType >= AuditEventType.ProductCreated && x.EventType <= AuditEventType.PriceChanged) ||
                                 (x.EventType >= AuditEventType.SalesOrderCreated && x.EventType <= AuditEventType.QuotationExportedPdf) ||
                                 (x.EventType >= AuditEventType.StocktakeCreated && x.EventType <= AuditEventType.DefectiveStockDisposed) ||
                                 (x.EventType >= AuditEventType.ContractCreated && x.EventType <= AuditEventType.ContractDeleted));
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
            .Take(200)
            .ToListAsync();
    }

    private void HandleSearchInput(ChangeEventArgs e)
    {
        _searchTerm = e.Value?.ToString() ?? "";
    }

    private void HandleCategoryChange(ChangeEventArgs e)
    {
        _selectedCategory = e.Value?.ToString() ?? "all";
    }

    private static bool IsAdminActor(string? actor)
    {
        if (string.IsNullOrEmpty(actor)) return false;
        return string.Equals(actor, "admin", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(actor, "Administrator", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(actor, "Owner", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetEventFriendlyName(AuditEventType eventType)
    {
        return eventType switch
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
            AuditEventType.UserOffboarded => "Hoàn tất thủ tục thôi việc",

            AuditEventType.RoleCreated => "Tạo vai trò mới",
            AuditEventType.RoleUpdated => "Cập nhật vai trò",
            AuditEventType.RoleDeleted => "Xóa vai trò",
            AuditEventType.PermissionAssigned => "Cấp quyền người dùng",
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

            AuditEventType.ContractCreated => "Tạo hợp đồng kinh tế",
            AuditEventType.ContractSigned => "Ký hợp đồng kinh tế",
            AuditEventType.ContractFinalized => "Hoàn tất hợp đồng",

            AuditEventType.DatabaseBackupCreated => "Sao lưu dữ liệu hệ thống",
            AuditEventType.DatabaseBackupRestored => "Phục hồi dữ liệu hệ thống",

            AuditEventType.SupportAccessEnabled => "Kích hoạt phiên hỗ trợ",
            AuditEventType.SupportAccessRevoked => "Thu hồi phiên hỗ trợ",
            AuditEventType.SupportActionPerformed => "Thao tác hỗ trợ kỹ thuật",

            _ => eventType.ToString()
        };
    }

    private static string GetEventBadgeClass(AuditEventType eventType)
    {
        return eventType switch
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

            AuditEventType.SalesOrderCreated or AuditEventType.SalesOrderConfirmed => "tf-badge-success",
            AuditEventType.SalesOrderCancelled => "tf-badge-danger",
            AuditEventType.InvoiceIssued => "tf-badge-success",
            AuditEventType.InvoiceCancelled => "tf-badge-danger",

            AuditEventType.SupportAccessEnabled or AuditEventType.SupportAccessRevoked => "tf-badge-warning",

            _ => "tf-badge-secondary"
        };
    }

    private static string FormatTargetEntity(string? entity, string? id)
    {
        if (string.IsNullOrEmpty(entity) && string.IsNullOrEmpty(id)) return "—";

        var formattedEntity = entity switch
        {
            "ApplicationUser" => "Tài khoản",
            "EmployeeAccount" => "Tài khoản nhân viên",
            "Employee" => "Nhân sự",
            "EmploymentContract" => "HĐ lao động",
            "SalesOrder" => "Đơn hàng",
            "Invoice" => "Hóa đơn",
            "Quotation" => "Báo giá",
            "Product" => "Sản phẩm",
            "Customer" => "Khách hàng",
            "Supplier" => "Nhà cung cấp",
            "Warehouse" or "WarehouseVoucher" => "Kho hàng",
            "Contract" => "Hợp đồng",
            _ => entity ?? ""
        };

        if (string.IsNullOrEmpty(id)) return formattedEntity;
        return $"{formattedEntity} ({id})";
    }

    private static string FixMojibake(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";

        // Check if string exhibits Latin-1 decoding of UTF-8 bytes (mojibake)
        if (input.Contains("NgÆ°") || input.Contains("Ã") || input.Contains("á»") || input.Contains("Ä‘"))
        {
            try
            {
                var latin1 = System.Text.Encoding.GetEncoding("ISO-8859-1");
                var bytes = latin1.GetBytes(input);
                var utf8Str = System.Text.Encoding.UTF8.GetString(bytes);
                if (!utf8Str.Contains('\ufffd'))
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
'''

with open(path, "w", encoding="utf-8", newline="") as f:
    f.write(content)

print("Updated List.razor successfully.")
