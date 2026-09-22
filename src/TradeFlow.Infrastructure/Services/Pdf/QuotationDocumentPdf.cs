using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.Settings;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class QuotationDocumentPdf : IDocument
{
    private readonly QuotationDto _model;
    private readonly CompanySettings? _company;
    private readonly string? _webRootPath;

    public QuotationDocumentPdf(QuotationDto model, CompanySettings? company, string? webRootPath = null)
    {
        _model = model;
        _company = company;
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
                        c.Item().Text(_company?.CompanyName ?? "LACASA").FontSize(13).Bold();
                        if (!string.IsNullOrWhiteSpace(_company?.TaxCode))
                            c.Item().PaddingTop(1).Text($"MST: {_company.TaxCode}").FontSize(9.5f);
                        if (!string.IsNullOrWhiteSpace(_company?.Address))
                            c.Item().PaddingTop(1).Text($"Địa chỉ: {_company.Address}").FontSize(9.5f);
                        if (!string.IsNullOrWhiteSpace(_company?.Email))
                            c.Item().PaddingTop(1).Text($"Email: {_company.Email}").FontSize(9.5f).Bold().Italic();
                        if (!string.IsNullOrWhiteSpace(_company?.Phone) || !string.IsNullOrWhiteSpace(_company?.OrderHotline))
                            c.Item().PaddingTop(1).Text($"Hotline/Zalo: {_company?.OrderHotline ?? _company?.Phone}").FontSize(9.5f).Bold().Italic();
                    });

                    // Right: Title & Quotation Info
                    row.RelativeItem(2).AlignRight().Column(c =>
                    {
                        c.Item().AlignRight().Text("BẢNG BÁO GIÁ").FontSize(16).Bold();
                        c.Item().AlignRight().PaddingTop(2).Text($"Số: {_model.Code}").FontSize(10.5f).Bold();
                        c.Item().AlignRight().PaddingTop(1).Text($"Ngày: {_model.QuotationDate:dd/MM/yyyy}").FontSize(10f);
                        if (_model.ExpiryDate.HasValue)
                        {
                            c.Item().AlignRight().PaddingTop(1).Text($"Hiệu lực đến: {_model.ExpiryDate.Value:dd/MM/yyyy}").FontSize(10f).Italic();
                        }
                    });
                });

                // 2. CUSTOMER INFO & SALUTATION
                col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
                {
                    c.Item().Text(t =>
                    {
                        t.Span("Kính gửi: ").Bold();
                        t.Span(_model.CustomerName).Bold();
                        if (!string.IsNullOrWhiteSpace(_model.CustomerContactPerson))
                        {
                            t.Span($"  -  Người liên hệ: ").Bold();
                            t.Span(_model.CustomerContactPerson);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(_model.CustomerPhone) || !string.IsNullOrWhiteSpace(_model.CustomerEmail))
                    {
                        c.Item().PaddingTop(2).Text(t =>
                        {
                            if (!string.IsNullOrWhiteSpace(_model.CustomerPhone))
                                t.Span($"Điện thoại: {_model.CustomerPhone}   ");
                            if (!string.IsNullOrWhiteSpace(_model.CustomerEmail))
                                t.Span($"Email: {_model.CustomerEmail}");
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(_model.CustomerAddress))
                    {
                        c.Item().PaddingTop(2).Text($"Địa chỉ: {_model.CustomerAddress}");
                    }

                    if (!string.IsNullOrWhiteSpace(_model.SalespersonName))
                    {
                        c.Item().PaddingTop(2).Text($"Nhân viên phụ trách: {_model.SalespersonName}").Italic();
                    }
                });

                col.Item().PaddingTop(2).Text("Chúng tôi xin trân trọng gửi đến Quý khách bảng báo giá chi tiết cho các sản phẩm/dịch vụ như sau:").Italic();

                // 3. PRODUCT TABLE
                col.Item().PaddingTop(2).Element(ComposeProductTable);

                // 4. TERMS, BANK & QR CODE BOX
                col.Item().PaddingTop(4).Element(ComposeTermsAndBankBox);

                // 5. SIGNATURES
                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().Text("ĐẠI DIỆN KHÁCH HÀNG").Bold();
                        c.Item().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(8.5f);
                        c.Item().Height(40);
                    });

                    row.RelativeItem().AlignCenter().Column(c =>
                    {
                        c.Item().Text("ĐẠI DIỆN BÊN BÁO GIÁ").Bold();
                        c.Item().Text("(Ký, đóng dấu, ghi rõ họ tên)").Italic().FontSize(8.5f);
                        c.Item().Height(40);
                        if (!string.IsNullOrWhiteSpace(_model.SalespersonName))
                        {
                            c.Item().Text(_model.SalespersonName).Bold();
                        }
                    });
                });
            });
        });
    }

    private void ComposeProductTable(IContainer container)
    {
        bool hasDiscount = _model.TotalDiscount > 0;

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28); // STT
                cols.ConstantColumn(75); // Mã hàng
                cols.RelativeColumn();   // Tên sản phẩm & Quy cách
                cols.ConstantColumn(45); // ĐVT
                cols.ConstantColumn(45); // SL
                cols.ConstantColumn(80); // Đơn giá
                if (hasDiscount)
                {
                    cols.ConstantColumn(75); // Chiết khấu
                }
                cols.ConstantColumn(90); // Thành tiền
            });

            // Table Header
            table.Header(header =>
            {
                header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("STT").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("Mã hàng").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("Tên hàng hóa, quy cách").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("ĐVT").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("SL").Bold();
                header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("Đơn giá").Bold();
                if (hasDiscount)
                {
                    header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("Chiết khấu").Bold();
                }
                header.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text("Thành tiền").Bold();
            });

            // Table Rows
            int index = 1;
            foreach (var item in _model.Items.OrderBy(x => x.SortOrder))
            {
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(index.ToString());
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.ProductCode);
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignLeft().Column(col =>
                {
                    col.Item().Text(item.ProductName).Bold();
                    if (!string.IsNullOrWhiteSpace(item.Notes))
                    {
                        col.Item().Text(item.Notes).Italic().FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                    }
                });
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.UnitName);
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.Quantity.ToString("N0"));
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.UnitPrice.ToString("N0"));
                if (hasDiscount)
                {
                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight()
                        .Text(item.DiscountAmount > 0 ? item.DiscountAmount.ToString("N0") : "-");
                }
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.LineTotal.ToString("N0"));

                index++;
            }

            // TOTALS ROW
            uint textSpan = hasDiscount ? 6u : 5u;

            if (hasDiscount)
            {
                table.Cell().ColumnSpan(textSpan).Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text("Cộng tiền hàng:").Bold();
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(_model.SubTotal.ToString("N0")).Bold();

                table.Cell().ColumnSpan(textSpan).Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text("Tiền chiết khấu:").Bold();
                table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(_model.TotalDiscount.ToString("N0")).Bold();
            }

            table.Cell().ColumnSpan(textSpan).Border(1).BorderColor(Colors.Black).Padding(4).AlignRight()
                .Text("TỔNG CỘNG TIỀN BÁO GIÁ:").Bold().FontSize(10.5f);
            table.Cell().Border(1).BorderColor(Colors.Black).Padding(4).AlignRight()
                .Text($"{_model.GrandTotal:N0} ₫").Bold().FontSize(10.5f);
        });
    }

    private void ComposeTermsAndBankBox(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Black).Row(row =>
        {
            // Left: Terms and Conditions
            row.RelativeItem(3).Column(col =>
            {
                col.Item().Padding(4).Text("ĐIỀU KHOẢN VÀ GHI CHÚ THƯƠNG MẠI:").Bold();
                col.Item().LineHorizontal(0.5f).LineColor(Colors.Black);

                col.Item().Padding(4).Column(c =>
                {
                    c.Item().Text("• Đơn giá trên là giá bán thương mại trước thuế GTGT (8%).").Bold();
                    if (_model.ExpiryDate.HasValue)
                    {
                        c.Item().PaddingTop(2).Text($"• Hiệu lực báo giá: Đến hết ngày {_model.ExpiryDate.Value:dd/MM/yyyy}.");
                    }
                    if (!string.IsNullOrWhiteSpace(_model.Terms))
                    {
                        c.Item().PaddingTop(2).Text(_model.Terms);
                    }
                    if (!string.IsNullOrWhiteSpace(_model.Notes))
                    {
                        c.Item().PaddingTop(2).Text($"• Ghi chú: {_model.Notes}").Italic();
                    }
                });

                col.Item().LineHorizontal(0.5f).LineColor(Colors.Black);
                col.Item().Padding(4).Column(c =>
                {
                    c.Item().Text("THÔNG TIN TÀI KHOẢN THANH TOÁN:").Bold();
                    c.Item().Text($"• Chủ tài khoản: {(_company?.BankAccountHolder ?? "LACASA").ToUpper()}");
                    c.Item().Text($"• Số tài khoản: {_company?.BankAccount ?? "---"}");
                    c.Item().Text($"• Ngân hàng: {(_company?.BankName ?? "---").ToUpper()}");
                });
            });

            // Right: VietQR Box if available
            row.ConstantItem(120).BorderLeft(1).BorderColor(Colors.Black).AlignCenter().AlignMiddle().Padding(4).Element(c =>
            {
                string? resolvedQrPath = null;
                if (!string.IsNullOrEmpty(_company?.OrderQrCodePath))
                {
                    if (File.Exists(_company.OrderQrCodePath))
                    {
                        resolvedQrPath = _company.OrderQrCodePath;
                    }
                    else if (!string.IsNullOrEmpty(_webRootPath))
                    {
                        var local = Path.Combine(_webRootPath, _company.OrderQrCodePath.TrimStart('/', '\\'));
                        if (File.Exists(local)) resolvedQrPath = local;
                    }
                }

                if (!string.IsNullOrEmpty(resolvedQrPath) && File.Exists(resolvedQrPath))
                {
                    c.Width(100).Height(100).Image(resolvedQrPath).FitArea();
                }
                else
                {
                    c.Width(100).Height(100).Border(0.5f).BorderColor(Colors.Grey.Lighten1).AlignCenter().AlignMiddle()
                        .Text("Quét mã QR\nđể thanh toán").FontSize(8).Italic().AlignCenter();
                }
            });
        });
    }
}
