using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class SalesService : ISalesService
{
    private readonly TradeFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public SalesService(TradeFlowDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

        private DateTime NormalizeToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified || date.Kind == DateTimeKind.Local)
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return date;
    }

    public async Task<List<SalesOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _context.SalesOrders
            .AsNoTracking()
            .OrderByDescending(x => x.OrderDate)
            .ToListAsync(cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    public async Task<SalesOrderDto?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(x => x.Items)
            .Include(x => x.Quotation)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (order == null) return null;
        return MapToDto(order);
    }

    public async Task<SalesOrderDto> CreateOrderAsync(SalesOrderDto dto, CancellationToken cancellationToken = default)
    {
        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, cancellationToken);
        if (customer == null) throw new Exception("Customer not found");

        var sequence = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "SalesOrder", cancellationToken);
        if (sequence == null)
        {
            sequence = new Domain.Entities.Settings.SystemSequence { SequenceKey = "SalesOrder", Prefix = "SO-", CurrentNumber = 1 };
            _context.SystemSequences.Add(sequence);
        }
        string code = $"{sequence.Prefix}{DateTime.Now.Year}-{sequence.CurrentNumber:D4}";
        sequence.CurrentNumber++;

        var order = new SalesOrder
        {
            Code = code,
            OrderDate = NormalizeToUtc(dto.OrderDate),
            DeliveryDate = dto.DeliveryDate.HasValue ? NormalizeToUtc(dto.DeliveryDate.Value) : null,
            CustomerId = dto.CustomerId,
            CustomerName = customer.Name,
            CustomerTaxCode = customer.TaxCode,
            CustomerAddress = customer.Address,
            CustomerPhone = customer.Phone,
            Notes = dto.Notes,
            Status = SalesOrderStatus.Draft
        };

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null) continue;

            decimal unitPrice = itemDto.UnitPrice ?? 0;
            decimal lineTotal = itemDto.Quantity * unitPrice;

            var item = new SalesOrderItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitName = product.Unit?.Name ?? string.Empty,
                Quantity = itemDto.Quantity,
                UnitPrice = unitPrice,
                PriceSource = string.IsNullOrEmpty(itemDto.PriceSource) ? "Bảng giá" : itemDto.PriceSource,
                DiscountAmount = 0m,
                TaxRate = 0m,
                TaxAmount = 0m,
                LineTotal = lineTotal,
                Notes = itemDto.Notes
            };
            order.Items.Add(item);
        }

        order.SubTotal = order.Items.Sum(x => x.Quantity * x.UnitPrice);
        order.TotalDiscount = 0m;
        order.TotalTax = 0m;
        order.GrandTotal = order.SubTotal;

        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(AuditEventType.SalesOrderCreated, "SalesOrder", order.Id.ToString(), "Draft order created", _currentUserService.UserId);

        return MapToDto(order);
    }

    public async Task<bool> UpdateOrderAsync(SalesOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (order == null || order.Status != SalesOrderStatus.Draft) return false;

        order.OrderDate = NormalizeToUtc(dto.OrderDate);
        order.DeliveryDate = dto.DeliveryDate.HasValue ? NormalizeToUtc(dto.DeliveryDate.Value) : null;
        order.Notes = dto.Notes;

        // Clear existing items and rebuild
        _context.SalesOrderItems.RemoveRange(order.Items);
        order.Items.Clear();

        foreach (var itemDto in dto.Items)
        {
            var product = await _context.Products.Include(p => p.Unit).FirstOrDefaultAsync(p => p.Id == itemDto.ProductId, cancellationToken);
            if (product == null) continue;

            decimal unitPrice = itemDto.UnitPrice ?? 0;
            decimal lineTotal = itemDto.Quantity * unitPrice;

            var item = new SalesOrderItem
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitName = product.Unit?.Name ?? string.Empty,
                Quantity = itemDto.Quantity,
                UnitPrice = unitPrice,
                PriceSource = string.IsNullOrEmpty(itemDto.PriceSource) ? "Bảng giá" : itemDto.PriceSource,
                DiscountAmount = 0m,
                TaxRate = 0m,
                TaxAmount = 0m,
                LineTotal = lineTotal,
                Notes = itemDto.Notes
            };
            order.Items.Add(item);
        }

        order.SubTotal = order.Items.Sum(x => x.Quantity * x.UnitPrice);
        order.TotalDiscount = 0m;
        order.TotalTax = 0m;
        order.GrandTotal = order.SubTotal;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.SalesOrderUpdated, "SalesOrder", order.Id.ToString(), "Draft order updated", _currentUserService.UserId);

        return true;
    }

    public async Task<bool> ConfirmOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders.FindAsync(new object[] { id }, cancellationToken);
        if (order == null || order.Status != SalesOrderStatus.Draft) return false;

        order.Status = SalesOrderStatus.Confirmed;
        await _context.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(AuditEventType.SalesOrderConfirmed, "SalesOrder", order.Id.ToString(), "Order confirmed", _currentUserService.UserId);
        return true;
    }

    public async Task<bool> CancelOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders.FindAsync(new object[] { id }, cancellationToken);
        if (order == null || order.Status == SalesOrderStatus.Invoiced) return false;

        order.Status = SalesOrderStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(AuditEventType.SalesOrderCancelled, "SalesOrder", order.Id.ToString(), "Order cancelled", _currentUserService.UserId);
        return true;
    }

    public async Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (order == null) return false;

        if (order.Status != SalesOrderStatus.Draft)
        {
            // Do not allow physical deletion, just soft-delete/cancel if not invoiced
            if (order.Status == SalesOrderStatus.Confirmed)
            {
                order.Status = SalesOrderStatus.Cancelled;
                await _context.SaveChangesAsync(cancellationToken);
                await _auditService.LogAsync(AuditEventType.SalesOrderCancelled, _currentUserService.UserName ?? "System", "SalesOrder", id.ToString(), "Hủy đơn hàng vì đã chốt (không thể xóa vật lý)", cancellationToken: cancellationToken);
            }
            return true;
        }

        // It is draft, allow physical delete
        _context.SalesOrderItems.RemoveRange(order.Items);
        _context.SalesOrders.Remove(order);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(AuditEventType.SalesOrderDeleted, _currentUserService.UserName ?? "System", "SalesOrder", id.ToString(), "Xóa vật lý đơn bán hàng nháp", cancellationToken: cancellationToken);
        return true;
    }

        public async Task<List<TradeFlow.Domain.Entities.MasterData.Customer>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower();
            var likeTerm = $"%{searchTerm}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, likeTerm) 
                                  || (x.TaxCode != null && EF.Functions.ILike(x.TaxCode, likeTerm))
                                  || (x.Phone != null && EF.Functions.ILike(x.Phone, likeTerm)));
        }
        return await query.OrderBy(x => x.Name).Take(20).ToListAsync(cancellationToken);
    }

    public async Task<List<TradeFlow.Domain.Entities.MasterData.Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.Include(p => p.Unit).AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var likeTerm = $"%{searchTerm}%";
            query = query.Where(x => EF.Functions.ILike(x.Name, likeTerm) 
                                  || EF.Functions.ILike(x.Code, likeTerm)
                                  || (x.NewCode != null && EF.Functions.ILike(x.NewCode, likeTerm))
                                  || (x.LegacyCode != null && EF.Functions.ILike(x.LegacyCode, likeTerm)));
        }
        return await query.OrderBy(x => x.Name).Take(20).ToListAsync(cancellationToken);
    }

    
    public async Task<List<TradeFlow.Domain.Entities.Pricing.PriceList>> GetApplicablePriceListsAsync(DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var utcTarget = NormalizeToUtc(targetDate);
        var inRange = await _context.PriceLists
            .AsNoTracking()
            .Where(x => x.Status == TradeFlow.Domain.Enums.PriceListStatus.Active 
                     && (x.EffectiveFrom == null || x.EffectiveFrom <= utcTarget)
                     && (x.EffectiveTo == null || x.EffectiveTo >= utcTarget))
            .OrderByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);

        if (inRange.Any())
        {
            return inRange;
        }

        // Fallback: If no price lists specifically cover targetDate, return all active price lists sorted by recency
        return await _context.PriceLists
            .AsNoTracking()
            .Where(x => x.Status == TradeFlow.Domain.Enums.PriceListStatus.Active)
            .OrderByDescending(x => x.Year)
            .ThenByDescending(x => x.Quarter)
            .ThenByDescending(x => x.EffectiveFrom)
            .ToListAsync(cancellationToken);
    }

    public async Task<(decimal? Price, string SourceName)> GetProductPriceAsync(int productId, int? priceListId, DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var utcTarget = NormalizeToUtc(targetDate);
        if (priceListId.HasValue)
        {
            var item = await _context.PriceListItems
                .Include(x => x.PriceList)
                .FirstOrDefaultAsync(x => x.ProductId == productId && x.PriceListId == priceListId.Value, cancellationToken);
            if (item != null)
            {
                return (item.UnitPrice, item.PriceList!.Name);
            }
        }
        
        // 1. Try active price list covering date
        var activeItem = await _context.PriceListItems
            .Include(x => x.PriceList)
            .Where(x => x.ProductId == productId 
                     && x.PriceList!.Status == TradeFlow.Domain.Enums.PriceListStatus.Active
                     && (x.PriceList.EffectiveFrom == null || x.PriceList.EffectiveFrom <= utcTarget)
                     && (x.PriceList.EffectiveTo == null || x.PriceList.EffectiveTo >= utcTarget))
            .OrderByDescending(x => x.PriceList!.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeItem != null)
        {
            return (activeItem.UnitPrice, activeItem.PriceList!.Name);
        }

        // 2. Fallback: latest Active PriceList item for this product
        var latestActiveItem = await _context.PriceListItems
            .Include(x => x.PriceList)
            .Where(x => x.ProductId == productId
                     && x.PriceList!.Status == TradeFlow.Domain.Enums.PriceListStatus.Active)
            .OrderByDescending(x => x.PriceList!.Year)
            .ThenByDescending(x => x.PriceList!.Quarter)
            .ThenByDescending(x => x.PriceList!.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestActiveItem != null)
        {
            return (latestActiveItem.UnitPrice, latestActiveItem.PriceList!.Name);
        }

        // 3. Fallback: match by Product code / NewCode / LegacyCode with healing
        var prod = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (prod != null)
        {
            var pNew = (prod.NewCode ?? "").Trim();
            var pLeg = (prod.LegacyCode ?? "").Trim();
            var pCode = prod.Code.Trim();

            var candidate = await _context.PriceListItems
                .Include(x => x.PriceList)
                .Where(x => x.PriceList!.Status == TradeFlow.Domain.Enums.PriceListStatus.Active
                         && ((pNew != "" && (x.NewCode == pNew || x.NewCode.Contains(pNew)))
                          || (pLeg != "" && (x.LegacyCode == pLeg || x.NewCode == pLeg))
                          || (pCode != "" && x.NewCode == pCode)))
                .OrderByDescending(x => x.PriceList!.Year)
                .ThenByDescending(x => x.PriceList!.Quarter)
                .FirstOrDefaultAsync(cancellationToken);

            if (candidate != null)
            {
                if (!candidate.ProductId.HasValue)
                {
                    candidate.ProductId = prod.Id;
                    try { await _context.SaveChangesAsync(cancellationToken); } catch { }
                }
                return (candidate.UnitPrice, candidate.PriceList!.Name);
            }
        }

        return (null, "Chưa thiết lập giá");
    }

    public async Task<decimal> GetActivePriceAsync(int productId, DateTime targetDate, CancellationToken cancellationToken = default)
    {
        var priceInfo = await GetProductPriceAsync(productId, null, targetDate, cancellationToken);
        return priceInfo.Price ?? 0m;
    }

    private static SalesOrderDto MapToDto(SalesOrder order)
    {
        return new SalesOrderDto
        {
            Id = order.Id,
            Code = order.Code,
            OrderDate = order.OrderDate,
            DeliveryDate = order.DeliveryDate,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            CustomerTaxCode = order.CustomerTaxCode,
            CustomerAddress = order.CustomerAddress,
            CustomerPhone = order.CustomerPhone,
            Notes = order.Notes,
            Status = order.Status,
            SubTotal = order.SubTotal,
            TotalDiscount = 0m,
            TotalTax = 0m,
            GrandTotal = order.SubTotal,
            QuotationId = order.QuotationId,
            QuotationCode = order.Quotation?.Code,
            Items = order.Items.Select(i => new SalesOrderItemDto
            {
                Id = i.Id,
                SalesOrderId = i.SalesOrderId,
                ProductId = i.ProductId,
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                UnitName = i.UnitName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                PriceSource = i.PriceSource,
                DiscountRate = 0m,
                DiscountAmount = 0m,
                TaxRate = 0m,
                TaxAmount = 0m,
                LineTotal = i.Quantity * i.UnitPrice,
                Notes = i.Notes
            }).ToList()
        };
    }
}
