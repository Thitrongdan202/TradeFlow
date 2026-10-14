using System;

namespace TradeFlow.Application.Common.Models.Notifications;

public class SystemNotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "system"; // system, order, invoice, warning, info
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string? TargetUrl { get; set; }
}
