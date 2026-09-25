using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Models.Sales;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class OrderDocumentPdf : IDocument
{
    private readonly OrderDocumentDto _model;
    private readonly string? _webRootPath;

    private static readonly System.Globalization.CultureInfo ViCulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

    public OrderDocumentPdf(OrderDocumentDto model, string? webRootPath = null)
    {
        _model = model;
        _webRootPath = webRootPath;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginTop(1.2f, Unit.Centimetre);
            page.MarginBottom(1.2f, Unit.Centimetre);
            page.MarginLeft(1.2f, Unit.Centimetre);
            page.MarginRight(1.2f, Unit.Centimetre);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10).FontColor(Colors.Black));

            page.Content().Column(col =>
            {
                col.Spacing(6);

                // 1. HEADER
                col.Item().Row(row =>
                {
                    // Left: Company Info
                    row.RelativeItem(3).Column(c =>
                    {
                        c.Item().Text(_model.CompanyName)
                            .FontSize(13).Bold();
                        c.Item().PaddingTop(2).Text($"Email: {_model.Email}")
                            .FontSize(10.5f).Bold().Italic();
                        c.Item().PaddingTop(1).Text($"Tel/Zalo: {_model.Hotline}")
                            .FontSize(10.5f).Bold().Italic();
                    });

                    // Right: Title & Order Info
                    row.RelativeItem(2).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("ĐƠN ĐẶT HÀNG")
                            .FontSize(16).Bold();
                        c.Item().AlignRight().PaddingTop(2).Text($"Số ĐĐH: {_model.OrderCode}")
                            .FontSize(10.5f);
                        c.Item().AlignRight().PaddingTop(1).Text($"Ngày {_model.OrderDate:dd} tháng {_model.OrderDate:MM} năm {_model.OrderDate:yyyy}")
                            .FontSize(10.5f);
                    });
                });

                // 2. CUSTOMER INFO & SALUTATION
                col.Item().PaddingTop(4).Column(c =>
                {
                    c.Item().Text($"Kính gửi: {_model.CustomerName} - SĐT: {_model.CustomerPhone}");
                    c.Item().PaddingTop(2).Text($"Địa chỉ: {_model.CustomerAddress}");
                    c.Item().PaddingTop(4).Text($"Lời đầu tiên, {_model.CompanyName} kính gửi đến Quý khách lời chào trân trọng và lời chúc thành công. Chúng tôi xin gửi đến Quý khách báo giá cho sản phẩm với chi tiết như sau:")
                        .Italic().Bold();
                });

                // 3. PRODUCT TABLE
                col.Item().PaddingTop(4).Element(ComposeProductTable);

                // 4. TOTALS ROW (matching reference table layout)
                col.Item().Element(ComposeTotalsTable);

                // 5. NOTES & BANK ACCOUNT & QR CODE BOX
                col.Item().PaddingTop(4).Element(ComposeBankAndQrBox);

                // 6. FOOTER DISCLAIMER
                col.Item().PaddingTop(4).Column(c =>
                {
                    if (!string.IsNullOrWhiteSpace(_model.FooterNote1))
                    {
                        c.Item().Text(_model.FooterNote1).Italic().FontSize(9.5f);
                    }
                    if (!string.IsNullOrWhiteSpace(_model.FooterNote2))
                    {
                        c.Item().PaddingTop(2).Text(_model.FooterNote2).Italic().FontSize(9.5f);
                    }
                });
            });
        });
    }

    private void ComposeProductTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28); // NO
                cols.ConstantColumn(85); // Sản Phẩm
                cols.ConstantColumn(85); // Tên hàng
                cols.RelativeColumn();   // Mô Tả Sản Phẩm
                cols.ConstantColumn(50); // Số lượng
                cols.ConstantColumn(80); // Đơn giá
                cols.ConstantColumn(90); // Thành tiền
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Border(1).BorderColor(Colors.Black).PaddingVertical(3).AlignCenter().Text("NO").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).PaddingVertical(3).AlignCenter().Text("Sản Phẩm").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).PaddingVertical(3).AlignCenter().Text("Tên hàng").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).PaddingVertical(3).AlignCenter().Text("Mô Tả Sản Phẩm").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).PaddingVertical(3).AlignCenter().Text("Số lượng").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).PaddingVertical(3).AlignCenter().Text("Đơn giá").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).PaddingVertical(3).AlignCenter().Text("Thành tiền").Bold();
            });

            // Items
            foreach (var item in _model.Items)
            {
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.No.ToString());
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.CategoryName);
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.ProductCode);
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignLeft().Text(item.Description);
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.Quantity.ToString("N0", ViCulture));
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.UnitPrice.ToString("N0", ViCulture));
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.LineTotal.ToString("N0", ViCulture));
            }
        });
    }

    private void ComposeTotalsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28); // NO
                cols.ConstantColumn(85); // Sản Phẩm
                cols.ConstantColumn(85); // Tên hàng
                cols.RelativeColumn();   // Mô Tả Sản Phẩm
                cols.ConstantColumn(50); // Số lượng
                cols.ConstantColumn(80); // Đơn giá
                cols.ConstantColumn(90); // Thành tiền
            });

            // Row 1: Tổng giá trị đơn hàng
            table.Cell().ColumnSpan(4).Border(1).BorderColor(Colors.Black).Padding(3).AlignRight()
                .Text("Tổng giá trị đơn hàng:").Bold();
            table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter()
                .Text(_model.TotalQuantity.ToString("N0", ViCulture)).Bold();
            table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("");
            table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight()
                .Text(_model.TotalAmount.ToString("N0", ViCulture)).Bold();

            // Row 2: Tổng cộng giá trị đơn hàng
            table.Cell().ColumnSpan(4).Border(1).BorderColor(Colors.Black).Padding(3).AlignRight()
                .Text("Tổng cộng giá trị đơn hàng:");
            table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("");
            table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Text("");
            table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight()
                .Text(_model.GrandTotal.ToString("N0", ViCulture)).Bold();
        });
    }

    private void ComposeBankAndQrBox(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Black).Row(row =>
        {
            // Left column: Notes and Banking details (divided into rows)
            row.RelativeItem().Column(col =>
            {
                // Row 1: Ghi chú KH
                col.Item().Padding(3).Text(t =>
                {
                    t.Span("Ghi chú KH: ").Bold().Italic();
                    if (!string.IsNullOrWhiteSpace(_model.CustomerNotes))
                    {
                        t.Span(_model.CustomerNotes);
                    }
                });
                col.Item().LineHorizontal(1).LineColor(Colors.Black);

                // Row 2: VAT Note
                col.Item().Padding(3).Text(_model.VatNote ?? "Đơn giá trên chưa bao gồm thuế GTGT (8%).").Bold();
                col.Item().LineHorizontal(1).LineColor(Colors.Black);

                // Row 3: Bank Title
                col.Item().Padding(3).Text("TÀI KHOẢN NGÂN HÀNG:").Bold();
                col.Item().LineHorizontal(1).LineColor(Colors.Black);

                // Row 4: Account Holder
                col.Item().Padding(3).Text($"TÊN TÀI KHOẢN: {(_model.BankAccountHolder ?? "").ToUpper()}");
                col.Item().LineHorizontal(1).LineColor(Colors.Black);

                // Row 5: Account Number
                col.Item().Padding(3).Text($"SỐ TÀI KHOẢN: {_model.BankAccount}");
                col.Item().LineHorizontal(1).LineColor(Colors.Black);

                // Row 6: Bank Name
                col.Item().Padding(3).Text((_model.BankName ?? "").ToUpper());
            });

            // Right column: QR Code
            row.ConstantItem(120).BorderLeft(1).BorderColor(Colors.Black).AlignCenter().AlignMiddle().Padding(4).Element(c =>
            {
                string? resolvedQrPath = null;
                if (!string.IsNullOrEmpty(_model.QrCodePath))
                {
                    if (File.Exists(_model.QrCodePath))
                    {
                        resolvedQrPath = _model.QrCodePath;
                    }
                    else if (!string.IsNullOrEmpty(_webRootPath))
                    {
                        var local = Path.Combine(_webRootPath, _model.QrCodePath.TrimStart('/', '\\'));
                        if (File.Exists(local))
                        {
                            resolvedQrPath = local;
                        }
                    }
                    else
                    {
                        var c1 = Path.Combine(Directory.GetCurrentDirectory(), "src", "TradeFlow.Web", "wwwroot", _model.QrCodePath.TrimStart('/', '\\'));
                        if (File.Exists(c1)) resolvedQrPath = c1;
                        else
                        {
                            var c2 = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "TradeFlow.Web", "wwwroot", _model.QrCodePath.TrimStart('/', '\\'));
                            if (File.Exists(c2)) resolvedQrPath = c2;
                        }
                    }
                }

                if (!string.IsNullOrEmpty(resolvedQrPath) && File.Exists(resolvedQrPath))
                {
                    c.Width(105).Height(105).Image(resolvedQrPath).FitArea();
                }
                else
                {
                    c.Width(105).Height(105).Border(1).BorderColor(Colors.Grey.Lighten1).AlignCenter().AlignMiddle()
                        .Text("QR CODE").FontSize(9).FontColor(Colors.Grey.Medium);
                }
            });
        });
    }
}
