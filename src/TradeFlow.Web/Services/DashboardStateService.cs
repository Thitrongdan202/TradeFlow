namespace TradeFlow.Web.Services;

using TradeFlow.Application.Common.Models.Notifications;
using TradeFlow.Domain.Entities.Users;

public class DashboardStateService : IDashboardStateService
{
    private readonly List<DashboardStatItem> _stats = [];
    private readonly List<AuditLog> _recentActivities = [];
    private readonly List<SystemNotificationDto> _notifications = [];

    public bool HasLoaded { get; private set; }
    public IReadOnlyList<DashboardStatItem> Stats => _stats;
    public IReadOnlyList<AuditLog> RecentActivities => _recentActivities;
    public IReadOnlyList<SystemNotificationDto> Notifications => _notifications;
    public DateTime? LastUpdatedUtc { get; private set; }

    public void SetState(
        IEnumerable<DashboardStatItem> stats,
        IEnumerable<AuditLog> activities,
        IEnumerable<SystemNotificationDto> notifications)
    {
        _stats.Clear();
        _stats.AddRange(stats);

        _recentActivities.Clear();
        _recentActivities.AddRange(activities);

        _notifications.Clear();
        _notifications.AddRange(notifications);

        LastUpdatedUtc = DateTime.UtcNow;
        HasLoaded = true;
    }

    public void Invalidate()
    {
        HasLoaded = false;
        _stats.Clear();
        _recentActivities.Clear();
        _notifications.Clear();
    }
}
