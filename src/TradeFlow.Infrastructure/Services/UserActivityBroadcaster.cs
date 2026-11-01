using TradeFlow.Application.Common.Interfaces;

namespace TradeFlow.Infrastructure.Services;

public class UserActivityBroadcaster : IUserActivityBroadcaster
{
    public event Action<ActivityToastNotification>? OnActivity;

    public void Broadcast(ActivityToastNotification notification)
    {
        var handlers = OnActivity;
        if (handlers == null) return;

        foreach (Action<ActivityToastNotification> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(notification);
            }
            catch
            {
                // Disconnected or stale circuit subscribers must never disrupt other circuits or callers
            }
        }
    }
}
