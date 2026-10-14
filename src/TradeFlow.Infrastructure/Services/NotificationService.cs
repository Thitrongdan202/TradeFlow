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

                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = $"Đề xuất nhân viên: {req.FullName}",
                        Message = $"Yêu cầu tuyển dụng/thêm nhân sự '{req.FullName}' ({posName}{deptName}) do {req.RequestedBy} đề xuất.",
                        Type = "approval",
                        CreatedAt = req.CreatedAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = "/nhan-vien",
                        IsApprovalRequest = true,
                        RequestType = "EmployeeRequest",
                        RequestTypeName = "Đề xuất thêm nhân sự",
                        EntityId = req.Id,
                        RequestCode = codeDisplay,
                        Requester = req.RequestedBy,
                        Status = "Chờ duyệt",
                        CanApprove = canApprove,
                        IsSelfRequest = isSelf
                    });
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

                // 3. Query actual system events from AuditLog (e.g. system backups, security events, user creation)
                var recentAuditLogs = await context.AuditLogs
                    .AsNoTracking()
                    .Where(x => x.EventType == AuditEventType.DatabaseBackupCreated
                             || x.EventType == AuditEventType.CompanySettingsChanged
                             || x.EventType == AuditEventType.UserLocked)
                    .OrderByDescending(x => x.OccurredAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                foreach (var log in recentAuditLogs)
                {
                    string id = $"audit_{log.Id}";
                    string title = log.EventType switch
                    {
                        AuditEventType.DatabaseBackupCreated => "Bản sao lưu dữ liệu đã tạo",
                        AuditEventType.CompanySettingsChanged => "Thông tin công ty đã cập nhật",
                        AuditEventType.UserLocked => "Cảnh báo bảo mật tài khoản",
                        _ => "Thông báo hệ thống"
                    };

                    string message = log.EventType switch
                    {
                        AuditEventType.DatabaseBackupCreated => $"Bản sao lưu cơ sở dữ liệu đã được tạo thành công bởi {log.PerformedBy ?? "quản trị viên"}.",
                        AuditEventType.CompanySettingsChanged => $"Cấu hình thông tin doanh nghiệp đã được cập nhật bởi {log.PerformedBy}.",
                        AuditEventType.UserLocked => $"Tài khoản {log.TargetId} đã bị khóa do vi phạm chính sách.",
                        _ => log.Details ?? "Cập nhật hệ thống"
                    };

                    result.Add(new SystemNotificationDto
                    {
                        Id = id,
                        Title = title,
                        Message = message,
                        Type = log.EventType == AuditEventType.UserLocked ? "warning" : "system",
                        CreatedAt = log.OccurredAt,
                        IsRead = readSet.Contains(id),
                        TargetUrl = log.EventType == AuditEventType.DatabaseBackupCreated ? "/cai-dat/sao-luu" : "/cai-dat"
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
