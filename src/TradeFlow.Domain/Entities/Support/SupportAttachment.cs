using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Support;

/// <summary>
/// Tệp đính kèm trong báo cáo sự cố hoặc bình luận phản hồi
/// </summary>
public class SupportAttachment : Entity<int>
{
    public SupportAttachment() { }

    /// <summary>ID của báo cáo sự cố</summary>
    public int TicketId { get; set; }
    public SupportTicket? Ticket { get; set; }

    /// <summary>ID của bình luận phản hồi (nếu tệp đính kèm trong lúc trả lời)</summary>
    public int? CommentId { get; set; }
    public SupportComment? Comment { get; set; }

    /// <summary>Tên tệp gốc do người dùng tải lên</summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>Đường dẫn lưu trữ an toàn trên File Storage (uploads/support/...)</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>MIME Content Type (image/png, application/pdf,...)</summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>Kích thước tệp (bytes)</summary>
    public long FileSizeBytes { get; set; }

    /// <summary>ID người tải lên</summary>
    public string UploadedByUserId { get; set; } = string.Empty;

    /// <summary>Username người tải lên</summary>
    public string UploadedByUserName { get; set; } = string.Empty;

    /// <summary>Thời điểm tải lên (UTC)</summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
