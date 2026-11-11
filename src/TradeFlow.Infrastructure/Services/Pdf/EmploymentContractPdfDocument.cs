using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class EmploymentContractPdfDocument : IDocument
{
    private readonly EmploymentContractDto _contract;
    private readonly bool _includeSignatures;

    public EmploymentContractPdfDocument(EmploymentContractDto contract, bool includeSignatures = true)
    {
        _contract = contract;
        _includeSignatures = includeSignatures;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        const float baseFontSize = 12.5f;
        const float lineHeight = 1.20f;

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            // Lề chuẩn A4: Trên 18mm, Dưới 16mm, Trái 28mm, Phải 20mm
            page.MarginTop(18f, Unit.Millimetre);
            page.MarginBottom(16f, Unit.Millimetre);
            page.MarginLeft(28f, Unit.Millimetre);
            page.MarginRight(20f, Unit.Millimetre);
            page.PageColor(Colors.White);

            page.DefaultTextStyle(x => x
                .FontFamily("Times New Roman")
                .FontSize(baseFontSize)
                .FontColor(Colors.Black)
                .LineHeight(lineHeight));

            page.Content().Element(c => ComposeContent(c, baseFontSize, lineHeight));
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container, float baseFontSize, float lineHeight)
    {
        var place = !string.IsNullOrWhiteSpace(_contract.ContractPlace) ? _contract.ContractPlace : "TP. Cần Thơ";

        container.Column(col =>
        {
            col.Spacing(2f);

            // ==========================================
            // 1. QUỐC HIỆU, TIÊU NGỮ, ĐỊA ĐIỂM - NGÀY THÁNG, TÊN & SỐ HỢP ĐỒNG (TRANG ĐẦU)
            // ==========================================
            col.Item().Column(headerCol =>
            {
                headerCol.Item().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(12.5f).Bold();
                    c.Item().PaddingTop(1.5f).AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").FontSize(13f).Bold();
                    c.Item().PaddingTop(2f).AlignCenter().Width(145).LineHorizontal(1f).LineColor(Colors.Black);
                    c.Item().PaddingTop(3f).AlignCenter().Text($"{place}, ngày {_contract.ContractDate:dd} tháng {_contract.ContractDate:MM} năm {_contract.ContractDate:yyyy}")
                        .FontSize(12f).Italic();
                });

                headerCol.Item().PaddingTop(6f).AlignCenter().Column(c =>
                {
                    var titleText = string.IsNullOrWhiteSpace(_contract.Title)
                        ? "HỢP ĐỒNG LAO ĐỘNG"
                        : _contract.Title.Trim().ToUpperInvariant();
                    c.Item().AlignCenter().Text(titleText).FontSize(15f).Bold();
                    c.Item().PaddingTop(2f).AlignCenter().Text($"Số: {_contract.ContractNumber}").FontSize(12.5f).Bold();
                });
            });

            // ==========================================
            // 2. CĂN CỨ PHÁP LÝ (IN NGHIÊNG ĐÚNG QUY CHUẨN)
            // ==========================================
            col.Item().PaddingTop(3f).Column(c =>
            {
                c.Spacing(1f);
                c.Item().Text("- Căn cứ Bộ luật Lao động số 45/2019/QH14 ngày 20/11/2019 (Điều 14 về hình thức hợp đồng lao động, Điều 21 về nội dung hợp đồng lao động) và các văn bản sửa đổi, bổ sung;").Italic().FontSize(12f).Justify();
                c.Item().Text("- Căn cứ Nghị định số 145/2020/NĐ-CP của Chính phủ và Thông tư số 10/2020/TT-BLĐTBXH hướng dẫn chi tiết về nội dung hợp đồng lao động;").Italic().FontSize(12f).Justify();
                c.Item().Text("- Căn cứ Luật Giao dịch điện tử số 20/2023/QH15 và các quy định về giao kết hợp đồng lao động điện tử;").Italic().FontSize(12f).Justify();
                c.Item().Text("- Căn cứ nhu cầu sử dụng lao động của Doanh nghiệp và sự thỏa thuận tự nguyện của hai bên.").Italic().FontSize(12f).Justify();
            });

            col.Item().PaddingTop(2f).Text(
                $"Hôm nay, ngày {_contract.ContractDate:dd} tháng {_contract.ContractDate:MM} năm {_contract.ContractDate:yyyy}, tại {place}, chúng tôi gồm có:")
                .FontSize(baseFontSize).Justify();

            // ==========================================
            // 3. BÊN A: NGƯỜI SỬ DỤNG LAO ĐỘNG
            // ==========================================
            col.Item().PaddingTop(2f).ShowEntire().Column(a =>
            {
                a.Spacing(1f);
                a.Item().Text(t =>
                {
                    t.Span("BÊN A (NGƯỜI SỬ DỤNG LAO ĐỘNG): ").Bold();
                    t.Span((_contract.EmployerName ?? string.Empty).ToUpperInvariant()).Bold();
                });

                if (!string.IsNullOrWhiteSpace(_contract.EmployerAddress))
                {
                    a.Item().Text(t => { t.Span("- Địa chỉ trụ sở chính: "); t.Span(_contract.EmployerAddress); });
                }

                a.Item().Row(r =>
                {
                    r.RelativeItem().Text(t => { t.Span("- Mã số thuế / MSDN: "); t.Span(_contract.EmployerTaxCode).Bold(); });
                    if (!string.IsNullOrWhiteSpace(_contract.EmployerPhone))
                        r.RelativeItem().Text(t => { t.Span("Điện thoại: "); t.Span(_contract.EmployerPhone); });
                });

                a.Item().Row(r =>
                {
                    r.RelativeItem(3).Text(t => { t.Span("- Đại diện bởi: "); t.Span(_contract.EmployerRepresentativeName).Bold(); });
                    r.RelativeItem(2).Text(t => { t.Span("Chức vụ: "); t.Span(_contract.EmployerRepresentativePosition); });
                });

                if (!string.IsNullOrWhiteSpace(_contract.EmployerRepresentativeAuthority))
                {
                    a.Item().Text(t => { t.Span("- Căn cứ đại diện: "); t.Span(_contract.EmployerRepresentativeAuthority).Italic(); });
                }
            });

            // ==========================================
            // 4. BÊN B: NGƯỜI LAO ĐỘNG
            // ==========================================
            col.Item().PaddingTop(2f).ShowEntire().Column(b =>
            {
                b.Spacing(1f);
                b.Item().Text(t =>
                {
                    t.Span("BÊN B (NGƯỜI LAO ĐỘNG): ").Bold();
                    t.Span((_contract.EmployeeFullName ?? string.Empty).ToUpperInvariant()).Bold();
                });

                b.Item().Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("- Ngày sinh: ");
                        t.Span(_contract.EmployeeDateOfBirth.HasValue ? _contract.EmployeeDateOfBirth.Value.ToString("dd/MM/yyyy") : "Chưa cập nhật");
                    });
                    r.RelativeItem().Text(t =>
                    {
                        t.Span("Giới tính: ");
                        t.Span(_contract.EmployeeGender ?? "Không xác định");
                    });
                    if (!string.IsNullOrWhiteSpace(_contract.EmployeeCode))
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.Span("Mã NV: ");
                            t.Span(_contract.EmployeeCode).Bold();
                        });
                    }
                });

                b.Item().Row(r =>
                {
                    r.RelativeItem(3).Text(t =>
                    {
                        t.Span("- Số CCCD/Định danh: ");
                        t.Span(_contract.EmployeeNationalId ?? "Chưa cập nhật").Bold();
                    });
                    if (_contract.EmployeeNationalIdIssueDate.HasValue)
                    {
                        r.RelativeItem(2).Text(t =>
                        {
                            t.Span("Ngày cấp: ");
                            t.Span(_contract.EmployeeNationalIdIssueDate.Value.ToString("dd/MM/yyyy"));
                        });
                    }
                });

                if (!string.IsNullOrWhiteSpace(_contract.EmployeeNationalIdIssuePlace))
                {
                    b.Item().Text(t => { t.Span("- Nơi cấp: "); t.Span(_contract.EmployeeNationalIdIssuePlace); });
                }

                b.Item().Text(t => { t.Span("- Địa chỉ thường trú / cư trú: "); t.Span(_contract.EmployeeAddress ?? "Chưa cập nhật"); });

                if (!string.IsNullOrWhiteSpace(_contract.EmployeePhone) || !string.IsNullOrWhiteSpace(_contract.EmployeeEmail))
                {
                    b.Item().Row(r =>
                    {
                        if (!string.IsNullOrWhiteSpace(_contract.EmployeePhone))
                            r.RelativeItem().Text(t => { t.Span("- Điện thoại: "); t.Span(_contract.EmployeePhone); });
                        if (!string.IsNullOrWhiteSpace(_contract.EmployeeEmail))
                            r.RelativeItem().Text(t => { t.Span("Email: "); t.Span(_contract.EmployeeEmail); });
                    });
                }

                if (!string.IsNullOrWhiteSpace(_contract.EmployeeBankInformation))
                {
                    b.Item().Text(t => { t.Span("- Tài khoản nhận lương: "); t.Span(_contract.EmployeeBankInformation); });
                }
            });

            col.Item().PaddingTop(2f).Text(
                "Cùng thỏa thuận ký kết Hợp đồng lao động và cam kết làm đúng những điều khoản sau đây:")
                .Italic().Justify();

            // ==========================================
            // 5. CÁC ĐIỀU KHOẢN HỢP ĐỒNG LAO ĐỘNG (CHỐNG RỚT TIÊU ĐỀ LẺ LOI + TRẢI TRANG TỰ NHIÊN + BẢO TOÀN ĐỊNH DẠNG)
            // ==========================================
            var clausesToRender = _contract.Clauses != null && _contract.Clauses.Any()
                ? _contract.Clauses.OrderBy(c => c.DisplayOrder).ToList()
                : EmploymentContractClauseDefaults.GenerateDefaultClauses(_contract);

            EmploymentContractClauseDefaults.NormalizeArticleNumbersIfDuplicate(clausesToRender);

            col.Item().PaddingTop(2f).Column(clauseCol =>
            {
                foreach (var clause in clausesToRender)
                {
                    var art = (clause.ArticleNumber ?? string.Empty).Trim().TrimEnd('.', ':');
                    var title = (clause.Title ?? string.Empty).Trim();
                    var heading = string.IsNullOrWhiteSpace(art)
                        ? title
                        : (string.IsNullOrWhiteSpace(title) ? $"{art}." : $"{art}. {title}");

                    clauseCol.RenderLegalClause(
                        heading,
                        clause.Content,
                        headingFontSize: 12.5f,
                        bodyFontSize: baseFontSize,
                        lineHeight: lineHeight);
                }
            });

            // ==========================================
            // 6. KHỐI CHỮ KÝ HAI BÊN (SHOW ENTIRE, HỖ TRỢ BẢN CHƯA KÝ & BẢN ĐÃ KÝ SỐ)
            // ==========================================
            bool showEmployeeSig = _includeSignatures && _contract.EmployeeSignedAt.HasValue;
            bool showEmployerSig = _includeSignatures && _contract.EmployerSignedAt.HasValue;

            col.Item().ShowEntire().PaddingTop(8f).Column(sigRoot =>
            {
                sigRoot.Item().Row(sigRow =>
                {
                    // Bên B: Người lao động
                    sigRow.RelativeItem().PaddingHorizontal(4).AlignCenter().Column(sigCol =>
                    {
                        sigCol.Item().AlignCenter().Text("NGƯỜI LAO ĐỘNG").FontSize(12.5f).Bold();
                        sigCol.Item().PaddingTop(1).AlignCenter().Text("(Ký, ghi rõ họ tên)").FontSize(10f).Italic();

                        if (showEmployeeSig)
                        {
                            sigCol.Item().PaddingTop(6).Border(0.75f).BorderColor(Colors.Green.Darken2).Background(Colors.Green.Lighten5).Padding(6).Column(s =>
                            {
                                s.Item().AlignCenter().Text("ĐÃ KÝ SỐ / ĐIỆN TỬ HỢP LỆ").FontSize(9.5f).Bold().FontColor(Colors.Green.Darken3);
                                if (!string.IsNullOrWhiteSpace(_contract.EmployeeSignatureData) && _contract.EmployeeSignatureData.StartsWith("data:image"))
                                {
                                    try
                                    {
                                        var base64 = _contract.EmployeeSignatureData.Substring(_contract.EmployeeSignatureData.IndexOf(',') + 1);
                                        var bytes = Convert.FromBase64String(base64);
                                        s.Item().PaddingVertical(2).AlignCenter().Height(40).Image(bytes).FitArea();
                                    }
                                    catch { }
                                }
                                s.Item().AlignCenter().Text(_contract.EmployeeFullName).Bold().FontSize(11f);
                                s.Item().AlignCenter().Text($"Ký ngày: {_contract.EmployeeSignedAt:dd/MM/yyyy HH:mm}").FontSize(9.5f).Italic().FontColor(Colors.Grey.Darken3);
                            });
                        }
                        else
                        {
                            sigCol.Item().Height(46);
                            sigCol.Item().AlignCenter().Text(_contract.EmployeeFullName).FontSize(12f).Bold();
                        }
                    });

                    // Bên A: Người sử dụng lao động
                    sigRow.RelativeItem().PaddingHorizontal(4).AlignCenter().Column(sigCol =>
                    {
                        sigCol.Item().AlignCenter().Text("NGƯỜI SỬ DỤNG LAO ĐỘNG").FontSize(12.5f).Bold();
                        sigCol.Item().PaddingTop(1).AlignCenter().Text("(Ký, ghi rõ họ tên và đóng dấu)").FontSize(10f).Italic();

                        if (showEmployerSig)
                        {
                            sigCol.Item().PaddingTop(6).Border(0.75f).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten5).Padding(6).Column(s =>
                            {
                                s.Item().AlignCenter().Text("ĐÃ KÝ SỐ / ĐIỆN TỬ & ĐÓNG DẤU").FontSize(9.5f).Bold().FontColor(Colors.Blue.Darken3);
                                if (!string.IsNullOrWhiteSpace(_contract.EmployerSignatureData) && _contract.EmployerSignatureData.StartsWith("data:image"))
                                {
                                    try
                                    {
                                        var base64 = _contract.EmployerSignatureData.Substring(_contract.EmployerSignatureData.IndexOf(',') + 1);
                                        var bytes = Convert.FromBase64String(base64);
                                        s.Item().PaddingVertical(2).AlignCenter().Height(40).Image(bytes).FitArea();
                                    }
                                    catch { }
                                }
                                s.Item().AlignCenter().Text(_contract.EmployerRepresentativeName).Bold().FontSize(11f);
                                s.Item().AlignCenter().Text(_contract.EmployerRepresentativePosition).FontSize(9.5f).Italic();
                                s.Item().AlignCenter().Text($"Ký ngày: {_contract.EmployerSignedAt:dd/MM/yyyy HH:mm}").FontSize(9.5f).Italic().FontColor(Colors.Grey.Darken3);
                            });
                        }
                        else
                        {
                            sigCol.Item().Height(46);
                            sigCol.Item().AlignCenter().Text(_contract.EmployerRepresentativeName).FontSize(12f).Bold();
                            if (!string.IsNullOrWhiteSpace(_contract.EmployerRepresentativePosition))
                                sigCol.Item().AlignCenter().Text(_contract.EmployerRepresentativePosition).FontSize(10f).Italic();
                        }
                    });
                });

                if (_includeSignatures && (showEmployeeSig || showEmployerSig) && !string.IsNullOrWhiteSpace(_contract.DigitalSignatureHash))
                {
                    sigRoot.Item().PaddingTop(10).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten5).PaddingVertical(4).PaddingHorizontal(8).Row(r =>
                    {
                        r.RelativeItem().Text(t =>
                        {
                            t.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Colors.Grey.Darken3));
                            t.Span("Chứng thực chữ ký điện tử & toàn vẹn dữ liệu (SHA-256): ").Bold();
                            t.Span(_contract.DigitalSignatureHash);
                        });
                    });
                }
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        // Footer chuẩn: Bên trái là Mã hợp đồng, Bên phải là Trang X / Y, cỡ chữ 9 - 9.5pt, không có chữ thừa
        const float footerFontSize = 9.2f;

        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text(_contract.ContractNumber ?? string.Empty)
                    .FontSize(footerFontSize)
                    .FontColor(Colors.Grey.Darken2);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(footerFontSize).FontColor(Colors.Grey.Darken2));
                    text.Span("Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        });
    }
}
