using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Support;

/// <summary>
/// Nhật ký kiểm toán mọi thay đổi và hành động trên yêu cầu hỗ trợ
/// </summary>
public class SupportAuditLog : Entity<int>
{
    public SupportAuditLog() { }

    /// <summary>ID của báo cáo sự cố</summary>
    public int TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }

    /// <summary>Loại hành động (Created, Acknowledged, StatusChanged, Assigned, Replied, InternalNote, Resolved, Closed, Rejected)</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>ID người thực hiện</summary>
    public string PerformedByUserId { get; set; } = string.Empty;

    /// <summary>Username người thực hiện</summary>
    public string PerformedByUserName { get; set; } = string.Empty;

    /// <summary>Giá trị cũ</summary>
    public string? OldValue { get; set; }

    /// <summary>Giá trị mới</summary>
    public string? NewValue { get; set; }

    /// <summary>Ghi chú chi tiết</summary>
    public string? Notes { get; set; }

    /// <summary>Thời điểm ghi nhận (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
