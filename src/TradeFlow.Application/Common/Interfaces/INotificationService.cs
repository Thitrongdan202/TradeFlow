using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Notifications;

namespace TradeFlow.Application.Common.Interfaces;

public interface INotificationService
{
    Task<List<SystemNotificationDto>> GetNotificationsAsync(ClaimsPrincipal? user = null, int limit = 10, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(ClaimsPrincipal? user = null, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string notificationId, ClaimsPrincipal? user = null, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(ClaimsPrincipal? user = null, CancellationToken cancellationToken = default);
}
