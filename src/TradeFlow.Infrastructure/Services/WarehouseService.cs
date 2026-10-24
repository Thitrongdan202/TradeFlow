using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ClosedXML.Excel;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Warehouse;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Entities.Warehouses;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services.Pdf;
using QuestPDF.Fluent;

namespace TradeFlow.Infrastructure.Services;

public class WarehouseService : IWarehouseService
{
    private readonly TradeFlowDbContext _context;
    private readonly IAuditService _auditService;

    public WarehouseService(TradeFlowDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    private static DateTime NormalizeToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified || date.Kind == DateTimeKind.Local)
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return date;
    }

    private async Task<string> GenerateCodeAsync(string sequenceKey, string prefix, CancellationToken cancellationToken)
    {
        var sequence = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == sequenceKey, cancellationToken);
        if (sequence == null)
        {
            sequence = new SystemSequence
            {
                SequenceKey = sequenceKey,
                Prefix = prefix,
                CurrentNumber = 1
            };
            _context.SystemSequences.Add(sequence);
        }

        string code = $"{sequence.Prefix}{DateTime.UtcNow.Year}-{sequence.CurrentNumber:D4}";
        sequence.CurrentNumber++;
        return code;
    }

    #region 1. Tồn kho & Thẻ kho biến động

    public async Task<List<InventoryStockDto>> GetStocksAsync(int? warehouseId = null, StockCondition? condition = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryStocks
            .Include(x => x.Warehouse)
            .Include(x => x.Product)
                .ThenInclude(p => p.Category)
            .Include(x => x.Product)
                .ThenInclude(p => p.Unit)
            .AsNoTracking();

        if (warehouseId.HasValue && warehouseId.Value > 0)
        {
            query = query.Where(x => x.WarehouseId == warehouseId.Value);
        }

        if (condition.HasValue)
        {
            query = query.Where(x => x.Condition == condition.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Product.Name.ToLower().Contains(term) ||
                x.Product.Code.ToLower().Contains(term) ||
                (x.Product.LegacyCode != null && x.Product.LegacyCode.ToLower().Contains(term)) ||
                (x.Warehouse.Name.ToLower().Contains(term)));
        }

        var list = await query
            .OrderBy(x => x.Warehouse.Name)
            .ThenBy(x => x.Product.Name)
            .ToListAsync(cancellationToken);

        return list.Select(x => new InventoryStockDto
        {
            Id = x.Id,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.Warehouse?.Name ?? string.Empty,
            WarehouseCode = x.Warehouse?.Code ?? string.Empty,
            ProductId = x.ProductId,
            ProductCode = x.Product?.Code ?? string.Empty,
            ProductName = x.Product?.Name ?? string.Empty,
            UnitName = x.Product?.Unit?.Name ?? "Cái",
            CategoryName = x.Product?.Category?.Name,
            Condition = x.Condition,
            Quantity = x.Quantity,
            LastMovementAt = x.LastMovementAt
        }).ToList();
    }

    public async Task<List<InventoryMovementDto>> GetMovementsAsync(int? warehouseId = null, int? productId = null, StockCondition? condition = null, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryMovements
            .Include(x => x.Warehouse)
            .Include(x => x.Product)
                .ThenInclude(p => p.Unit)
            .AsNoTracking();

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (productId.HasValue && productId.Value > 0)
            query = query.Where(x => x.ProductId == productId.Value);

        if (condition.HasValue)
            query = query.Where(x => x.Condition == condition.Value);

        if (fromDate.HasValue)
        {
            var f = NormalizeToUtc(fromDate.Value.Date);
            query = query.Where(x => x.MovementDate >= f);
        }

        if (toDate.HasValue)
        {
            var t = NormalizeToUtc(toDate.Value.Date.AddDays(1).AddTicks(-1));
            query = query.Where(x => x.MovementDate <= t);
        }

        var list = await query
            .OrderByDescending(x => x.MovementDate)
            .ThenByDescending(x => x.Id)
            .Take(500)
            .ToListAsync(cancellationToken);

        return list.Select(x => new InventoryMovementDto
        {
            Id = x.Id,
            MovementDate = x.MovementDate,
            MovementType = x.MovementType,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.Warehouse?.Name ?? string.Empty,
            ProductId = x.ProductId,
            ProductCode = x.Product?.Code ?? string.Empty,
            ProductName = x.Product?.Name ?? string.Empty,
            UnitName = x.Product?.Unit?.Name ?? string.Empty,
            Condition = x.Condition,
            Quantity = x.Quantity,
            Direction = x.Direction,
            PreviousBalance = x.PreviousBalance,
            NewBalance = x.NewBalance,
            UnitCost = x.UnitCost,
            VoucherId = x.VoucherId,
            VoucherNumber = x.VoucherNumber,
            SourceReference = x.SourceReference,
            Notes = x.Notes,
            CreatedByName = x.CreatedByName
        }).ToList();
    }

    public async Task EnsureInitialStockAsync(int warehouseId, int productId, decimal quantity, string user, CancellationToken cancellationToken = default)
    {
        var stock = await _context.InventoryStocks
            .FirstOrDefaultAsync(x => x.WarehouseId == warehouseId && x.ProductId == productId && x.Condition == StockCondition.Saleable, cancellationToken);

        if (stock == null)
        {
            stock = new InventoryStock
            {
                WarehouseId = warehouseId,
                ProductId = productId,
                Condition = StockCondition.Saleable,
                Quantity = quantity,
                LastMovementAt = DateTime.UtcNow
            };
            _context.InventoryStocks.Add(stock);

            _context.InventoryMovements.Add(new InventoryMovement
            {
                MovementDate = DateTime.UtcNow,
                MovementType = InventoryMovementType.InitialBalance,
                WarehouseId = warehouseId,
                ProductId = productId,
                Condition = StockCondition.Saleable,
                Quantity = quantity,
                Direction = "IN",
                PreviousBalance = 0,
                NewBalance = quantity,
                UnitCost = 0,
                Notes = "Khởi tạo số dư tồn kho ban đầu",
                CreatedByName = user
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    #endregion

    #region 2. Chứng từ kho (Mẫu 01-VT, Mẫu 02-VT, Mẫu 03-VT, Mẫu 05-VT) & Chứng từ

    public async Task<List<WarehouseVoucherDto>> GetVouchersAsync(int? warehouseId = null, WarehouseVoucherType? type = null, WarehouseVoucherStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.WarehouseVouchers
            .Include(x => x.Warehouse)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking();

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (type.HasValue)
            query = query.Where(x => x.VoucherType == type.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.VoucherNumber.ToLower().Contains(term) ||
                x.Reason.ToLower().Contains(term) ||
                (x.PartnerName != null && x.PartnerName.ToLower().Contains(term)) ||
                (x.SourceReference != null && x.SourceReference.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(x => x.VoucherDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return list.Select(MapVoucherToDto).ToList();
    }

    public async Task<WarehouseVoucherDto?> GetVoucherByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var v = await _context.WarehouseVouchers
            .Include(x => x.Warehouse)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return v == null ? null : MapVoucherToDto(v);
    }

    public async Task<WarehouseVoucherDto> CreateVoucherAsync(WarehouseVoucherDto dto, string userId, string userName, CancellationToken cancellationToken = default)
    {
        string seqKey = dto.VoucherType switch
        {
            WarehouseVoucherType.InboundReceipt => "ReceiptVoucher",
            WarehouseVoucherType.OutboundIssue => "IssueVoucher",
            WarehouseVoucherType.InspectionRecord => "InspectionVoucher",
            WarehouseVoucherType.StocktakeReport => "StocktakeVoucher",
            _ => "WarehouseVoucher"
        };

        string prefix = dto.VoucherType switch
        {
            WarehouseVoucherType.InboundReceipt => "PNK-",
            WarehouseVoucherType.OutboundIssue => "PXK-",
            WarehouseVoucherType.InspectionRecord => "BBKN-",
            WarehouseVoucherType.StocktakeReport => "BBKK-",
            _ => "PK-"
        };

        string voucherNum = await GenerateCodeAsync(seqKey, prefix, cancellationToken);

        var voucher = new WarehouseVoucher
        {
            VoucherType = dto.VoucherType,
            VoucherNumber = voucherNum,
            VoucherDate = NormalizeToUtc(dto.VoucherDate),
            WarehouseId = dto.WarehouseId,
            Reason = dto.Reason,
            PartnerName = dto.PartnerName,
            PartnerAddress = dto.PartnerAddress,
            SourceModule = string.IsNullOrEmpty(dto.SourceModule) ? "Warehouse" : dto.SourceModule,
            SourceRecordId = dto.SourceRecordId,
            SourceReference = dto.SourceReference,
            Status = WarehouseVoucherStatus.Draft,
            Notes = dto.Notes,
            CreatedByUserId = userId,
            CreatedByName = userName,
            WarehouseKeeperName = dto.WarehouseKeeperName ?? userName,
            DelivererOrReceiverName = dto.DelivererOrReceiverName ?? dto.PartnerName
        };

        decimal totalQty = 0;
        decimal totalAmt = 0;

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null) continue;

            decimal lineTotal = itemDto.ActualQuantity * itemDto.UnitPrice;
            totalQty += itemDto.ActualQuantity;
            totalAmt += lineTotal;

            voucher.Items.Add(new WarehouseVoucherItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitName = product.Unit?.Name ?? "Cái",
                Condition = itemDto.Condition,
                DocumentQuantity = itemDto.DocumentQuantity > 0 ? itemDto.DocumentQuantity : itemDto.ActualQuantity,
                ActualQuantity = itemDto.ActualQuantity,
                UnitPrice = itemDto.UnitPrice,
                LineTotal = lineTotal,
                Notes = itemDto.Notes
            });
        }

        voucher.TotalQuantity = totalQty;
        voucher.TotalAmount = totalAmt;

        _context.WarehouseVouchers.Add(voucher);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.WarehouseVoucherCreated,
            performedBy: userName,
            targetEntity: "WarehouseVoucher",
            targetId: voucher.Id.ToString(),
            details: $"Tạo {voucher.VoucherNumber} ({voucher.Reason}), SL: {voucher.TotalQuantity:N0}",
            cancellationToken: cancellationToken);

        return MapVoucherToDto(voucher);
    }

    public async Task<bool> ApproveVoucherAsync(int voucherId, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            IDbContextTransaction? transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
            var voucher = await _context.WarehouseVouchers
                .Include(v => v.Items)
                .FirstOrDefaultAsync(v => v.Id == voucherId, cancellationToken);

            if (voucher == null || voucher.Status == WarehouseVoucherStatus.Cancelled)
                return false;

            // Idempotency: nếu đã duyệt rồi thì không ghi sổ lại
            if (voucher.Status == WarehouseVoucherStatus.Approved)
                return true;

            voucher.Status = WarehouseVoucherStatus.Approved;
            voucher.ApprovedByUserId = userId;
            voucher.ApprovedByName = userName;
            voucher.ApprovedAt = DateTime.UtcNow;

            string direction = voucher.VoucherType == WarehouseVoucherType.InboundReceipt ? "IN" : "OUT";

            InventoryMovementType movementType;
            if (voucher.VoucherType == WarehouseVoucherType.InboundReceipt)
            {
                if (voucher.Reason.Contains("kiểm kê", StringComparison.OrdinalIgnoreCase))
                    movementType = InventoryMovementType.InboundStocktakeSurplus;
                else if (voucher.Reason.Contains("trả", StringComparison.OrdinalIgnoreCase))
                    movementType = InventoryMovementType.InboundCustomerReturn;
                else
                    movementType = InventoryMovementType.InboundPurchase;
            }
            else
            {
                if (voucher.Reason.Contains("kiểm kê", StringComparison.OrdinalIgnoreCase))
                    movementType = InventoryMovementType.OutboundStocktakeShortage;
                else if (voucher.Reason.Contains("trả", StringComparison.OrdinalIgnoreCase))
                    movementType = InventoryMovementType.OutboundSupplierReturn;
                else if (voucher.Reason.Contains("hủy", StringComparison.OrdinalIgnoreCase) || voucher.Reason.Contains("thanh lý", StringComparison.OrdinalIgnoreCase))
                    movementType = InventoryMovementType.OutboundScrap;
                else
                    movementType = InventoryMovementType.OutboundSale;
            }

            // Ghi nhận biến động thẻ kho và cập nhật số dư tồn kho
            foreach (var item in voucher.Items)
            {
                var stock = await _context.InventoryStocks
                    .FirstOrDefaultAsync(s => s.WarehouseId == voucher.WarehouseId &&
                                              s.ProductId == item.ProductId &&
                                              s.Condition == item.Condition, cancellationToken);

                if (stock == null)
                {
                    stock = new InventoryStock
                    {
                        WarehouseId = voucher.WarehouseId,
                        ProductId = item.ProductId,
                        Condition = item.Condition,
                        Quantity = 0,
                        LastMovementAt = DateTime.UtcNow
                    };
                    _context.InventoryStocks.Add(stock);
                }

                decimal prev = stock.Quantity;
                decimal next = direction == "IN" ? prev + item.ActualQuantity : prev - item.ActualQuantity;

                stock.Quantity = next;
                stock.LastMovementAt = DateTime.UtcNow;

                _context.InventoryMovements.Add(new InventoryMovement
                {
                    MovementDate = DateTime.UtcNow,
                    MovementType = movementType,
                    WarehouseId = voucher.WarehouseId,
                    ProductId = item.ProductId,
                    Condition = item.Condition,
                    Quantity = item.ActualQuantity,
                    Direction = direction,
                    PreviousBalance = prev,
                    NewBalance = next,
                    UnitCost = item.UnitPrice,
                    VoucherId = voucher.Id,
                    VoucherNumber = voucher.VoucherNumber,
                    SourceReference = voucher.SourceReference ?? voucher.VoucherNumber,
                    Notes = $"{voucher.Reason} ({voucher.VoucherNumber})",
                    CreatedByUserId = userId,
                    CreatedByName = userName
                });
            }

            // Đăng ký chứng từ vào phân hệ Chứng từ trung tâm (DocumentAttachment)
            await RegisterToCentralDocumentsAsync(voucher, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            await _auditService.LogAsync(
                AuditEventType.WarehouseVoucherApproved,
                performedBy: userName,
                targetEntity: "WarehouseVoucher",
                targetId: voucher.Id.ToString(),
                details: $"Phê duyệt và ghi sổ chứng từ kho {voucher.VoucherNumber} ({voucher.Reason})",
                cancellationToken: cancellationToken);

            return true;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
        });
    }

    public async Task<bool> CancelVoucherAsync(int voucherId, string reason, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var voucher = await _context.WarehouseVouchers.FirstOrDefaultAsync(v => v.Id == voucherId, cancellationToken);
        if (voucher == null || voucher.Status == WarehouseVoucherStatus.Approved)
            return false; // Không được hủy trực tiếp phiếu đã ghi sổ, phải lập phiếu hoàn nhập/điều chỉnh đảo

        voucher.Status = WarehouseVoucherStatus.Cancelled;
        voucher.Notes = $"{voucher.Notes} [Hủy bởi {userName}: {reason}]";

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.WarehouseVoucherCancelled,
            performedBy: userName,
            targetEntity: "WarehouseVoucher",
            targetId: voucher.Id.ToString(),
            details: $"Hủy chứng từ kho {voucher.VoucherNumber}: {reason}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<byte[]> GenerateVoucherPdfAsync(int voucherId, CancellationToken cancellationToken = default)
    {
        var voucherDto = await GetVoucherByIdAsync(voucherId, cancellationToken);
        if (voucherDto == null)
            throw new KeyNotFoundException($"Không tìm thấy chứng từ kho #{voucherId}");

        var companySettings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        var document = new WarehouseVoucherPdfDocument(voucherDto, companySettings);
        return document.GeneratePdf();
    }

    private async Task RegisterToCentralDocumentsAsync(WarehouseVoucher voucher, CancellationToken cancellationToken)
    {
        string catCode = voucher.VoucherType switch
        {
            WarehouseVoucherType.InboundReceipt => "PHIEU_NHAP",
            WarehouseVoucherType.OutboundIssue => "PHIEU_XUAT",
            WarehouseVoucherType.InspectionRecord => "BIEN_BAN",
            WarehouseVoucherType.StocktakeReport => "BIEN_BAN",
            _ => "CHUNG_TU"
        };

        var cat = await _context.DocumentCategories.FirstOrDefaultAsync(c => c.Code == catCode, cancellationToken);
        int? catId = cat?.Id;

        bool exists = await _context.DocumentAttachments.AnyAsync(
            d => d.RelatedEntityType == "WarehouseVoucher" && d.RelatedEntityId == voucher.Id, cancellationToken);

        if (!exists)
        {
            string formTitle = voucher.VoucherType switch
            {
                WarehouseVoucherType.InboundReceipt => "Phiếu nhập kho Mẫu 01-VT",
                WarehouseVoucherType.OutboundIssue => "Phiếu xuất kho Mẫu 02-VT",
                WarehouseVoucherType.InspectionRecord => "Biên bản kiểm nghiệm Mẫu 03-VT",
                WarehouseVoucherType.StocktakeReport => "Biên bản kiểm kê Mẫu 05-VT",
                _ => "Chứng từ kho"
            };

            _context.DocumentAttachments.Add(new DocumentAttachment
            {
                FileName = $"{voucher.VoucherNumber}.pdf",
                StoragePath = $"/vouchers/{voucher.VoucherNumber}",
                ContentType = "application/pdf",
                FileSize = 0,
                CategoryId = catId,
                RelatedEntityType = "WarehouseVoucher",
                RelatedEntityId = voucher.Id,
                Description = $"{formTitle}: {voucher.VoucherNumber} - {voucher.Reason}",
                Status = "Active"
            });
        }
    }

    #endregion

    #region 3. Kiểm kê kho (Stocktaking)

    public async Task<List<StocktakeSessionDto>> GetStocktakeSessionsAsync(int? warehouseId = null, StocktakeStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.StocktakeSessions
            .Include(x => x.Warehouse)
            .Include(x => x.AdjustmentInboundVoucher)
            .Include(x => x.AdjustmentOutboundVoucher)
            .Include(x => x.Items)
            .AsNoTracking();

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                x.Reason.ToLower().Contains(term) ||
                (x.CountedByName != null && x.CountedByName.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(x => x.StocktakeDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return list.Select(MapStocktakeToDto).ToList();
    }

    public async Task<StocktakeSessionDto?> GetStocktakeSessionByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var session = await _context.StocktakeSessions
            .Include(x => x.Warehouse)
            .Include(x => x.AdjustmentInboundVoucher)
            .Include(x => x.AdjustmentOutboundVoucher)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return session == null ? null : MapStocktakeToDto(session);
    }

    public async Task<StocktakeSessionDto> CreateStocktakeSessionAsync(StocktakeSessionDto dto, string userId, string userName, CancellationToken cancellationToken = default)
    {
        string code = await GenerateCodeAsync("StocktakeSession", "KK-", cancellationToken);

        var session = new StocktakeSession
        {
            Code = code,
            WarehouseId = dto.WarehouseId,
            StocktakeDate = NormalizeToUtc(dto.StocktakeDate),
            Scope = string.IsNullOrEmpty(dto.Scope) ? "All" : dto.Scope,
            Reason = dto.Reason,
            Notes = dto.Notes,
            Status = StocktakeStatus.Counting,
            SnapshotTakenAt = DateTime.UtcNow,
            CountedByUserId = userId,
            CountedByName = userName
        };

        // Lấy danh sách sản phẩm theo phạm vi để lấy snapshot tồn sổ sách
        var allProducts = await _context.Products
            .Include(p => p.Unit)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var currentStocks = await _context.InventoryStocks
            .Where(s => s.WarehouseId == session.WarehouseId && s.Condition == StockCondition.Saleable)
            .ToDictionaryAsync(s => s.ProductId, s => s.Quantity, cancellationToken);

        foreach (var prod in allProducts)
        {
            decimal bookQty = currentStocks.TryGetValue(prod.Id, out var q) ? q : 0m;

            session.Items.Add(new StocktakeItem
            {
                ProductId = prod.Id,
                ProductCode = prod.Code,
                ProductName = prod.Name,
                UnitName = prod.Unit?.Name ?? "Cái",
                BookQuantity = bookQty,
                CountedQuantity = bookQty,
                VarianceQuantity = 0,
                Condition = StockCondition.Saleable,
                UnitCost = 0,
                VarianceValue = 0
            });
        }

        _context.StocktakeSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.StocktakeCreated,
            performedBy: userName,
            targetEntity: "StocktakeSession",
            targetId: session.Id.ToString(),
            details: $"Tạo đợt kiểm kê {session.Code} tại kho ID {session.WarehouseId} ({session.Reason}) - Snapshot {session.Items.Count} mặt hàng",
            cancellationToken: cancellationToken);

        return MapStocktakeToDto(session);
    }

    public async Task<bool> StartCountingAsync(int sessionId, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var session = await _context.StocktakeSessions
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null || session.Status != StocktakeStatus.Draft)
            return false;

        // Chốt thời điểm snapshot số liệu sổ sách
        session.SnapshotTakenAt = DateTime.UtcNow;
        session.Status = StocktakeStatus.Counting;
        session.CountedByUserId = userId;
        session.CountedByName = userName;

        // Lấy danh sách sản phẩm theo phạm vi để lấy snapshot tồn sổ sách
        var allProducts = await _context.Products
            .Include(p => p.Unit)
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

        var currentStocks = await _context.InventoryStocks
            .Where(s => s.WarehouseId == session.WarehouseId && s.Condition == StockCondition.Saleable)
            .ToDictionaryAsync(s => s.ProductId, s => s.Quantity, cancellationToken);

        session.Items.Clear();

        foreach (var prod in allProducts)
        {
            decimal bookQty = currentStocks.TryGetValue(prod.Id, out var q) ? q : 0m;

            session.Items.Add(new StocktakeItem
            {
                ProductId = prod.Id,
                ProductCode = prod.Code,
                ProductName = prod.Name,
                UnitName = prod.Unit?.Name ?? "Cái",
                BookQuantity = bookQty,
                CountedQuantity = bookQty, // Mặc định ban đầu bằng sổ sách, thủ kho sẽ nhập số đếm thực tế
                VarianceQuantity = 0,
                Condition = StockCondition.Saleable,
                UnitCost = 0,
                VarianceValue = 0
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.StocktakeUpdated,
            performedBy: userName,
            targetEntity: "StocktakeSession",
            targetId: session.Id.ToString(),
            details: $"Chốt snapshot sổ sách đợt kiểm kê {session.Code} ({session.Items.Count} mặt hàng)",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> SaveCountsAsync(int sessionId, List<StocktakeItemDto> counts, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var session = await _context.StocktakeSessions
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null || (session.Status != StocktakeStatus.Counting && session.Status != StocktakeStatus.Rejected))
            return false;

        foreach (var count in counts)
        {
            var item = session.Items.FirstOrDefault(i => i.Id == count.Id || i.ProductId == count.ProductId);
            if (item != null)
            {
                item.CountedQuantity = count.CountedQuantity;
                item.VarianceQuantity = item.CountedQuantity - item.BookQuantity;
                item.Condition = count.Condition;
                item.UnitCost = count.UnitCost;
                item.VarianceValue = item.VarianceQuantity * item.UnitCost;
                item.DiscrepancyReason = count.DiscrepancyReason;
                item.Notes = count.Notes;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SubmitStocktakeAsync(int sessionId, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var session = await _context.StocktakeSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session == null || (session.Status != StocktakeStatus.Counting && session.Status != StocktakeStatus.Rejected))
            return false;

        session.Status = StocktakeStatus.PendingReview;
        session.SubmittedAt = DateTime.UtcNow;
        session.CountedByUserId = userId;
        session.CountedByName = userName;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.StocktakeSubmitted,
            performedBy: userName,
            targetEntity: "StocktakeSession",
            targetId: session.Id.ToString(),
            details: $"Thủ kho nộp kết quả đợt kiểm kê {session.Code} chờ phê duyệt",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> RejectStocktakeAsync(int sessionId, string reason, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var session = await _context.StocktakeSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session == null || session.Status != StocktakeStatus.PendingReview)
            return false;

        session.Status = StocktakeStatus.Rejected;
        session.ReviewedByUserId = userId;
        session.ReviewedByName = userName;
        session.ReviewedAt = DateTime.UtcNow;
        session.RejectionReason = reason;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.StocktakeRejected,
            performedBy: userName,
            targetEntity: "StocktakeSession",
            targetId: session.Id.ToString(),
            details: $"Từ chối duyệt đợt kiểm kê {session.Code}: {reason}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> ApproveAndReconcileStocktakeAsync(int sessionId, string userId, string userName, bool isSuperAdmin, CancellationToken cancellationToken = default)
    {
        var session = await _context.StocktakeSessions
            .Include(s => s.Warehouse)
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null) return false;

        // BẮT BUỘC: Kiểm tra phân tách nhiệm vụ (Separation of Duties)
        // Người kiểm đếm (Counter) không được tự duyệt kết quả của chính mình
        if (!isSuperAdmin && !string.IsNullOrEmpty(session.CountedByUserId) && session.CountedByUserId == userId)
        {
            throw new InvalidOperationException("Vi phạm nguyên tắc phân tách nhiệm vụ (Separation of Duties): Người thực hiện kiểm đếm không được phép tự phê duyệt kết quả kiểm kê. Cần cấp quản lý hoặc giám sát độc lập xem xét.");
        }

        // Idempotency: nếu đã hoàn tất rồi thì không ghi sổ lại
        if (session.Status == StocktakeStatus.Completed)
            return true;

        if (session.Status != StocktakeStatus.PendingReview && session.Status != StocktakeStatus.Approved)
            return false;

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            IDbContextTransaction? transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
            session.ReviewedByUserId = userId;
            session.ReviewedByName = userName;
            session.ReviewedAt = DateTime.UtcNow;
            session.ApprovedAt = DateTime.UtcNow;

            var surplusItems = session.Items.Where(i => i.VarianceQuantity > 0).ToList();
            var shortageItems = session.Items.Where(i => i.VarianceQuantity < 0).ToList();
            var defectiveItems = session.Items.Where(i => i.Condition == StockCondition.Defective && i.CountedQuantity > 0).ToList();

            // 1. Nếu có thừa: Lập Phiếu nhập kho điều chỉnh (Mẫu 01-VT)
            if (surplusItems.Any())
            {
                string surplusNum = await GenerateCodeAsync("ReceiptVoucher", "PNK-", cancellationToken);
                var surplusVoucher = new WarehouseVoucher
                {
                    VoucherType = WarehouseVoucherType.InboundReceipt,
                    VoucherNumber = surplusNum,
                    VoucherDate = DateTime.UtcNow,
                    WarehouseId = session.WarehouseId,
                    Reason = $"Thừa phát hiện khi kiểm kê đợt {session.Code}",
                    SourceModule = "Stocktaking",
                    SourceRecordId = session.Id,
                    SourceReference = session.Code,
                    Status = WarehouseVoucherStatus.Approved,
                    TotalQuantity = surplusItems.Sum(i => i.VarianceQuantity),
                    TotalAmount = surplusItems.Sum(i => i.VarianceQuantity * i.UnitCost),
                    CreatedByUserId = userId,
                    CreatedByName = userName,
                    ApprovedByUserId = userId,
                    ApprovedByName = userName,
                    ApprovedAt = DateTime.UtcNow,
                    WarehouseKeeperName = session.CountedByName ?? userName,
                    DelivererOrReceiverName = "Hội đồng kiểm kê"
                };

                foreach (var item in surplusItems)
                {
                    surplusVoucher.Items.Add(new WarehouseVoucherItem
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        UnitName = item.UnitName,
                        Condition = item.Condition,
                        DocumentQuantity = item.VarianceQuantity,
                        ActualQuantity = item.VarianceQuantity,
                        UnitPrice = item.UnitCost,
                        LineTotal = item.VarianceQuantity * item.UnitCost,
                        Notes = $"Kiểm kê thừa: {item.DiscrepancyReason}"
                    });
                }

                _context.WarehouseVouchers.Add(surplusVoucher);
                await _context.SaveChangesAsync(cancellationToken);

                session.AdjustmentInboundVoucherId = surplusVoucher.Id;

                // Ghi nhận biến động thẻ kho
                foreach (var sItem in surplusItems)
                {
                    var stock = await _context.InventoryStocks
                        .FirstOrDefaultAsync(s => s.WarehouseId == session.WarehouseId &&
                                                  s.ProductId == sItem.ProductId &&
                                                  s.Condition == sItem.Condition, cancellationToken);

                    if (stock == null)
                    {
                        stock = new InventoryStock
                        {
                            WarehouseId = session.WarehouseId,
                            ProductId = sItem.ProductId,
                            Condition = sItem.Condition,
                            Quantity = 0,
                            LastMovementAt = DateTime.UtcNow
                        };
                        _context.InventoryStocks.Add(stock);
                    }

                    decimal prev = stock.Quantity;
                    decimal next = prev + sItem.VarianceQuantity;
                    stock.Quantity = next;
                    stock.LastMovementAt = DateTime.UtcNow;

                    _context.InventoryMovements.Add(new InventoryMovement
                    {
                        MovementDate = DateTime.UtcNow,
                        MovementType = InventoryMovementType.InboundStocktakeSurplus,
                        WarehouseId = session.WarehouseId,
                        ProductId = sItem.ProductId,
                        Condition = sItem.Condition,
                        Quantity = sItem.VarianceQuantity,
                        Direction = "IN",
                        PreviousBalance = prev,
                        NewBalance = next,
                        UnitCost = sItem.UnitCost,
                        VoucherId = surplusVoucher.Id,
                        VoucherNumber = surplusVoucher.VoucherNumber,
                        SourceReference = session.Code,
                        Notes = $"Thừa phát hiện khi kiểm kê ({session.Code})",
                        CreatedByUserId = userId,
                        CreatedByName = userName
                    });
                }

                await RegisterToCentralDocumentsAsync(surplusVoucher, cancellationToken);
            }

            // 2. Nếu có thiếu: Lập Phiếu xuất kho điều chỉnh (Mẫu 02-VT)
            if (shortageItems.Any())
            {
                string shortageNum = await GenerateCodeAsync("IssueVoucher", "PXK-", cancellationToken);
                var shortageVoucher = new WarehouseVoucher
                {
                    VoucherType = WarehouseVoucherType.OutboundIssue,
                    VoucherNumber = shortageNum,
                    VoucherDate = DateTime.UtcNow,
                    WarehouseId = session.WarehouseId,
                    Reason = $"Thiếu phát hiện khi kiểm kê đợt {session.Code}",
                    SourceModule = "Stocktaking",
                    SourceRecordId = session.Id,
                    SourceReference = session.Code,
                    Status = WarehouseVoucherStatus.Approved,
                    TotalQuantity = shortageItems.Sum(i => Math.Abs(i.VarianceQuantity)),
                    TotalAmount = shortageItems.Sum(i => Math.Abs(i.VarianceQuantity) * i.UnitCost),
                    CreatedByUserId = userId,
                    CreatedByName = userName,
                    ApprovedByUserId = userId,
                    ApprovedByName = userName,
                    ApprovedAt = DateTime.UtcNow,
                    WarehouseKeeperName = session.CountedByName ?? userName,
                    DelivererOrReceiverName = "Hội đồng kiểm kê"
                };

                foreach (var item in shortageItems)
                {
                    decimal shortageQty = Math.Abs(item.VarianceQuantity);
                    shortageVoucher.Items.Add(new WarehouseVoucherItem
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        UnitName = item.UnitName,
                        Condition = item.Condition,
                        DocumentQuantity = shortageQty,
                        ActualQuantity = shortageQty,
                        UnitPrice = item.UnitCost,
                        LineTotal = shortageQty * item.UnitCost,
                        Notes = $"Kiểm kê thiếu: {item.DiscrepancyReason}"
                    });
                }

                _context.WarehouseVouchers.Add(shortageVoucher);
                await _context.SaveChangesAsync(cancellationToken);

                session.AdjustmentOutboundVoucherId = shortageVoucher.Id;

                // Ghi nhận biến động thẻ kho
                foreach (var sItem in shortageItems)
                {
                    decimal shortageQty = Math.Abs(sItem.VarianceQuantity);

                    var stock = await _context.InventoryStocks
                        .FirstOrDefaultAsync(s => s.WarehouseId == session.WarehouseId &&
                                                  s.ProductId == sItem.ProductId &&
                                                  s.Condition == sItem.Condition, cancellationToken);

                    if (stock == null)
                    {
                        stock = new InventoryStock
                        {
                            WarehouseId = session.WarehouseId,
                            ProductId = sItem.ProductId,
                            Condition = sItem.Condition,
                            Quantity = 0,
                            LastMovementAt = DateTime.UtcNow
                        };
                        _context.InventoryStocks.Add(stock);
                    }

                    decimal prev = stock.Quantity;
                    decimal next = Math.Max(0, prev - shortageQty);
                    stock.Quantity = next;
                    stock.LastMovementAt = DateTime.UtcNow;

                    _context.InventoryMovements.Add(new InventoryMovement
                    {
                        MovementDate = DateTime.UtcNow,
                        MovementType = InventoryMovementType.OutboundStocktakeShortage,
                        WarehouseId = session.WarehouseId,
                        ProductId = sItem.ProductId,
                        Condition = sItem.Condition,
                        Quantity = shortageQty,
                        Direction = "OUT",
                        PreviousBalance = prev,
                        NewBalance = next,
                        UnitCost = sItem.UnitCost,
                        VoucherId = shortageVoucher.Id,
                        VoucherNumber = shortageVoucher.VoucherNumber,
                        SourceReference = session.Code,
                        Notes = $"Thiếu phát hiện khi kiểm kê ({session.Code})",
                        CreatedByUserId = userId,
                        CreatedByName = userName
                    });
                }

                await RegisterToCentralDocumentsAsync(shortageVoucher, cancellationToken);
            }

            // 3. Nếu phát hiện hàng hỏng/lỗi trong kiểm kê -> Chuyển vào khu cách ly
            foreach (var dItem in defectiveItems)
            {
                string clCode = await GenerateCodeAsync("QuarantineItem", "CL-", cancellationToken);
                _context.QuarantinedInventoryItems.Add(new QuarantinedInventoryItem
                {
                    Code = clCode,
                    WarehouseId = session.WarehouseId,
                    ProductId = dItem.ProductId,
                    ProductCode = dItem.ProductCode,
                    ProductName = dItem.ProductName,
                    UnitName = dItem.UnitName,
                    Quantity = dItem.CountedQuantity,
                    DefectReason = string.IsNullOrEmpty(dItem.DiscrepancyReason) ? "Hỏng phát hiện khi kiểm kê" : dItem.DiscrepancyReason,
                    DateIdentified = DateTime.UtcNow,
                    SourceType = "Stocktaking",
                    SourceReference = session.Code,
                    ReportedByName = session.CountedByName ?? userName,
                    Status = QuarantineStatus.InQuarantine
                });
            }

            session.Status = StocktakeStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            await _auditService.LogAsync(
                AuditEventType.StocktakeApproved,
                performedBy: userName,
                targetEntity: "StocktakeSession",
                targetId: session.Id.ToString(),
                details: $"Phê duyệt và hoàn tất đối soát đợt kiểm kê {session.Code}. Thừa: {surplusItems.Count}, Thiếu: {shortageItems.Count}",
                cancellationToken: cancellationToken);

            return true;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
        });
    }

    public async Task<bool> CancelStocktakeAsync(int sessionId, string reason, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var session = await _context.StocktakeSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
        if (session == null || session.Status == StocktakeStatus.Completed)
            return false;

        session.Status = StocktakeStatus.Cancelled;
        session.Notes = $"{session.Notes} [Hủy bởi {userName}: {reason}]";

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.StocktakeCancelled,
            performedBy: userName,
            targetEntity: "StocktakeSession",
            targetId: session.Id.ToString(),
            details: $"Hủy đợt kiểm kê {session.Code}: {reason}",
            cancellationToken: cancellationToken);

        return true;
    }

    #endregion

    #region 4. Hàng trả lại (Customer & Supplier Returns)

    public async Task<List<WarehouseReturnOrderDto>> GetReturnOrdersAsync(WarehouseReturnType? type = null, WarehouseReturnStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.WarehouseReturnOrders
            .Include(x => x.Warehouse)
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .Include(x => x.ResultingVoucher)
            .Include(x => x.Items)
            .AsNoTracking();

        if (type.HasValue)
            query = query.Where(x => x.ReturnType == type.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                x.Reason.ToLower().Contains(term) ||
                (x.CustomerName != null && x.CustomerName.ToLower().Contains(term)) ||
                (x.SupplierName != null && x.SupplierName.ToLower().Contains(term)) ||
                (x.OriginalInvoiceNumber != null && x.OriginalInvoiceNumber.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(x => x.ReturnDate)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return list.Select(MapReturnOrderToDto).ToList();
    }

    public async Task<WarehouseReturnOrderDto?> GetReturnOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.WarehouseReturnOrders
            .Include(x => x.Warehouse)
            .Include(x => x.Customer)
            .Include(x => x.Supplier)
            .Include(x => x.ResultingVoucher)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return order == null ? null : MapReturnOrderToDto(order);
    }

    public async Task<WarehouseReturnOrderDto> CreateReturnOrderAsync(WarehouseReturnOrderDto dto, string userId, string userName, CancellationToken cancellationToken = default)
    {
        string seqKey = dto.ReturnType == WarehouseReturnType.CustomerReturn ? "CustomerReturn" : "SupplierReturn";
        string prefix = dto.ReturnType == WarehouseReturnType.CustomerReturn ? "KTH-" : "TNCC-";
        string code = await GenerateCodeAsync(seqKey, prefix, cancellationToken);

        var order = new WarehouseReturnOrder
        {
            Code = code,
            ReturnType = dto.ReturnType,
            WarehouseId = dto.WarehouseId,
            ReturnDate = NormalizeToUtc(dto.ReturnDate),
            CustomerId = dto.CustomerId,
            CustomerName = dto.CustomerName,
            SupplierId = dto.SupplierId,
            SupplierName = dto.SupplierName,
            OriginalInvoiceId = dto.OriginalInvoiceId,
            OriginalInvoiceNumber = dto.OriginalInvoiceNumber,
            OriginalSalesOrderId = dto.OriginalSalesOrderId,
            OriginalSalesOrderCode = dto.OriginalSalesOrderCode,
            Reason = dto.Reason,
            Notes = dto.Notes,
            Status = WarehouseReturnStatus.PendingIntake
        };

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null) continue;

            order.Items.Add(new WarehouseReturnOrderItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitName = product.Unit?.Name ?? "Cái",
                RequestedQuantity = itemDto.RequestedQuantity,
                ReceivedQuantity = 0,
                Condition = StockCondition.Undetermined,
                Disposition = ReturnDisposition.PendingInspection,
                DefectDetails = itemDto.DefectDetails,
                UnitPrice = itemDto.UnitPrice
            });
        }

        _context.WarehouseReturnOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.WarehouseReturnCreated,
            performedBy: userName,
            targetEntity: "WarehouseReturnOrder",
            targetId: order.Id.ToString(),
            details: $"Tạo đơn {order.Code} ({order.ReturnTypeName}): {order.Reason}",
            cancellationToken: cancellationToken);

        return MapReturnOrderToDto(order);
    }

    public async Task<bool> ReceiveReturnIntakeAsync(int returnOrderId, List<WarehouseReturnOrderItemDto> items, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var order = await _context.WarehouseReturnOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == returnOrderId, cancellationToken);

        if (order == null || (order.Status != WarehouseReturnStatus.PendingIntake && order.Status != WarehouseReturnStatus.ReceivedAwaitingInspection))
            return false;

        order.Status = WarehouseReturnStatus.ReceivedAwaitingInspection;
        order.ReceivedByUserId = userId;
        order.ReceivedByName = userName;
        order.ReceivedAt = DateTime.UtcNow;

        foreach (var it in items)
        {
            var target = order.Items.FirstOrDefault(i => i.Id == it.Id || i.ProductId == it.ProductId);
            if (target != null)
            {
                target.ReceivedQuantity = it.ReceivedQuantity;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> InspectReturnOrderAsync(int returnOrderId, List<WarehouseReturnOrderItemDto> items, string inspectionNotes, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var order = await _context.WarehouseReturnOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == returnOrderId, cancellationToken);

        if (order == null || (order.Status != WarehouseReturnStatus.ReceivedAwaitingInspection && order.Status != WarehouseReturnStatus.Inspected))
            return false;

        order.Status = WarehouseReturnStatus.Inspected;
        order.InspectedByUserId = userId;
        order.InspectedByName = userName;
        order.InspectedAt = DateTime.UtcNow;
        order.InspectionNotes = inspectionNotes;

        foreach (var it in items)
        {
            var target = order.Items.FirstOrDefault(i => i.Id == it.Id || i.ProductId == it.ProductId);
            if (target != null)
            {
                target.Condition = it.Condition;
                target.Disposition = it.Disposition;
                target.DefectDetails = it.DefectDetails;
                target.InspectionNotes = it.InspectionNotes;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.WarehouseReturnInspected,
            performedBy: userName,
            targetEntity: "WarehouseReturnOrder",
            targetId: order.Id.ToString(),
            details: $"Kiểm định chất lượng đơn hàng trả {order.Code}: {inspectionNotes}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<bool> ApproveAndProcessReturnAsync(int returnOrderId, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var order = await _context.WarehouseReturnOrders
            .Include(o => o.Warehouse)
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == returnOrderId, cancellationToken);

        if (order == null || order.Status == WarehouseReturnStatus.Completed)
            return true;

        if (order.Status != WarehouseReturnStatus.Inspected && order.Status != WarehouseReturnStatus.Approved)
            return false;

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            IDbContextTransaction? transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
            order.Status = WarehouseReturnStatus.Completed;
            order.ApprovedByUserId = userId;
            order.ApprovedByName = userName;
            order.ApprovedAt = DateTime.UtcNow;

            if (order.ReturnType == WarehouseReturnType.CustomerReturn)
            {
                // Khách trả hàng: Chỉ những hàng được duyệt nhập kho (RestockSaleable, TransferUsable) mới tạo phiếu nhập kho
                var validItems = order.Items.Where(i => i.Disposition == ReturnDisposition.RestockSaleable || i.Disposition == ReturnDisposition.TransferUsable).ToList();
                var defectiveItems = order.Items.Where(i => i.Disposition == ReturnDisposition.QuarantineDefective).ToList();

                if (validItems.Any())
                {
                    string vNum = await GenerateCodeAsync("ReceiptVoucher", "PNK-", cancellationToken);
                    var voucher = new WarehouseVoucher
                    {
                        VoucherType = WarehouseVoucherType.InboundReceipt,
                        VoucherNumber = vNum,
                        VoucherDate = DateTime.UtcNow,
                        WarehouseId = order.WarehouseId,
                        Reason = $"Nhập hàng khách trả lại theo đơn {order.Code}",
                        PartnerName = order.CustomerName,
                        SourceModule = "Returns",
                        SourceRecordId = order.Id,
                        SourceReference = order.Code,
                        Status = WarehouseVoucherStatus.Approved,
                        TotalQuantity = validItems.Sum(i => i.ReceivedQuantity),
                        TotalAmount = validItems.Sum(i => i.ReceivedQuantity * i.UnitPrice),
                        CreatedByUserId = userId,
                        CreatedByName = userName,
                        ApprovedByUserId = userId,
                        ApprovedByName = userName,
                        ApprovedAt = DateTime.UtcNow
                    };

                    foreach (var vIt in validItems)
                    {
                        voucher.Items.Add(new WarehouseVoucherItem
                        {
                            ProductId = vIt.ProductId,
                            ProductCode = vIt.ProductCode,
                            ProductName = vIt.ProductName,
                            UnitName = vIt.UnitName,
                            Condition = vIt.Disposition == ReturnDisposition.RestockSaleable ? StockCondition.Saleable : StockCondition.Usable,
                            DocumentQuantity = vIt.ReceivedQuantity,
                            ActualQuantity = vIt.ReceivedQuantity,
                            UnitPrice = vIt.UnitPrice,
                            LineTotal = vIt.ReceivedQuantity * vIt.UnitPrice,
                            Notes = $"Khách trả: {vIt.DispositionName}"
                        });
                    }

                    _context.WarehouseVouchers.Add(voucher);
                    await _context.SaveChangesAsync(cancellationToken);

                    order.ResultingVoucherId = voucher.Id;

                    // Ghi sổ kho
                    foreach (var it in validItems)
                    {
                        var cond = it.Disposition == ReturnDisposition.RestockSaleable ? StockCondition.Saleable : StockCondition.Usable;
                        var stock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == order.WarehouseId && s.ProductId == it.ProductId && s.Condition == cond, cancellationToken);
                        if (stock == null)
                        {
                            stock = new InventoryStock
                            {
                                WarehouseId = order.WarehouseId,
                                ProductId = it.ProductId,
                                Condition = cond,
                                Quantity = 0,
                                LastMovementAt = DateTime.UtcNow
                            };
                            _context.InventoryStocks.Add(stock);
                        }

                        decimal prev = stock.Quantity;
                        decimal next = prev + it.ReceivedQuantity;
                        stock.Quantity = next;
                        stock.LastMovementAt = DateTime.UtcNow;

                        _context.InventoryMovements.Add(new InventoryMovement
                        {
                            MovementDate = DateTime.UtcNow,
                            MovementType = InventoryMovementType.InboundCustomerReturn,
                            WarehouseId = order.WarehouseId,
                            ProductId = it.ProductId,
                            Condition = cond,
                            Quantity = it.ReceivedQuantity,
                            Direction = "IN",
                            PreviousBalance = prev,
                            NewBalance = next,
                            UnitCost = it.UnitPrice,
                            VoucherId = voucher.Id,
                            VoucherNumber = voucher.VoucherNumber,
                            SourceReference = order.Code,
                            Notes = $"Khách hàng trả lại ({order.Code})",
                            CreatedByUserId = userId,
                            CreatedByName = userName
                        });
                    }

                    await RegisterToCentralDocumentsAsync(voucher, cancellationToken);
                }

                // Nếu có hàng lỗi/hỏng -> Chuyển vào khu cách ly theo dõi riêng, KHÔNG tính vào hàng khả dụng bán
                foreach (var dIt in defectiveItems)
                {
                    string clCode = await GenerateCodeAsync("QuarantineItem", "CL-", cancellationToken);
                    _context.QuarantinedInventoryItems.Add(new QuarantinedInventoryItem
                    {
                        Code = clCode,
                        WarehouseId = order.WarehouseId,
                        ProductId = dIt.ProductId,
                        ProductCode = dIt.ProductCode,
                        ProductName = dIt.ProductName,
                        UnitName = dIt.UnitName,
                        Quantity = dIt.ReceivedQuantity,
                        DefectReason = string.IsNullOrEmpty(dIt.DefectDetails) ? "Hàng khách trả bị lỗi" : dIt.DefectDetails,
                        DateIdentified = DateTime.UtcNow,
                        SourceType = "CustomerReturn",
                        SourceReference = order.Code,
                        ReportedByName = order.InspectedByName ?? userName,
                        Status = QuarantineStatus.InQuarantine
                    });

                    // Cập nhật tồn kho hàng cách ly (Defective) trong sổ cái tồn kho
                    var defStock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == order.WarehouseId && s.ProductId == dIt.ProductId && s.Condition == StockCondition.Defective, cancellationToken);
                    if (defStock == null)
                    {
                        defStock = new InventoryStock
                        {
                            WarehouseId = order.WarehouseId,
                            ProductId = dIt.ProductId,
                            Condition = StockCondition.Defective,
                            Quantity = 0,
                            LastMovementAt = DateTime.UtcNow
                        };
                        _context.InventoryStocks.Add(defStock);
                    }

                    decimal prevDef = defStock.Quantity;
                    decimal nextDef = prevDef + dIt.ReceivedQuantity;
                    defStock.Quantity = nextDef;
                    defStock.LastMovementAt = DateTime.UtcNow;

                    _context.InventoryMovements.Add(new InventoryMovement
                    {
                        MovementDate = DateTime.UtcNow,
                        MovementType = InventoryMovementType.QuarantineTransferIn,
                        WarehouseId = order.WarehouseId,
                        ProductId = dIt.ProductId,
                        Condition = StockCondition.Defective,
                        Quantity = dIt.ReceivedQuantity,
                        Direction = "IN",
                        PreviousBalance = prevDef,
                        NewBalance = nextDef,
                        UnitCost = dIt.UnitPrice,
                        SourceReference = order.Code,
                        Notes = $"Hàng khách trả lỗi chuyển cách ly ({order.Code})",
                        CreatedByUserId = userId,
                        CreatedByName = userName
                    });
                }
            }
            else
            {
                // Trả hàng nhà cung cấp -> Xuất kho Mẫu 02-VT
                string vNum = await GenerateCodeAsync("IssueVoucher", "PXK-", cancellationToken);
                var voucher = new WarehouseVoucher
                {
                    VoucherType = WarehouseVoucherType.OutboundIssue,
                    VoucherNumber = vNum,
                    VoucherDate = DateTime.UtcNow,
                    WarehouseId = order.WarehouseId,
                    Reason = $"Xuất trả hàng nhà cung cấp theo đơn {order.Code}",
                    PartnerName = order.SupplierName,
                    SourceModule = "Returns",
                    SourceRecordId = order.Id,
                    SourceReference = order.Code,
                    Status = WarehouseVoucherStatus.Approved,
                    TotalQuantity = order.Items.Sum(i => i.RequestedQuantity),
                    TotalAmount = order.Items.Sum(i => i.RequestedQuantity * i.UnitPrice),
                    CreatedByUserId = userId,
                    CreatedByName = userName,
                    ApprovedByUserId = userId,
                    ApprovedByName = userName,
                    ApprovedAt = DateTime.UtcNow
                };

                foreach (var it in order.Items)
                {
                    voucher.Items.Add(new WarehouseVoucherItem
                    {
                        ProductId = it.ProductId,
                        ProductCode = it.ProductCode,
                        ProductName = it.ProductName,
                        UnitName = it.UnitName,
                        Condition = it.Condition == StockCondition.Undetermined ? StockCondition.Saleable : it.Condition,
                        DocumentQuantity = it.RequestedQuantity,
                        ActualQuantity = it.RequestedQuantity,
                        UnitPrice = it.UnitPrice,
                        LineTotal = it.RequestedQuantity * it.UnitPrice,
                        Notes = $"Trả NCC: {order.Reason}"
                    });
                }

                _context.WarehouseVouchers.Add(voucher);
                await _context.SaveChangesAsync(cancellationToken);

                order.ResultingVoucherId = voucher.Id;

                // Giảm tồn kho
                foreach (var it in order.Items)
                {
                    var cond = it.Condition == StockCondition.Undetermined ? StockCondition.Saleable : it.Condition;
                    var stock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == order.WarehouseId && s.ProductId == it.ProductId && s.Condition == cond, cancellationToken);
                    if (stock != null)
                    {
                        decimal prev = stock.Quantity;
                        decimal next = Math.Max(0, prev - it.RequestedQuantity);
                        stock.Quantity = next;
                        stock.LastMovementAt = DateTime.UtcNow;

                        _context.InventoryMovements.Add(new InventoryMovement
                        {
                            MovementDate = DateTime.UtcNow,
                            MovementType = InventoryMovementType.OutboundSupplierReturn,
                            WarehouseId = order.WarehouseId,
                            ProductId = it.ProductId,
                            Condition = cond,
                            Quantity = it.RequestedQuantity,
                            Direction = "OUT",
                            PreviousBalance = prev,
                            NewBalance = next,
                            UnitCost = it.UnitPrice,
                            VoucherId = voucher.Id,
                            VoucherNumber = voucher.VoucherNumber,
                            SourceReference = order.Code,
                            Notes = $"Xuất trả NCC ({order.Code})",
                            CreatedByUserId = userId,
                            CreatedByName = userName
                        });
                    }
                }

                await RegisterToCentralDocumentsAsync(voucher, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            await _auditService.LogAsync(
                AuditEventType.WarehouseReturnCompleted,
                performedBy: userName,
                targetEntity: "WarehouseReturnOrder",
                targetId: order.Id.ToString(),
                details: $"Hoàn tất quy trình xử lý đơn hàng trả {order.Code}",
                cancellationToken: cancellationToken);

            return true;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
        });
    }

    public async Task<bool> RejectReturnOrderAsync(int returnOrderId, string reason, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var order = await _context.WarehouseReturnOrders.FirstOrDefaultAsync(o => o.Id == returnOrderId, cancellationToken);
        if (order == null || order.Status == WarehouseReturnStatus.Completed)
            return false;

        order.Status = WarehouseReturnStatus.Rejected;
        order.Notes = $"{order.Notes} [Từ chối nhận bởi {userName}: {reason}]";

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    #endregion

    #region 5. Hàng lỗi / Cách ly (Defective Stock)

    public async Task<List<QuarantinedItemDto>> GetQuarantinedItemsAsync(int? warehouseId = null, QuarantineStatus? status = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.QuarantinedInventoryItems
            .Include(x => x.Warehouse)
            .Include(x => x.ResultingVoucher)
            .AsNoTracking();

        if (warehouseId.HasValue && warehouseId.Value > 0)
            query = query.Where(x => x.WarehouseId == warehouseId.Value);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string term = searchTerm.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                x.ProductName.ToLower().Contains(term) ||
                x.ProductCode.ToLower().Contains(term) ||
                x.DefectReason.ToLower().Contains(term) ||
                (x.SourceReference != null && x.SourceReference.ToLower().Contains(term)));
        }

        var list = await query
            .OrderByDescending(x => x.DateIdentified)
            .ThenByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        return list.Select(x => new QuarantinedItemDto
        {
            Id = x.Id,
            Code = x.Code,
            WarehouseId = x.WarehouseId,
            WarehouseName = x.Warehouse?.Name ?? string.Empty,
            ProductId = x.ProductId,
            ProductCode = x.ProductCode,
            ProductName = x.ProductName,
            UnitName = x.UnitName,
            Quantity = x.Quantity,
            DefectReason = x.DefectReason,
            DateIdentified = x.DateIdentified,
            SourceType = x.SourceType,
            SourceReference = x.SourceReference,
            ReportedByName = x.ReportedByName,
            Status = x.Status,
            ResolutionNotes = x.ResolutionNotes,
            ResolvedAt = x.ResolvedAt,
            ResolvedByName = x.ResolvedByName,
            ResultingVoucherId = x.ResultingVoucherId,
            ResultingVoucherNumber = x.ResultingVoucher?.VoucherNumber
        }).ToList();
    }

    public async Task<QuarantinedItemDto?> GetQuarantinedItemByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _context.QuarantinedInventoryItems
            .Include(x => x.Warehouse)
            .Include(x => x.ResultingVoucher)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null) return null;

        return new QuarantinedItemDto
        {
            Id = item.Id,
            Code = item.Code,
            WarehouseId = item.WarehouseId,
            WarehouseName = item.Warehouse?.Name ?? string.Empty,
            ProductId = item.ProductId,
            ProductCode = item.ProductCode,
            ProductName = item.ProductName,
            UnitName = item.UnitName,
            Quantity = item.Quantity,
            DefectReason = item.DefectReason,
            DateIdentified = item.DateIdentified,
            SourceType = item.SourceType,
            SourceReference = item.SourceReference,
            ReportedByName = item.ReportedByName,
            Status = item.Status,
            ResolutionNotes = item.ResolutionNotes,
            ResolvedAt = item.ResolvedAt,
            ResolvedByName = item.ResolvedByName,
            ResultingVoucherId = item.ResultingVoucherId,
            ResultingVoucherNumber = item.ResultingVoucher?.VoucherNumber
        };
    }

    public async Task<bool> QuarantineDirectStockAsync(int warehouseId, int productId, decimal quantity, string defectReason, string sourceRef, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var product = await _context.Products.Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
            if (product == null) return false;

            var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            IDbContextTransaction? transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
            // Trừ số lượng tồn khả dụng (Saleable)
            var saleableStock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == warehouseId && s.ProductId == productId && s.Condition == StockCondition.Saleable, cancellationToken);
            if (saleableStock != null)
            {
                decimal prev = saleableStock.Quantity;
                decimal next = Math.Max(0, prev - quantity);
                saleableStock.Quantity = next;
                saleableStock.LastMovementAt = DateTime.UtcNow;

                _context.InventoryMovements.Add(new InventoryMovement
                {
                    MovementDate = DateTime.UtcNow,
                    MovementType = InventoryMovementType.QuarantineTransferIn,
                    WarehouseId = warehouseId,
                    ProductId = productId,
                    Condition = StockCondition.Saleable,
                    Quantity = quantity,
                    Direction = "OUT",
                    PreviousBalance = prev,
                    NewBalance = next,
                    SourceReference = sourceRef,
                    Notes = $"Chuyển vào khu cách ly hàng hỏng: {defectReason}",
                    CreatedByUserId = userId,
                    CreatedByName = userName
                });
            }

            // Tăng số lượng tồn cách ly (Defective)
            var defStock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == warehouseId && s.ProductId == productId && s.Condition == StockCondition.Defective, cancellationToken);
            if (defStock == null)
            {
                defStock = new InventoryStock
                {
                    WarehouseId = warehouseId,
                    ProductId = productId,
                    Condition = StockCondition.Defective,
                    Quantity = 0,
                    LastMovementAt = DateTime.UtcNow
                };
                _context.InventoryStocks.Add(defStock);
            }

            decimal prevDef = defStock.Quantity;
            decimal nextDef = prevDef + quantity;
            defStock.Quantity = nextDef;
            defStock.LastMovementAt = DateTime.UtcNow;

            _context.InventoryMovements.Add(new InventoryMovement
            {
                MovementDate = DateTime.UtcNow,
                MovementType = InventoryMovementType.QuarantineTransferIn,
                WarehouseId = warehouseId,
                ProductId = productId,
                Condition = StockCondition.Defective,
                Quantity = quantity,
                Direction = "IN",
                PreviousBalance = prevDef,
                NewBalance = nextDef,
                SourceReference = sourceRef,
                Notes = $"Nhận hàng cách ly ({defectReason})",
                CreatedByUserId = userId,
                CreatedByName = userName
            });

            // Tạo bản ghi theo dõi cách ly
            string clCode = await GenerateCodeAsync("QuarantineItem", "CL-", cancellationToken);
            _context.QuarantinedInventoryItems.Add(new QuarantinedInventoryItem
            {
                Code = clCode,
                WarehouseId = warehouseId,
                ProductId = productId,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitName = product.Unit?.Name ?? "Cái",
                Quantity = quantity,
                DefectReason = defectReason,
                DateIdentified = DateTime.UtcNow,
                SourceType = "InternalDamage",
                SourceReference = sourceRef,
                ReportedByName = userName,
                Status = QuarantineStatus.InQuarantine
            });

            await _context.SaveChangesAsync(cancellationToken);
            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            await _auditService.LogAsync(
                AuditEventType.DefectiveStockQuarantined,
                performedBy: userName,
                targetEntity: "QuarantinedInventoryItem",
                targetId: clCode,
                details: $"Cách ly {quantity:N0} {product.Name}: {defectReason}",
                cancellationToken: cancellationToken);

            return true;
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
        });
    }

    public async Task<bool> ResolveQuarantineAsync(int quarantineId, QuarantineStatus targetStatus, string resolutionNotes, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            var item = await _context.QuarantinedInventoryItems.FirstOrDefaultAsync(q => q.Id == quarantineId, cancellationToken);
            if (item == null || item.Status != QuarantineStatus.InQuarantine)
                return false;

            var isInMemory = _context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory";
            IDbContextTransaction? transaction = isInMemory ? null : await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                item.Status = targetStatus;
                item.ResolutionNotes = resolutionNotes;
                item.ResolvedAt = DateTime.UtcNow;
                item.ResolvedByName = userName;

                // Giảm tồn kho Defective
                var defStock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == item.WarehouseId && s.ProductId == item.ProductId && s.Condition == StockCondition.Defective, cancellationToken);
                if (defStock != null)
                {
                    decimal prevDef = defStock.Quantity;
                    decimal nextDef = Math.Max(0, prevDef - item.Quantity);
                    defStock.Quantity = nextDef;
                    defStock.LastMovementAt = DateTime.UtcNow;

                    _context.InventoryMovements.Add(new InventoryMovement
                    {
                        MovementDate = DateTime.UtcNow,
                        MovementType = InventoryMovementType.QuarantineDisposition,
                        WarehouseId = item.WarehouseId,
                        ProductId = item.ProductId,
                        Condition = StockCondition.Defective,
                        Quantity = item.Quantity,
                        Direction = "OUT",
                        PreviousBalance = prevDef,
                        NewBalance = nextDef,
                        SourceReference = item.Code,
                        Notes = $"Giải tỏa cách ly: {targetStatus} ({resolutionNotes})",
                        CreatedByUserId = userId,
                        CreatedByName = userName
                    });
                }

                // Nếu phục hồi -> Chuyển về kho Saleable
                if (targetStatus == QuarantineStatus.Restored)
                {
                    var saleableStock = await _context.InventoryStocks.FirstOrDefaultAsync(s => s.WarehouseId == item.WarehouseId && s.ProductId == item.ProductId && s.Condition == StockCondition.Saleable, cancellationToken);
                    if (saleableStock == null)
                    {
                        saleableStock = new InventoryStock
                        {
                            WarehouseId = item.WarehouseId,
                            ProductId = item.ProductId,
                            Condition = StockCondition.Saleable,
                            Quantity = 0,
                            LastMovementAt = DateTime.UtcNow
                        };
                        _context.InventoryStocks.Add(saleableStock);
                    }

                    decimal prev = saleableStock.Quantity;
                    decimal next = prev + item.Quantity;
                    saleableStock.Quantity = next;
                    saleableStock.LastMovementAt = DateTime.UtcNow;

                    _context.InventoryMovements.Add(new InventoryMovement
                    {
                        MovementDate = DateTime.UtcNow,
                        MovementType = InventoryMovementType.QuarantineDisposition,
                        WarehouseId = item.WarehouseId,
                        ProductId = item.ProductId,
                        Condition = StockCondition.Saleable,
                        Quantity = item.Quantity,
                        Direction = "IN",
                        PreviousBalance = prev,
                        NewBalance = next,
                        SourceReference = item.Code,
                        Notes = $"Phục hồi hàng cách ly về hàng tốt: {resolutionNotes}",
                        CreatedByUserId = userId,
                        CreatedByName = userName
                    });
                }
                // Nếu xuất trả Nhà cung cấp / Hãng
                else if (targetStatus == QuarantineStatus.ReturnedToSupplier)
                {
                    string vNum = await GenerateCodeAsync("IssueVoucher", "PXK-NCC-", cancellationToken);
                    var voucher = new WarehouseVoucher
                    {
                        VoucherType = WarehouseVoucherType.OutboundIssue,
                        VoucherNumber = vNum,
                        VoucherDate = DateTime.UtcNow,
                        WarehouseId = item.WarehouseId,
                        Reason = $"Xuất trả nhà cung cấp hàng cách ly {item.Code} ({item.DefectReason})",
                        PartnerName = "Nhà cung cấp / Hãng bảo hành",
                        SourceModule = "Quarantine",
                        SourceRecordId = item.Id,
                        SourceReference = item.Code,
                        Status = WarehouseVoucherStatus.Approved,
                        TotalQuantity = item.Quantity,
                        TotalAmount = 0,
                        CreatedByUserId = userId,
                        CreatedByName = userName,
                        ApprovedByUserId = userId,
                        ApprovedByName = userName,
                        ApprovedAt = DateTime.UtcNow,
                        Notes = resolutionNotes
                    };

                    voucher.Items.Add(new WarehouseVoucherItem
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        UnitName = item.UnitName,
                        Condition = StockCondition.Defective,
                        DocumentQuantity = item.Quantity,
                        ActualQuantity = item.Quantity,
                        UnitPrice = 0,
                        LineTotal = 0,
                        Notes = resolutionNotes
                    });

                    _context.WarehouseVouchers.Add(voucher);
                    await _context.SaveChangesAsync(cancellationToken);
                    item.ResultingVoucherId = voucher.Id;

                    await RegisterToCentralDocumentsAsync(voucher, cancellationToken);
                }
                // Nếu tiêu hủy -> Lập Phiếu xuất kho tiêu hủy (Mẫu 02-VT)
                else if (targetStatus == QuarantineStatus.Scrapped)
                {
                    string vNum = await GenerateCodeAsync("IssueVoucher", "PXK-", cancellationToken);
                    var voucher = new WarehouseVoucher
                    {
                        VoucherType = WarehouseVoucherType.OutboundIssue,
                        VoucherNumber = vNum,
                        VoucherDate = DateTime.UtcNow,
                        WarehouseId = item.WarehouseId,
                        Reason = $"Xuất tiêu hủy / thanh lý hàng cách ly {item.Code} ({item.DefectReason})",
                        PartnerName = "Hội đồng thanh lý",
                        SourceModule = "Quarantine",
                        SourceRecordId = item.Id,
                        SourceReference = item.Code,
                        Status = WarehouseVoucherStatus.Approved,
                        TotalQuantity = item.Quantity,
                        TotalAmount = 0,
                        CreatedByUserId = userId,
                        CreatedByName = userName,
                        ApprovedByUserId = userId,
                        ApprovedByName = userName,
                        ApprovedAt = DateTime.UtcNow,
                        Notes = resolutionNotes
                    };

                    voucher.Items.Add(new WarehouseVoucherItem
                    {
                        ProductId = item.ProductId,
                        ProductCode = item.ProductCode,
                        ProductName = item.ProductName,
                        UnitName = item.UnitName,
                        Condition = StockCondition.Defective,
                        DocumentQuantity = item.Quantity,
                        ActualQuantity = item.Quantity,
                        UnitPrice = 0,
                        LineTotal = 0,
                        Notes = resolutionNotes
                    });

                    _context.WarehouseVouchers.Add(voucher);
                    await _context.SaveChangesAsync(cancellationToken);
                    item.ResultingVoucherId = voucher.Id;

                    await RegisterToCentralDocumentsAsync(voucher, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
                if (transaction != null)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                await _auditService.LogAsync(
                    AuditEventType.DefectiveStockDisposed,
                    performedBy: userName,
                    targetEntity: "QuarantinedInventoryItem",
                    targetId: item.Code,
                    details: $"Xử lý giải tỏa hàng cách ly {item.Code} -> {targetStatus}: {resolutionNotes}",
                    cancellationToken: cancellationToken);

                return true;
            }
            catch
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw;
            }
            finally
            {
                if (transaction != null)
                {
                    await transaction.DisposeAsync();
                }
            }
        });
    }

    #endregion

    #region Helper Mappers

    private static WarehouseVoucherDto MapVoucherToDto(WarehouseVoucher v) => new()
    {
        Id = v.Id,
        VoucherType = v.VoucherType,
        VoucherNumber = v.VoucherNumber,
        VoucherDate = v.VoucherDate,
        WarehouseId = v.WarehouseId,
        WarehouseName = v.Warehouse?.Name ?? string.Empty,
        WarehouseAddress = v.Warehouse?.Address,
        Reason = v.Reason,
        PartnerName = v.PartnerName,
        PartnerAddress = v.PartnerAddress,
        SourceModule = v.SourceModule,
        SourceRecordId = v.SourceRecordId,
        SourceReference = v.SourceReference,
        Status = v.Status,
        TotalQuantity = v.TotalQuantity,
        TotalAmount = v.TotalAmount,
        Notes = v.Notes,
        CreatedByName = v.CreatedByName,
        ApprovedByName = v.ApprovedByName,
        ApprovedAt = v.ApprovedAt,
        WarehouseKeeperName = v.WarehouseKeeperName,
        DelivererOrReceiverName = v.DelivererOrReceiverName,
        Items = v.Items.Select(i => new WarehouseVoucherItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            UnitName = i.UnitName,
            Condition = i.Condition,
            DocumentQuantity = i.DocumentQuantity,
            ActualQuantity = i.ActualQuantity,
            UnitPrice = i.UnitPrice,
            LineTotal = i.LineTotal,
            Notes = i.Notes
        }).ToList()
    };

    private static StocktakeSessionDto MapStocktakeToDto(StocktakeSession s) => new()
    {
        Id = s.Id,
        Code = s.Code,
        WarehouseId = s.WarehouseId,
        WarehouseName = s.Warehouse?.Name ?? string.Empty,
        StocktakeDate = s.StocktakeDate,
        Scope = s.Scope,
        Reason = s.Reason,
        Notes = s.Notes,
        Status = s.Status,
        SnapshotTakenAt = s.SnapshotTakenAt,
        CountedByUserId = s.CountedByUserId,
        CountedByName = s.CountedByName,
        SubmittedAt = s.SubmittedAt,
        ReviewedByUserId = s.ReviewedByUserId,
        ReviewedByName = s.ReviewedByName,
        ReviewedAt = s.ReviewedAt,
        RejectionReason = s.RejectionReason,
        ApprovedAt = s.ApprovedAt,
        CompletedAt = s.CompletedAt,
        AdjustmentInboundVoucherId = s.AdjustmentInboundVoucherId,
        AdjustmentInboundVoucherNumber = s.AdjustmentInboundVoucher?.VoucherNumber,
        AdjustmentOutboundVoucherId = s.AdjustmentOutboundVoucherId,
        AdjustmentOutboundVoucherNumber = s.AdjustmentOutboundVoucher?.VoucherNumber,
        Items = s.Items.Select(i => new StocktakeItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            UnitName = i.UnitName,
            BookQuantity = i.BookQuantity,
            CountedQuantity = i.CountedQuantity,
            VarianceQuantity = i.VarianceQuantity,
            Condition = i.Condition,
            UnitCost = i.UnitCost,
            VarianceValue = i.VarianceValue,
            DiscrepancyReason = i.DiscrepancyReason,
            Notes = i.Notes
        }).ToList()
    };

    private static WarehouseReturnOrderDto MapReturnOrderToDto(WarehouseReturnOrder r) => new()
    {
        Id = r.Id,
        Code = r.Code,
        ReturnType = r.ReturnType,
        WarehouseId = r.WarehouseId,
        WarehouseName = r.Warehouse?.Name ?? string.Empty,
        ReturnDate = r.ReturnDate,
        CustomerId = r.CustomerId,
        CustomerName = r.CustomerName,
        SupplierId = r.SupplierId,
        SupplierName = r.SupplierName,
        OriginalInvoiceId = r.OriginalInvoiceId,
        OriginalInvoiceNumber = r.OriginalInvoiceNumber,
        OriginalSalesOrderId = r.OriginalSalesOrderId,
        OriginalSalesOrderCode = r.OriginalSalesOrderCode,
        Reason = r.Reason,
        Notes = r.Notes,
        Status = r.Status,
        ReceivedByName = r.ReceivedByName,
        ReceivedAt = r.ReceivedAt,
        InspectedByName = r.InspectedByName,
        InspectedAt = r.InspectedAt,
        InspectionNotes = r.InspectionNotes,
        ApprovedByName = r.ApprovedByName,
        ApprovedAt = r.ApprovedAt,
        ResultingVoucherId = r.ResultingVoucherId,
        ResultingVoucherNumber = r.ResultingVoucher?.VoucherNumber,
        Items = r.Items.Select(i => new WarehouseReturnOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductCode = i.ProductCode,
            ProductName = i.ProductName,
            UnitName = i.UnitName,
            RequestedQuantity = i.RequestedQuantity,
            ReceivedQuantity = i.ReceivedQuantity,
            Condition = i.Condition,
            Disposition = i.Disposition,
            DefectDetails = i.DefectDetails,
            InspectionNotes = i.InspectionNotes,
            UnitPrice = i.UnitPrice
        }).ToList()
    };

    public Task<List<InventoryStockDto>> GetStockBalancesAsync(int? warehouseId = null, StockCondition? condition = null, string? searchTerm = null, CancellationToken cancellationToken = default)
        => GetStocksAsync(warehouseId, condition, searchTerm, cancellationToken);

    public async Task<StocktakeSessionDto> CreateStocktakeSessionAsync(int warehouseId, DateTime cutoffDate, string reason, string userId, string userName, CancellationToken cancellationToken = default)
    {
        var dto = new StocktakeSessionDto
        {
            WarehouseId = warehouseId,
            StocktakeDate = cutoffDate,
            Reason = reason,
            Scope = "All"
        };
        return await CreateStocktakeSessionAsync(dto, userId, userName, cancellationToken);
    }

    public Task<bool> SubmitStocktakeCountsAsync(int sessionId, List<StocktakeItemDto> counts, string userId, string userName, CancellationToken cancellationToken = default)
        => SaveCountsAsync(sessionId, counts, userId, userName, cancellationToken);

    public Task<bool> RejectStocktakeSessionAsync(int sessionId, string reason, string userId, string userName, CancellationToken cancellationToken = default)
        => RejectStocktakeAsync(sessionId, reason, userId, userName, cancellationToken);

    public Task<bool> InspectReturnItemsAsync(int returnOrderId, List<WarehouseReturnOrderItemDto> items, string userId, string userName, CancellationToken cancellationToken = default)
        => InspectReturnOrderAsync(returnOrderId, items, "Kiểm định tại cửa kho", userId, userName, cancellationToken);

    public Task<bool> ResolveQuarantineItemAsync(int quarantineId, QuarantineStatus targetStatus, string resolutionNotes, string userId, string userName, CancellationToken cancellationToken = default)
        => ResolveQuarantineAsync(quarantineId, targetStatus, resolutionNotes, userId, userName, cancellationToken);

    public async Task<List<WarehouseOverviewDto>> GetWarehouseOverviewAsync(CancellationToken cancellationToken = default)
    {
        var warehouses = await _context.Warehouses
            .AsNoTracking()
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        var stocks = await _context.InventoryStocks
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var movements = await _context.InventoryMovements
            .AsNoTracking()
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, AvgCost = g.Average(m => m.UnitCost) })
            .ToListAsync(cancellationToken);

        var costDict = movements.ToDictionary(m => m.ProductId, m => m.AvgCost > 0 ? m.AvgCost : 1000000m);

        var latestMovements = await _context.InventoryMovements
            .AsNoTracking()
            .GroupBy(m => m.WarehouseId)
            .Select(g => new { WarehouseId = g.Key, Latest = g.Max(m => m.MovementDate) })
            .ToListAsync(cancellationToken);
        var latestDict = latestMovements.ToDictionary(m => m.WarehouseId, m => (DateTime?)m.Latest);

        var result = new List<WarehouseOverviewDto>();
        foreach (var wh in warehouses)
        {
            var whStocks = stocks.Where(s => s.WarehouseId == wh.Id).ToList();
            var distinctSkus = whStocks.Where(s => s.Quantity > 0).Select(s => s.ProductId).Distinct().Count();
            var lowStockSkus = whStocks.Where(s => s.Condition == StockCondition.Saleable && s.Quantity > 0 && s.Quantity < 15).Select(s => s.ProductId).Distinct().Count();
            var defectiveCount = whStocks.Where(s => s.Condition == StockCondition.Defective && s.Quantity > 0).Select(s => s.ProductId).Distinct().Count();
            var totalValuation = whStocks.Sum(s => s.Quantity * (costDict.TryGetValue(s.ProductId, out var c) ? c : 1000000m));
            latestDict.TryGetValue(wh.Id, out var latest);

            result.Add(new WarehouseOverviewDto
            {
                WarehouseId = wh.Id,
                WarehouseCode = wh.Code,
                WarehouseName = wh.Name,
                Address = wh.Address,
                ManagerName = wh.ManagerName,
                Phone = wh.Phone,
                IsActive = wh.IsActive,
                DistinctSkuCount = distinctSkus,
                LowStockSkuCount = lowStockSkus,
                DefectiveItemCount = defectiveCount,
                TotalValuation = totalValuation,
                LatestMovementAt = latest
            });
        }

        return result;
    }

    public async Task<List<CrossWarehouseMatrixItemDto>> GetCrossWarehouseMatrixAsync(
        string? searchTerm = null,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        var prodQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            prodQuery = prodQuery.Where(p => p.Code.ToLower().Contains(term) || p.Name.ToLower().Contains(term));
        }

        if (categoryId.HasValue && categoryId.Value > 0)
        {
            prodQuery = prodQuery.Where(p => p.CategoryId == categoryId.Value);
        }

        var products = await prodQuery.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        var productIds = products.Select(p => p.Id).ToList();

        var stocks = await _context.InventoryStocks
            .AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId) && s.Condition == StockCondition.Saleable)
            .ToListAsync(cancellationToken);

        var movements = await _context.InventoryMovements
            .AsNoTracking()
            .Where(m => productIds.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, AvgCost = g.Average(m => m.UnitCost) })
            .ToListAsync(cancellationToken);
        var costDict = movements.ToDictionary(m => m.ProductId, m => m.AvgCost > 0 ? m.AvgCost : 1000000m);

        var result = new List<CrossWarehouseMatrixItemDto>();
        foreach (var p in products)
        {
            var pStocks = stocks.Where(s => s.ProductId == p.Id).ToList();
            var whDict = pStocks.GroupBy(s => s.WarehouseId).ToDictionary(g => g.Key, g => g.Sum(s => s.Quantity));
            var totalQty = whDict.Values.Sum();
            var cost = costDict.TryGetValue(p.Id, out var c) ? c : 1000000m;

            result.Add(new CrossWarehouseMatrixItemDto
            {
                ProductId = p.Id,
                ProductCode = p.Code,
                ProductName = p.Name,
                UnitName = p.Unit?.Name ?? "Cái",
                CategoryName = p.Category?.Name,
                WarehouseQuantities = whDict,
                TotalQuantity = totalQty,
                EstimatedCost = cost
            });
        }

        return result;
    }

    public async Task<byte[]> ExportStockExcelAsync(
        int? warehouseId = null,
        StockCondition? condition = null,
        CancellationToken cancellationToken = default)
    {
        var items = await GetStocksAsync(warehouseId, condition, null, cancellationToken);
        
        var movements = await _context.InventoryMovements
            .AsNoTracking()
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, AvgCost = g.Average(m => m.UnitCost) })
            .ToListAsync(cancellationToken);
        var costDict = movements.ToDictionary(m => m.ProductId, m => m.AvgCost > 0 ? m.AvgCost : 1000000m);

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("BaoCaoTonKho");

        // Title
        ws.Cell(1, 1).Value = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;

        ws.Cell(2, 1).Value = "BÁO CÁO TỒN KHO THỰC TẾ THEO KHO BÃI";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 15;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromArgb(15, 23, 42);

        ws.Cell(3, 1).Value = $"Thời điểm xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm}";
        ws.Cell(3, 1).Style.Font.Italic = true;

        // Table Header
        string[] headers = { "STT", "Mã SP", "Tên sản phẩm", "ĐVT", "Nhóm hàng", "Kho hàng", "Phẩm chất", "Tồn thực tế", "Giá vốn (VND)", "Tổng giá trị (VND)" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(5, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromArgb(241, 245, 249);
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        int row = 6;
        int stt = 1;
        foreach (var item in items)
        {
            var unitCost = costDict.TryGetValue(item.ProductId, out var c) ? c : 1000000m;
            var totalVal = item.Quantity * unitCost;

            ws.Cell(row, 1).Value = stt++;
            ws.Cell(row, 2).Value = item.ProductCode;
            ws.Cell(row, 3).Value = item.ProductName;
            ws.Cell(row, 4).Value = item.UnitName;
            ws.Cell(row, 5).Value = item.CategoryName ?? "";
            ws.Cell(row, 6).Value = item.WarehouseName;
            ws.Cell(row, 7).Value = item.ConditionName;
            ws.Cell(row, 8).Value = item.Quantity;
            ws.Cell(row, 9).Value = unitCost;
            ws.Cell(row, 10).Value = totalVal;

            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 9).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 10).Style.NumberFormat.Format = "#,##0";

            for (int col = 1; col <= 10; col++)
            {
                ws.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                ws.Cell(row, col).Style.Border.OutsideBorderColor = XLColor.FromArgb(226, 232, 240);
            }

            row++;
        }

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    #endregion
}
