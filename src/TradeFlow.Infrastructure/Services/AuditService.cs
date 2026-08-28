using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Concrete implementation of IAuditService.
/// Writes audit log entries to the database.
/// IMPORTANT: Never logs plaintext passwords.
/// </summary>
public class AuditService : IAuditService
{
    private readonly Persistence.TradeFlowDbContext _dbContext;

    public AuditService(Persistence.TradeFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task LogAsync(
        AuditEventType eventType,
        string? performedBy = null,
        string? targetEntity = null,
        string? targetId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog(
            eventType,
            performedBy,
            targetEntity,
            targetId,
            details,
            ipAddress,
            userAgent);

        _dbContext.AuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
