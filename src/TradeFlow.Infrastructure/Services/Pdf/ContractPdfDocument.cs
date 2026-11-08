using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Enums;

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
        bool isB2C = _model.PartyBType == ContractPartyBType.Individual || 
                     (_model.TemplateCode != null && _model.TemplateCode.Equals("B2C_RETAIL", StringComparison.OrdinalIgnoreCase));

        // Chuẩn hóa theo Nghị định 30/2020/NĐ-CP: Phông Times New Roman 13pt, giãn dòng 1.25
        float baseFontSize = 13f;

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginTop(2.0f, Unit.Centimetre);
            page.MarginBottom(2.0f, Unit.Centimetre);
            page.MarginLeft(3.0f, Unit.Centimetre);
            page.MarginRight(2.0f, Unit.Centimetre);
            page.PageColor(Colors.White);

            // Phông chữ Times New Roman quy chuẩn văn bản pháp lý Việt Nam (NĐ 30/2020/NĐ-CP)
            page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(baseFontSize).FontColor(Colors.Black).LineHeight(1.25f));

            // Trang đầu chứa đầy đủ Quốc hiệu, Tiêu ngữ, Tên HĐ nằm trong Content. Trang sau KHÔNG lặp lại.
            page.Content().Element(c => ComposeContent(c, isB2C, baseFontSize));
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container, bool isB2C, float baseFontSize)
    {
        container.Column(col =>
        {
            col.Spacing(6);

            // ==========================================
            // TRANG 1: QUỐC HIỆU, TIÊU NGỮ, TÊN HỢP ĐỒNG (CHỈ XUẤT HIỆN TRANG ĐẦU TIÊN)
            // ==========================================
            col.Item().Column(headerCol =>
            {
                headerCol.Item().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(13f).Bold();
                    c.Item().PaddingTop(3).AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").FontSize(13f).Bold();
                    c.Item().PaddingTop(4).AlignCenter().Width(140).LineHorizontal(1.5f).LineColor(Colors.Black);
                    c.Item().PaddingTop(5).AlignCenter().Text($"{(!string.IsNullOrWhiteSpace(_model.ContractPlace) ? _model.ContractPlace : "Cần Thơ")}, ngày {_model.ContractDate:dd} tháng {_model.ContractDate:MM} năm {_model.ContractDate:yyyy}").FontSize(13f).Italic();
                });

                headerCol.Item().PaddingTop(12).AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text(_model.Title.ToUpper()).FontSize(16f).Bold();
                    c.Item().AlignCenter().PaddingTop(3).Text($"Số: {_model.ContractNumber}").FontSize(13f).Bold();

                    if (_model.Status == ContractStatus.Draft)
                    {
                        c.Item().AlignCenter().PaddingTop(2).Text("(BẢN DỰ THẢO - CHƯA CÓ HIỆU LỰC GIAO KẾT)").FontSize(10f).Italic().FontColor(Colors.Red.Darken2);
                    }
                });

                headerCol.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
            });

            // ==========================================
            // CĂN CỨ PHÁP LÝ (CHỈ TRANG ĐẦU TIÊN)
            // ==========================================
            col.Item().PaddingTop(4).Column(c =>
            {
                c.Item().Text("• Căn cứ Bộ luật Dân sự số 91/2015/QH13 được Quốc hội nước CHXHCN Việt Nam thông qua ngày 24/11/2015;").Italic().FontSize(baseFontSize - 1f);
                if (isB2C)
                {
                    c.Item().Text("• Căn cứ Luật Bảo vệ quyền lợi người tiêu dùng số 19/2023/QH15 có hiệu lực từ ngày 01/07/2024;").Italic().FontSize(baseFontSize - 1f);
                    c.Item().Text("• Căn cứ Nghị định số 55/2024/NĐ-CP quy định chi tiết một số điều của Luật Bảo vệ quyền lợi người tiêu dùng;").Italic().FontSize(baseFontSize - 1f);
                }
                else
                {
                    c.Item().Text("• Căn cứ Luật Thương mại số 36/2005/QH11 được Quốc hội nước CHXHCN Việt Nam thông qua ngày 14/06/2005;").Italic().FontSize(baseFontSize - 1f);
                    c.Item().Text("• Căn cứ Luật Doanh nghiệp số 59/2020/QH14 được Quốc hội nước CHXHCN Việt Nam thông qua ngày 17/06/2020;").Italic().FontSize(baseFontSize - 1f);
                }
                c.Item().Text("• Căn cứ Luật Giao dịch điện tử số 20/2023/QH15 có hiệu lực thi hành từ ngày 01/07/2024;").Italic().FontSize(baseFontSize - 1f);
                c.Item().Text("• Căn cứ vào nhu cầu và sự thỏa thuận tự nguyện của các bên.").Italic().FontSize(baseFontSize - 1f);
            });

            col.Item().PaddingTop(4).Text($"Hôm nay, ngày {_model.ContractDate:dd/MM/yyyy}, tại {(!string.IsNullOrWhiteSpace(_model.ContractPlace) ? _model.ContractPlace : "Cần Thơ")}, chúng tôi gồm các bên:").Italic();

            // ==========================================
            // THÔNG TIN BÊN A (BÊN BÁN)
            // ==========================================
            col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(8).Column(c =>
            {
                c.Item().Text("BÊN A (BÊN BÁN / ĐƠN VỊ CUNG CẤP):").Bold().FontSize(baseFontSize + 0.5f);
                c.Item().PaddingTop(2).Text(t => { t.Span("Tên đơn vị: ").Bold(); t.Span(_model.PartyAName); });
                
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
                c.Item().PaddingTop(1).Text(t => { t.Span("Căn cứ đại diện: ").Bold(); t.Span(_model.PartyAAuthority ?? "Đại diện theo pháp luật"); });

                if (!string.IsNullOrWhiteSpace(_model.PartyABankAccount))
                {
                    c.Item().PaddingTop(1).Text(t =>
                    {
                        t.Span("Tài khoản ngân hàng: ").Bold();
                        t.Span($"{_model.PartyABankAccount} tại {_model.PartyABankName} {_model.PartyABankBranch}");
                    });
                }
            });

            // ==========================================
            // THÔNG TIN BÊN B (BÊN MUA / KHÁCH HÀNG)
            // ==========================================
            col.Item().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Medium).Padding(8).Column(c =>
            {
                string partyBHeader = isB2C ? "BÊN B (BÊN MUA / KHÁCH HÀNG CÁ NHÂN):" : "BÊN B (BÊN MUA / ĐỐI TÁC THƯƠNG MẠI):";
                c.Item().Text(partyBHeader).Bold().FontSize(baseFontSize + 0.5f);
                c.Item().PaddingTop(2).Text(t => { t.Span(isB2C ? "Họ và tên khách hàng: " : "Tên đơn vị: ").Bold(); t.Span(_model.PartyBName); });

                c.Item().PaddingTop(1).Row(r =>
                {
                    if (!string.IsNullOrWhiteSpace(_model.PartyBTaxCode))
                        r.RelativeItem().Text(t => { t.Span("Mã số thuế: ").Bold(); t.Span(_model.PartyBTaxCode); });
                    else if (!string.IsNullOrWhiteSpace(_model.PartyBIdCardNumber))
                        r.RelativeItem().Text(t => { t.Span("Số CCCD/Định danh: ").Bold(); t.Span(_model.PartyBIdCardNumber); });

                    if (!string.IsNullOrWhiteSpace(_model.PartyBPhone))
                        r.RelativeItem().Text(t => { t.Span("Điện thoại: ").Bold(); t.Span(_model.PartyBPhone); });
                });

                if (!string.IsNullOrWhiteSpace(_model.PartyBAddress))
                {
                    c.Item().PaddingTop(1).Text(t => { t.Span(isB2C ? "Nơi cư trú / Địa chỉ: " : "Địa chỉ trụ sở: ").Bold(); t.Span(_model.PartyBAddress); });
                }

                if (!isB2C && !string.IsNullOrWhiteSpace(_model.PartyBRepresentative))
                {
                    c.Item().PaddingTop(1).Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Người đại diện: ").Bold(); t.Span(_model.PartyBRepresentative); });
                        if (!string.IsNullOrWhiteSpace(_model.PartyBPosition))
                            r.RelativeItem().Text(t => { t.Span("Chức vụ: ").Bold(); t.Span(_model.PartyBPosition); });
                    });
                    c.Item().PaddingTop(1).Text(t => { t.Span("Căn cứ đại diện: ").Bold(); t.Span(_model.PartyBAuthority ?? "Đại diện theo pháp luật hoặc Ủy quyền hợp pháp"); });
                }
            });

            col.Item().PaddingTop(4).Text("Hai bên cùng thỏa thuận và thống nhất ký kết hợp đồng với các điều khoản cụ thể sau đây:").Italic();

            // ==========================================
            // BẢNG KÊ HÀNG HÓA NẾU CÓ (TỰ ĐỘNG PHÂN TRANG VÀ LẶP LẠI HEADER NẾU DÀI)
            // ==========================================
            if (_model.Items != null && _model.Items.Count > 0)
            {
                col.Item().PaddingTop(4).Element(ComposeItemsTable);
            }

            // ==========================================
            // TỔNG KẾT TÀI CHÍNH
            // ==========================================
            col.Item().ShowEntire().PaddingTop(4).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(6).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("Tổng giá trị hợp đồng: ").Bold(); t.Span($"{_model.TotalAmount:N0} {_model.CurrencyCode}").Bold().FontSize(baseFontSize + 1f); });
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

            // ==========================================
            // CÁC ĐIỀU KHOẢN HỢP ĐỒNG (SHOW ENTIRE TỪNG ĐIỀU ĐỂ KHÔNG BỊ CẮT CHỮ)
            // ==========================================
            if (_model.Clauses != null && _model.Clauses.Count > 0)
            {
                col.Item().PaddingTop(4).Column(clauseCol =>
                {
                    clauseCol.Spacing(6);
                    foreach (var clause in _model.Clauses)
                    {
                        clauseCol.Item().ShowEntire().Column(c =>
                        {
                            c.Item().Text($"{clause.ClauseNumber}. {clause.Title}").Bold().FontSize(baseFontSize + 0.5f);
                            c.Item().PaddingTop(1).Text(clause.Content).FontSize(baseFontSize);
                        });
                    }
                });
            }

            // Điều khoản bổ sung nếu có
            if (!string.IsNullOrWhiteSpace(_model.PaymentTerms))
            {
                col.Item().ShowEntire().Column(c =>
                {
                    c.Item().Text("Điều khoản thanh toán bổ sung:").Bold();
                    c.Item().Text(_model.PaymentTerms);
                });
            }
            if (!string.IsNullOrWhiteSpace(_model.DeliveryTerms))
            {
                col.Item().ShowEntire().Column(c =>
                {
                    c.Item().Text("Điều kiện giao nhận & Địa điểm:").Bold();
                    c.Item().Text(_model.DeliveryTerms);
                });
            }
            if (!string.IsNullOrWhiteSpace(_model.WarrantyTerms))
            {
                col.Item().ShowEntire().Column(c =>
                {
                    c.Item().Text("Chính sách bảo hành & Hỗ trợ kỹ thuật:").Bold();
                    c.Item().Text(_model.WarrantyTerms);
                });
            }

            // ==========================================
            // HIỆU LỰC VÀ SỐ BẢN HỢP ĐỒNG
            // ==========================================
            int copies = isB2C ? 2 : 4;
            int eachCopies = isB2C ? 1 : 2;
            string copiesText = $"Hợp đồng này có hiệu lực kể từ ngày hai bên hoàn tất việc ký kết hợp pháp. Hợp đồng được lập thành 0{copies} bản chính bằng tiếng Việt có giá trị pháp lý như nhau, Bên A giữ 0{eachCopies} bản, Bên B giữ 0{eachCopies} bản để cùng thực hiện.";

            col.Item().ShowEntire().PaddingTop(6).Column(c =>
            {
                c.Item().Text(copiesText).Italic();
            });

            // ==========================================
            // KHỐI CHỮ KÝ HAI BÊN (SHOW ENTIRE, TRÁNH CẮT TRANG)
            // ==========================================
            col.Item().ShowEntire().PaddingTop(14).Row(row =>
            {
                // Bên A (Doanh nghiệp bán hàng)
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("ĐẠI DIỆN BÊN A").Bold().FontSize(baseFontSize + 0.5f);
                    c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên và đóng dấu nếu có)").FontSize(9f).Italic();

                    if (_model.PartyASignedAt.HasValue)
                    {
                        c.Item().PaddingTop(6).Element(RenderSignatureBlockPartyA);
                    }
                    else
                    {
                        c.Item().Height(65);
                        if (!string.IsNullOrWhiteSpace(_model.PartyARepresentative))
                            c.Item().AlignCenter().Text(_model.PartyARepresentative).Bold();
                    }
                });

                // Bên B (Doanh nghiệp hoặc Cá nhân)
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    string sigTitle = isB2C ? "BÊN MUA / KHÁCH HÀNG" : "ĐẠI DIỆN BÊN B";
                    string sigNote = isB2C ? "(Ký và ghi rõ họ tên)" : "(Ký, ghi rõ họ tên và đóng dấu nếu có)";

                    c.Item().AlignCenter().Text(sigTitle).Bold().FontSize(baseFontSize + 0.5f);
                    c.Item().AlignCenter().Text(sigNote).FontSize(9f).Italic();

                    if (_model.PartyBSignedAt.HasValue)
                    {
                        c.Item().PaddingTop(6).Element(RenderSignatureBlockPartyB);
                    }
                    else
                    {
                        c.Item().Height(65);
                        string partyBNameDisplay = (!isB2C && !string.IsNullOrWhiteSpace(_model.PartyBRepresentative))
                            ? _model.PartyBRepresentative
                            : _model.PartyBName;

                        if (!string.IsNullOrWhiteSpace(partyBNameDisplay))
                            c.Item().AlignCenter().Text(partyBNameDisplay).Bold();
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
                cols.ConstantColumn(26); // STT
                cols.ConstantColumn(65); // Mã hàng
                cols.RelativeColumn();   // Tên thiết bị & quy cách
                cols.ConstantColumn(38); // ĐVT
                cols.ConstantColumn(38); // SL
                cols.ConstantColumn(72); // Đơn giá
                cols.ConstantColumn(76); // Thành tiền
            });

            table.Header(header =>
            {
                header.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text("STT").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text("Mã hàng").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text("Tên hàng hóa, quy cách").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text("ĐVT").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text("SL").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text("Đơn giá (đ)").Bold().FontSize(9.5f);
                header.Cell().Border(0.5f).BorderColor(Colors.Grey.Darken1).Background(Colors.Grey.Lighten4).Padding(3).AlignCenter().Text("Thành tiền (đ)").Bold().FontSize(9.5f);
            });

            int index = 1;
            foreach (var item in _model.Items)
            {
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().Text(index++.ToString()).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().Text(item.ProductCode).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignLeft().Text(item.ProductName).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().Text(item.Unit).FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignCenter().Text($"{item.Quantity:N0}").FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text($"{item.UnitPrice:N0}").FontSize(9f);
                table.Cell().Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3).AlignRight().Text($"{item.LineTotal:N0}").FontSize(9f);
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
            c.Item().AlignCenter().Text($"Ngày ký: {_model.PartyASignedAt:dd/MM/yyyy HH:mm}").FontSize(8f).Italic();
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
            c.Item().AlignCenter().Text($"Ngày ký: {_model.PartyBSignedAt:dd/MM/yyyy HH:mm}").FontSize(8f).Italic();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text($"Hợp đồng số: {_model.ContractNumber}").FontSize(8f).FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.Span("Trang ").FontSize(8f);
                    text.CurrentPageNumber().FontSize(8f);
                    text.Span(" / ").FontSize(8f);
                    text.TotalPages().FontSize(8f);
                });
            });
        });
    }
}
