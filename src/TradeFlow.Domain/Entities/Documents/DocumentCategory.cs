using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Documents;

public class DocumentCategory : AuditableEntity<int>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DocumentTypeCategory Group { get; set; } = DocumentTypeCategory.Attachment;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }

    public ICollection<DocumentAttachment> Attachments { get; set; } = new List<DocumentAttachment>();
}
