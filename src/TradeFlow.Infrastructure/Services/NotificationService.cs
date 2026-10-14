using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Notifications;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private static readonly ConcurrentDictionary<string, HashSet<string>> _userReadNotifications = new();
    private readonly TradeFlowDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(TradeFlowDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<SystemNotificationDto>> GetNotificationsAsync(ClaimsPrincipal? user = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        try
        {
            string userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name ?? "anonymous";
            _userReadNotifications.TryGetValue(userId, out var readSet);
            readSet ??= new HashSet<string>();

            // Query actual system events from AuditLog (e.g. system backups, security events, user creation)
            var recentAuditLogs = await _context.AuditLogs
                .Where(x => x.EventType == AuditEventType.DatabaseBackupCreated
                         || x.EventType == AuditEventType.CompanySettingsChanged
                         || x.EventType == AuditEventType.UserLocked)
                .OrderByDescending(x => x.OccurredAt)
                .Take(limit)
                .ToListAsync(cancellationToken);

            var result = new List<SystemNotificationDto>();

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

            return result;
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

    public Task MarkAllAsReadAsync(ClaimsPrincipal? user = null, CancellationToken cancellationToken = default)
    {
        string userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.Identity?.Name ?? "anonymous";
        var readSet = _userReadNotifications.GetOrAdd(userId, _ => new HashSet<string>());
        // We will mark all current in the set
        return Task.CompletedTask;
    }
}
