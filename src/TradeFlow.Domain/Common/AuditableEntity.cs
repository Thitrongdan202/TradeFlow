namespace TradeFlow.Domain.Common;

/// <summary>
/// Base class for auditable entities that track creation and modification timestamps.
/// </summary>
public abstract class AuditableEntity<TId> : Entity<TId>
{
    protected AuditableEntity() { }

    /// <summary>Ngày tạo (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Người tạo (username)</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Ngày cập nhật lần cuối (UTC)</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Người cập nhật lần cuối (username)</summary>
    public string? UpdatedBy { get; set; }
}
