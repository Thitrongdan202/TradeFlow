using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services.Pdf;

namespace TradeFlow.Infrastructure.Services;

public class QuotationService : IQuotationService
{
    private readonly TradeFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly ISalesService _salesService;
    private readonly IWebHostEnvironment _environment;

    public QuotationService(
        TradeFlowDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        ISalesService salesService,
        IWebHostEnvironment environment)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _salesService = salesService;
        _environment = environment;
    }

    private static DateTime NormalizeToUtc(DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified || date.Kind == DateTimeKind.Local)
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return date;
    }

    public async Task<List<QuotationDto>> GetQuotationsAsync(QuotationFilterDto? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Quotations
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .AsNoTracking();

        if (filter != null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim().ToLower();
                query = query.Where(x => x.Code.ToLower().Contains(term)
                    || x.CustomerName.ToLower().Contains(term)
                    || (x.CustomerPhone != null && x.CustomerPhone.Contains(term))
                    || (x.Notes != null && x.Notes.ToLower().Contains(term)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x => x.Status == filter.Status.Value);
            }

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(x => x.CustomerId == filter.CustomerId.Value);
            }

            if (filter.FromDate.HasValue)
            {
                var fromUtc = NormalizeToUtc(filter.FromDate.Value.Date);
                query = query.Where(x => x.QuotationDate >= fromUtc);
            }

            if (filter.ToDate.HasValue)
            {
                var toUtc = NormalizeToUtc(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
                query = query.Where(x => x.QuotationDate <= toUtc);
            }
        }

        query = query.OrderByDescending(x => x.QuotationDate);

        var list = await query.ToListAsync(cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<QuotationDto?> GetQuotationByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations
            .Include(x => x.Customer)
            .Include(x => x.SalesOrder)
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (quotation == null) return null;
        return MapToDto(quotation);
    }

    public async Task<QuotationDto> CreateQuotationAsync(QuotationDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.CustomerId <= 0)
            throw new ArgumentException("Khách hàng không được để trống.", nameof(dto.CustomerId));

        if (dto.Items == null || !dto.Items.Any())
            throw new ArgumentException("Báo giá phải có ít nhất một mặt hàng.", nameof(dto.Items));

        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, cancellationToken);
        if (customer == null)
            throw new InvalidOperationException("Không tìm thấy thông tin khách hàng trong hệ thống.");

        // Generate Code if not provided
        string code = dto.Code;
        if (string.IsNullOrWhiteSpace(code))
        {
            var seq = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "Quotation", cancellationToken);
            if (seq == null)
            {
                seq = new SystemSequence("Quotation", "BG-", "{Prefix}{Year}-{Number:D4}", "Mã báo giá hệ thống")
                {
                    CurrentNumber = 1,
                    Step = 1,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId ?? "System"
                };
                _context.SystemSequences.Add(seq);
            }
            code = $"{seq.Prefix}{DateTime.Now.Year}-{seq.CurrentNumber:D4}";
            seq.CurrentNumber++;
        }

        var quotation = new Quotation
        {
            Code = code,
            QuotationDate = NormalizeToUtc(dto.QuotationDate == default ? DateTime.UtcNow : dto.QuotationDate),
            ExpiryDate = dto.ExpiryDate.HasValue ? NormalizeToUtc(dto.ExpiryDate.Value) : null,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerTaxCode = customer.TaxCode,
            CustomerAddress = customer.Address,
            CustomerPhone = customer.Phone,
            CustomerContactPerson = dto.CustomerContactPerson ?? customer.ContactPerson,
            CustomerEmail = dto.CustomerEmail ?? customer.Email,
            SalespersonName = dto.SalespersonName ?? _currentUserService.UserName ?? "Kinh doanh",
            Notes = dto.Notes,
            Terms = dto.Terms ?? "1. Đơn giá trên là giá bán chưa bao gồm thuế GTGT (8%).\n2. Thanh toán: Tiền mặt hoặc chuyển khoản theo thỏa thuận.\n3. Thời gian giao hàng: Theo tiến độ hợp đồng/đơn hàng.\n4. Bảo hành: Theo tiêu chuẩn kỹ thuật của nhà sản xuất.",
            Status = dto.Status == QuotationStatus.Sent ? QuotationStatus.Sent : QuotationStatus.Draft
        };

        decimal subTotal = 0;
        decimal totalDiscount = 0;
        int sortOrder = 1;

        foreach (var itemDto in dto.Items)
        {
            if (itemDto.Quantity <= 0)
                throw new ArgumentException($"Số lượng của sản phẩm {itemDto.ProductName} phải lớn hơn 0.");
            if (itemDto.UnitPrice < 0)
                throw new ArgumentException($"Đơn giá của sản phẩm {itemDto.ProductName} không được âm.");

            decimal discountAmount = itemDto.DiscountAmount;
            if (discountAmount == 0 && itemDto.DiscountRate > 0)
            {
                discountAmount = Math.Round((itemDto.Quantity * itemDto.UnitPrice) * (itemDto.DiscountRate / 100m), 0);
            }

            decimal lineTotal = (itemDto.Quantity * itemDto.UnitPrice) - discountAmount;
            if (lineTotal < 0) lineTotal = 0;

            subTotal += (itemDto.Quantity * itemDto.UnitPrice);
            totalDiscount += discountAmount;

            quotation.Items.Add(new QuotationItem
            {
                ProductId = itemDto.ProductId,
                ProductCode = itemDto.ProductCode,
                ProductName = itemDto.ProductName,
                UnitName = itemDto.UnitName,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                PriceSource = string.IsNullOrWhiteSpace(itemDto.PriceSource) ? "Thủ công" : itemDto.PriceSource,
                DiscountRate = itemDto.DiscountRate,
                DiscountAmount = discountAmount,
                LineTotal = lineTotal,
                SortOrder = sortOrder++,
                Notes = itemDto.Notes
            });
        }

        quotation.SubTotal = subTotal;
        quotation.TotalDiscount = totalDiscount;
        quotation.GrandTotal = subTotal - totalDiscount;

        _context.Quotations.Add(quotation);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.QuotationCreated,
            _currentUserService.UserName ?? "System",
            "Quotation",
            quotation.Id.ToString(),
            $"Tạo báo giá mới: {quotation.Code} cho khách hàng {quotation.CustomerName}, tổng giá trị: {quotation.GrandTotal:N0} đ");

        return MapToDto(quotation);
    }

    public async Task<QuotationDto> UpdateQuotationAsync(QuotationDto dto, CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);

        if (quotation == null)
            throw new KeyNotFoundException($"Không tìm thấy báo giá ID {dto.Id}");

        if (quotation.Status == QuotationStatus.Converted)
            throw new InvalidOperationException("Không thể chỉnh sửa báo giá đã được chuyển thành đơn bán hàng.");

        if (quotation.Status == QuotationStatus.Cancelled)
            throw new InvalidOperationException("Không thể chỉnh sửa báo giá đã hủy.");

        var customer = await _context.Customers.FindAsync(new object[] { dto.CustomerId }, cancellationToken);
        if (customer != null)
        {
            quotation.CustomerId = customer.Id;
            quotation.CustomerName = customer.Name;
            quotation.CustomerTaxCode = customer.TaxCode;
            quotation.CustomerAddress = customer.Address;
            quotation.CustomerPhone = customer.Phone;
        }

        quotation.CustomerContactPerson = dto.CustomerContactPerson;
        quotation.CustomerEmail = dto.CustomerEmail;
        quotation.QuotationDate = NormalizeToUtc(dto.QuotationDate);
        quotation.ExpiryDate = dto.ExpiryDate.HasValue ? NormalizeToUtc(dto.ExpiryDate.Value) : null;
        quotation.SalespersonName = dto.SalespersonName ?? quotation.SalespersonName;
        quotation.Notes = dto.Notes;
        quotation.Terms = dto.Terms;

        // Clear existing items and re-add
        _context.QuotationItems.RemoveRange(quotation.Items);
        quotation.Items.Clear();

        decimal subTotal = 0;
        decimal totalDiscount = 0;
        int sortOrder = 1;

        foreach (var itemDto in dto.Items)
        {
            decimal discountAmount = itemDto.DiscountAmount;
            if (discountAmount == 0 && itemDto.DiscountRate > 0)
            {
                discountAmount = Math.Round((itemDto.Quantity * itemDto.UnitPrice) * (itemDto.DiscountRate / 100m), 0);
            }

            decimal lineTotal = (itemDto.Quantity * itemDto.UnitPrice) - discountAmount;
            if (lineTotal < 0) lineTotal = 0;

            subTotal += (itemDto.Quantity * itemDto.UnitPrice);
            totalDiscount += discountAmount;

            quotation.Items.Add(new QuotationItem
            {
                ProductId = itemDto.ProductId,
                ProductCode = itemDto.ProductCode,
                ProductName = itemDto.ProductName,
                UnitName = itemDto.UnitName,
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                PriceSource = string.IsNullOrWhiteSpace(itemDto.PriceSource) ? "Thủ công" : itemDto.PriceSource,
                DiscountRate = itemDto.DiscountRate,
                DiscountAmount = discountAmount,
                LineTotal = lineTotal,
                SortOrder = sortOrder++,
                Notes = itemDto.Notes
            });
        }

        quotation.SubTotal = subTotal;
        quotation.TotalDiscount = totalDiscount;
        quotation.GrandTotal = subTotal - totalDiscount;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.QuotationUpdated,
            _currentUserService.UserName ?? "System",
            "Quotation",
            quotation.Id.ToString(),
            $"Cập nhật báo giá: {quotation.Code}, tổng giá trị mới: {quotation.GrandTotal:N0} đ");

        return MapToDto(quotation);
    }

    public async Task<bool> UpdateStatusAsync(int id, QuotationStatus status, CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations.FindAsync(new object[] { id }, cancellationToken);
        if (quotation == null) return false;

        if (quotation.Status == QuotationStatus.Converted && status != QuotationStatus.Converted)
        {
            throw new InvalidOperationException("Báo giá đã chuyển đổi sang đơn hàng, không thể thay đổi trạng thái.");
        }

        var oldStatus = quotation.Status;
        quotation.Status = status;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.QuotationStatusChanged,
            _currentUserService.UserName ?? "System",
            "Quotation",
            quotation.Id.ToString(),
            $"Chuyển trạng thái báo giá {quotation.Code} từ '{oldStatus}' sang '{status}'");

        return true;
    }

    public async Task<bool> DeleteQuotationAsync(int id, CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (quotation == null) return false;

        if (quotation.Status == QuotationStatus.Converted)
        {
            throw new InvalidOperationException("Không thể xóa báo giá đã chuyển thành đơn hàng.");
        }

        if (quotation.Status != QuotationStatus.Draft && quotation.Status != QuotationStatus.Cancelled)
        {
            throw new InvalidOperationException("Chỉ có thể xóa báo giá ở trạng thái Bản nháp hoặc Đã hủy.");
        }

        _context.Quotations.Remove(quotation);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.QuotationDeleted,
            _currentUserService.UserName ?? "System",
            "Quotation",
            quotation.Id.ToString(),
            $"Đã xóa báo giá: {quotation.Code} ({quotation.CustomerName})");

        return true;
    }

    public async Task<SalesOrderDto> ConvertToSalesOrderAsync(int quotationId, CancellationToken cancellationToken = default)
    {
        var quotation = await _context.Quotations
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == quotationId, cancellationToken);

        if (quotation == null)
            throw new KeyNotFoundException($"Không tìm thấy báo giá ID {quotationId}");

        if (quotation.Status == QuotationStatus.Converted || quotation.SalesOrderId.HasValue)
        {
            throw new InvalidOperationException($"Báo giá {quotation.Code} đã được chuyển thành đơn bán hàng trước đó.");
        }

        if (quotation.Status == QuotationStatus.Cancelled)
        {
            throw new InvalidOperationException($"Không thể chuyển đổi báo giá đã bị hủy ({quotation.Code}).");
        }

        // Build SalesOrderDto
        var orderDto = new SalesOrderDto
        {
            CustomerId = quotation.CustomerId,
            CustomerName = quotation.CustomerName,
            CustomerTaxCode = quotation.CustomerTaxCode,
            CustomerAddress = quotation.CustomerAddress,
            CustomerPhone = quotation.CustomerPhone,
            OrderDate = DateTime.UtcNow,
            Notes = $"Chuyển đổi từ Báo giá {quotation.Code}. {(string.IsNullOrWhiteSpace(quotation.Notes) ? "" : quotation.Notes)}",
            Items = quotation.Items.OrderBy(x => x.SortOrder).Select(qi => new SalesOrderItemDto
            {
                ProductId = qi.ProductId,
                ProductCode = qi.ProductCode,
                ProductName = qi.ProductName,
                UnitName = qi.UnitName,
                Quantity = qi.Quantity,
                UnitPrice = qi.UnitPrice,
                PriceSource = qi.PriceSource,
                DiscountRate = qi.DiscountRate,
                DiscountAmount = qi.DiscountAmount,
                TaxRate = 0,
                TaxAmount = 0,
                LineTotal = (qi.Quantity * qi.UnitPrice) - qi.DiscountAmount
            }).ToList()
        };

        var createdOrder = await _salesService.CreateOrderAsync(orderDto, cancellationToken);

        // Update 2-way relationship in database
        var orderEntity = await _context.SalesOrders.FindAsync(new object[] { createdOrder.Id }, cancellationToken);
        if (orderEntity != null)
        {
            orderEntity.QuotationId = quotation.Id;
        }

        quotation.SalesOrderId = createdOrder.Id;
        quotation.Status = QuotationStatus.Converted;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.QuotationConverted,
            _currentUserService.UserName ?? "System",
            "Quotation",
            quotation.Id.ToString(),
            $"Chuyển đổi thành công Báo giá {quotation.Code} sang Đơn bán hàng {createdOrder.Code}");

        return createdOrder;
    }

    public async Task<byte[]> GeneratePdfAsync(int quotationId, CancellationToken cancellationToken = default)
    {
        var quotation = await GetQuotationByIdAsync(quotationId, cancellationToken);
        if (quotation == null)
            throw new KeyNotFoundException($"Không tìm thấy báo giá ID {quotationId}");

        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var doc = new QuotationDocumentPdf(quotation, company, _environment.WebRootPath);
        var bytes = doc.GeneratePdf();

        await _auditService.LogAsync(
            AuditEventType.QuotationExportedPdf,
            _currentUserService.UserName ?? "System",
            "Quotation",
            quotation.Id.ToString(),
            $"Xuất file PDF báo giá {quotation.Code}");

        return bytes;
    }

    private static QuotationDto MapToDto(Quotation entity)
    {
        return new QuotationDto
        {
            Id = entity.Id,
            Code = entity.Code,
            QuotationDate = entity.QuotationDate,
            ExpiryDate = entity.ExpiryDate,
            CustomerId = entity.CustomerId,
            CustomerName = entity.CustomerName,
            CustomerTaxCode = entity.CustomerTaxCode,
            CustomerAddress = entity.CustomerAddress,
            CustomerPhone = entity.CustomerPhone,
            CustomerContactPerson = entity.CustomerContactPerson,
            CustomerEmail = entity.CustomerEmail,
            SalespersonName = entity.SalespersonName,
            Notes = entity.Notes,
            Terms = entity.Terms,
            Status = entity.Status,
            SubTotal = entity.SubTotal,
            TotalDiscount = entity.TotalDiscount,
            GrandTotal = entity.GrandTotal,
            SalesOrderId = entity.SalesOrderId,
            SalesOrderCode = entity.SalesOrder?.Code,
            CreatedAt = entity.CreatedAt,
            CreatedBy = entity.CreatedBy,
            Items = entity.Items.OrderBy(x => x.SortOrder).Select(item => new QuotationItemDto
            {
                Id = item.Id,
                QuotationId = item.QuotationId,
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                PriceSource = item.PriceSource,
                DiscountRate = item.DiscountRate,
                DiscountAmount = item.DiscountAmount,
                LineTotal = item.LineTotal,
                SortOrder = item.SortOrder,
                Notes = item.Notes
            }).ToList()
        };
    }
}
