using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Documents;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class DocumentService : IDocumentService
{
    private readonly TradeFlowDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".docx", ".doc", ".xlsx", ".xls", ".png", ".jpg", ".jpeg"
    };

    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    public DocumentService(
        TradeFlowDbContext context,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        IAuditService auditService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    private static DateTime NormalizeToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified || date.Kind == DateTimeKind.Local)
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return date;
    }

    public async Task<List<DocumentAttachmentDto>> GetDocumentsAsync(DocumentFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentAttachments
            .Include(x => x.Category)
            .AsNoTracking();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(x => x.FileName.ToLower().Contains(term)
                    || (x.Description != null && x.Description.ToLower().Contains(term))
                    || (x.Category != null && x.Category.Name.ToLower().Contains(term)));
            }

            if (filter.CategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == filter.CategoryId.Value);
            }

            if (filter.Group.HasValue)
            {
                query = query.Where(x => x.Category != null && x.Category.Group == filter.Group.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.RelatedEntityType))
            {
                query = query.Where(x => x.RelatedEntityType == filter.RelatedEntityType);
            }

            if (filter.RelatedEntityId.HasValue)
            {
                query = query.Where(x => x.RelatedEntityId == filter.RelatedEntityId.Value);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = NormalizeToUtc(filter.FromDate.Value.Date);
                query = query.Where(x => x.CreatedAt >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = NormalizeToUtc(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
                query = query.Where(x => x.CreatedAt <= toUtc);
            }
        }

        query = query.OrderByDescending(x => x.CreatedAt);

        var list = await query.ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<DocumentAttachmentDto>> GetDocumentsByEntityAsync(string entityType, int entityId, CancellationToken cancellationToken = default)
    {
        var list = await _context.DocumentAttachments
            .Include(x => x.Category)
            .AsNoTracking()
            .Where(x => x.RelatedEntityType == entityType && x.RelatedEntityId == entityId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDto).ToList();
    }

    public async Task<DocumentAttachmentDto?> GetDocumentByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var doc = await _context.DocumentAttachments
            .Include(x => x.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (doc == null) return null;
        return MapToDto(doc);
    }

    public async Task<DocumentAttachmentDto> UploadDocumentAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long fileSize,
        string entityType,
        int? entityId = null,
        int? categoryId = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        if (fileStream == null || fileStream.Length == 0)
            throw new ArgumentException("Tệp tin không được rỗng.", nameof(fileStream));

        if (string.IsNullOrWhiteSpace(originalFileName))
            throw new ArgumentException("Tên tệp tin không được để trống.", nameof(originalFileName));

        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            throw new InvalidOperationException($"Định dạng tệp '{ext}' không được hỗ trợ. Chỉ chấp nhận các định dạng: {string.Join(", ", AllowedExtensions)}");
        }

        if (fileSize > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"Dung lượng tệp vượt quá giới hạn tối đa cho phép (20MB). Dung lượng thực tế: {fileSize / (1024.0 * 1024.0):F2} MB");
        }

        // Save file physically via IFileStorageService
        var storagePath = await _fileStorageService.SaveFileAsync(
            fileStream,
            originalFileName,
            contentType,
            "documents",
            cancellationToken);

        // Ensure category exists if specified
        if (categoryId.HasValue)
        {
            var categoryExists = await _context.DocumentCategories.AnyAsync(x => x.Id == categoryId.Value, cancellationToken);
            if (!categoryExists) categoryId = null;
        }

        var doc = new DocumentAttachment
        {
            FileName = Path.GetFileName(originalFileName),
            StoragePath = storagePath,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
            FileSize = fileSize,
            CategoryId = categoryId,
            RelatedEntityType = string.IsNullOrWhiteSpace(entityType) ? "General" : entityType,
            RelatedEntityId = entityId,
            Description = description,
            Status = "Active"
        };

        _context.DocumentAttachments.Add(doc);
        await _context.SaveChangesAsync(cancellationToken);

        // Load category if any
        if (doc.CategoryId.HasValue)
        {
            await _context.Entry(doc).Reference(x => x.Category).LoadAsync(cancellationToken);
        }

        await _auditService.LogAsync(
            AuditEventType.DocumentUploaded,
            _currentUserService.UserName ?? "System",
            "DocumentAttachment",
            doc.Id.ToString(),
            $"Tải lên tài liệu: {doc.FileName} ({doc.RelatedEntityType} #{doc.RelatedEntityId}), dung lượng: {doc.FileSize / 1024.0:F1} KB");

        return MapToDto(doc);
    }

    public async Task<bool> DeleteDocumentAsync(int id, CancellationToken cancellationToken = default)
    {
        var doc = await _context.DocumentAttachments.FindAsync(new object[] { id }, cancellationToken);
        if (doc == null) return false;

        // Delete physical file
        try
        {
            await _fileStorageService.DeleteFileAsync(doc.StoragePath, cancellationToken);
        }
        catch
        {
            // Log or ignore if file already missing physically
        }

        _context.DocumentAttachments.Remove(doc);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.DocumentDeleted,
            _currentUserService.UserName ?? "System",
            "DocumentAttachment",
            doc.Id.ToString(),
            $"Xóa tài liệu: {doc.FileName} ({doc.RelatedEntityType} #{doc.RelatedEntityId})");

        return true;
    }

    public async Task<(Stream? Stream, string ContentType, string FileName)> GetFileStreamAsync(int id, CancellationToken cancellationToken = default)
    {
        var doc = await _context.DocumentAttachments.FindAsync(new object[] { id }, cancellationToken);
        if (doc == null) return (null, string.Empty, string.Empty);

        var stream = await _fileStorageService.GetFileAsync(doc.StoragePath, cancellationToken);
        return (stream, doc.ContentType, doc.FileName);
    }

    public async Task<List<DocumentCategoryDto>> GetCategoriesAsync(bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentCategories
            .Include(x => x.Attachments)
            .AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(x => x.IsActive);
        }

        var list = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return list.Select(c => new DocumentCategoryDto
        {
            Id = c.Id,
            Code = c.Code,
            Name = c.Name,
            Group = c.Group,
            Description = c.Description,
            IsActive = c.IsActive,
            DisplayOrder = c.DisplayOrder,
            IsSystem = c.IsSystem,
            AttachmentCount = c.Attachments.Count
        }).ToList();
    }

    public async Task<DocumentCategoryDto> CreateCategoryAsync(DocumentCategoryDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Code))
            throw new ArgumentException("Mã loại tài liệu không được để trống.", nameof(dto.Code));
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Tên loại tài liệu không được để trống.", nameof(dto.Name));

        var codeExists = await _context.DocumentCategories.AnyAsync(x => x.Code == dto.Code.Trim(), cancellationToken);
        if (codeExists)
            throw new InvalidOperationException($"Mã loại tài liệu '{dto.Code}' đã tồn tại.");

        var category = new DocumentCategory
        {
            Code = dto.Code.Trim().ToUpperInvariant(),
            Name = dto.Name.Trim(),
            Group = dto.Group,
            Description = dto.Description,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            IsSystem = false
        };

        _context.DocumentCategories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        dto.Id = category.Id;
        dto.Code = category.Code;
        return dto;
    }

    public async Task<bool> UpdateCategoryAsync(DocumentCategoryDto dto, CancellationToken cancellationToken = default)
    {
        var category = await _context.DocumentCategories.FindAsync(new object[] { dto.Id }, cancellationToken);
        if (category == null) return false;

        if (!category.IsSystem)
        {
            if (!string.IsNullOrWhiteSpace(dto.Code) && dto.Code.Trim().ToUpperInvariant() != category.Code)
            {
                var codeExists = await _context.DocumentCategories.AnyAsync(x => x.Id != dto.Id && x.Code == dto.Code.Trim(), cancellationToken);
                if (codeExists)
                    throw new InvalidOperationException($"Mã loại tài liệu '{dto.Code}' đã được sử dụng.");
                category.Code = dto.Code.Trim().ToUpperInvariant();
            }
        }

        category.Name = dto.Name.Trim();
        category.Group = dto.Group;
        category.Description = dto.Description;
        category.IsActive = dto.IsActive;
        category.DisplayOrder = dto.DisplayOrder;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static DocumentAttachmentDto MapToDto(DocumentAttachment entity)
    {
        return new DocumentAttachmentDto
        {
            Id = entity.Id,
            FileName = entity.FileName,
            StoragePath = entity.StoragePath,
            ContentType = entity.ContentType,
            FileSize = entity.FileSize,
            CategoryId = entity.CategoryId,
            CategoryCode = entity.Category?.Code,
            CategoryName = entity.Category?.Name,
            CategoryGroup = entity.Category?.Group,
            RelatedEntityType = entity.RelatedEntityType,
            RelatedEntityId = entity.RelatedEntityId,
            Description = entity.Description,
            Status = entity.Status,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy
        };
    }
}
