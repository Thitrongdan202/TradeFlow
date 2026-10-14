using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Models.Contracts;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class ContractPdfDocument : IDocument
{
    private static readonly System.Globalization.CultureInfo ViCulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

    private readonly ContractDto _model;
    private readonly ContractPartyASettingsDto? _partyASettings;

    public ContractPdfDocument(ContractDto model, ContractPartyASettingsDto? partyASettings)
    {
        _model = model;
        _partyASettings = partyASettings;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginTop(1.8f, Unit.Centimetre);
            page.MarginBottom(1.8f, Unit.Centimetre);
            page.MarginLeft(2.2f, Unit.Centimetre);
            page.MarginRight(1.8f, Unit.Centimetre);
            page.PageColor(Colors.White);

            // Default font: Times New Roman standard per Vietnamese Clerical Practice
            page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10.5f).FontColor(Colors.Black).LineHeight(1.25f));

            page.Header().Element(ComposeHeader);
            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(11.5f).Bold();
                    c.Item().PaddingTop(1).Text("Độc lập - Tự do - Hạnh phúc").FontSize(11f).Bold();
                    c.Item().PaddingTop(3).AlignCenter().Width(130).LineHorizontal(0.75f).LineColor(Colors.Black);
                    c.Item().PaddingTop(6).Text($"{_model.ContractPlace}, ngày {_model.ContractDate:dd} tháng {_model.ContractDate:MM} năm {_model.ContractDate:yyyy}").FontSize(10f).Italic();
                });
            });

            col.Item().PaddingTop(10).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text(_model.Title.ToUpper()).FontSize(14).Bold();
                c.Item().AlignCenter().PaddingTop(2).Text($"Số: {_model.ContractNumber}").FontSize(10.5f).Bold();
            });

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(10).Column(col =>
        {
            col.Spacing(6);

            // 1. CĂN CỨ PHÁP LÝ
            col.Item().Column(c =>
            {
                c.Item().Text("• Căn cứ Bộ luật Dân sự số 91/2015/QH13 được Quốc hội nước CHXHCN Việt Nam thông qua ngày 24/11/2015;").Italic().FontSize(9.5f);
                c.Item().Text("• Căn cứ Luật Thương mại số 36/2005/QH11 được Quốc hội nước CHXHCN Việt Nam thông qua ngày 14/06/2005;").Italic().FontSize(9.5f);
                c.Item().Text("• Căn cứ Luật Giao dịch điện tử số 20/2023/QH15 có hiệu lực thi hành từ ngày 01/07/2024;").Italic().FontSize(9.5f);
                c.Item().Text("• Căn cứ vào nhu cầu và năng lực thực tế của hai bên.").Italic().FontSize(9.5f);
            });

            col.Item().PaddingTop(4).Text($"Hôm nay, ngày {_model.ContractDate:dd/MM/yyyy}, tại {_model.ContractPlace}, chúng tôi gồm các bên dưới đây:").Italic();

            // 2. THÔNG TIN BÊN A
            col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(8).Column(c =>
            {
                c.Item().Text("BÊN A (BÊN BÁN / ĐƠN VỊ CUNG CẤP):").Bold().FontSize(11f);
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("Tên đơn vị: ").Bold(); t.Span(_model.PartyAName); });
                });
                if (!string.IsNullOrWhiteSpace(_model.PartyATaxCode))
                {
                    c.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Mã số thuế: ").Bold(); t.Span(_model.PartyATaxCode); });
                        if (!string.IsNullOrWhiteSpace(_model.PartyAPhone))
                            r.RelativeItem().Text(t => { t.Span("Điện thoại: ").Bold(); t.Span(_model.PartyAPhone); });
                    });
                }
                if (!string.IsNullOrWhiteSpace(_model.PartyAAddress))
                {
                    c.Item().PaddingTop(1).Text(t => { t.Span("Địa chỉ trụ sở: ").Bold(); t.Span(_model.PartyAAddress); });
                }
                c.Item().PaddingTop(1).Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("Người đại diện: ").Bold(); t.Span(_model.PartyARepresentative ?? "Nguyễn Văn An"); });
                    r.RelativeItem().Text(t => { t.Span("Chức vụ: ").Bold(); t.Span(_model.PartyAPosition ?? "Giám đốc"); });
                });
                if (!string.IsNullOrWhiteSpace(_model.PartyABankAccount))
                {
                    c.Item().PaddingTop(1).Text(t =>
                    {
                        t.Span("Tài khoản ngân hàng: ").Bold();
                        t.Span($"{_model.PartyABankAccount} tại {_model.PartyABankName} {_model.PartyABankBranch}");
                    });
                }
            });

            // 3. THÔNG TIN BÊN B
            col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(8).Column(c =>
            {
                c.Item().Text("BÊN B (BÊN MUA / KHÁCH HÀNG / ĐỐI TÁC):").Bold().FontSize(11f);
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("Tên đối tác: ").Bold(); t.Span(_model.PartyBName); });
                });

                c.Item().PaddingTop(1).Row(r =>
                {
                    if (!string.IsNullOrWhiteSpace(_model.PartyBTaxCode))
                        r.RelativeItem().Text(t => { t.Span("Mã số thuế: ").Bold(); t.Span(_model.PartyBTaxCode); });
                    else if (!string.IsNullOrWhiteSpace(_model.PartyBIdCardNumber))
                        r.RelativeItem().Text(t => { t.Span("Số CCCD: ").Bold(); t.Span(_model.PartyBIdCardNumber); });

                    if (!string.IsNullOrWhiteSpace(_model.PartyBPhone))
                        r.RelativeItem().Text(t => { t.Span("Điện thoại: ").Bold(); t.Span(_model.PartyBPhone); });
                });

                if (!string.IsNullOrWhiteSpace(_model.PartyBAddress))
                {
                    c.Item().PaddingTop(1).Text(t => { t.Span("Địa chỉ: ").Bold(); t.Span(_model.PartyBAddress); });
                }

                if (!string.IsNullOrWhiteSpace(_model.PartyBRepresentative))
                {
                    c.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Đại diện: ").Bold(); t.Span(_model.PartyBRepresentative); });
                        if (!string.IsNullOrWhiteSpace(_model.PartyBPosition))
                            r.RelativeItem().Text(t => { t.Span("Chức vụ: ").Bold(); t.Span(_model.PartyBPosition); });
                    });
                }
            });

            col.Item().PaddingTop(4).Text("Hai bên cùng thỏa thuận và thống nhất ký kết hợp đồng với các điều khoản cụ thể sau đây:").Italic();

            // 4. DANH MỤC HÀNG HÓA NẾU CÓ
            if (_model.Items != null && _model.Items.Count > 0)
            {
                col.Item().PaddingTop(4).Element(ComposeItemsTable);
            }

            // 5. GIÁ TRỊ TÀI CHÍNH
            col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("Tổng giá trị hợp đồng: ").Bold(); t.Span($"{_model.TotalAmount:N0} {_model.CurrencyCode}").Bold().FontSize(11.5f); });
                    if (_model.DepositAmount > 0)
                    {
                        r.RelativeItem().Text(t => { t.Span("Tiền đặt cọc: ").Bold(); t.Span($"{_model.DepositAmount:N0} {_model.CurrencyCode}"); });
                    }
                });
                if (!string.IsNullOrWhiteSpace(_model.AmountInWords))
                {
                    c.Item().PaddingTop(2).Text(t => { t.Span("Bằng chữ: ").Bold(); t.Span(_model.AmountInWords).Italic(); });
                }
            });

            // 6. CÁC ĐIỀU KHOẢN HỢP ĐỒNG
            if (_model.Clauses != null && _model.Clauses.Count > 0)
            {
                col.Item().PaddingTop(4).Column(clauseCol =>
                {
                    clauseCol.Spacing(6);
                    foreach (var clause in _model.Clauses)
                    {
                        clauseCol.Item().Column(c =>
                        {
                            c.Item().Text($"{clause.ClauseNumber}: {clause.Title}").Bold().FontSize(11f);
                            c.Item().PaddingTop(1).Text(clause.Content).FontSize(10f);
                        });
                    }
                });
            }

            // 7. CÁC ĐIỀU KHOẢN BỔ SUNG NẾU CÓ
            if (!string.IsNullOrWhiteSpace(_model.PaymentTerms))
            {
                col.Item().Column(c =>
                {
                    c.Item().Text("Điều khoản thanh toán bổ sung:").Bold();
                    c.Item().Text(_model.PaymentTerms);
                });
            }
            if (!string.IsNullOrWhiteSpace(_model.DeliveryTerms))
            {
                col.Item().Column(c =>
                {
                    c.Item().Text("Điều kiện giao nhận & Địa điểm:").Bold();
                    c.Item().Text(_model.DeliveryTerms);
                });
            }
            if (!string.IsNullOrWhiteSpace(_model.WarrantyTerms))
            {
                col.Item().Column(c =>
                {
                    c.Item().Text("Chính sách bảo hành & Hỗ trợ kỹ thuật:").Bold();
                    c.Item().Text(_model.WarrantyTerms);
                });
            }

            // 8. ĐIỀU KHOẢN CHUNG & HIỆU LỰC
            col.Item().PaddingTop(6).Column(c =>
            {
                c.Item().Text("Hợp đồng này có hiệu lực kể từ ngày ký. Hợp đồng được lập thành 02 (hai) bản có giá trị pháp lý như nhau, mỗi bên giữ 01 (một) bản để cùng thực hiện.").Italic();
            });

            // 9. CHỮ KÝ HAI BÊN
            col.Item().PaddingTop(12).Row(row =>
            {
                // Bên A
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("ĐẠI DIỆN BÊN A").Bold().FontSize(11f);
                    c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên và đóng dấu)").FontSize(8.5f).Italic();

                    if (_model.PartyASignedAt.HasValue)
                    {
                        c.Item().PaddingTop(6).Element(RenderSignatureBlockPartyA);
                    }
                    else
                    {
                        c.Item().Height(60);
                        if (!string.IsNullOrWhiteSpace(_model.PartyARepresentative))
                            c.Item().AlignCenter().Text(_model.PartyARepresentative).Bold();
                    }
                });

                // Bên B
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("ĐẠI DIỆN BÊN B").Bold().FontSize(11f);
                    c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên và đóng dấu)").FontSize(8.5f).Italic();

                    if (_model.PartyBSignedAt.HasValue)
                    {
                        c.Item().PaddingTop(6).Element(RenderSignatureBlockPartyB);
                    }
                    else
                    {
                        c.Item().Height(60);
                        if (!string.IsNullOrWhiteSpace(_model.PartyBRepresentative))
                            c.Item().AlignCenter().Text(_model.PartyBRepresentative).Bold();
                        else if (!string.IsNullOrWhiteSpace(_model.PartyBName))
                            c.Item().AlignCenter().Text(_model.PartyBName).Bold();
                    }
                });
            });
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28); // STT
                cols.ConstantColumn(70); // Mã
                cols.RelativeColumn();   // Tên thiết bị
                cols.ConstantColumn(40); // ĐVT
                cols.ConstantColumn(40); // SL
                cols.ConstantColumn(75); // Đơn giá
                cols.ConstantColumn(80); // Thành tiền
            });

            table.Header(header =>
            {
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("STT").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("Mã hàng").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("Tên hàng hóa, quy cách").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("ĐVT").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("SL").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("Đơn giá").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten3).Padding(3).AlignCenter().Text("Thành tiền").Bold().FontSize(9.5f);
            });

            int index = 1;
            foreach (var item in _model.Items)
            {
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3).AlignCenter().Text(index++.ToString()).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3).Text(item.ProductCode).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3).Text(item.ProductName).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3).AlignCenter().Text(item.Unit).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3).AlignRight().Text($"{item.Quantity:N0}").FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3).AlignRight().Text($"{item.UnitPrice:N0}").FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(3).AlignRight().Text($"{item.LineTotal:N0}").FontSize(9f);
            }
        });
    }

    private void RenderSignatureBlockPartyA(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten5).Padding(6).Column(c =>
        {
            c.Item().AlignCenter().Text("XÁC NHẬN CHỮ KÝ ĐIỆN TỬ").Bold().FontSize(9f).FontColor(Colors.Blue.Darken3);
            if (!string.IsNullOrWhiteSpace(_model.PartyASignatureData) && _model.PartyASignatureData.StartsWith("data:image"))
            {
                try
                {
                    var base64 = _model.PartyASignatureData.Substring(_model.PartyASignatureData.IndexOf(',') + 1);
                    var bytes = Convert.FromBase64String(base64);
                    c.Item().AlignCenter().Height(40).Image(bytes).FitArea();
                }
                catch { }
            }
            c.Item().AlignCenter().Text($"Ký bởi: {_model.PartyASignedBy ?? _model.PartyARepresentative}").Bold().FontSize(9.5f);
            c.Item().AlignCenter().Text($"Ngày ký: {_model.PartyASignedAt:dd/MM/yyyy HH:mm} (UTC)").FontSize(8f).Italic();
        });
    }

    private void RenderSignatureBlockPartyB(IContainer container)
    {
        container.Border(1).BorderColor(Colors.Green.Darken2).Background(Colors.Green.Lighten5).Padding(6).Column(c =>
        {
            c.Item().AlignCenter().Text("XÁC NHẬN CHỮ KÝ ĐIỆN TỬ").Bold().FontSize(9f).FontColor(Colors.Green.Darken3);
            if (!string.IsNullOrWhiteSpace(_model.PartyBSignatureData) && _model.PartyBSignatureData.StartsWith("data:image"))
            {
                try
                {
                    var base64 = _model.PartyBSignatureData.Substring(_model.PartyBSignatureData.IndexOf(',') + 1);
                    var bytes = Convert.FromBase64String(base64);
                    c.Item().AlignCenter().Height(40).Image(bytes).FitArea();
                }
                catch { }
            }
            c.Item().AlignCenter().Text($"Ký bởi: {_model.PartyBSignedBy ?? _model.PartyBRepresentative ?? _model.PartyBName}").Bold().FontSize(9.5f);
            c.Item().AlignCenter().Text($"Ngày ký: {_model.PartyBSignedAt:dd/MM/yyyy HH:mm} (UTC)").FontSize(8f).Italic();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem(3).Column(c =>
                {
                    c.Item().Text("Tài liệu được quản lý và lưu trữ trên Hệ thống Quản trị Doanh nghiệp TradeFlow ERP.").FontSize(8f).Italic();
                    if (!string.IsNullOrWhiteSpace(_model.DocumentHash))
                    {
                        var shortHash = _model.DocumentHash.Length > 32 ? _model.DocumentHash[..32] + "..." : _model.DocumentHash;
                        c.Item().Text($"Mã kiểm tra toàn vẹn SHA-256: {shortHash}").FontSize(7.5f).FontFamily(Fonts.Consolas);
                    }
                });

                row.RelativeItem(1).AlignRight().Text(text =>
                {
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }
}
