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

    public async Task<DocumentSummaryKpiDto> GetDocumentSummaryKpiAsync(CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentAttachments.Include(x => x.Category).AsNoTracking();
        var allDocs = await query.Select(x => new
        {
            x.Id,
            x.RelatedEntityType,
            x.RelatedEntityId,
            x.CreatedAt,
            CategoryCode = x.Category != null ? x.Category.Code : "KHAC"
        }).ToListAsync(cancellationToken);

        var total = allDocs.Count;
        var linked = allDocs.Count(x => !string.IsNullOrEmpty(x.RelatedEntityType) && x.RelatedEntityType != "General" && x.RelatedEntityId.HasValue && x.RelatedEntityId.Value > 0);
        var unlinked = total - linked;
        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
        var recent = allDocs.Count(x => x.CreatedAt >= thirtyDaysAgo);

        var groupCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["all"] = total,
            ["contracts"] = allDocs.Count(x => x.CategoryCode == "HOP_DONG" || x.CategoryCode == "HD_LAO_DONG" || x.RelatedEntityType == "Contract"),
            ["invoices"] = allDocs.Count(x => x.CategoryCode == "HOA_DON" || x.CategoryCode == "CHUNG_TU" || x.RelatedEntityType == "Invoice" || x.RelatedEntityType == "DebtPayment"),
            ["hr"] = allDocs.Count(x => x.CategoryCode == "HS_CA_NHAN" || x.CategoryCode == "GIAY_TO_TUY_THAN" || x.CategoryCode == "CV_SO_YEU" || x.CategoryCode == "BANG_CAP_CHUNG_CHI" || x.CategoryCode == "TL_CONG_VIEC" || x.CategoryCode == "TL_KHAC" || x.RelatedEntityType == "Employee" || x.RelatedEntityType == "EmployeeRequest"),
            ["warehouse"] = allDocs.Count(x => x.CategoryCode == "PHIEU_GIAO" || x.CategoryCode == "PHIEU_XUAT" || x.CategoryCode == "PHIEU_NHAP" || x.CategoryCode == "BIEN_BAN" || x.RelatedEntityType == "WarehouseVoucher"),
            ["commercial"] = allDocs.Count(x => x.CategoryCode == "BAO_GIA" || x.CategoryCode == "DON_DAT_HANG" || x.RelatedEntityType == "Quotation" || x.RelatedEntityType == "SalesOrder"),
            ["technical"] = allDocs.Count(x => x.CategoryCode == "HO_SO"),
            ["unlinked"] = unlinked,
            ["other"] = allDocs.Count(x => x.CategoryCode == "KHAC")
        };

        var catCounts = allDocs
            .GroupBy(x => x.CategoryCode)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        return new DocumentSummaryKpiDto
        {
            TotalDocuments = total,
            LinkedDocuments = linked,
            UnlinkedDocuments = unlinked,
            RecentUploads = recent,
            BusinessGroupCounts = groupCounts,
            CategoryCounts = catCounts
        };
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

            if (!string.IsNullOrWhiteSpace(filter.BusinessCategory) && filter.BusinessCategory != "all")
            {
                switch (filter.BusinessCategory.ToLowerInvariant())
                {
                    case "contracts":
                        query = query.Where(x => (x.Category != null && (x.Category.Code == "HOP_DONG" || x.Category.Code == "HD_LAO_DONG")) || x.RelatedEntityType == "Contract");
                        break;
                    case "invoices":
                        query = query.Where(x => (x.Category != null && (x.Category.Code == "HOA_DON" || x.Category.Code == "CHUNG_TU")) || x.RelatedEntityType == "Invoice" || x.RelatedEntityType == "DebtPayment" || x.RelatedEntityType == "PartnerPayment");
                        break;
                    case "hr":
                        query = query.Where(x => (x.Category != null && (x.Category.Code == "HS_CA_NHAN" || x.Category.Code == "GIAY_TO_TUY_THAN" || x.Category.Code == "CV_SO_YEU" || x.Category.Code == "BANG_CAP_CHUNG_CHI" || x.Category.Code == "TL_CONG_VIEC" || x.Category.Code == "TL_KHAC")) || x.RelatedEntityType == "Employee" || x.RelatedEntityType == "EmployeeRequest");
                        break;
                    case "warehouse":
                        query = query.Where(x => (x.Category != null && (x.Category.Code == "PHIEU_GIAO" || x.Category.Code == "PHIEU_XUAT" || x.Category.Code == "PHIEU_NHAP" || x.Category.Code == "BIEN_BAN")) || x.RelatedEntityType == "WarehouseVoucher");
                        break;
                    case "commercial":
                        query = query.Where(x => (x.Category != null && (x.Category.Code == "BAO_GIA" || x.Category.Code == "DON_DAT_HANG")) || x.RelatedEntityType == "Quotation" || x.RelatedEntityType == "SalesOrder");
                        break;
                    case "technical":
                        query = query.Where(x => x.Category != null && x.Category.Code == "HO_SO");
                        break;
                    case "unlinked":
                        query = query.Where(x => x.RelatedEntityType == "General" || x.RelatedEntityId == null || x.RelatedEntityId <= 0);
                        break;
                    case "other":
                        query = query.Where(x => (x.Category != null && x.Category.Code == "KHAC") || x.CategoryId == null);
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.QuickPeriod) && filter.QuickPeriod != "all")
            {
                var nowUtc = DateTime.UtcNow;
                var periodFrom = filter.QuickPeriod switch
                {
                    "today" => nowUtc.Date,
                    "7d" => nowUtc.Date.AddDays(-7),
                    "30d" => nowUtc.Date.AddDays(-30),
                    "this_month" => new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    _ => (DateTime?)null
                };
                if (periodFrom.HasValue)
                {
                    query = query.Where(x => x.CreatedAt >= periodFrom.Value);
                }
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
        var dtos = list.Select(MapToDto).ToList();
        await PopulateRelatedEntityTitlesAsync(dtos, cancellationToken);
        return dtos;
    }

    private async Task PopulateRelatedEntityTitlesAsync(List<DocumentAttachmentDto> dtos, CancellationToken cancellationToken)
    {
        var entitiesByType = dtos
            .Where(d => !string.IsNullOrEmpty(d.RelatedEntityType) && d.RelatedEntityId.HasValue && d.RelatedEntityId.Value > 0 && d.RelatedEntityType != "General")
            .GroupBy(d => d.RelatedEntityType)
            .ToDictionary(g => g.Key, g => g.Select(d => d.RelatedEntityId!.Value).Distinct().ToList());

        if (entitiesByType.TryGetValue("Customer", out var customerIds) && customerIds.Count > 0)
        {
            var dict = await _context.Customers
                .AsNoTracking()
                .Where(c => customerIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => $"{c.Code} - {c.Name}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "Customer" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }

        if (entitiesByType.TryGetValue("Supplier", out var supplierIds) && supplierIds.Count > 0)
        {
            var dict = await _context.Suppliers
                .AsNoTracking()
                .Where(s => supplierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, s => $"{s.Code} - {s.Name}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "Supplier" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }

        if (entitiesByType.TryGetValue("SalesOrder", out var orderIds) && orderIds.Count > 0)
        {
            var dict = await _context.SalesOrders
                .AsNoTracking()
                .Where(o => orderIds.Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => $"Đơn hàng {o.Code}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "SalesOrder" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }

        if (entitiesByType.TryGetValue("Invoice", out var invoiceIds) && invoiceIds.Count > 0)
        {
            var dict = await _context.Invoices
                .AsNoTracking()
                .Where(i => invoiceIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, i => $"Hóa đơn {i.InvoiceNumber}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "Invoice" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }

        if (entitiesByType.TryGetValue("Contract", out var contractIds) && contractIds.Count > 0)
        {
            var dict = await _context.Contracts
                .AsNoTracking()
                .Where(c => contractIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => $"Hợp đồng {c.ContractNumber}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "Contract" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }

        if (entitiesByType.TryGetValue("Employee", out var employeeIds) && employeeIds.Count > 0)
        {
            var dict = await _context.Employees
                .AsNoTracking()
                .Where(e => employeeIds.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => $"{e.Code} - {e.FullName}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "Employee" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }

        if (entitiesByType.TryGetValue("EmployeeRequest", out var requestIds) && requestIds.Count > 0)
        {
            var dict = await _context.EmployeeRequests
                .AsNoTracking()
                .Where(r => requestIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => $"Hồ sơ: {r.FullName}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "EmployeeRequest" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }

        if (entitiesByType.TryGetValue("WarehouseVoucher", out var voucherIds) && voucherIds.Count > 0)
        {
            var dict = await _context.WarehouseVouchers
                .AsNoTracking()
                .Where(v => voucherIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => $"Phiếu kho {v.VoucherNumber}", cancellationToken);
            foreach (var d in dtos.Where(x => x.RelatedEntityType == "WarehouseVoucher" && x.RelatedEntityId.HasValue && dict.ContainsKey(x.RelatedEntityId.Value)))
                d.RelatedEntityTitle = dict[d.RelatedEntityId!.Value];
        }
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

    public async Task<DocumentValidationDto> ValidateDocumentForPreviewAsync(int id, CancellationToken cancellationToken = default)
    {
        var doc = await _context.DocumentAttachments.FindAsync(new object[] { id }, cancellationToken);
        if (doc == null)
        {
            return new DocumentValidationDto
            {
                Status = DocumentPreviewStatus.FileNotFound,
                Message = "Tài liệu không tồn tại",
                Details = "Bản ghi tài liệu không được tìm thấy trong hệ thống.",
                CanDownload = false
            };
        }

        var ext = Path.GetExtension(doc.FileName).ToLowerInvariant();
        var (stream, _, _) = await GetFileStreamAsync(id, cancellationToken);
        if (stream == null)
        {
            var fallbackSize = doc.FileSize > 0 ? (doc.FileSize < 1024 ? $"{doc.FileSize} B" : $"{doc.FileSize / 1024.0:F1} KB") : "0 B";
            return new DocumentValidationDto
            {
                Status = DocumentPreviewStatus.FileNotFound,
                Message = "Tệp tin lưu trữ không khả dụng",
                Details = $"Không tìm thấy tệp vật lý tương ứng tại đường dẫn lưu trữ '{doc.StoragePath}'.",
                FileSizeBytes = doc.FileSize,
                FormattedFileSize = fallbackSize,
                CanDownload = false
            };
        }

        using (stream)
        {
            var length = stream.Length;
            var formattedSize = length < 1024 ? $"{length} B" : (length < 1024 * 1024 ? $"{length / 1024.0:F1} KB" : $"{length / (1024.0 * 1024.0):F2} MB");

            byte[] header = new byte[Math.Min(1024, (int)length)];
            int bytesRead = await stream.ReadAsync(header, 0, header.Length, cancellationToken);
            string headerText = System.Text.Encoding.UTF8.GetString(header, 0, bytesRead);

            if (ext == ".pdf")
            {
                bool startsWithPdf = headerText.StartsWith("%PDF-", StringComparison.OrdinalIgnoreCase);
                bool isDemoPlaceholder = headerText.Contains("DEMO DATA") || length < 500;

                if (!startsWithPdf || isDemoPlaceholder)
                {
                    return new DocumentValidationDto
                    {
                        Status = DocumentPreviewStatus.InvalidPlaceholder,
                        Message = "Tài liệu mẫu chưa có nội dung xem trước hợp lệ",
                        Details = $"Tệp tin này được khởi tạo dưới dạng dữ liệu mẫu placeholder (dung lượng: {formattedSize}) và chưa chứa cấu trúc nhị phân PDF hoàn chỉnh để hiển thị trên trình duyệt.",
                        FileSizeBytes = length,
                        FormattedFileSize = formattedSize,
                        DetectedFormat = "Văn bản mẫu (Placeholder Text)",
                        CanDownload = true
                    };
                }

                // Check for %%EOF in tail if length > 1024
                if (length > 1024)
                {
                    stream.Seek(Math.Max(0, length - 1024), SeekOrigin.Begin);
                    byte[] tail = new byte[1024];
                    int tailRead = await stream.ReadAsync(tail, 0, tail.Length, cancellationToken);
                    string tailText = System.Text.Encoding.UTF8.GetString(tail, 0, tailRead);
                    if (!tailText.Contains("%%EOF"))
                    {
                        return new DocumentValidationDto
                        {
                            Status = DocumentPreviewStatus.InvalidPlaceholder,
                            Message = "Tài liệu PDF chưa hoàn chỉnh hoặc bị cắt ngắn",
                            Details = "Tệp tin PDF không chứa mã kết thúc tài liệu hợp lệ (%%EOF).",
                            FileSizeBytes = length,
                            FormattedFileSize = formattedSize,
                            DetectedFormat = "PDF không hoàn chỉnh",
                            CanDownload = true
                        };
                    }
                }

                return new DocumentValidationDto
                {
                    Status = DocumentPreviewStatus.Valid,
                    Message = "Tài liệu PDF hợp lệ",
                    FileSizeBytes = length,
                    FormattedFileSize = formattedSize,
                    DetectedFormat = "PDF",
                    CanDownload = true
                };
            }
            else if (ext is ".png" or ".jpg" or ".jpeg" or ".webp")
            {
                bool isValidImage = false;
                string detected = "Hình ảnh";

                if (ext == ".png")
                {
                    isValidImage = bytesRead >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
                    detected = "PNG Image";
                }
                else if (ext is ".jpg" or ".jpeg")
                {
                    isValidImage = bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
                    detected = "JPEG Image";
                }
                else if (ext == ".webp")
                {
                    isValidImage = bytesRead >= 12 && headerText.StartsWith("RIFF") && headerText.Contains("WEBP");
                    detected = "WebP Image";
                }

                if (!isValidImage)
                {
                    return new DocumentValidationDto
                    {
                        Status = DocumentPreviewStatus.InvalidPlaceholder,
                        Message = "Tài liệu mẫu chưa có nội dung xem trước hợp lệ",
                        Details = $"Tệp tin hình ảnh mẫu ({formattedSize}) không chứa dữ liệu nhị phân ảnh hợp lệ.",
                        FileSizeBytes = length,
                        FormattedFileSize = formattedSize,
                        DetectedFormat = "Văn bản mẫu (Placeholder Text)",
                        CanDownload = true
                    };
                }

                return new DocumentValidationDto
                {
                    Status = DocumentPreviewStatus.Valid,
                    Message = "Hình ảnh hợp lệ",
                    FileSizeBytes = length,
                    FormattedFileSize = formattedSize,
                    DetectedFormat = detected,
                    CanDownload = true
                };
            }
            else
            {
                return new DocumentValidationDto
                {
                    Status = DocumentPreviewStatus.UnsupportedFormat,
                    Message = $"Định dạng {ext.ToUpperInvariant().TrimStart('.')} chưa hỗ trợ xem trước trên trình duyệt",
                    Details = "Trình duyệt không hỗ trợ hiển thị trực tiếp định dạng này. Vui lòng tải về máy tính để mở bằng ứng dụng tương thích.",
                    FileSizeBytes = length,
                    FormattedFileSize = formattedSize,
                    DetectedFormat = ext.ToUpperInvariant().TrimStart('.'),
                    CanDownload = true
                };
            }
        }
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
