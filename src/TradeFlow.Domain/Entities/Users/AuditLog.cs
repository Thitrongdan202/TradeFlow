using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Users;

/// <summary>
/// Nhật ký hoạt động - ghi lại mọi sự kiện quan trọng trong hệ thống.
/// Chỉ đọc đối với người dùng thông thường.
/// </summary>
public class AuditLog : Entity<long>
{
    protected AuditLog() { }

    public AuditLog(
        AuditEventType eventType,
        string? performedBy,
        string? targetEntity,
        string? targetId,
        string? details,
        string? ipAddress,
        string? userAgent)
    {
        EventType = eventType;
        PerformedBy = performedBy;
        TargetEntity = targetEntity;
        TargetId = targetId;
        Details = details;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>Loại sự kiện</summary>
    public AuditEventType EventType { get; private set; }

    /// <summary>Người thực hiện (username)</summary>
    public string? PerformedBy { get; private set; }

    /// <summary>Thực thể bị tác động</summary>
    public string? TargetEntity { get; private set; }

    /// <summary>ID của thực thể bị tác động</summary>
    public string? TargetId { get; private set; }

    /// <summary>Chi tiết sự kiện (không bao giờ chứa mật khẩu)</summary>
    public string? Details { get; private set; }

    /// <summary>Địa chỉ IP</summary>
    public string? IpAddress { get; private set; }

    /// <summary>User Agent</summary>
    public string? UserAgent { get; private set; }

    /// <summary>Thời gian xảy ra (UTC)</summary>
    public DateTime OccurredAt { get; private set; }
}
