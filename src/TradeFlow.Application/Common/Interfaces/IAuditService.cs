using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Service for writing audit log entries.
/// Implementations must never log plaintext passwords.
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Ghi một sự kiện vào nhật ký hoạt động.
    /// </summary>
    Task LogAsync(
        AuditEventType eventType,
        string? performedBy = null,
        string? targetEntity = null,
        string? targetId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default);
}
