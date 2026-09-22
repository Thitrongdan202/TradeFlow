using TradeFlow.Application.Common.Models.Documents;

namespace TradeFlow.Application.Common.Interfaces;

public interface IDocumentService
{
    Task<List<DocumentAttachmentDto>> GetDocumentsAsync(DocumentFilterDto? filter = null, CancellationToken cancellationToken = default);
    Task<List<DocumentAttachmentDto>> GetDocumentsByEntityAsync(string entityType, int entityId, CancellationToken cancellationToken = default);
    Task<DocumentAttachmentDto?> GetDocumentByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DocumentAttachmentDto> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize,
        string entityType,
        int? entityId = null,
        int? categoryId = null,
        string? description = null,
        CancellationToken cancellationToken = default);
    Task<bool> DeleteDocumentAsync(int id, CancellationToken cancellationToken = default);
    Task<(Stream? Stream, string ContentType, string FileName)> GetFileStreamAsync(int id, CancellationToken cancellationToken = default);

    Task<List<DocumentCategoryDto>> GetCategoriesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<DocumentCategoryDto> CreateCategoryAsync(DocumentCategoryDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateCategoryAsync(DocumentCategoryDto dto, CancellationToken cancellationToken = default);
}
