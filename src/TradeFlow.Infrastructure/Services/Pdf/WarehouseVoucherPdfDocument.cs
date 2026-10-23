using System;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Helpers;
using TradeFlow.Application.Common.Models.Warehouse;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class WarehouseVoucherPdfDocument : IDocument
{
    private readonly WarehouseVoucherDto _voucher;
    private readonly CompanySettings? _companySettings;

    public WarehouseVoucherPdfDocument(WarehouseVoucherDto voucher, CompanySettings? companySettings)
    {
        _voucher = voucher;
        _companySettings = companySettings;
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
            page.MarginLeft(1.5f, Unit.Centimetre);
            page.MarginRight(1.5f, Unit.Centimetre);
            page.PageColor(Colors.White);

            page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10f).FontColor(Colors.Black).LineHeight(1.25f));

            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container)
    {
        bool isCircular99 = (_voucher.VoucherDate.Year >= 2026) || (_companySettings?.AccountingRegime == "TT99_2025");

        container.Column(col =>
        {
            // 1. Enterprise Info & Form Standard Header
            col.Item().Row(row =>
            {
                // Left: Company Info
                row.RelativeItem(7).Column(c =>
                {
                    c.Item().Text(_companySettings?.CompanyName ?? "TỔNG KHO THIẾT BỊ VỆ SINH LACASA").Bold().FontSize(10f);
                    c.Item().Text($"Địa chỉ: {_companySettings?.Address ?? "TP. Cần Thơ, Việt Nam"}").FontSize(8.5f);
                    c.Item().Text($"Mã số thuế: {_companySettings?.TaxCode ?? "0316899999"} | Hotline: {_companySettings?.Phone ?? "0909 123 456"}").FontSize(8.5f);
                });

                // Right: Regulatory Form Standard
                row.RelativeItem(5).AlignRight().Column(c =>
                {
                    c.Item().AlignRight().Text(GetFormTemplateCode(_voucher.VoucherType)).Bold().FontSize(10f);
                    if (isCircular99)
                    {
                        c.Item().AlignRight().Text("Ban hành theo Thông tư số 99/2025/TT-BTC").Italic().FontSize(8f);
                        c.Item().AlignRight().Text("Ngày 27/10/2025 của Bộ Tài chính").Italic().FontSize(8f);
                    }
                    else
                    {
                        c.Item().AlignRight().Text("Ban hành theo Thông tư số 200/2014/TT-BTC").Italic().FontSize(8f);
                        c.Item().AlignRight().Text("Ngày 22/12/2014 của Bộ Tài chính").Italic().FontSize(8f);
                    }
                });
            });

            // 2. Voucher Title & Metadata
            col.Item().PaddingTop(10).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text(GetVoucherTitle(_voucher.VoucherType)).Bold().FontSize(15f);
                c.Item().AlignCenter().Text($"Ngày {_voucher.VoucherDate:dd} tháng {_voucher.VoucherDate:MM} năm {_voucher.VoucherDate:yyyy}").Italic().FontSize(9.5f);
                c.Item().AlignCenter().Text(t =>
                {
                    t.Span("Số: ").FontSize(9.5f);
                    t.Span(_voucher.VoucherNumber).Bold().FontSize(9.5f);
                });
            });

            // 3. Voucher Context Information
            col.Item().PaddingTop(8).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.AutoItem().Text("- Họ tên người giao/nhận: ").SemiBold();
                    r.RelativeItem().Text(_voucher.DelivererOrReceiverName ?? _voucher.PartnerName ?? "....................................................................................").Bold();
                });
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.AutoItem().Text("- Đơn vị / Đối tác: ");
                    r.RelativeItem().Text(_voucher.PartnerName ?? "....................................................................................");
                });
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.AutoItem().Text("- Theo chứng từ số: ");
                    r.RelativeItem().Text(_voucher.SourceReference ?? "....................................................................................");
                });
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.AutoItem().Text("- Lý do nhập/xuất kho: ");
                    r.RelativeItem().Text(_voucher.Reason);
                });
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.AutoItem().Text("- Kho hàng: ");
                    r.RelativeItem().Text($"{_voucher.WarehouseName} ({_voucher.WarehouseCode})").SemiBold();
                });
            });

            // 4. Line Items Table
            col.Item().PaddingTop(8).Table(table =>
            {
                table.ColumnsDefinition(cols =>
                {
                    cols.ConstantColumn(28); // STT
                    cols.RelativeColumn(5);  // Tên hàng
                    cols.RelativeColumn(2);  // Mã
                    cols.RelativeColumn(1);  // ĐVT
                    cols.RelativeColumn(1.3f); // SL CT
                    cols.RelativeColumn(1.3f); // SL Thực
                    cols.RelativeColumn(2f); // Đơn giá
                    cols.RelativeColumn(2.2f); // Thành tiền
                });

                // Header
                table.Header(h =>
                {
                    h.Cell().RowSpan(2).Element(HeaderCellStyle).AlignCenter().AlignMiddle().Text("STT").Bold();
                    h.Cell().RowSpan(2).Element(HeaderCellStyle).AlignLeft().AlignMiddle().Text("Tên, nhãn hiệu, quy cách vật tư").Bold();
                    h.Cell().RowSpan(2).Element(HeaderCellStyle).AlignCenter().AlignMiddle().Text("Mã số").Bold();
                    h.Cell().RowSpan(2).Element(HeaderCellStyle).AlignCenter().AlignMiddle().Text("ĐVT").Bold();
                    h.Cell().ColumnSpan(2).Element(HeaderCellStyle).AlignCenter().Text("Số lượng").Bold();
                    h.Cell().RowSpan(2).Element(HeaderCellStyle).AlignRight().AlignMiddle().Text("Đơn giá (đ)").Bold();
                    h.Cell().RowSpan(2).Element(HeaderCellStyle).AlignRight().AlignMiddle().Text("Thành tiền (đ)").Bold();

                    // Sub-header for Qty
                    h.Cell().Element(HeaderCellStyle).AlignCenter().Text("Chứng từ").FontSize(8f);
                    h.Cell().Element(HeaderCellStyle).AlignCenter().Text("Thực xuất").FontSize(8f);
                });

                int stt = 1;
                foreach (var item in _voucher.Items)
                {
                    table.Cell().Element(BodyCellStyle).AlignCenter().Text(stt++.ToString());
                    table.Cell().Element(BodyCellStyle).AlignLeft().Column(c =>
                    {
                        c.Item().Text(item.ProductName).SemiBold();
                        if (!string.IsNullOrWhiteSpace(item.Notes))
                        {
                            c.Item().Text($"Ghi chú: {item.Notes}").Italic().FontSize(8f);
                        }
                    });
                    table.Cell().Element(BodyCellStyle).AlignCenter().Text(item.ProductCode).FontSize(8.5f);
                    table.Cell().Element(BodyCellStyle).AlignCenter().Text(item.UnitName);
                    table.Cell().Element(BodyCellStyle).AlignRight().Text((item.DocumentQuantity > 0 ? item.DocumentQuantity : item.ActualQuantity).ToString("N0"));
                    table.Cell().Element(BodyCellStyle).AlignRight().Text(item.ActualQuantity.ToString("N0")).Bold();
                    table.Cell().Element(BodyCellStyle).AlignRight().Text(item.UnitPrice.ToString("N0"));
                    table.Cell().Element(BodyCellStyle).AlignRight().Text(item.LineTotal.ToString("N0")).Bold();
                }

                // Summary Row
                table.Cell().ColumnSpan(4).Element(SummaryCellStyle).AlignCenter().Text("CỘNG").Bold();
                table.Cell().Element(SummaryCellStyle).AlignRight().Text("-");
                table.Cell().Element(SummaryCellStyle).AlignRight().Text(_voucher.TotalQuantity.ToString("N0")).Bold();
                table.Cell().Element(SummaryCellStyle).AlignRight().Text("-");
                table.Cell().Element(SummaryCellStyle).AlignRight().Text($"{_voucher.TotalAmount:N0} đ").Bold();
            });

            // 5. Amount in Words
            col.Item().PaddingTop(6).Row(r =>
            {
                r.AutoItem().Text("- Tổng số tiền (viết bằng chữ): ").Italic();
                r.RelativeItem().Text(VietnameseNumberToWords.ConvertToWords(_voucher.TotalAmount)).Bold().Italic();
            });

            // 6. Signatures (Strictly Avoid Page Split)
            col.Item().ShowEntire().PaddingTop(12).Row(row =>
            {
                // Người lập biểu
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("Người lập biểu").Bold();
                    c.Item().AlignCenter().Text("(Ký, họ tên)").Italic().FontSize(8f);
                    c.Item().Height(45);
                    c.Item().AlignCenter().Text(_voucher.CreatedByName ?? "Trần Văn Tuấn").SemiBold();
                });

                // Người giao / nhận
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("Người giao / nhận").Bold();
                    c.Item().AlignCenter().Text("(Ký, họ tên)").Italic().FontSize(8f);
                    c.Item().Height(45);
                    c.Item().AlignCenter().Text(_voucher.DelivererOrReceiverName ?? "............................").SemiBold();
                });

                // Thủ kho
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("Thủ kho").Bold();
                    c.Item().AlignCenter().Text("(Ký, họ tên)").Italic().FontSize(8f);
                    c.Item().Height(45);
                    c.Item().AlignCenter().Text(_voucher.WarehouseKeeperName ?? "Trần Văn Tuấn").SemiBold();
                });

                // Giám đốc / Quản lý
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("Giám đốc / Quản lý").Bold();
                    c.Item().AlignCenter().Text("(Ký, họ tên, đóng dấu)").Italic().FontSize(8f);
                    c.Item().Height(45);
                    c.Item().AlignCenter().Text(_voucher.ApprovedByName ?? "Quản Lý 01").SemiBold();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Hệ thống quản trị TradeFlow — In lúc {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8f).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Trang ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    private static IContainer HeaderCellStyle(IContainer c) =>
        c.Border(0.5f).BorderColor(Colors.Grey.Medium).Background(Colors.Grey.Lighten4).PaddingVertical(3).PaddingHorizontal(4);

    private static IContainer BodyCellStyle(IContainer c) =>
        c.Border(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).PaddingHorizontal(4);

    private static IContainer SummaryCellStyle(IContainer c) =>
        c.Border(0.5f).BorderColor(Colors.Grey.Medium).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4);

    private static string GetFormTemplateCode(WarehouseVoucherType type) => type switch
    {
        WarehouseVoucherType.InboundReceipt => "Mẫu số 01 - VT",
        WarehouseVoucherType.OutboundIssue => "Mẫu số 02 - VT",
        WarehouseVoucherType.InspectionRecord => "Mẫu số 03 - VT",
        WarehouseVoucherType.StocktakeReport => "Mẫu số 05 - VT",
        _ => "Biểu mẫu kế toán VT"
    };

    private static string GetVoucherTitle(WarehouseVoucherType type) => type switch
    {
        WarehouseVoucherType.InboundReceipt => "PHIẾU NHẬP KHO",
        WarehouseVoucherType.OutboundIssue => "PHIẾU XUẤT KHO",
        WarehouseVoucherType.InspectionRecord => "BIÊN BẢN KIỂM NGHIỆM VẬT TƯ HÀNG HÓA",
        WarehouseVoucherType.StocktakeReport => "BIÊN BẢN KIỂM KÊ VẬT TƯ HÀNG HÓA",
        _ => "PHIẾU KHO"
    };
}
