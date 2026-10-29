namespace TradeFlow.Web.Services;

using TradeFlow.Application.Common.Models.Notifications;
using TradeFlow.Domain.Entities.Users;

public record DashboardStatItem(string Label, string Value, string Meta, string Color);

public interface IDashboardStateService
{
    bool HasLoaded { get; }
    IReadOnlyList<DashboardStatItem> Stats { get; }
    IReadOnlyList<AuditLog> RecentActivities { get; }
    IReadOnlyList<SystemNotificationDto> Notifications { get; }
    DateTime? LastUpdatedUtc { get; }

    void SetState(
        IEnumerable<DashboardStatItem> stats,
        IEnumerable<AuditLog> activities,
        IEnumerable<SystemNotificationDto> notifications);

    void Invalidate();
}
