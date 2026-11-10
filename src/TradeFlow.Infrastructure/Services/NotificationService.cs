using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Employees;
using TradeFlow.Application.Common.Models.Notifications;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Domain.Entities.Support;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userReadNotifications = new();
    private readonly IDbContextFactory<TradeFlowDbContext> _contextFactory;
    private readonly ILogger<NotificationService> _logger;
    private readonly IServiceProvider? _serviceProvider;

    public NotificationService(
        IDbContextFactory<TradeFlowDbContext> contextFactory,
        ILogger<NotificationService> logger,
        IServiceProvider? serviceProvider = null)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider;
    }

    public async Task<List<SystemNotificationDto>> GetNotificationsAsync(ClaimsPrincipal? user = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            string userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name ?? "anonymous";
            string currentUserName = user?.Identity?.Name ?? "";
            _userReadNotifications.TryGetValue(userId, out var readSet);
            readSet ??= new HashSet<string>();

            bool isAdmin = user?.IsInRole("Administrator") ?? false;
            bool hasEmployeeApprovePermission = isAdmin || (user != null && (user.HasClaim("Permission", "Permission:EmployeeRequests:Approve") || user.HasClaim("Permission", "EmployeeRequests:Approve")));
            bool hasPriceListApprovePermission = isAdmin || (user != null && (user.HasClaim("Permission", "Permission:Pricing:Edit") || user.HasClaim("Permission", "Pricing:Edit")));
            bool hasAttendanceApprovePermission = isAdmin || (user != null && (user.HasClaim("Permission", "Permission:Attendance:Approve") || user.HasClaim("Permission", "Attendance:Approve")));

            var result = new List<SystemNotificationDto>();

            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

            // 1. Pending Employee Requests (Status == Pending)
                var pendingEmpRequests = await context.EmployeeRequests
                    .AsNoTracking()
                    .Include(r => r.Position)
                    .Include(r => r.Department)
                    .Where(r => r.Status == EmployeeRequestStatus.Pending)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var req in pendingEmpRequests)
                {
                    string id = $"appr_emp_{req.Id}";
                    bool isSelf = !string.IsNullOrEmpty(currentUserName) && string.Equals(req.RequestedBy, currentUserName, StringComparison.OrdinalIgnoreCase);
                    bool canApprove = hasEmployeeApprovePermission && !isSelf;

                    string posName = req.Position?.Name ?? "Nhân viên";
                    string deptName = req.Department?.Name != null ? $" - {req.Department.Name}" : "";
                    string codeDisplay = !string.IsNullOrWhiteSpace(req.Code) ? req.Code : $"YC-{req.Id:D4}";

                    bool isResubmitted = req.UpdatedAt.HasValue && req.UpdatedAt.Value > req.CreatedAt.AddSeconds(5);
                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = isResubmitted ? $"Hồ sơ nhân sự gửi lại chờ duyệt: {req.FullName}" : $"Đề xuất nhân viên: {req.FullName}",
                        Message = isResubmitted
                            ? $"Yêu cầu nhân sự '{req.FullName}' ({posName}{deptName}) đã được cập nhật/bổ sung hồ sơ và gửi lại để phê duyệt."
                            : $"Yêu cầu tuyển dụng/thêm nhân sự '{req.FullName}' ({posName}{deptName}) do {req.RequestedBy} đề xuất.",
                        Type = "approval",
                        CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                        ActionButtonText = "Xem hồ sơ chờ duyệt",
                        IsApprovalRequest = true,
                        RequestType = "EmployeeRequest",
                        RequestTypeName = isResubmitted ? "Gửi lại chờ duyệt" : "Đề xuất thêm nhân sự",
                        EntityId = req.Id,
                        RequestCode = codeDisplay,
                        Requester = req.RequestedBy,
                        Status = isResubmitted ? "Gửi lại chờ duyệt" : "Chờ duyệt",
                        CanApprove = canApprove,
                        IsSelfRequest = isSelf
                    });
                }

                // 1b. Employee Feedback Notifications (NeedAdditionalDocs, Rejected, Approved)
                var feedbackEmpRequests = await context.EmployeeRequests
                    .AsNoTracking()
                    .Include(r => r.Position)
                    .Include(r => r.Department)
                    .Where(r => (r.Status == EmployeeRequestStatus.NeedAdditionalDocs
                              || r.Status == EmployeeRequestStatus.Rejected
                              || r.Status == EmployeeRequestStatus.Approved)
                              && (string.IsNullOrEmpty(currentUserName) || r.RequestedBy == currentUserName || isAdmin))
                    .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var req in feedbackEmpRequests)
                {
                    string posName = req.Position?.Name ?? "Nhân viên";
                    string deptName = req.Department?.Name != null ? $" - {req.Department.Name}" : "";
                    string codeDisplay = !string.IsNullOrWhiteSpace(req.Code) ? req.Code : $"YC-{req.Id:D4}";

                    if (req.Status == EmployeeRequestStatus.NeedAdditionalDocs)
                    {
                        string id = $"emp_needdocs_{req.Id}_{req.UpdatedAt?.Ticks ?? req.CreatedAt.Ticks}";
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Yêu cầu bổ sung hồ sơ: {req.FullName}",
                            Message = $"Hồ sơ nhân sự '{req.FullName}' ({posName}{deptName}) cần bổ sung tài liệu. Ý kiến người duyệt: {req.ReviewNotes ?? "Vui lòng hoàn thiện hồ sơ"}",
                            Type = "warning",
                            CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                            ActionButtonText = "Mở yêu cầu bổ sung",
                            IsApprovalRequest = false,
                            RequestType = "EmployeeRequest",
                            RequestTypeName = "Cần bổ sung hồ sơ",
                            EntityId = req.Id,
                            RequestCode = codeDisplay,
                            Requester = req.RequestedBy,
                            Status = "Cần bổ sung hồ sơ"
                        });
                    }
                    else if (req.Status == EmployeeRequestStatus.Rejected)
                    {
                        string id = $"emp_rejected_{req.Id}_{req.UpdatedAt?.Ticks ?? req.CreatedAt.Ticks}";
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Hồ sơ nhân sự bị từ chối: {req.FullName}",
                            Message = $"Yêu cầu tuyển dụng/thêm nhân sự '{req.FullName}' ({posName}{deptName}) đã bị từ chối. Lý do: {req.ReviewNotes ?? "Không đáp ứng yêu cầu tuyển dụng"}",
                            Type = "error",
                            CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                            ActionButtonText = "Xem lý do từ chối",
                            IsApprovalRequest = false,
                            RequestType = "EmployeeRequest",
                            RequestTypeName = "Từ chối hồ sơ",
                            EntityId = req.Id,
                            RequestCode = codeDisplay,
                            Requester = req.RequestedBy,
                            Status = "Từ chối"
                        });
                    }
                    else if (req.Status == EmployeeRequestStatus.Approved)
                    {
                        string id = $"emp_approved_{req.Id}_{req.UpdatedAt?.Ticks ?? req.CreatedAt.Ticks}";
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Hồ sơ nhân sự đã duyệt: {req.FullName}",
                            Message = $"Hồ sơ nhân sự '{req.FullName}' ({posName}{deptName}) đã được phê duyệt thành công bởi {req.ApprovedBy}.",
                            Type = "success",
                            CreatedAt = req.UpdatedAt ?? req.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = $"/nhan-vien?tab=requests&requestId={req.Id}",
                            ActionButtonText = "Xem hồ sơ nhân viên",
                            IsApprovalRequest = false,
                            RequestType = "EmployeeRequest",
                            RequestTypeName = "Đã phê duyệt",
                            EntityId = req.Id,
                            RequestCode = codeDisplay,
                            Requester = req.RequestedBy,
                            Status = "Đã duyệt"
                        });
                    }
                }

                // 2. Draft Price Lists (Status == Draft)
                var draftPriceLists = await context.PriceLists
                    .AsNoTracking()
                    .Where(p => p.Status == PriceListStatus.Draft)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var pl in draftPriceLists)
                {
                    string id = $"appr_pl_{pl.Id}";
                    bool isSelf = !string.IsNullOrEmpty(currentUserName) && string.Equals(pl.CreatedBy, currentUserName, StringComparison.OrdinalIgnoreCase);
                    bool canApprove = hasPriceListApprovePermission && !isSelf;

                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = $"Bảng giá chờ duyệt: {pl.Code} - {pl.Name}",
                        Message = $"Bảng giá '{pl.Name}' ({pl.TotalItems} mặt hàng) được tạo bởi {pl.CreatedBy ?? "người dùng"} đang ở bản nháp chờ phê duyệt áp dụng.",
                        Type = "approval",
                        CreatedAt = pl.CreatedAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = $"/bang-gia/chi-tiet/{pl.Id}",
                        IsApprovalRequest = true,
                        RequestType = "PriceList",
                        RequestTypeName = "Bảng giá mới",
                        EntityId = pl.Id,
                        RequestCode = pl.Code,
                        Requester = pl.CreatedBy ?? "Hệ thống",
                        Status = "Bản nháp",
                        CanApprove = canApprove,
                        IsSelfRequest = isSelf
                    });
                }

                // 2b. Pending Attendance Adjustment Requests
                var pendingAdjustments = await context.AttendanceAdjustmentRequests
                    .AsNoTracking()
                    .Include(r => r.Employee)
                    .Where(r => r.Status == AttendanceRequestStatus.Pending)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var req in pendingAdjustments)
                {
                    string id = $"appr_adj_{req.Id}";
                    bool isSelf = !string.IsNullOrEmpty(currentUserName) &&
                        ((req.Employee != null && (string.Equals(req.Employee.FullName, currentUserName, StringComparison.OrdinalIgnoreCase) || string.Equals(req.Employee.Code, currentUserName, StringComparison.OrdinalIgnoreCase))) ||
                         string.Equals(req.Employee?.UserId, userId, StringComparison.OrdinalIgnoreCase));
                    bool canApprove = hasAttendanceApprovePermission && !isSelf;

                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = $"Giải trình chấm công: {req.Employee?.FullName ?? "Nhân viên"}",
                        Message = $"Yêu cầu điều chỉnh công ngày {req.TargetDate:dd/MM/yyyy} do {req.Employee?.FullName} gửi: {req.Reason}",
                        Type = "approval",
                        CreatedAt = req.CreatedAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = "/cham-cong",
                        ActionButtonText = "Xem đơn chờ duyệt",
                        IsApprovalRequest = true,
                        RequestType = "AttendanceAdjustment",
                        RequestTypeName = "Điều chỉnh chấm công",
                        EntityId = req.Id,
                        RequestCode = $"YC-CC-{req.Id:D4}",
                        Requester = req.Employee?.FullName ?? "Nhân viên",
                        Status = "Chờ duyệt",
                        CanApprove = canApprove,
                        IsSelfRequest = isSelf
                    });
                }

                // 2c. Pending Leave Requests
                var pendingLeaves = await context.LeaveRequests
                    .AsNoTracking()
                    .Include(r => r.Employee)
                    .Where(r => r.Status == AttendanceRequestStatus.Pending)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var req in pendingLeaves)
                {
                    string id = $"appr_leave_{req.Id}";
                    bool isSelf = !string.IsNullOrEmpty(currentUserName) &&
                        ((req.Employee != null && (string.Equals(req.Employee.FullName, currentUserName, StringComparison.OrdinalIgnoreCase) || string.Equals(req.Employee.Code, currentUserName, StringComparison.OrdinalIgnoreCase))) ||
                         string.Equals(req.Employee?.UserId, userId, StringComparison.OrdinalIgnoreCase));
                    bool canApprove = hasAttendanceApprovePermission && !isSelf;

                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = $"Đơn xin nghỉ phép: {req.Employee?.FullName ?? "Nhân viên"}",
                        Message = $"Đơn nghỉ phép từ {req.StartDate:dd/MM/yyyy} đến {req.EndDate:dd/MM/yyyy} ({req.TotalDays} ngày) do {req.Employee?.FullName} gửi: {req.Reason}",
                        Type = "approval",
                        CreatedAt = req.CreatedAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = "/cham-cong",
                        ActionButtonText = "Xem đơn nghỉ phép",
                        IsApprovalRequest = true,
                        RequestType = "LeaveRequest",
                        RequestTypeName = "Đơn xin nghỉ phép",
                        EntityId = req.Id,
                        RequestCode = $"NP-{req.Id:D4}",
                        Requester = req.Employee?.FullName ?? "Nhân viên",
                        Status = "Chờ duyệt",
                        CanApprove = canApprove,
                        IsSelfRequest = isSelf
                    });
                }

                // 2c. Support Tickets (New tickets for Manager/Admin, feedback/replies for Submitter)
                bool isManager = user?.IsInRole("Manager") ?? false;
                if (isAdmin || isManager)
                {
                    var newTickets = await context.SupportTickets
                        .AsNoTracking()
                        .Where(t => t.Status == SupportStatus.New)
                        .OrderByDescending(t => t.CreatedAt)
                        .Take(limit)
                        .ToListAsync(cancellationToken);

                    foreach (var t in newTickets)
                    {
                        string id = $"supp_new_{t.Id}";
                        string notifType = t.Priority == SupportPriority.Urgent ? "error" : (t.Priority == SupportPriority.Medium ? "warning" : "system");
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Báo cáo hỗ trợ mới: [{t.Code}] {t.Title}",
                            Message = $"Nhân sự {t.SubmitterFullName ?? t.CreatedByUserName} ({t.SubmitterRole}) gửi báo cáo: {t.Title}.",
                            Type = notifType,
                            CreatedAt = t.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = "#support-center",
                            ActionButtonText = "Xử lý hỗ trợ",
                            IsApprovalRequest = true,
                            RequestType = "SupportTicket",
                            RequestTypeName = "Báo cáo hỗ trợ",
                            EntityId = t.Id,
                            RequestCode = t.Code,
                            Requester = t.SubmitterFullName ?? t.CreatedByUserName,
                            Status = "Mới gửi",
                            CanApprove = true
                        });
                    }
                }

                if (!string.IsNullOrEmpty(userId))
                {
                    var myTicketUpdates = await context.SupportTickets
                        .AsNoTracking()
                        .Where(t => t.CreatedByUserId == userId && (t.Status == SupportStatus.WaitingForCustomer || t.Status == SupportStatus.Resolved || t.Status == SupportStatus.Rejected))
                        .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                        .Take(limit)
                        .ToListAsync(cancellationToken);

                    foreach (var t in myTicketUpdates)
                    {
                        string id = $"supp_update_{t.Id}_{t.Status}_{t.UpdatedAt?.Ticks ?? t.CreatedAt.Ticks}";
                        string notifType = t.Status == SupportStatus.Resolved ? "success" : (t.Status == SupportStatus.Rejected ? "error" : "warning");
                        string statusDesc = t.Status == SupportStatus.Resolved ? "Đã giải quyết" : (t.Status == SupportStatus.Rejected ? $"Từ chối: {t.RejectionReason}" : "Chờ bạn phản hồi");
                        result.Add(new SystemNotificationDto
                        {
                            Id = id,
                            Title = $"Cập nhật hỗ trợ [{t.Code}]: {t.Title}",
                            Message = $"Yêu cầu hỗ trợ của bạn có cập nhật mới. Trạng thái: {statusDesc}.",
                            Type = notifType,
                            CreatedAt = t.UpdatedAt ?? t.CreatedAt,
                            IsRead = readSet.Contains(id),
                            TargetUrl = "#support-center",
                            ActionButtonText = "Xem trao đổi",
                            IsApprovalRequest = false,
                            RequestType = "SupportTicket",
                            RequestTypeName = "Hỗ trợ nội bộ",
                            EntityId = t.Id,
                            RequestCode = t.Code,
                            Requester = t.SubmitterFullName ?? t.CreatedByUserName,
                            Status = statusDesc
                        });
                    }
                }

                // 3. Query actual system events from AuditLog (e.g. system backups, security events, user creation, approvals)
                var recentAuditLogs = await context.AuditLogs
                    .AsNoTracking()
                    .Where(x => x.EventType == AuditEventType.DatabaseBackupCreated
                             || x.EventType == AuditEventType.CompanySettingsChanged
                             || x.EventType == AuditEventType.UserLocked
                             || x.EventType == AuditEventType.PasswordRecoveryRequested
                             || x.EventType == AuditEventType.AttendanceAdjustmentApproved
                             || x.EventType == AuditEventType.AttendanceAdjustmentRejected
                             || x.EventType == AuditEventType.LeaveRequestApproved
                             || x.EventType == AuditEventType.LeaveRequestRejected
                             || x.EventType == AuditEventType.RoleCreated
                             || x.EventType == AuditEventType.RoleUpdated)
                    .OrderByDescending(x => x.OccurredAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var log in recentAuditLogs)
                {
                    string id = $"audit_{log.Id}";
                    string title = log.EventType switch
                    {
                        AuditEventType.PasswordRecoveryRequested => "Yêu cầu hỗ trợ mật khẩu",
                        AuditEventType.DatabaseBackupCreated => "Bản sao lưu dữ liệu đã tạo",
                        AuditEventType.CompanySettingsChanged => "Thông tin công ty đã cập nhật",
                        AuditEventType.UserLocked => "Cảnh báo bảo mật tài khoản",
                        AuditEventType.AttendanceAdjustmentApproved => "Giải trình chấm công đã duyệt",
                        AuditEventType.AttendanceAdjustmentRejected => "Giải trình chấm công bị từ chối",
                        AuditEventType.LeaveRequestApproved => "Đơn nghỉ phép đã duyệt",
                        AuditEventType.LeaveRequestRejected => "Đơn nghỉ phép bị từ chối",
                        AuditEventType.RoleCreated => "Vai trò mới đã tạo",
                        AuditEventType.RoleUpdated => "Vai trò & quyền đã cập nhật",
                        _ => "Thông báo hệ thống"
                    };

                    string message = log.EventType switch
                    {
                        AuditEventType.PasswordRecoveryRequested => $"Tài khoản '{log.TargetId}' yêu cầu hỗ trợ cấp lại mật khẩu. {log.Details}".Trim(),
                        AuditEventType.DatabaseBackupCreated => $"Bản sao lưu cơ sở dữ liệu đã được tạo thành công bởi {log.PerformedBy ?? "quản trị viên"}.",
                        AuditEventType.CompanySettingsChanged => $"Cấu hình thông tin doanh nghiệp đã được cập nhật bởi {log.PerformedBy}.",
                        AuditEventType.UserLocked => $"Tài khoản {log.TargetId} đã bị khóa do vi phạm chính sách.",
                        AuditEventType.AttendanceAdjustmentApproved => log.Details ?? "Yêu cầu điều chỉnh công đã được phê duyệt.",
                        AuditEventType.AttendanceAdjustmentRejected => log.Details ?? "Yêu cầu điều chỉnh công đã bị từ chối.",
                        AuditEventType.LeaveRequestApproved => log.Details ?? "Đơn xin nghỉ phép đã được phê duyệt.",
                        AuditEventType.LeaveRequestRejected => log.Details ?? "Đơn xin nghỉ phép đã bị từ chối.",
                        AuditEventType.RoleCreated => log.Details ?? "Đã tạo vai trò mới trên hệ thống.",
                        AuditEventType.RoleUpdated => log.Details ?? "Đã cập nhật vai trò và phân quyền.",
                        _ => log.Details ?? "Cập nhật hệ thống"
                    };

                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = title,
                        Message = message,
                        Type = (log.EventType == AuditEventType.UserLocked || log.EventType == AuditEventType.PasswordRecoveryRequested) ? "warning" : "system",
                        CreatedAt = log.OccurredAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = log.EventType switch
                        {
                            AuditEventType.PasswordRecoveryRequested => "/cai-dat/nguoi-dung",
                            AuditEventType.DatabaseBackupCreated => "/cai-dat/sao-luu",
                            AuditEventType.AttendanceAdjustmentApproved or AuditEventType.AttendanceAdjustmentRejected => "/cham-cong",
                            AuditEventType.LeaveRequestApproved or AuditEventType.LeaveRequestRejected => "/cham-cong",
                            AuditEventType.RoleCreated or AuditEventType.RoleUpdated => "/cai-dat/vai-tro",
                            _ => "/cai-dat"
                        }
                    });
                }

            // Sort: Unread and pending approvals first, then by CreatedAt descending
            return result
                .OrderByDescending(x => x.IsApprovalRequest && !x.IsRead)
                .ThenByDescending(x => x.CreatedAt)
                .Take(limit)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load notifications from database");
            throw;
        }
    }

    public async Task<int> GetUnreadCountAsync(ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        var list = await GetNotificationsAsync(user, 50, cancellationToken);
        return list.Count(x => !x.IsRead);
    }

    public Task MarkAsReadAsync(string notificationId, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(notificationId)) return Task.CompletedTask;

        string userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name ?? "anonymous";
        var readSet = _userReadNotifications.GetOrAdd(userId, _ => new HashSet<string>());
        lock (readSet)
        {
            readSet.Add(notificationId);
        }

        return Task.CompletedTask;
    }

    public async Task MarkAllAsReadAsync(ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        string userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name ?? "anonymous";
        var readSet = _userReadNotifications.GetOrAdd(userId, _ => new HashSet<string>());

        var list = await GetNotificationsAsync(user, 50, cancellationToken);
        lock (readSet)
        {
            foreach (var item in list)
            {
                readSet.Add(item.Id);
            }
        }
    }

    public async Task<bool> ApproveNotificationAsync(string notificationId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
        }

        string currentUserName = user.Identity.Name ?? "";
        bool isAdmin = user.IsInRole("Administrator");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (notificationId.StartsWith("appr_emp_") && int.TryParse(notificationId.Replace("appr_emp_", ""), out int empReqId))
        {
            bool hasPermission = isAdmin || user.HasClaim("Permission", "Permission:EmployeeRequests:Approve") || user.HasClaim("Permission", "EmployeeRequests:Approve");
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền phê duyệt yêu cầu thêm nhân viên.");
            }

            var req = await context.EmployeeRequests.FirstOrDefaultAsync(r => r.Id == empReqId, cancellationToken);
            if (req == null) return false;

            if (string.Equals(req.RequestedBy, currentUserName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Không thể tự phê duyệt yêu cầu do chính mình đề xuất.");
            }

            var employeeService = _serviceProvider?.GetService(typeof(IEmployeeService)) as IEmployeeService;
            if (employeeService != null)
            {
                await employeeService.ReviewEmployeeRequestAsync(new ReviewEmployeeRequestCommand
                {
                    RequestId = empReqId,
                    Approve = true
                }, currentUserName, cancellationToken);
            }
            else
            {
                req.Status = EmployeeRequestStatus.Approved;
                req.ApprovedBy = currentUserName;
                req.ApprovedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }

            await MarkAsReadAsync(notificationId, user, cancellationToken);
            return true;
        }
        else if (notificationId.StartsWith("appr_pl_") && int.TryParse(notificationId.Replace("appr_pl_", ""), out int plId))
        {
            bool hasPermission = isAdmin || user.HasClaim("Permission", "Permission:Pricing:Edit") || user.HasClaim("Permission", "Pricing:Edit");
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền phê duyệt bảng giá.");
            }

            var pl = await context.PriceLists.FirstOrDefaultAsync(p => p.Id == plId, cancellationToken);
            if (pl == null) return false;

            if (string.Equals(pl.CreatedBy, currentUserName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Không thể tự phê duyệt bảng giá do chính mình tạo.");
            }

            var priceListService = _serviceProvider?.GetService(typeof(IPriceListService)) as IPriceListService;
            if (priceListService != null)
            {
                await priceListService.ApprovePriceListAsync(plId, cancellationToken);
            }
            else
            {
                pl.Status = PriceListStatus.Active;
                pl.UpdatedAt = DateTime.UtcNow;
                pl.UpdatedBy = currentUserName;
                await context.SaveChangesAsync(cancellationToken);
            }

            await MarkAsReadAsync(notificationId, user, cancellationToken);
            return true;
        }

        return false;
    }

    public async Task<bool> RejectNotificationAsync(string notificationId, string? reason, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Người dùng chưa được xác thực.");
        }

        string currentUserName = user.Identity.Name ?? "";
        bool isAdmin = user.IsInRole("Administrator");

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        if (notificationId.StartsWith("appr_emp_") && int.TryParse(notificationId.Replace("appr_emp_", ""), out int empReqId))
        {
            bool hasPermission = isAdmin || user.HasClaim("Permission", "Permission:EmployeeRequests:Approve") || user.HasClaim("Permission", "EmployeeRequests:Approve");
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền từ chối yêu cầu thêm nhân viên.");
            }

            var employeeService = _serviceProvider?.GetService(typeof(IEmployeeService)) as IEmployeeService;
            if (employeeService != null)
            {
                await employeeService.ReviewEmployeeRequestAsync(new ReviewEmployeeRequestCommand
                {
                    RequestId = empReqId,
                    Approve = false,
                    ReviewNotes = reason ?? "Từ chối phê duyệt từ trung tâm thông báo"
                }, currentUserName, cancellationToken);
            }
            else
            {
                var req = await context.EmployeeRequests.FirstOrDefaultAsync(r => r.Id == empReqId, cancellationToken);
                if (req != null)
                {
                    req.Status = EmployeeRequestStatus.Rejected;
                    req.ApprovedBy = currentUserName;
                    req.ApprovedAt = DateTime.UtcNow;
                    req.ReviewNotes = reason;
                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            await MarkAsReadAsync(notificationId, user, cancellationToken);
            return true;
        }
        else if (notificationId.StartsWith("appr_pl_") && int.TryParse(notificationId.Replace("appr_pl_", ""), out int plId))
        {
            bool hasPermission = isAdmin || user.HasClaim("Permission", "Permission:Pricing:Edit") || user.HasClaim("Permission", "Pricing:Edit");
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền hủy bỏ bảng giá.");
            }

            var priceListService = _serviceProvider?.GetService(typeof(IPriceListService)) as IPriceListService;
            if (priceListService != null)
            {
                await priceListService.CancelPriceListAsync(plId, cancellationToken);
            }
            else
            {
                var pl = await context.PriceLists.FirstOrDefaultAsync(p => p.Id == plId, cancellationToken);
                if (pl != null)
                {
                    pl.Status = PriceListStatus.Cancelled;
                    pl.UpdatedAt = DateTime.UtcNow;
                    pl.UpdatedBy = currentUserName;
                    await context.SaveChangesAsync(cancellationToken);
                }
            }

            await MarkAsReadAsync(notificationId, user, cancellationToken);
            return true;
        }

        return false;
    }
}
