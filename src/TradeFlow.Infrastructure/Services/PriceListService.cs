using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class PriceListService : IPriceListService
{
    private readonly TradeFlowDbContext _context;
    private readonly ISystemCodeGenerator _codeGenerator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IFileStorageService _fileStorage;

    public PriceListService(
        TradeFlowDbContext context,
        ISystemCodeGenerator codeGenerator,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IFileStorageService fileStorage)
    {
        _context = context;
        _codeGenerator = codeGenerator;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _fileStorage = fileStorage;
    }

    public async Task<List<PriceListDto>> GetPriceListsAsync(
        int? year = null,
        int? quarter = null,
        int? month = null,
        PriceListStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.PriceLists.AsNoTracking().AsQueryable();

        if (year.HasValue)
        {
            query = query.Where(p => p.Year == year.Value);
        }

        if (quarter.HasValue)
        {
            query = query.Where(p => p.Quarter == quarter.Value);
        }

        if (month.HasValue)
        {
            query = query.Where(p => p.Month == month.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var priceLists = await query
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Quarter)
            .ThenByDescending(p => p.Month)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new PriceListDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                QuotationNumber = p.QuotationNumber,
                QuotationDate = p.QuotationDate,
                EffectiveFrom = p.EffectiveFrom,
                EffectiveTo = p.EffectiveTo,
                Month = p.Month,
                Quarter = p.Quarter,
                Year = p.Year,
                ProgramTitle = p.ProgramTitle,
                PriceCondition = p.PriceCondition,
                VatNote = p.VatNote,
                Status = p.Status,
                OriginalFileName = p.OriginalFileName,
                OriginalFileStorageRef = p.OriginalFileStorageRef,
                TotalItems = p.TotalItems,
                Notes = p.Notes,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy
            })
            .ToListAsync(cancellationToken);

        return priceLists;
    }

    public async Task<PriceListDto?> GetPriceListDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PriceLists
            .AsNoTracking()
            .Include(p => p.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        var dto = new PriceListDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            QuotationNumber = entity.QuotationNumber,
            QuotationDate = entity.QuotationDate,
            EffectiveFrom = entity.EffectiveFrom,
            EffectiveTo = entity.EffectiveTo,
            Month = entity.Month,
            Quarter = entity.Quarter,
            Year = entity.Year,
            ProgramTitle = entity.ProgramTitle,
            PriceCondition = entity.PriceCondition,
            VatNote = entity.VatNote,
            Status = entity.Status,
            OriginalFileName = entity.OriginalFileName,
            OriginalFileStorageRef = entity.OriginalFileStorageRef,
            TotalItems = entity.TotalItems,
            Notes = entity.Notes,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            Items = entity.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => new PriceListItemDto
                {
                    Id = i.Id,
                    PriceListId = i.PriceListId,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name,
                    SortOrder = i.SortOrder,
                    Group = i.Group,
                    NewCode = i.NewCode,
                    LegacyCode = i.LegacyCode,
                    ProductInfo = i.ProductInfo,
                    ImageStorageRef = i.ImageStorageRef,
                    UnitPrice = i.UnitPrice,
                    CurrencyCode = i.CurrencyCode,
                    VatRate = i.VatRate,
                    Note = i.Note,
                    MatchStatus = i.MatchStatus
                }).ToList()
        };

        return dto;
    }

    public async Task<PriceListDto> CreatePriceListAsync(PriceListDto dto, CancellationToken cancellationToken = default)
    {
        var code = string.IsNullOrWhiteSpace(dto.Code)
            ? await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.PriceList, cancellationToken)
            : dto.Code;

        var entity = new PriceList
        {
            Code = code,
            Name = dto.Name,
            QuotationNumber = dto.QuotationNumber,
            QuotationDate = dto.QuotationDate,
            EffectiveFrom = dto.EffectiveFrom,
            EffectiveTo = dto.EffectiveTo,
            Month = dto.Month,
            Quarter = dto.Quarter,
            Year = dto.Year > 0 ? dto.Year : DateTime.UtcNow.Year,
            ProgramTitle = dto.ProgramTitle,
            PriceCondition = dto.PriceCondition,
            VatNote = dto.VatNote,
            Status = dto.Status,
            OriginalFileName = dto.OriginalFileName,
            OriginalFileStorageRef = dto.OriginalFileStorageRef,
            TotalItems = dto.Items.Count,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserName ?? "System"
        };

        int sort = 1;
        foreach (var itemDto in dto.Items)
        {
            entity.Items.Add(new PriceListItem
            {
                ProductId = itemDto.ProductId,
                SortOrder = itemDto.SortOrder > 0 ? itemDto.SortOrder : sort++,
                Group = itemDto.Group,
                NewCode = itemDto.NewCode,
                LegacyCode = itemDto.LegacyCode,
                ProductInfo = itemDto.ProductInfo,
                ImageStorageRef = itemDto.ImageStorageRef,
                UnitPrice = itemDto.UnitPrice,
                CurrencyCode = string.IsNullOrWhiteSpace(itemDto.CurrencyCode) ? "VND" : itemDto.CurrencyCode,
                VatRate = itemDto.VatRate,
                Note = itemDto.Note,
                MatchStatus = itemDto.MatchStatus,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserName ?? "System"
            });
        }

        _context.PriceLists.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListCreated,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            entity.Id.ToString(),
            $"Tạo mới bảng giá {entity.Code} - {entity.Name} (Năm {entity.Year}, {entity.TotalItems} mặt hàng)",
            cancellationToken: cancellationToken);

        dto.Id = entity.Id;
        dto.Code = entity.Code;
        return dto;
    }

    public async Task<bool> UpdatePriceListAsync(PriceListDto dto, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PriceLists.FirstOrDefaultAsync(p => p.Id == dto.Id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        entity.Name = dto.Name;
        entity.QuotationNumber = dto.QuotationNumber;
        entity.QuotationDate = dto.QuotationDate;
        entity.EffectiveFrom = dto.EffectiveFrom;
        entity.EffectiveTo = dto.EffectiveTo;
        entity.Month = dto.Month;
        entity.Quarter = dto.Quarter;
        entity.Year = dto.Year;
        entity.ProgramTitle = dto.ProgramTitle;
        entity.PriceCondition = dto.PriceCondition;
        entity.VatNote = dto.VatNote;
        entity.Status = dto.Status;
        entity.Notes = dto.Notes;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUserService.UserName ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListUpdated,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            entity.Id.ToString(),
            $"Cập nhật bảng giá {entity.Code} - {entity.Name} (Hiệu lực: {entity.EffectiveFrom:dd/MM/yyyy} - {entity.EffectiveTo:dd/MM/yyyy})",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> DeletePriceListAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PriceLists
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (entity == null)
        {
            return false;
        }

        if (entity.Status != PriceListStatus.Draft)
        {
            entity.Status = PriceListStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(
                AuditEventType.PriceListDeleted,
                _currentUserService.UserName ?? "System",
                nameof(PriceList),
                id.ToString(),
                $"Ngừng áp dụng bảng giá {entity.Code} - {entity.Name}",
                cancellationToken: cancellationToken);
            return true;
        }

        _context.PriceListItems.RemoveRange(entity.Items);
        _context.PriceLists.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListDeleted,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            id.ToString(),
            $"Xóa bảng giá {entity.Code} - {entity.Name} ({entity.TotalItems} mặt hàng)",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> ApprovePriceListAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PriceLists.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        var previousStatus = entity.Status;
        entity.Status = PriceListStatus.Active;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = _currentUserService.UserName ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListApproved,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            id.ToString(),
            $"Phê duyệt áp dụng bảng giá {entity.Code} - {entity.Name} (Trạng thái cũ: {previousStatus})",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<PriceComparisonResultDto> ComparePriceListsAsync(
        int basePriceListId,
        int targetPriceListId,
        CancellationToken cancellationToken = default)
    {
        var baseList = await GetPriceListDetailAsync(basePriceListId, cancellationToken);
        var targetList = await GetPriceListDetailAsync(targetPriceListId, cancellationToken);

        if (baseList == null || targetList == null)
        {
            throw new ArgumentException("Một trong hai bảng giá so sánh không tồn tại trong hệ thống.");
        }

        var result = new PriceComparisonResultDto
        {
            BasePriceList = baseList,
            TargetPriceList = targetList
        };

        string BuildKey(PriceListItemDto item)
        {
            if (item.ProductId.HasValue && item.ProductId.Value > 0)
                return $"ID:{item.ProductId.Value}";
            if (!string.IsNullOrWhiteSpace(item.NewCode))
                return $"NEW:{item.NewCode.Trim().ToUpperInvariant()}";
            if (!string.IsNullOrWhiteSpace(item.LegacyCode))
                return $"LEG:{item.LegacyCode.Trim().ToUpperInvariant()}";
            return $"SORT:{item.SortOrder}";
        }

        var baseMap = baseList.Items.ToDictionary(BuildKey, item => item);
        var targetMap = targetList.Items.ToDictionary(BuildKey, item => item);

        var allKeys = new HashSet<string>(baseMap.Keys);
        allKeys.UnionWith(targetMap.Keys);

        foreach (var key in allKeys)
        {
            baseMap.TryGetValue(key, out var baseItem);
            targetMap.TryGetValue(key, out var targetItem);

            var compItem = new PriceComparisonItemDto();

            if (targetItem != null)
            {
                compItem.ProductId = targetItem.ProductId;
                compItem.ProductCode = targetItem.NewCode;
                compItem.ProductName = targetItem.ProductName ?? targetItem.ProductInfo;
                compItem.NewCode = targetItem.NewCode;
                compItem.LegacyCode = targetItem.LegacyCode;
                compItem.Group = targetItem.Group;
                compItem.ImageStorageRef = targetItem.ImageStorageRef;
                compItem.TargetPrice = targetItem.UnitPrice;
            }

            if (baseItem != null)
            {
                compItem.ProductId ??= baseItem.ProductId;
                if (string.IsNullOrWhiteSpace(compItem.ProductCode)) compItem.ProductCode = baseItem.NewCode;
                if (string.IsNullOrWhiteSpace(compItem.ProductName)) compItem.ProductName = baseItem.ProductName ?? baseItem.ProductInfo;
                if (string.IsNullOrWhiteSpace(compItem.NewCode)) compItem.NewCode = baseItem.NewCode;
                compItem.LegacyCode ??= baseItem.LegacyCode;
                compItem.Group ??= baseItem.Group;
                compItem.ImageStorageRef ??= baseItem.ImageStorageRef;
                compItem.BasePrice = baseItem.UnitPrice;
            }

            if (baseItem == null && targetItem != null)
            {
                compItem.ChangeStatus = PriceChangeStatus.NewProduct;
            }
            else if (baseItem != null && targetItem == null)
            {
                compItem.ChangeStatus = PriceChangeStatus.Discontinued;
            }
            else if (compItem.TargetPrice.HasValue && compItem.BasePrice.HasValue)
            {
                if (compItem.TargetPrice.Value > compItem.BasePrice.Value)
                    compItem.ChangeStatus = PriceChangeStatus.Increased;
                else if (compItem.TargetPrice.Value < compItem.BasePrice.Value)
                    compItem.ChangeStatus = PriceChangeStatus.Decreased;
                else
                    compItem.ChangeStatus = PriceChangeStatus.Unchanged;
            }

            result.Items.Add(compItem);
        }

        result.Items = result.Items
            .OrderBy(i => i.Group)
            .ThenBy(i => i.NewCode)
            .ToList();

        return result;
    }

    public async Task<List<PriceListItemDto>> GetProductPriceHistoryAsync(
        string productCodeOrNewCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productCodeOrNewCode))
        {
            return new List<PriceListItemDto>();
        }

        var query = productCodeOrNewCode.Trim();

        var items = await _context.PriceListItems
            .AsNoTracking()
            .Include(i => i.PriceList)
            .Include(i => i.Product)
            .Where(i =>
                i.NewCode == query ||
                i.LegacyCode == query ||
                (i.Product != null && (i.Product.Code == query || i.Product.NewCode == query || i.Product.LegacyCode == query)))
            .OrderByDescending(i => i.PriceList!.Year)
            .ThenByDescending(i => i.PriceList!.Quarter)
            .ThenByDescending(i => i.PriceList!.Month)
            .ThenByDescending(i => i.PriceList!.EffectiveFrom)
            .Select(i => new PriceListItemDto
            {
                Id = i.Id,
                PriceListId = i.PriceListId,
                ProductId = i.ProductId,
                ProductName = i.Product != null ? i.Product.Name : i.ProductInfo,
                SortOrder = i.SortOrder,
                Group = i.Group,
                NewCode = i.NewCode,
                LegacyCode = i.LegacyCode,
                ProductInfo = i.ProductInfo,
                ImageStorageRef = i.ImageStorageRef,
                UnitPrice = i.UnitPrice,
                CurrencyCode = i.CurrencyCode,
                VatRate = i.VatRate,
                Note = i.PriceList != null ? $"{i.PriceList.Code} - {i.PriceList.Name} ({i.PriceList.Year})" : i.Note,
                MatchStatus = i.MatchStatus
            })
            .ToListAsync(cancellationToken);

        return items;
    }


    public async Task<bool> DeleteOriginalFileAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PriceLists.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null || string.IsNullOrEmpty(entity.OriginalFileStorageRef))
        {
            return false;
        }

        await _fileStorage.DeleteFileAsync(entity.OriginalFileStorageRef, cancellationToken);
        entity.OriginalFileStorageRef = null;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListUpdated,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            id.ToString(),
            $"Xóa file Excel gốc của bảng giá {entity.Code}",
            cancellationToken: cancellationToken);

        return true;
    }
}
