using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public record ActivityToastNotification(
    string EventId,
    string PerformedBy,
    AuditEventType EventType,
    string Title,
    string Description,
    string? TargetUrl,
    DateTime OccurredAt
);

public interface IUserActivityBroadcaster
{
    event Action<ActivityToastNotification>? OnActivity;
    void Broadcast(ActivityToastNotification notification);
}
