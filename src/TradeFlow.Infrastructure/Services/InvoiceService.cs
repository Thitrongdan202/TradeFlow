using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly TradeFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;

    public InvoiceService(TradeFlowDbContext context, ICurrentUserService currentUserService, IAuditService auditService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _context.Invoices
            .AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .ToListAsync(cancellationToken);

        return invoices.Select(MapToDto).ToList();
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (inv == null) return null;
        return MapToDto(inv);
    }

    public async Task<InvoiceDto> CreateInvoiceFromOrderAsync(int salesOrderId, InvoiceType type = InvoiceType.SalesInvoice, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);
            
        if (order == null || order.Status != SalesOrderStatus.Confirmed)
        {
            throw new Exception("Sales Order must be Confirmed to create an Invoice.");
        }

        var customer = await _context.Customers.FindAsync(new object[] { order.CustomerId }, cancellationToken);
        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        var sequence = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "Invoice", cancellationToken);
        if (sequence == null)
        {
            sequence = new Domain.Entities.Settings.SystemSequence { SequenceKey = "Invoice", Prefix = "INV-", CurrentNumber = 1 };
            _context.SystemSequences.Add(sequence);
        }
        string invNumber = $"{sequence.Prefix}{DateTime.Now.Year}-{sequence.CurrentNumber:D4}";
        sequence.CurrentNumber++;

        var invoice = new Invoice
        {
            InvoiceNumber = invNumber,
            InvoiceDate = DateTime.UtcNow,
            SalesOrderId = order.Id,
            CustomerId = order.CustomerId,
            
            CompanyName = company?.CompanyName ?? "TradeFlow Company",
            CompanyTaxCode = company?.TaxCode,
            CompanyAddress = company?.Address,
            CompanyPhone = company?.Phone,
            CompanyEmail = company?.Email,
            CompanyLogoUrl = company?.LogoPath,
            
            CustomerName = customer?.Name ?? order.CustomerName,
            CustomerCompanyName = customer?.CompanyName,
            CustomerTaxCode = customer?.TaxCode ?? order.CustomerTaxCode,
            CustomerAddress = customer?.Address ?? order.CustomerAddress,
            CustomerEmail = customer?.Email,
            
            Notes = order.Notes,
            Status = InvoiceStatus.Draft, Type = type,
            
            SubTotal = order.SubTotal,
            TotalDiscount = order.TotalDiscount,
            TotalTax = order.TotalTax,
            GrandTotal = order.GrandTotal
        };

        foreach (var oi in order.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                ProductId = oi.ProductId,
                ProductCode = oi.ProductCode,
                ProductName = oi.ProductName,
                UnitName = oi.UnitName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                DiscountAmount = oi.DiscountAmount,
                TaxRate = oi.TaxRate,
                TaxAmount = oi.TaxAmount,
                LineTotal = oi.LineTotal
            });
        }

        order.Status = SalesOrderStatus.Invoiced;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(AuditEventType.InvoiceCreated, "Invoice", invoice.Id.ToString(), $"Invoice created from SO {order.Code}", _currentUserService.UserId);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Direct invoice creation without Sales Order is not yet supported in Phase 5.");
    }

    public async Task<bool> UpdateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        inv.Notes = dto.Notes;
        // In this phase, we lock the quantities to the sales order, so we only update notes/dates.

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceUpdated, "Invoice", inv.Id.ToString(), "Draft invoice updated", _currentUserService.UserId);

        return true;
    }

    public async Task<bool> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.FindAsync(new object[] { id }, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        inv.Status = InvoiceStatus.Issued;
        await _context.SaveChangesAsync(cancellationToken);
        
        await _auditService.LogAsync(AuditEventType.InvoiceIssued, "Invoice", inv.Id.ToString(), "Invoice issued", _currentUserService.UserId);
        return true;
    }

    public async Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (inv == null) throw new Exception("Invoice not found");
        
        await _auditService.LogAsync(AuditEventType.InvoiceExportedPdf, "Invoice", inv.Id.ToString(), "PDF exported", _currentUserService.UserId);
        
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(compose => 
                {
                    compose.Row(row =>
                    {
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text(inv.CompanyName).FontSize(18).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                            if (!string.IsNullOrEmpty(inv.CompanyTaxCode))
                                column.Item().Text($"MST: {inv.CompanyTaxCode}");
                            if (!string.IsNullOrEmpty(inv.CompanyAddress))
                                column.Item().Text(inv.CompanyAddress);
                            if (!string.IsNullOrEmpty(inv.CompanyPhone))
                                column.Item().Text($"SĐT: {inv.CompanyPhone}");
                        });
                        row.ConstantItem(200).AlignRight().Column(column =>
                        {
                            var title = inv.Type == TradeFlow.Domain.Enums.InvoiceType.VatInvoice ? "HÓA ĐƠN GIÁ TRỊ GIA TĂNG" : "HÓA ĐƠN BÁN HÀNG";
                            column.Item().Text(title).FontSize(24).SemiBold().FontColor(QuestPDF.Helpers.Colors.Grey.Darken3);
                            column.Item().Text($"Số: {inv.InvoiceNumber}").SemiBold();
                            column.Item().Text($"Ngày: {inv.InvoiceDate:dd/MM/yyyy}");
                        });
                    });
                });

                page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                {
                    column.Item().PaddingBottom(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(c =>
                    {
                        c.Item().Text("THÔNG TIN KHÁCH HÀNG:").SemiBold().FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        c.Item().Text($"Đơn vị mua: {inv.CustomerName}");
                        if (!string.IsNullOrEmpty(inv.CustomerTaxCode))
                            c.Item().Text($"MST: {inv.CustomerTaxCode}");
                        if (!string.IsNullOrEmpty(inv.CustomerAddress))
                            c.Item().Text($"Địa chỉ: {inv.CustomerAddress}");
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn();
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(60);
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingBottom(5).Text("STT").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingBottom(5).Text("Tên hàng hóa, dịch vụ").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("ĐVT").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("Số lượng").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("Đơn giá").SemiBold();
                            header.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingBottom(5).AlignRight().Text("Thành tiền").SemiBold();
                        });

                        int stt = 1;
                        foreach (var item in inv.Items)
                        {
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).PaddingVertical(5).Text(stt++.ToString());
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).PaddingVertical(5).Text(item.ProductName);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).PaddingVertical(5).AlignRight().Text(item.UnitName);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).PaddingVertical(5).AlignRight().Text(item.Quantity.ToString("N0"));
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).PaddingVertical(5).AlignRight().Text(item.UnitPrice.ToString("N0"));
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten4).PaddingVertical(5).AlignRight().Text(item.LineTotal.ToString("N0")).SemiBold();
                        }
                    });

                    column.Item().PaddingTop(1, QuestPDF.Infrastructure.Unit.Centimetre).Row(row =>
                    {
                        row.RelativeItem();
                        row.ConstantItem(250).Column(c =>
                        {
                            c.Item().Row(r => { r.RelativeItem().Text("Tổng tiền hàng:"); r.RelativeItem().AlignRight().Text(inv.SubTotal.ToString("N0")); });
                            c.Item().Row(r => { r.RelativeItem().Text("Chiết khấu:"); r.RelativeItem().AlignRight().Text(inv.TotalDiscount.ToString("N0")); });
                            c.Item().PaddingTop(5).BorderTop(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten2).PaddingTop(5).Row(r => { r.RelativeItem().Text("Tổng thanh toán:").SemiBold(); r.RelativeItem().AlignRight().Text(inv.GrandTotal.ToString("N0")).SemiBold().FontColor(QuestPDF.Helpers.Colors.Red.Medium); });
                            
                            // "Số tiền viết bằng chữ"
                            c.Item().PaddingTop(10).Text($"Số tiền viết bằng chữ: {TradeFlow.Application.Common.Helpers.NumberToTextHelper.ConvertToWords((long)inv.GrandTotal)} đồng").Italic().FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Darken2);
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static InvoiceDto MapToDto(Invoice inv)
    {
        return new InvoiceDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            InvoiceDate = inv.InvoiceDate,
            SalesOrderId = inv.SalesOrderId,
            CustomerId = inv.CustomerId,
            CompanyName = inv.CompanyName,
            CompanyTaxCode = inv.CompanyTaxCode,
            CompanyAddress = inv.CompanyAddress,
            CompanyPhone = inv.CompanyPhone,
            CompanyEmail = inv.CompanyEmail,
            CompanyLogoUrl = inv.CompanyLogoUrl,
            CustomerName = inv.CustomerName,
            CustomerCompanyName = inv.CustomerCompanyName,
            CustomerTaxCode = inv.CustomerTaxCode,
            CustomerAddress = inv.CustomerAddress,
            CustomerEmail = inv.CustomerEmail,
            Notes = inv.Notes,
            Status = inv.Status,
            SubTotal = inv.SubTotal,
            TotalDiscount = inv.TotalDiscount,
            TotalTax = inv.TotalTax,
            GrandTotal = inv.GrandTotal,
            Items = inv.Items.Select(i => new InvoiceItemDto
            {
                Id = i.Id,
                InvoiceId = i.InvoiceId,
                ProductId = i.ProductId,
                ProductCode = i.ProductCode,
                ProductName = i.ProductName,
                UnitName = i.UnitName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                DiscountAmount = i.DiscountAmount,
                TaxRate = i.TaxRate,
                TaxAmount = i.TaxAmount,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    public async Task<bool> DeleteInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (inv == null) return false;

        if (inv.Status != InvoiceStatus.Draft && inv.Status != InvoiceStatus.Pending)
        {
            inv.Status = InvoiceStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(AuditEventType.InvoiceCancelled, _currentUserService.UserName ?? "System", "Invoice", id.ToString(), "Hủy hóa đơn vì đã phát hành", cancellationToken: cancellationToken);
            return true;
        }

        _context.InvoiceItems.RemoveRange(inv.Items);
        _context.Invoices.Remove(inv);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceDeleted, _currentUserService.UserName ?? "System", "Invoice", id.ToString(), "Xóa vật lý hóa đơn nháp", cancellationToken: cancellationToken);
        return true;
    }
}
