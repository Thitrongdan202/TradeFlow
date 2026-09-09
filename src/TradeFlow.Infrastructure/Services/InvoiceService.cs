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

    
    public async Task<InvoiceDto> CreateManualInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
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
            InvoiceDate = dto.InvoiceDate == default ? DateTime.UtcNow : dto.InvoiceDate,
            CustomerId = dto.CustomerId,
            
            CompanyName = company?.CompanyName ?? "TradeFlow Company",
            CompanyTaxCode = company?.TaxCode,
            CompanyAddress = company?.Address,
            CompanyPhone = company?.Phone,
            CompanyEmail = company?.Email,
            CompanyLogoUrl = company?.LogoPath,
            CompanyBankAccount = company?.BankAccount,
            
            CustomerName = dto.CustomerName,
            CustomerCompanyName = dto.CustomerCompanyName,
            CustomerTaxCode = dto.CustomerTaxCode,
            CustomerAddress = dto.CustomerAddress,
            CustomerEmail = dto.CustomerEmail,
            PaymentMethod = dto.PaymentMethod ?? "TM/CK",
            CustomerBankAccount = dto.CustomerBankAccount,
            
            Notes = dto.Notes,
            Status = InvoiceStatus.Draft, 
            Type = dto.Type,
            
            SubTotal = dto.SubTotal,
            TotalDiscount = dto.TotalDiscount,
            TotalTax = dto.TotalTax,
            GrandTotal = dto.GrandTotal
        };

        foreach (var item in dto.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                LineTotal = item.LineTotal
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceCreated, "Invoice", invoice.Id.ToString(), "Manual invoice created", _currentUserService.UserId);

        return MapToDto(invoice);
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
            InvoiceDate = order.OrderDate,
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

        inv.CustomerName = dto.CustomerName;
        inv.CustomerCompanyName = dto.CustomerCompanyName;
        inv.CustomerTaxCode = dto.CustomerTaxCode;
        inv.CustomerAddress = dto.CustomerAddress;
        inv.PaymentMethod = dto.PaymentMethod;
        inv.Notes = dto.Notes;
        inv.Type = dto.Type;

        _context.InvoiceItems.RemoveRange(inv.Items);
        inv.Items.Clear();

        foreach(var item in dto.Items)
        {
            inv.Items.Add(new InvoiceItem
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                LineTotal = item.LineTotal
            });
        }

        inv.SubTotal = dto.SubTotal;
        inv.TotalDiscount = dto.TotalDiscount;
        inv.TotalTax = dto.TotalTax;
        inv.GrandTotal = dto.GrandTotal;

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
                page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontFamily(QuestPDF.Helpers.Fonts.Arial).FontSize(11));

                page.Header().Element(compose => 
                {
                    compose.Row(row =>
                    {
                        // Left: Company Info
                        row.RelativeItem().Column(column =>
                        {
                            column.Item().Text(inv.CompanyName).FontSize(14).SemiBold().FontColor(QuestPDF.Helpers.Colors.Blue.Darken2);
                            if (!string.IsNullOrEmpty(inv.CompanyTaxCode))
                                column.Item().Text($"Mã số thuế: {inv.CompanyTaxCode}");
                            if (!string.IsNullOrEmpty(inv.CompanyAddress))
                                column.Item().Text($"Địa chỉ: {inv.CompanyAddress}");
                            if (!string.IsNullOrEmpty(inv.CompanyPhone))
                                column.Item().Text($"Điện thoại: {inv.CompanyPhone}");
                            if (!string.IsNullOrEmpty(inv.CompanyEmail))
                                column.Item().Text($"Email: {inv.CompanyEmail}");
                        });
                        
                        // Right: Title & Invoice No
                        row.ConstantItem(220).AlignRight().Column(column =>
                        {
                            var title = inv.Type == TradeFlow.Domain.Enums.InvoiceType.VatInvoice ? "HÓA ĐƠN GIÁ TRỊ GIA TĂNG" : "HÓA ĐƠN BÁN HÀNG";
                            column.Item().Text(title).FontSize(16).SemiBold().FontColor(QuestPDF.Helpers.Colors.Black);
                            column.Item().Text($"Ký hiệu: .........").FontSize(10);
                            column.Item().Text($"Số: {inv.InvoiceNumber}").FontSize(10).SemiBold();
                            column.Item().Text($"Ngày {inv.InvoiceDate:dd} tháng {inv.InvoiceDate:MM} năm {inv.InvoiceDate:yyyy}").FontSize(10).Italic();
                        });
                    });
                });

                page.Content().PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre).Column(column =>
                {
                    column.Item().PaddingBottom(10).Column(c => {
                        c.Item().Text(t => {
                            t.Span("Họ tên người mua hàng: ");
                            t.Span(inv.CustomerName).SemiBold();
                        });
                        if (!string.IsNullOrEmpty(inv.CustomerCompanyName))
                            c.Item().Text($"Tên đơn vị: {inv.CustomerCompanyName}");
                        if (!string.IsNullOrEmpty(inv.CustomerTaxCode))
                            c.Item().Text($"Mã số thuế: {inv.CustomerTaxCode}");
                        c.Item().Text($"Địa chỉ: {inv.CustomerAddress}");
                        c.Item().Text($"Hình thức thanh toán: {inv.PaymentMethod}");
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30); // STT
                            columns.RelativeColumn(3); // Tên
                            columns.RelativeColumn(1); // ĐVT
                            columns.RelativeColumn(1); // Số lượng
                            columns.RelativeColumn(2); // Đơn giá
                            columns.RelativeColumn(2); // Thành tiền
                            if (inv.Type == TradeFlow.Domain.Enums.InvoiceType.VatInvoice)
                            {
                                columns.RelativeColumn(1); // Thuế suất
                                columns.RelativeColumn(2); // Tiền thuế
                            }
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Border(1).Padding(2).AlignCenter().Text("STT").SemiBold();
                            header.Cell().Border(1).Padding(2).AlignCenter().Text("Tên hàng hóa, dịch vụ").SemiBold();
                            header.Cell().Border(1).Padding(2).AlignCenter().Text("ĐVT").SemiBold();
                            header.Cell().Border(1).Padding(2).AlignCenter().Text("SL").SemiBold();
                            header.Cell().Border(1).Padding(2).AlignCenter().Text("Đơn giá").SemiBold();
                            header.Cell().Border(1).Padding(2).AlignCenter().Text("Thành tiền").SemiBold();
                            
                            if (inv.Type == TradeFlow.Domain.Enums.InvoiceType.VatInvoice)
                            {
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Thuế suất").SemiBold();
                                header.Cell().Border(1).Padding(2).AlignCenter().Text("Tiền thuế").SemiBold();
                            }
                        });

                        // Rows
                        int stt = 1;
                        foreach (var item in inv.Items)
                        {
                            table.Cell().Border(1).Padding(2).AlignCenter().Text(stt.ToString());
                            table.Cell().Border(1).Padding(2).Text(item.ProductName);
                            table.Cell().Border(1).Padding(2).AlignCenter().Text(item.UnitName);
                            table.Cell().Border(1).Padding(2).AlignRight().Text(item.Quantity.ToString("G29"));
                            table.Cell().Border(1).Padding(2).AlignRight().Text(item.UnitPrice.ToString("N0"));
                            var totalBeforeTax = item.Quantity * item.UnitPrice - item.DiscountAmount;
                            table.Cell().Border(1).Padding(2).AlignRight().Text(totalBeforeTax.ToString("N0"));

                            if (inv.Type == TradeFlow.Domain.Enums.InvoiceType.VatInvoice)
                            {
                                table.Cell().Border(1).Padding(2).AlignRight().Text($"{item.TaxRate}%");
                                table.Cell().Border(1).Padding(2).AlignRight().Text(item.TaxAmount.ToString("N0"));
                            }
                            stt++;
                        }
                    });

                    // Summary
                    column.Item().PaddingTop(10).AlignRight().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        if (inv.Type == TradeFlow.Domain.Enums.InvoiceType.VatInvoice)
                        {
                            table.Cell().Padding(2).AlignRight().Text("Tổng tiền chưa thuế: ");
                            table.Cell().Padding(2).AlignRight().Text(inv.SubTotal.ToString("N0"));
                            
                            table.Cell().Padding(2).AlignRight().Text("Tiền thuế GTGT: ");
                            table.Cell().Padding(2).AlignRight().Text(inv.TotalTax.ToString("N0"));
                        }

                        table.Cell().Padding(2).AlignRight().Text("Tổng cộng tiền thanh toán: ").SemiBold();
                        table.Cell().Padding(2).AlignRight().Text(inv.GrandTotal.ToString("N0")).SemiBold();
                    });

                    column.Item().PaddingTop(5).Text($"Số tiền viết bằng chữ: {TradeFlow.Application.Common.Helpers.NumberToTextHelper.ConvertToWords((long)inv.GrandTotal)} đồng chẵn.").Italic();

                    // Signatures
                    column.Item().PaddingTop(30).Row(row =>
                    {
                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("Người mua hàng").SemiBold();
                            c.Item().Text("(Chữ ký số (nếu có))").FontSize(9).Italic();
                        });

                        row.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().Text("Người bán hàng").SemiBold();
                            c.Item().Text("(Chữ ký điện tử, chữ ký số)").FontSize(9).Italic();
                            
                            c.Item().PaddingTop(20).Container().Border(1).BorderColor(QuestPDF.Helpers.Colors.Green.Lighten1).Background(QuestPDF.Helpers.Colors.Green.Lighten5).Padding(10).Column(sig => 
                            {
                                sig.Item().Text("Xác nhận của công ty").SemiBold().FontColor(QuestPDF.Helpers.Colors.Green.Darken2);
                                sig.Item().Text($"Ký bởi: {inv.CompanyName}").FontColor(QuestPDF.Helpers.Colors.Red.Darken2);
                                sig.Item().Text($"Ký ngày: {DateTime.Now:dd/MM/yyyy}").FontColor(QuestPDF.Helpers.Colors.Red.Darken2);
                            });
                        });
                    });
                });

                page.Footer().AlignCenter().Text("(Cần kiểm tra, đối chiếu khi lập, giao, nhận hóa đơn)").FontSize(10).Italic();
            });
        });

        return document.GeneratePdf();
    }

    public async Task<bool> DeleteInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        _context.Invoices.Remove(inv);
        
        if (inv.SalesOrderId.HasValue)
        {
            var so = await _context.SalesOrders.FindAsync(new object[] { inv.SalesOrderId.Value }, cancellationToken);
            if (so != null && so.Status == SalesOrderStatus.Invoiced)
            {
                so.Status = SalesOrderStatus.Confirmed;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceDeleted, "Invoice", inv.Id.ToString(), "Draft invoice deleted", _currentUserService.UserId);

        return true;
    }


    private InvoiceDto MapToDto(Invoice invoice)
    {
        var dto = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            SalesOrderId = invoice.SalesOrderId,
            CustomerId = invoice.CustomerId,
            CompanyName = invoice.CompanyName,
            CompanyTaxCode = invoice.CompanyTaxCode,
            CompanyAddress = invoice.CompanyAddress,
            CompanyPhone = invoice.CompanyPhone,
            CompanyEmail = invoice.CompanyEmail,
            CompanyLogoUrl = invoice.CompanyLogoUrl,
            CustomerName = invoice.CustomerName,
            CustomerCompanyName = invoice.CustomerCompanyName,
            CustomerTaxCode = invoice.CustomerTaxCode,
            CustomerAddress = invoice.CustomerAddress,
            CustomerEmail = invoice.CustomerEmail,
            PaymentMethod = invoice.PaymentMethod,
            Notes = invoice.Notes,
            Status = invoice.Status,
            Type = invoice.Type,
            SubTotal = invoice.SubTotal,
            TotalDiscount = invoice.TotalDiscount,
            TotalTax = invoice.TotalTax,
            GrandTotal = invoice.GrandTotal
        };

        foreach (var item in invoice.Items)
        {
            dto.Items.Add(new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                LineTotal = item.LineTotal
            });
        }

        return dto;
    }

}
