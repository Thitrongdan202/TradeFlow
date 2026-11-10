using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services.Security;

/// <summary>
/// Dịch vụ nền tự động kiểm tra và thu hồi quyền truy cập tạm thời đã hết hạn (chạy định kỳ 15 giây).
/// Đảm bảo quyền tự hết hạn bền vững tại Database kể cả khi người dùng không thao tác.
/// </summary>
public class TemporaryRoleExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TemporaryRoleExpirationWorker> _logger;
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(15);

    public TemporaryRoleExpirationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<TemporaryRoleExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TemporaryRoleExpirationWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredAssignmentsAsync(stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Error processing expired temporary role assignments.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("TemporaryRoleExpirationWorker stopped.");
    }

    private async Task ProcessExpiredAssignmentsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var permissionService = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

        var now = DateTime.UtcNow;

        var expiredList = await context.TemporaryRoleAssignments
            .Where(t => t.Status == TemporaryRoleAssignmentStatus.Active && t.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        if (!expiredList.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} expired temporary role assignments to revoke.", expiredList.Count);

        foreach (var assignment in expiredList)
        {
            assignment.Status = TemporaryRoleAssignmentStatus.Expired;

            // Invalidate in-memory permission cache immediately
            permissionService.InvalidateUserPermissions(assignment.UserId);

            // Fetch role name for audit log
            var role = await roleManager.FindByIdAsync(assignment.RoleId);
            var roleName = role?.Name ?? assignment.RoleId;

            var user = await userManager.FindByIdAsync(assignment.UserId);
            var username = user?.UserName ?? assignment.UserId;

            if (user != null)
            {
                // Refresh security stamp to invalidate cached auth cookie/claims
                await userManager.UpdateSecurityStampAsync(user);
            }

            // Audit log
            await auditService.LogAsync(
                AuditEventType.TemporaryRoleExpired,
                "System",
                "User",
                username,
                $"Hết hạn quyền tạm thời vai trò '{roleName}' (Bắt đầu: {assignment.StartsAt:dd/MM/yyyy HH:mm:ss} UTC, Hết hạn: {assignment.ExpiresAt:dd/MM/yyyy HH:mm:ss} UTC, Lý do: {assignment.Reason})"
            );
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
