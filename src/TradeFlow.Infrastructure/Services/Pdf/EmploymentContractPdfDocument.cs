using System;
using System.IO;
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

    public EmploymentContractPdfDocument(EmploymentContractDto contract)
    {
        _contract = contract;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginTop(2.0f, Unit.Centimetre);
            page.MarginBottom(2.0f, Unit.Centimetre);
            page.MarginLeft(3.0f, Unit.Centimetre);
            page.MarginRight(2.0f, Unit.Centimetre);
            page.PageColor(Colors.White);

            // Phông chữ Times New Roman quy chuẩn thể thức văn bản pháp lý Việt Nam (NĐ 30/2020/NĐ-CP)
            page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(13f).FontColor(Colors.Black).LineHeight(1.25f));

            page.Content().Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container)
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
                    c.Item().PaddingTop(5).AlignCenter().Text($"{(!string.IsNullOrWhiteSpace(_contract.ContractPlace) ? _contract.ContractPlace : "Cần Thơ")}, ngày {_contract.ContractDate:dd} tháng {_contract.ContractDate:MM} năm {_contract.ContractDate:yyyy}").FontSize(13f).Italic();
                });

                headerCol.Item().PaddingTop(12).AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text(_contract.Title.ToUpper()).FontSize(16f).Bold();
                    c.Item().AlignCenter().PaddingTop(3).Text($"Số: {_contract.ContractNumber}").FontSize(13f).Bold();
                    c.Item().AlignCenter().PaddingTop(2).Text("(Căn cứ Bộ luật Lao động - Văn bản hợp nhất số 18/VBHN-VPQH ngày 12/02/2026)").FontSize(10f).Italic().FontColor(Colors.Grey.Darken1);

                    if (_contract.Status == EmploymentContractStatus.Draft)
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
                c.Item().Text("• Căn cứ Bộ luật Lao động số 45/2019/QH14 và Văn bản hợp nhất số 18/VBHN-VPQH ngày 12/02/2026;").Italic().FontSize(10f);
                c.Item().Text("• Căn cứ Nghị định số 145/2020/NĐ-CP và Thông tư 10/2020/TT-BLĐTBXH hướng dẫn Bộ luật Lao động;").Italic().FontSize(10f);
                c.Item().Text("• Căn cứ Luật Giao dịch điện tử số 20/2023/QH15 có hiệu lực thi hành từ ngày 01/07/2024;").Italic().FontSize(10f);
                c.Item().Text("• Căn cứ nhu cầu sử dụng lao động của Doanh nghiệp và sự thỏa thuận tự nguyện của hai bên.").Italic().FontSize(10f);
            });

            col.Item().PaddingTop(4).Text($"Hôm nay, ngày {_contract.ContractDate:dd/MM/yyyy}, tại {(!string.IsNullOrWhiteSpace(_contract.ContractPlace) ? _contract.ContractPlace : "Cần Thơ")}, chúng tôi gồm các bên:").Italic();

            // ==========================================
            // BÊN A: NGƯỜI SỬ DỤNG LAO ĐỘNG
            // ==========================================
            col.Item().PaddingTop(5).Column(a =>
            {
                a.Item().Text("BÊN A (NGƯỜI SỬ DỤNG LAO ĐỘNG):").FontSize(13f).Bold();
                a.Item().PaddingLeft(10).Column(details =>
                {
                    details.Item().Text(t => { t.Span("Tên doanh nghiệp: ").Bold(); t.Span(_contract.EmployerName.ToUpper()); });
                    details.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Mã số thuế / MSDN: ").Bold(); t.Span(_contract.EmployerTaxCode); });
                        if (!string.IsNullOrWhiteSpace(_contract.EmployerPhone))
                            r.RelativeItem().Text(t => { t.Span("Điện thoại: ").Bold(); t.Span(_contract.EmployerPhone); });
                    });
                    details.Item().Text(t => { t.Span("Địa chỉ trụ sở chính: ").Bold(); t.Span(_contract.EmployerAddress); });
                    details.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Người đại diện: ").Bold(); t.Span(_contract.EmployerRepresentativeName); });
                        r.RelativeItem().Text(t => { t.Span("Chức vụ: ").Bold(); t.Span(_contract.EmployerRepresentativePosition); });
                    });
                    if (!string.IsNullOrWhiteSpace(_contract.EmployerRepresentativeAuthority))
                    {
                        details.Item().Text(t => { t.Span("Thẩm quyền giao kết: ").Bold(); t.Span(_contract.EmployerRepresentativeAuthority); });
                    }
                });
            });

            // ==========================================
            // BÊN B: NGƯỜI LAO ĐỘNG
            // ==========================================
            col.Item().PaddingTop(4).Column(b =>
            {
                b.Item().Text("BÊN B (NGƯỜI LAO ĐỘNG):").FontSize(13f).Bold();
                b.Item().PaddingLeft(10).Column(details =>
                {
                    details.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Họ và tên: ").Bold(); t.Span(_contract.EmployeeFullName.ToUpper()); });
                        r.RelativeItem().Text(t => { t.Span("Mã nhân viên: ").Bold(); t.Span(_contract.EmployeeCode); });
                    });
                    details.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Ngày sinh: ").Bold(); t.Span(_contract.EmployeeDateOfBirth.HasValue ? _contract.EmployeeDateOfBirth.Value.ToString("dd/MM/yyyy") : "Chưa cập nhật"); });
                        r.RelativeItem().Text(t => { t.Span("Giới tính: ").Bold(); t.Span(_contract.EmployeeGender ?? "Không xác định"); });
                    });
                    details.Item().Row(r =>
                    {
                        r.RelativeItem().Text(t => { t.Span("Số CCCD/Hộ chiếu: ").Bold(); t.Span(_contract.EmployeeNationalId ?? "Chưa cập nhật"); });
                        if (_contract.EmployeeNationalIdIssueDate.HasValue)
                            r.RelativeItem().Text(t => { t.Span("Ngày cấp: ").Bold(); t.Span(_contract.EmployeeNationalIdIssueDate.Value.ToString("dd/MM/yyyy")); });
                    });
                    details.Item().Text(t => { t.Span("Địa chỉ nơi cư trú: ").Bold(); t.Span(_contract.EmployeeAddress ?? "Chưa cập nhật"); });
                    details.Item().Row(r =>
                    {
                        if (!string.IsNullOrWhiteSpace(_contract.EmployeePhone))
                            r.RelativeItem().Text(t => { t.Span("Điện thoại: ").Bold(); t.Span(_contract.EmployeePhone); });
                        if (!string.IsNullOrWhiteSpace(_contract.EmployeeEmail))
                            r.RelativeItem().Text(t => { t.Span("Email: ").Bold(); t.Span(_contract.EmployeeEmail); });
                    });
                    if (!string.IsNullOrWhiteSpace(_contract.EmployeeBankInformation))
                    {
                        details.Item().Text(t => { t.Span("Tài khoản nhận lương: ").Bold(); t.Span(_contract.EmployeeBankInformation); });
                    }
                });
            });

            col.Item().PaddingTop(4).Text("Hai bên cùng thỏa thuận và thống nhất ký kết Hợp đồng lao động với các điều khoản sau đây:").Italic().FontSize(12.5f);

            // ==========================================
            // CÁC ĐIỀU KHOẢN HỢP ĐỒNG LAO ĐỘNG
            // ==========================================
            var clausesToRender = _contract.Clauses != null && _contract.Clauses.Any()
                ? _contract.Clauses.OrderBy(c => c.DisplayOrder).ToList()
                : EmploymentContractClauseDefaults.GenerateDefaultClauses(_contract);

            foreach (var clause in clausesToRender)
            {
                col.Item().PaddingTop(3).Column(c =>
                {
                    var heading = string.IsNullOrWhiteSpace(clause.ArticleNumber)
                        ? clause.Title
                        : $"{clause.ArticleNumber}. {clause.Title}";
                    c.Item().Text(heading).Bold().FontSize(13f);
                    c.Item().PaddingTop(1).Text(clause.Content ?? string.Empty).FontSize(12.5f);
                });
            }

            // ==========================================
            // KHỐI CHỮ KÝ HAI BÊN (SHOW ENTIRE, TRÁNH CẮT TRANG)
            // ==========================================
            col.Item().ShowEntire().PaddingTop(16).Row(sigRow =>
            {
                // Người lao động
                sigRow.RelativeItem().AlignCenter().Column(sigCol =>
                {
                    sigCol.Item().Text("NGƯỜI LAO ĐỘNG").FontSize(13.5f).Bold();
                    sigCol.Item().Text("(Ký và ghi rõ họ tên)").FontSize(9.5f).Italic();

                    sigCol.Item().Height(65).AlignCenter().AlignMiddle().Element(sigBox =>
                    {
                        if (_contract.EmployeeSignedAt.HasValue)
                        {
                            sigBox.Border(1).BorderColor(Colors.Green.Darken2).Background(Colors.Green.Lighten5).Padding(4).Column(s =>
                            {
                                s.Item().Text("ĐÃ KÝ ĐIỆN TỬ").FontSize(8.5f).Bold().FontColor(Colors.Green.Darken3);
                                s.Item().Text($"{_contract.EmployeeSignedAt:dd/MM/yyyy HH:mm}").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                            });
                        }
                    });

                    sigCol.Item().Text(_contract.EmployeeFullName).FontSize(13f).Bold();
                });

                // Người sử dụng lao động
                sigRow.RelativeItem().AlignCenter().Column(sigCol =>
                {
                    sigCol.Item().Text("NGƯỜI SỬ DỤNG LAO ĐỘNG").FontSize(13.5f).Bold();
                    sigCol.Item().Text("(Ký, ghi rõ họ tên và đóng dấu nếu có)").FontSize(9.5f).Italic();

                    sigCol.Item().Height(65).AlignCenter().AlignMiddle().Element(sigBox =>
                    {
                        if (_contract.EmployerSignedAt.HasValue)
                        {
                            sigBox.Border(1).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten5).Padding(4).Column(s =>
                            {
                                s.Item().Text("ĐÃ KÝ ĐIỆN TỬ & ĐÓNG DẤU").FontSize(8.5f).Bold().FontColor(Colors.Blue.Darken3);
                                s.Item().Text($"{_contract.EmployerSignedAt:dd/MM/yyyy HH:mm}").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                            });
                        }
                    });

                    sigCol.Item().Text(_contract.EmployerRepresentativeName).FontSize(13f).Bold();
                    sigCol.Item().Text(_contract.EmployerRepresentativePosition).FontSize(10f).Italic();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text($"{GetContractPrefixText()} — Chuẩn VBHN 18/VBHN-VPQH").FontSize(8f).FontColor(Colors.Grey.Darken1);

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

    private string GetContractPrefixText()
    {
        var num = _contract.ContractNumber ?? string.Empty;
        if (num.StartsWith("HDTV", StringComparison.OrdinalIgnoreCase) || _contract.ContractType == EmploymentContractType.Probation)
            return $"HĐTV số: {num}";
        if (num.StartsWith("TTTT", StringComparison.OrdinalIgnoreCase) || _contract.ContractType == EmploymentContractType.Internship)
            return $"TTTT số: {num}";
        if (num.StartsWith("HDDT", StringComparison.OrdinalIgnoreCase) || _contract.ContractType == EmploymentContractType.VocationalTraining)
            return $"HĐĐT số: {num}";
        return $"HĐLĐ số: {num}";
    }
}
