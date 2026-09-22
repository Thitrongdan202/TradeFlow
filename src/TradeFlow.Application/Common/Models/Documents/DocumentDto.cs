using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Documents;

public class DocumentAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }

    public int? CategoryId { get; set; }
    public string? CategoryCode { get; set; }
    public string? CategoryName { get; set; }
    public DocumentTypeCategory? CategoryGroup { get; set; }

    public string RelatedEntityType { get; set; } = "General";
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityTitle { get; set; }

    public string? Description { get; set; }
    public string Status { get; set; } = "Active";

    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public string FormattedFileSize
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            return $"{FileSize / (1024.0 * 1024.0):F2} MB";
        }
    }
}

public class DocumentCategoryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DocumentTypeCategory Group { get; set; } = DocumentTypeCategory.Attachment;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
    public bool IsSystem { get; set; }
    public int AttachmentCount { get; set; }
}

public class DocumentFilterDto
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public DocumentTypeCategory? Group { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
