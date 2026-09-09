const fs = require('fs');

let serviceCode = fs.readFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', 'utf8');

let startIndex = serviceCode.indexOf('public async Task<byte[]> GeneratePdfAsync');
let endIndex = serviceCode.lastIndexOf('}'); // Assuming it's the last method in the class

let newMethod = `public async Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken cancellationToken = default)
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
    }`;

let combined = serviceCode.substring(0, startIndex) + newMethod + '\n}\n';
fs.writeFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', combined, 'utf8');
