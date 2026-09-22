using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.Documents;

public class DocumentAttachment : AuditableEntity<int>
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public int? CategoryId { get; set; }
    public DocumentCategory? Category { get; set; }

    /// <summary>
    /// Loại thực thể liên kết (e.g. "Quotation", "SalesOrder", "Invoice", "Customer", "Supplier", "Product", "General")
    /// </summary>
    public string RelatedEntityType { get; set; } = "General";

    /// <summary>
    /// Khóa chính của thực thể liên kết (hoặc null nếu là tài liệu chung)
    /// </summary>
    public int? RelatedEntityId { get; set; }

    public string? Description { get; set; }
    public string Status { get; set; } = "Active";
}
