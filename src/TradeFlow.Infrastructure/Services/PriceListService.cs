using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Entities.MasterData;
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

    public async Task<bool> CancelPriceListAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.PriceLists.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity == null)
        {
            return false;
        }

        entity.Status = PriceListStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListDeleted,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            id.ToString(),
            $"Hủy bảng giá {entity.Code} - {entity.Name}",
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

        bool isReferenced = await _context.SalesOrders.AnyAsync(o => 
            o.Status != TradeFlow.Domain.Enums.SalesOrderStatus.Draft && 
            o.Items.Any(i => i.PriceSource == entity.Name), cancellationToken);

        if (isReferenced)
        {
            throw new InvalidOperationException("Không thể xóa vĩnh viễn vì bảng giá đang được dữ liệu nghiệp vụ sử dụng.");
        }

        _context.PriceListItems.RemoveRange(entity.Items);
        _context.PriceLists.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceListDeleted,
            _currentUserService.UserName ?? "System",
            nameof(PriceList),
            id.ToString(),
            $"Xóa vĩnh viễn bảng giá {entity.Code} - {entity.Name} ({entity.TotalItems} mặt hàng)",
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


    public async Task<decimal?> GetProductCurrentPriceAsync(int productId, DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var dict = await GetCurrentPricesForProductsAsync(new[] { productId }, asOfDate, cancellationToken);
        return dict.TryGetValue(productId, out var price) ? price : null;
    }

    public async Task<Dictionary<int, decimal?>> GetCurrentPricesForProductsAsync(IEnumerable<int> productIds, DateTime? asOfDate = null, CancellationToken cancellationToken = default)
    {
        var idList = productIds.Distinct().ToList();
        var result = new Dictionary<int, decimal?>();
        if (!idList.Any()) return result;

        var utcTarget = asOfDate.HasValue 
            ? (asOfDate.Value.Kind == DateTimeKind.Utc ? asOfDate.Value : DateTime.SpecifyKind(asOfDate.Value, DateTimeKind.Utc))
            : DateTime.UtcNow;

        // 1. Tìm các dòng giá đang hiệu lực theo ngày áp dụng và UnitPrice > 0
        var activeItems = await _context.PriceListItems
            .AsNoTracking()
            .Include(i => i.PriceList)
            .Where(i => i.ProductId.HasValue && idList.Contains(i.ProductId.Value)
                     && i.UnitPrice > 0
                     && i.PriceList!.Status == PriceListStatus.Active
                     && (i.PriceList.EffectiveFrom == null || i.PriceList.EffectiveFrom <= utcTarget)
                     && (i.PriceList.EffectiveTo == null || i.PriceList.EffectiveTo >= utcTarget))
            .OrderByDescending(i => i.PriceList!.EffectiveFrom)
            .ThenByDescending(i => i.PriceList!.Year)
            .ThenByDescending(i => i.PriceList!.Quarter)
            .ToListAsync(cancellationToken);

        foreach (var item in activeItems)
        {
            if (item.ProductId.HasValue && !result.ContainsKey(item.ProductId.Value))
            {
                result[item.ProductId.Value] = item.UnitPrice;
            }
        }

        // 2. Với các sản phẩm chưa tìm thấy theo khoảng ngày, lấy theo bảng giá Active mới nhất có giá > 0
        var remainingIds = idList.Where(id => !result.ContainsKey(id)).ToList();
        if (remainingIds.Any())
        {
            var latestActiveItems = await _context.PriceListItems
                .AsNoTracking()
                .Include(i => i.PriceList)
                .Where(i => i.ProductId.HasValue && remainingIds.Contains(i.ProductId.Value)
                         && i.UnitPrice > 0
                         && i.PriceList!.Status == PriceListStatus.Active)
                .OrderByDescending(i => i.PriceList!.Year)
                .ThenByDescending(i => i.PriceList!.Quarter)
                .ThenByDescending(i => i.PriceList!.EffectiveFrom)
                .ToListAsync(cancellationToken);

            foreach (var item in latestActiveItems)
            {
                if (item.ProductId.HasValue && !result.ContainsKey(item.ProductId.Value))
                {
                    result[item.ProductId.Value] = item.UnitPrice;
                }
            }
        }

        // 3. Với các sản phẩm chưa được gán ProductId trên PriceListItem (e.g. import cũ hoặc đồng bộ chưa kịp ghi ID), đối chiếu theo mã
        remainingIds = idList.Where(id => !result.ContainsKey(id)).ToList();
        if (remainingIds.Any())
        {
            var unlinkedProds = await _context.Products
                .AsNoTracking()
                .Where(p => remainingIds.Contains(p.Id))
                .ToListAsync(cancellationToken);

            var activeCandidates = await _context.PriceListItems
                .Include(i => i.PriceList)
                .Where(i => i.PriceList!.Status == PriceListStatus.Active && i.UnitPrice > 0)
                .OrderByDescending(i => i.PriceList!.Year)
                .ThenByDescending(i => i.PriceList!.Quarter)
                .ThenByDescending(i => i.PriceList!.EffectiveFrom)
                .ToListAsync(cancellationToken);

            bool needsHealingSave = false;

            foreach (var p in unlinkedProds)
            {
                var pNew = (p.NewCode ?? "").Trim();
                var pLeg = (p.LegacyCode ?? "").Trim();
                var pCode = p.Code.Trim();

                var matchedItem = activeCandidates.FirstOrDefault(i =>
                    (!string.IsNullOrEmpty(pNew) && (i.NewCode.Equals(pNew, StringComparison.OrdinalIgnoreCase) || pNew.StartsWith(i.NewCode, StringComparison.OrdinalIgnoreCase) || i.NewCode.StartsWith(pNew, StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrEmpty(pLeg) && (!string.IsNullOrEmpty(i.LegacyCode) && i.LegacyCode.Equals(pLeg, StringComparison.OrdinalIgnoreCase) || i.NewCode.Equals(pLeg, StringComparison.OrdinalIgnoreCase))) ||
                    (!string.IsNullOrEmpty(pCode) && i.NewCode.Equals(pCode, StringComparison.OrdinalIgnoreCase)));

                if (matchedItem != null)
                {
                    result[p.Id] = matchedItem.UnitPrice;

                    // Tự động gắn kết ProductId nếu đang bị khuyết
                    if (!matchedItem.ProductId.HasValue)
                    {
                        matchedItem.ProductId = p.Id;
                        needsHealingSave = true;
                    }
                }
            }

            if (needsHealingSave)
            {
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch
                {
                    // Ignore background healing errors
                }
            }
        }

        // Gán null cho các ID không có bảng giá
        foreach (var id in idList)
        {
            if (!result.ContainsKey(id)) result[id] = null;
        }

        return result;
    }

    public async Task<MultiPeriodPriceComparisonDto> CompareMultiplePriceListsAsync(
        List<int>? priceListIds = null,
        DateTime? asOfDate = null,
        CancellationToken cancellationToken = default)
    {
        List<PriceList> targetLists;

        if (priceListIds != null && priceListIds.Any())
        {
            targetLists = await _context.PriceLists
                .AsNoTracking()
                .Where(p => priceListIds.Contains(p.Id))
                .OrderBy(p => p.Year)
                .ThenBy(p => p.Quarter)
                .ThenBy(p => p.Month)
                .ThenBy(p => p.EffectiveFrom)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Tải động các bảng giá thực tế từ cơ sở dữ liệu (tối đa 6 kỳ gần nhất, loại bỏ Đã hủy)
            var recentLists = await _context.PriceLists
                .AsNoTracking()
                .Where(p => p.Status != PriceListStatus.Cancelled)
                .OrderByDescending(p => p.Year)
                .ThenByDescending(p => p.Quarter)
                .ThenByDescending(p => p.Month)
                .ThenByDescending(p => p.EffectiveFrom)
                .Take(6)
                .ToListAsync(cancellationToken);

            targetLists = recentLists
                .OrderBy(p => p.Year)
                .ThenBy(p => p.Quarter)
                .ThenBy(p => p.Month)
                .ThenBy(p => p.EffectiveFrom)
                .ToList();
        }

        var result = new MultiPeriodPriceComparisonDto();
        if (!targetLists.Any())
        {
            return result;
        }

        // Tạo các cột kỳ bảng giá
        foreach (var pl in targetLists)
        {
            string label;
            if (pl.Quarter.HasValue) label = $"Q{pl.Quarter}/{pl.Year}";
            else if (pl.Month.HasValue) label = $"T{pl.Month:D2}/{pl.Year}";
            else label = $"{pl.Year}";

            result.Periods.Add(new PriceListPeriodColumnDto
            {
                PriceListId = pl.Id,
                Code = pl.Code,
                Name = pl.Name,
                PeriodLabel = label,
                Year = pl.Year,
                Quarter = pl.Quarter,
                Month = pl.Month,
                EffectiveFrom = pl.EffectiveFrom,
                Status = pl.Status
            });
        }

        // Lấy tất cả items của các bảng giá này
        var targetListIds = targetLists.Select(p => p.Id).ToList();
        var allItems = await _context.PriceListItems
            .AsNoTracking()
            .Include(i => i.Product)
            .Where(i => targetListIds.Contains(i.PriceListId))
            .ToListAsync(cancellationToken);

        string BuildMultiKey(PriceListItem item)
        {
            if (item.ProductId.HasValue && item.ProductId.Value > 0)
                return $"ID:{item.ProductId.Value}";
            if (!string.IsNullOrWhiteSpace(item.NewCode))
                return $"NEW:{item.NewCode.Trim().ToUpperInvariant()}";
            if (!string.IsNullOrWhiteSpace(item.LegacyCode))
                return $"LEG:{item.LegacyCode.Trim().ToUpperInvariant()}";
            return $"ITEM:{item.Id}";
        }

        var itemMap = new Dictionary<string, MultiPeriodComparisonItemDto>();

        foreach (var item in allItems)
        {
            var key = BuildMultiKey(item);
            if (!itemMap.TryGetValue(key, out var compItem))
            {
                compItem = new MultiPeriodComparisonItemDto
                {
                    ProductId = item.ProductId,
                    ProductCode = item.Product?.Code ?? item.NewCode,
                    ProductName = item.Product?.Name ?? item.ProductInfo ?? item.NewCode,
                    NewCode = item.NewCode,
                    LegacyCode = item.LegacyCode ?? item.Product?.LegacyCode,
                    Group = item.Group,
                    ImageStorageRef = item.ImageStorageRef
                };
                itemMap[key] = compItem;
            }

            // Gán giá chưa VAT cho kỳ tương ứng
            compItem.PeriodPrices[item.PriceListId] = item.UnitPrice;
        }

        // Lấy Giá hiện tại hiệu lực cho các sản phẩm có ProductId
        var productIds = itemMap.Values
            .Where(x => x.ProductId.HasValue && x.ProductId.Value > 0)
            .Select(x => x.ProductId!.Value)
            .Distinct()
            .ToList();

        var currentPrices = await GetCurrentPricesForProductsAsync(productIds, asOfDate, cancellationToken);
        foreach (var compItem in itemMap.Values)
        {
            if (compItem.ProductId.HasValue && currentPrices.TryGetValue(compItem.ProductId.Value, out var cp))
            {
                compItem.CurrentPrice = cp;
            }
            else
            {
                // Nếu chưa có trong danh mục Master Data, lấy giá kỳ áp dụng mới nhất
                var latestPeriodWithPrice = result.Periods
                    .AsEnumerable()
                    .Reverse()
                    .FirstOrDefault(p => compItem.PeriodPrices.ContainsKey(p.PriceListId));
                if (latestPeriodWithPrice != null)
                {
                    compItem.CurrentPrice = compItem.PeriodPrices[latestPeriodWithPrice.PriceListId];
                }
            }
        }

        result.Items = itemMap.Values
            .OrderBy(x => x.Group)
            .ThenBy(x => x.NewCode)
            .ToList();

        return result;
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

    public async Task<bool> UpdateItemPriceAsync(int itemId, decimal newUnitPrice, decimal? vatRate = null, string? note = null, CancellationToken cancellationToken = default)
    {
        var item = await _context.PriceListItems
            .Include(i => i.PriceList)
            .FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

        if (item == null) return false;

        var oldPrice = item.UnitPrice;
        item.UnitPrice = newUnitPrice;
        if (vatRate.HasValue) item.VatRate = vatRate.Value;
        if (!string.IsNullOrWhiteSpace(note)) item.Note = note.Trim();
        item.UpdatedAt = DateTime.UtcNow;
        item.UpdatedBy = _currentUserService.UserName ?? "System";

        if (item.PriceList != null)
        {
            item.PriceList.UpdatedAt = DateTime.UtcNow;
            item.PriceList.UpdatedBy = _currentUserService.UserName ?? "System";
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.PriceChanged,
            _currentUserService.UserName ?? "System",
            nameof(PriceListItem),
            item.Id.ToString(),
            $"Điều chỉnh giá mặt hàng {item.NewCode} từ {oldPrice:N0} ₫ thành {newUnitPrice:N0} ₫ (Bảng giá #{item.PriceListId})",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<int?> CreateProductFromPriceListItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _context.PriceListItems.FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);
        if (item == null) return null;

        if (item.ProductId.HasValue) return item.ProductId.Value;

        var trimmedNewCode = item.NewCode.Trim();
        var trimmedLegacyCode = item.LegacyCode?.Trim();

        var existingInDb = await _context.Products.FirstOrDefaultAsync(p =>
            (!string.IsNullOrEmpty(p.NewCode) && p.NewCode == trimmedNewCode) ||
            (!string.IsNullOrEmpty(p.LegacyCode) && !string.IsNullOrEmpty(trimmedLegacyCode) && p.LegacyCode == trimmedLegacyCode) ||
            (!string.IsNullOrEmpty(p.Code) && p.Code == trimmedNewCode),
            cancellationToken);

        if (existingInDb != null)
        {
            item.ProductId = existingInDb.Id;
            item.MatchStatus = PriceMatchStatus.Matched;
            await _context.SaveChangesAsync(cancellationToken);
            return existingInDb.Id;
        }

        var prodCode = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.Product, cancellationToken);
        while (await _context.Products.AnyAsync(p => p.Code == prodCode, cancellationToken))
        {
            prodCode = await _codeGenerator.GenerateCodeAsync(SystemCodeConstants.Product, cancellationToken);
        }

        var safeName = !string.IsNullOrWhiteSpace(item.ProductInfo)
            ? item.ProductInfo.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)[0].Trim()
            : trimmedNewCode;
        if (safeName.Length > 300) safeName = safeName.Substring(0, 300);

        var newProd = new Product
        {
            Code = prodCode,
            NewCode = trimmedNewCode.Length > 50 ? trimmedNewCode.Substring(0, 50) : trimmedNewCode,
            LegacyCode = trimmedLegacyCode != null && trimmedLegacyCode.Length > 50 ? trimmedLegacyCode.Substring(0, 50) : trimmedLegacyCode,
            Name = string.IsNullOrWhiteSpace(safeName) ? trimmedNewCode : safeName,
            Description = item.ProductInfo != null && item.ProductInfo.Length > 2000 ? item.ProductInfo.Substring(0, 2000) : item.ProductInfo,
            Specifications = item.ProductInfo != null && item.ProductInfo.Length > 4000 ? item.ProductInfo.Substring(0, 4000) : item.ProductInfo,
            TaxTreatment = TaxTreatment.Standard10,
            IsTaxReductionEligible = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserName ?? "System"
        };

        _context.Products.Add(newProd);
        await _context.SaveChangesAsync(cancellationToken);

        item.ProductId = newProd.Id;
        item.MatchStatus = PriceMatchStatus.Matched;
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.ProductCreated,
            _currentUserService.UserName ?? "System",
            nameof(Product),
            newProd.Code,
            $"Tạo sản phẩm {newProd.Code} - {newProd.Name} từ Bảng giá #{item.PriceListId}",
            cancellationToken: cancellationToken);

        return newProd.Id;
    }
}
