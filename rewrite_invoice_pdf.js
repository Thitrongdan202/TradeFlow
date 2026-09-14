// rewrite_invoice_pdf.js
// Rewrites only the GeneratePdfAsync method in InvoiceService.cs
const fs = require('fs');

const filePath = 'src/TradeFlow.Infrastructure/Services/InvoiceService.cs';
let src = fs.readFileSync(filePath, 'utf8');

const START_MARKER = '    public async Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken cancellationToken = default)';
const END_MARKER = '\n    public async Task<bool> DeleteInvoiceAsync';

const startIdx = src.indexOf(START_MARKER);
const endIdx = src.indexOf(END_MARKER);

if (startIdx === -1 || endIdx === -1) {
    console.error('Could not find method boundaries');
    process.exit(1);
}

const newMethod = `    public async Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (inv == null) throw new Exception("Invoice not found");
        
        await _auditService.LogAsync(AuditEventType.InvoiceExportedPdf, "Invoice", inv.Id.ToString(), "PDF exported", _currentUserService.UserId);
        
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        
        bool isVat = inv.Type == TradeFlow.Domain.Enums.InvoiceType.VatInvoice;
        var invoiceDate = inv.InvoiceDate;
        
        // Colors matching reference (blue VAT invoice style)
        string borderBlue    = "#1a3766";
        string headerBg      = "#b8d1e8";
        string tablHeadBg    = "#4472c4";
        string rowAltBg      = "#dce6f1";
        string labelColor    = "#1a3766";
        string totRowBg      = "#dce6f1";
        string white         = "#ffffff";
        string black         = "#000000";

        // Helper: render one info row "Label (Eng): .............value............."
        static void InfoRow(ColumnDescriptor col, string label, string? value, float fontSize = 8.5f)
        {
            col.Item().BorderBottom(0.3f).BorderColor("#a0b8d0").PaddingHorizontal(4).PaddingVertical(2).Row(row =>
            {
                row.AutoItem().Text(label + ": ").FontSize(fontSize).FontColor("#1a3766");
                row.RelativeItem().Text(value ?? "").FontSize(fontSize).FontColor("#000000");
            });
        }

        static void InfoRowDual(ColumnDescriptor col, string label1, string? val1, string label2, string? val2, float fontSize = 8.5f)
        {
            col.Item().BorderBottom(0.3f).BorderColor("#a0b8d0").PaddingHorizontal(4).PaddingVertical(2).Row(row =>
            {
                row.RelativeItem().Row(r2 =>
                {
                    r2.AutoItem().Text(label1 + ": ").FontSize(fontSize).FontColor("#1a3766");
                    r2.RelativeItem().Text(val1 ?? "").FontSize(fontSize).FontColor("#000000");
                });
                row.RelativeItem().Row(r2 =>
                {
                    r2.AutoItem().Text(label2 + ": ").FontSize(fontSize).FontColor("#1a3766");
                    r2.RelativeItem().Text(val2 ?? "").FontSize(fontSize).FontColor("#000000");
                });
            });
        }
        
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(QuestPDF.Helpers.PageSizes.A4);
                page.MarginTop(10, QuestPDF.Infrastructure.Unit.Millimetre);
                page.MarginBottom(10, QuestPDF.Infrastructure.Unit.Millimetre);
                page.MarginLeft(12, QuestPDF.Infrastructure.Unit.Millimetre);
                page.MarginRight(12, QuestPDF.Infrastructure.Unit.Millimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(9).FontColor(labelColor));

                page.Content().Border(2).BorderColor(borderBlue).Column(main =>
                {
                    // ── HEADER (blue bg) ─────────────────────────────────
                    main.Item().Background(headerBg).Column(hdr =>
                    {
                        hdr.Item().PaddingTop(6).PaddingBottom(2).Row(titleRow =>
                        {
                            // Left spacer
                            titleRow.ConstantItem(20);
                            
                            // Center: title
                            titleRow.RelativeItem().AlignCenter().Column(tc =>
                            {
                                var titleText = isVat ? "HÓA ĐƠN GIÁ TRỊ GIA TĂNG" : "HÓA ĐƠN BÁN HÀNG";
                                var subtitleText = isVat ? "(VAT INVOICE)" : "(SALES INVOICE)";
                                tc.Item().AlignCenter().Text(titleText)
                                    .FontSize(16).Bold().FontColor(borderBlue);
                                tc.Item().AlignCenter().Text(subtitleText)
                                    .FontSize(10).Italic().FontColor(borderBlue);
                            });
                            
                            // Right: form number block
                            titleRow.ConstantItem(130).AlignRight().PaddingRight(6).Column(fc =>
                            {
                                fc.Item().Text(t =>
                                {
                                    t.Span("Mẫu số ").FontSize(8).Italic().FontColor(borderBlue);
                                    t.Span("(Form No.)").FontSize(7).Italic().FontColor(borderBlue);
                                    t.Span(": ............").FontSize(8).FontColor(borderBlue);
                                });
                                fc.Item().Text(t =>
                                {
                                    t.Span("Ký hiệu ").FontSize(8).Italic().FontColor(borderBlue);
                                    t.Span("(Serial No.)").FontSize(7).Italic().FontColor(borderBlue);
                                    t.Span(": ............").FontSize(8).FontColor(borderBlue);
                                });
                                fc.Item().Text(t =>
                                {
                                    t.Span("Số ").FontSize(8).Italic().FontColor(borderBlue);
                                    t.Span("(Invoice No.)").FontSize(7).Italic().FontColor(borderBlue);
                                    t.Span(": ").FontSize(8).FontColor(borderBlue);
                                    t.Span(inv.InvoiceNumber).FontSize(8).Bold().FontColor(borderBlue);
                                });
                            });
                        });
                        
                        // Date row
                        hdr.Item().PaddingBottom(6).AlignCenter().Text(t =>
                        {
                            t.Span("Ngày ").FontSize(9).Italic().FontColor(borderBlue);
                            t.Span("(day) ").FontSize(8).Italic().FontColor(borderBlue);
                            t.Span(invoiceDate.ToString("dd")).FontSize(9).Bold().FontColor(borderBlue);
                            t.Span("  tháng ").FontSize(9).Italic().FontColor(borderBlue);
                            t.Span("(month) ").FontSize(8).Italic().FontColor(borderBlue);
                            t.Span(invoiceDate.ToString("MM")).FontSize(9).Bold().FontColor(borderBlue);
                            t.Span("  năm ").FontSize(9).Italic().FontColor(borderBlue);
                            t.Span("(year) ").FontSize(8).Italic().FontColor(borderBlue);
                            t.Span(invoiceDate.ToString("yyyy")).FontSize(9).Bold().FontColor(borderBlue);
                        });
                    });

                    // ── SELLER BLOCK ─────────────────────────────────────
                    main.Item().BorderTop(0.5f).BorderColor(borderBlue).Column(sel =>
                    {
                        InfoRow(sel, "Đơn vị bán hàng (Seller)", inv.CompanyName);
                        InfoRow(sel, "MST (Tax Code)", inv.CompanyTaxCode);
                        InfoRow(sel, "Địa chỉ (Address)", inv.CompanyAddress);
                        InfoRowDual(sel,
                            "Điện thoại (Tel.)", inv.CompanyPhone,
                            "Email", inv.CompanyEmail);
                        InfoRowDual(sel,
                            "STK (Account No.)", inv.CompanyBankAccount,
                            "Ngân hàng (Bank)", null);
                    });

                    // ── BUYER BLOCK ──────────────────────────────────────
                    main.Item().BorderTop(0.5f).BorderColor(borderBlue).Column(buy =>
                    {
                        InfoRow(buy, "Người mua hàng (Buyer)", inv.CustomerName);
                        InfoRow(buy, "Đơn vị (Co. name)", inv.CustomerCompanyName);
                        InfoRow(buy, "MST (Tax Code)", inv.CustomerTaxCode);
                        InfoRow(buy, "Địa chỉ (Address)", inv.CustomerAddress);
                        InfoRow(buy, "HTTT (Pay. method)", inv.PaymentMethod ?? "TM/CK");
                        InfoRowDual(buy,
                            "STK (Account No.)", inv.CustomerBankAccount,
                            "Ngân hàng (Bank)", null);
                    });

                    // ── PRODUCT TABLE ────────────────────────────────────
                    main.Item().BorderTop(1).BorderColor(borderBlue).Table(table =>
                    {
                        // Column widths (matching reference proportions)
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);    // STT
                            cols.RelativeColumn(3.5f);  // Tên hàng hoá
                            cols.ConstantColumn(36);    // ĐVT
                            cols.ConstantColumn(42);    // Số lượng
                            cols.ConstantColumn(62);    // Đơn giá
                            cols.ConstantColumn(62);    // Thành tiền
                            if (isVat)
                            {
                                cols.ConstantColumn(42); // Thuế suất
                                cols.ConstantColumn(58); // Tiền thuế
                            }
                        });

                        // Column headers — row 1 (labels)
                        table.Header(h =>
                        {
                            void Th(string vi, string en, bool center = true)
                            {
                                var cell = h.Cell().Background(tablHeadBg).Border(0.5f).BorderColor(white).Padding(3);
                                if (center)
                                    cell.AlignCenter().Column(c2 => {
                                        c2.Item().AlignCenter().Text(vi).FontSize(8).SemiBold().FontColor(white);
                                        c2.Item().AlignCenter().Text(en).FontSize(7).Italic().FontColor(white);
                                    });
                                else
                                    cell.Column(c2 => {
                                        c2.Item().Text(vi).FontSize(8).SemiBold().FontColor(white);
                                        c2.Item().Text(en).FontSize(7).Italic().FontColor(white);
                                    });
                            }

                            Th("STT", "(No.)");
                            Th("Tên hàng hoá, dịch vụ", "(Description)", false);
                            Th("ĐVT", "(Unit)");
                            Th("Số lượng", "(Quantity)");
                            Th("Đơn giá", "(Unit Price)");
                            Th("Thành tiền", "(Amount)");
                            if (isVat)
                            {
                                Th("Thuế suất", "(Tax Rate)");
                                Th("Tiền thuế", "(Tax Amount)");
                            }
                        });

                        // Column number row
                        void NumCell(string n) => table.Cell().Border(0.3f).BorderColor("#a0b8d0").Background(rowAltBg).Padding(2).AlignCenter().Text(n).FontSize(8).Italic().FontColor(borderBlue);
                        NumCell("1"); NumCell("2"); NumCell("3"); NumCell("4"); NumCell("5"); NumCell("6 = 4 x 5");
                        if (isVat) { NumCell("7"); NumCell("8 = 6 x 7"); }

                        // Data rows (minimum 8 rows to match reference height)
                        int stt = 1;
                        foreach (var item in inv.Items)
                        {
                            var lineAmt = item.Quantity * item.UnitPrice - item.DiscountAmount;
                            table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).AlignCenter().Text(stt.ToString()).FontSize(8.5f).FontColor(black);
                            table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).Text(item.ProductName).FontSize(8.5f).FontColor(black);
                            table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).AlignCenter().Text(item.UnitName).FontSize(8.5f).FontColor(black);
                            table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).AlignRight().Text(item.Quantity.ToString("G29")).FontSize(8.5f).FontColor(black);
                            table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).AlignRight().Text(item.UnitPrice.ToString("N0")).FontSize(8.5f).FontColor(black);
                            table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).AlignRight().Text(lineAmt.ToString("N0")).FontSize(8.5f).FontColor(black);
                            if (isVat)
                            {
                                table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).AlignCenter().Text($"{item.TaxRate}%").FontSize(8.5f).FontColor(black);
                                table.Cell().Border(0.3f).BorderColor("#a0b8d0").Padding(3).AlignRight().Text(item.TaxAmount.ToString("N0")).FontSize(8.5f).FontColor(black);
                            }
                            stt++;
                        }

                        // Pad with empty rows to ensure minimum height (reference shows ~8 item rows)
                        int emptyRows = Math.Max(0, 8 - inv.Items.Count);
                        int totalCols = isVat ? 8 : 6;
                        for (int r = 0; r < emptyRows; r++)
                        {
                            for (int c = 0; c < totalCols; c++)
                                table.Cell().Border(0.3f).BorderColor("#a0b8d0").MinHeight(16).Text("");
                        }
                    });

                    // ── TOTALS ───────────────────────────────────────────
                    int totalColSpan = isVat ? 8 : 6;
                    int labelCols    = isVat ? 6 : 4;

                    void TotalRow(ColumnDescriptor col, string viLabel, string enLabel, string value, bool bold = false)
                    {
                        col.Item().Background(totRowBg).BorderBottom(0.3f).BorderColor("#a0b8d0").Row(tr =>
                        {
                            tr.RelativeItem().AlignRight().PaddingRight(4).PaddingVertical(2).Text(t =>
                            {
                                t.Span(viLabel).FontSize(9).FontColor(borderBlue);
                                if (!string.IsNullOrEmpty(enLabel))
                                {
                                    t.Span(" ").FontSize(9);
                                    t.Span(enLabel).FontSize(8).Italic().FontColor(borderBlue);
                                }
                                t.Span(":").FontSize(9).FontColor(borderBlue);
                            });
                            tr.ConstantItem(80).BorderLeft(0.3f).BorderColor("#a0b8d0").PaddingRight(4).PaddingVertical(2).AlignRight().Text(value)
                                .FontSize(9).FontColor(black);
                            if (bold) { /* already styled */ }
                        });
                    }

                    main.Item().BorderTop(1).BorderColor(borderBlue).Column(totals =>
                    {
                        if (isVat)
                        {
                            TotalRow(totals,
                                "Cộng tiền hàng", "(Sub total)",
                                inv.SubTotal.ToString("N0"));
                            TotalRow(totals,
                                "Cộng tiền thuế GTGT", "(VAT amount)",
                                inv.TotalTax.ToString("N0"));
                        }
                        TotalRow(totals,
                            "Tổng cộng tiền thanh toán", "(Total payment)",
                            inv.GrandTotal.ToString("N0"));
                    });

                    // ── AMOUNT IN WORDS ──────────────────────────────────
                    string amountWords = TradeFlow.Application.Common.Helpers.NumberToTextHelper.ConvertToWords((long)inv.GrandTotal);
                    main.Item().BorderTop(0.5f).BorderColor(borderBlue).PaddingHorizontal(4).PaddingVertical(3).Row(awr =>
                    {
                        awr.AutoItem().Text(t =>
                        {
                            t.Span("Số tiền viết bằng chữ ").FontSize(8.5f).FontColor(borderBlue);
                            t.Span("(Amount In words)").FontSize(7.5f).Italic().FontColor(borderBlue);
                            t.Span(": ").FontSize(8.5f).FontColor(borderBlue);
                        });
                        awr.RelativeItem().Text($"{amountWords} đồng chẵn.").FontSize(8.5f).Italic().FontColor(black);
                    });

                    // ── SIGNATURE AREA ───────────────────────────────────
                    main.Item().BorderTop(1).BorderColor(borderBlue).Row(sigRow =>
                    {
                        // Buyer signature (left)
                        sigRow.RelativeItem().BorderRight(0.5f).BorderColor(borderBlue).PaddingVertical(8).AlignCenter().Column(bsig =>
                        {
                            bsig.Item().AlignCenter().Text(t =>
                            {
                                t.Span("Người mua hàng ").FontSize(9).Bold().FontColor(borderBlue);
                                t.Span("(Buyer)").FontSize(8).Italic().FontColor(borderBlue);
                            });
                            bsig.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").FontSize(8).Italic().FontColor("#4b5563");
                            bsig.Item().AlignCenter().Text("(Sign, full name)").FontSize(7.5f).Italic().FontColor("#4b5563");
                            bsig.Item().MinHeight(60);
                        });

                        // Seller signature (right) — with electronic confirmation block
                        sigRow.RelativeItem().PaddingVertical(8).AlignCenter().Column(ssig =>
                        {
                            ssig.Item().AlignCenter().Text(t =>
                            {
                                t.Span("Người bán hàng ").FontSize(9).Bold().FontColor(borderBlue);
                                t.Span("(Seller)").FontSize(8).Italic().FontColor(borderBlue);
                            });
                            ssig.Item().AlignCenter().Text("(Ký, ghi rõ họ tên, đóng dấu nếu có)").FontSize(8).Italic().FontColor("#4b5563");
                            ssig.Item().AlignCenter().Text("(Sign, full name, stamp if any)").FontSize(7.5f).Italic().FontColor("#4b5563");

                            // Electronic signature confirmation box (TradeFlow/LACASA own — NOT a qualified digital sig)
                            ssig.Item().PaddingTop(8).PaddingHorizontal(16).Border(1).BorderColor("#8bc34a").Background("#f1f8e9").Padding(8).Column(esig =>
                            {
                                esig.Item().AlignCenter().Text("Đã được ký điện tử bởi").FontSize(8.5f).SemiBold().FontColor("#2e7d32");
                                esig.Item().AlignCenter().Text("(Signed digitally by)").FontSize(7.5f).Italic().FontColor("#2e7d32");
                                esig.Item().PaddingTop(4).AlignCenter().Text("✓").FontSize(24).Bold().FontColor("#66bb6a");
                                esig.Item().PaddingTop(2).AlignCenter().Text(inv.CompanyName.ToUpper()).FontSize(9).SemiBold().FontColor("#1b5e20");
                                esig.Item().PaddingTop(4).AlignCenter().Text($"Ký ngày: {inv.InvoiceDate:dd/MM/yyyy}").FontSize(8).FontColor("#2e7d32");
                            });
                        });
                    });

                    // ── FOOTER NOTE ──────────────────────────────────────
                    main.Item().BorderTop(0.5f).BorderColor(borderBlue).Background(headerBg)
                        .PaddingVertical(3).AlignCenter()
                        .Text("(Cần kiểm tra, đối chiếu khi lập, giao, nhận hóa đơn)")
                        .FontSize(8).Italic().FontColor(borderBlue);
                });
            });
        });

        return document.GeneratePdf();
    }
`;

const replacement = src.substring(0, startIdx) + newMethod + src.substring(endIdx);
fs.writeFileSync(filePath, replacement, 'utf8');
console.log('Done. File written:', filePath);
