using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Support;

/// <summary>
/// Bình luận phản hồi / trao đổi thông tin hoặc ghi chú nội bộ trong yêu cầu hỗ trợ
/// </summary>
public class SupportComment : Entity<int>
{
    public SupportComment() { }

    /// <summary>ID của báo cáo sự cố</summary>
    public int TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }

    /// <summary>ID người gửi phản hồi</summary>
    public string AuthorUserId { get; set; } = string.Empty;

    /// <summary>Username người gửi</summary>
    public string AuthorUserName { get; set; } = string.Empty;

    /// <summary>Họ tên đầy đủ người gửi</summary>
    public string AuthorFullName { get; set; } = string.Empty;

    /// <summary>Vai trò người gửi</summary>
    public string AuthorRole { get; set; } = string.Empty;

    /// <summary>Nội dung phản hồi hoặc ghi chú</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Cờ ghi chú nội bộ:
    /// - True: Chỉ dành cho Manager / Admin có quyền xem và xử lý.
    /// - False: Phản hồi công khai cho người gửi yêu cầu.
    /// </summary>
    public bool IsInternalNote { get; set; } = false;

    /// <summary>Thời điểm gửi phản hồi (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Tệp đính kèm trong phản hồi này</summary>
    public ICollection<SupportAttachment> Attachments { get; set; } = [];
}
