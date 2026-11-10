using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Helpers;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services.Pdf;

public class ContractPdfDocument : IDocument
{
    private readonly ContractDto _model;
    private readonly ContractPartyASettingsDto? _partyASettings;
    private readonly bool _includeSignatures;

    public ContractPdfDocument(
        ContractDto model,
        ContractPartyASettingsDto? partyASettings,
        bool includeSignatures = true)
    {
        _model = model;
        _partyASettings = partyASettings;
        _includeSignatures = includeSignatures;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        bool isB2C = _model.PartyBType == ContractPartyBType.Individual ||
                     (_model.TemplateCode != null && _model.TemplateCode.Equals("B2C_RETAIL", StringComparison.OrdinalIgnoreCase));

        // Chuẩn hóa trình bày văn bản hợp đồng trên khổ A4 dọc:
        // Font Times New Roman, Nội dung 12.2pt (trong khoảng chuẩn 12-13pt), giãn dòng 1.18 (trong khoảng chuẩn 1.15-1.25)
        const float baseFontSize = 12.2f;
        const float lineHeight = 1.18f;

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            // Lề chuẩn A4: Trên 20mm, Dưới 20mm, Trái 28mm (chừa lề đóng gáy), Phải 20mm
            page.MarginTop(20f, Unit.Millimetre);
            page.MarginBottom(20f, Unit.Millimetre);
            page.MarginLeft(28f, Unit.Millimetre);
            page.MarginRight(20f, Unit.Millimetre);
            page.PageColor(Colors.White);

            page.DefaultTextStyle(x => x
                .FontFamily("Times New Roman")
                .FontSize(baseFontSize)
                .FontColor(Colors.Black)
                .LineHeight(lineHeight));

            // Không lặp lại Header ở các trang sau; Quốc hiệu, Tiêu ngữ và Tên hợp đồng nằm ở phần đầu Trang 1
            page.Content().Element(c => ComposeContent(c, isB2C, baseFontSize, lineHeight));
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeContent(IContainer container, bool isB2C, float baseFontSize, float lineHeight)
    {
        bool hasThirdParty = ContractRichTextFormatter.TryExtractThirdParty(_model, out var thirdParty);

        container.Column(col =>
        {
            col.Spacing(1.5f);

            // ==========================================
            // 1. QUỐC HIỆU, TIÊU NGỮ, ĐỊA ĐIỂM - NGÀY THÁNG, TÊN & SỐ HỢP ĐỒNG (TRANG ĐẦU)
            // ==========================================
            col.Item().Column(headerCol =>
            {
                headerCol.Item().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(12.2f).Bold();
                    c.Item().PaddingTop(1).AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").FontSize(12.8f).Bold();
                    c.Item().PaddingTop(2).AlignCenter().Width(145).LineHorizontal(1f).LineColor(Colors.Black);

                    var place = !string.IsNullOrWhiteSpace(_model.ContractPlace)
                        ? _model.ContractPlace
                        : (_partyASettings?.DefaultSigningLocation ?? "TP. Cần Thơ");
                    c.Item().PaddingTop(3).AlignCenter().Text($"{place}, ngày {_model.ContractDate:dd} tháng {_model.ContractDate:MM} năm {_model.ContractDate:yyyy}")
                        .FontSize(12f).Italic();
                });

                headerCol.Item().PaddingTop(6).AlignCenter().Column(c =>
                {
                    var titleText = string.IsNullOrWhiteSpace(_model.Title)
                        ? "HỢP ĐỒNG MUA BÁN HÀNG HÓA"
                        : _model.Title.Trim().ToUpperInvariant();
                    c.Item().AlignCenter().Text(titleText).FontSize(15f).Bold();
                    c.Item().PaddingTop(2).AlignCenter().Text($"Số: {_model.ContractNumber}").FontSize(12.2f).Bold();
                });
            });

            // ==========================================
            // 2. CĂN CỨ PHÁP LÝ (IN NGHIÊNG ĐÚNG QUY CHUẨN)
            // ==========================================
            col.Item().PaddingTop(3).Column(c =>
            {
                c.Spacing(1);
                c.Item().Text("- Căn cứ Bộ luật Dân sự số 91/2015/QH13 được Quốc hội nước Cộng hòa xã hội chủ nghĩa Việt Nam thông qua ngày 24/11/2015;").Italic().FontSize(12f).Justify();
                if (isB2C)
                {
                    c.Item().Text("- Căn cứ Luật Bảo vệ quyền lợi người tiêu dùng số 19/2023/QH15 và Nghị định số 55/2024/NĐ-CP quy định chi tiết thi hành;").Italic().FontSize(12f).Justify();
                }
                else
                {
                    c.Item().Text("- Căn cứ Luật Thương mại số 36/2005/QH11 được Quốc hội nước Cộng hòa xã hội chủ nghĩa Việt Nam thông qua ngày 14/06/2005;").Italic().FontSize(12f).Justify();
                }
                c.Item().Text("- Căn cứ Luật Giao dịch điện tử số 20/2023/QH15 và các văn bản hướng dẫn thi hành;").Italic().FontSize(12f).Justify();
                c.Item().Text("- Căn cứ vào nhu cầu và năng lực thực tế, sự thỏa thuận tự nguyện của các bên.").Italic().FontSize(12f).Justify();
            });

            var signingPlace = !string.IsNullOrWhiteSpace(_model.ContractPlace)
                ? _model.ContractPlace
                : (_partyASettings?.DefaultSigningLocation ?? "TP. Cần Thơ");

            col.Item().PaddingTop(3).Text(
                $"Hôm nay, ngày {_model.ContractDate:dd} tháng {_model.ContractDate:MM} năm {_model.ContractDate:yyyy}, tại {signingPlace}, chúng tôi gồm có:")
                .FontSize(baseFontSize).Justify();

            // ==========================================
            // 3. THÔNG TIN CÁC BÊN GIAO KẾT (TRÌNH BÀY CHUẨN VĂN BẢN PHÁP LÝ)
            // ==========================================
            // BÊN A
            col.Item().PaddingTop(2).ShowEntire().Column(c =>
            {
                c.Spacing(1);
                var partyAName = !string.IsNullOrWhiteSpace(_model.PartyAName)
                    ? _model.PartyAName
                    : (_partyASettings?.CompanyName ?? "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM");
                var partyATax = !string.IsNullOrWhiteSpace(_model.PartyATaxCode)
                    ? _model.PartyATaxCode
                    : _partyASettings?.TaxCode;
                var partyAAddr = !string.IsNullOrWhiteSpace(_model.PartyAAddress)
                    ? _model.PartyAAddress
                    : _partyASettings?.RegisteredAddress;
                var partyARep = !string.IsNullOrWhiteSpace(_model.PartyARepresentative)
                    ? _model.PartyARepresentative
                    : (_partyASettings?.RepresentativeName ?? "Trần Thị Mai Phương");
                var partyAPos = !string.IsNullOrWhiteSpace(_model.PartyAPosition)
                    ? _model.PartyAPosition
                    : (_partyASettings?.RepresentativePosition ?? "Chủ tịch Hội đồng thành viên");
                var partyAAuth = !string.IsNullOrWhiteSpace(_model.PartyAAuthority)
                    ? _model.PartyAAuthority
                    : (_partyASettings?.RepresentativeAuthority ?? "Người đại diện theo pháp luật");
                var partyAPhone = !string.IsNullOrWhiteSpace(_model.PartyAPhone)
                    ? _model.PartyAPhone
                    : _partyASettings?.Phone;
                var partyABankAcc = !string.IsNullOrWhiteSpace(_model.PartyABankAccount)
                    ? _model.PartyABankAccount
                    : _partyASettings?.BankAccount;
                var partyABankName = !string.IsNullOrWhiteSpace(_model.PartyABankName)
                    ? _model.PartyABankName
                    : _partyASettings?.BankName;
                var partyABankBranch = !string.IsNullOrWhiteSpace(_model.PartyABankBranch)
                    ? _model.PartyABankBranch
                    : _partyASettings?.BankBranch;

                c.Item().Text(t =>
                {
                    t.Span("BÊN A (BÊN BÁN / BÊN CUNG CẤP): ").Bold();
                    t.Span(partyAName.ToUpperInvariant()).Bold();
                });

                if (!string.IsNullOrWhiteSpace(partyAAddr))
                {
                    c.Item().Text(t => { t.Span("- Địa chỉ trụ sở: "); t.Span(partyAAddr); });
                }

                c.Item().Row(r =>
                {
                    if (!string.IsNullOrWhiteSpace(partyATax))
                        r.RelativeItem().Text(t => { t.Span("- Mã số thuế: "); t.Span(partyATax).Bold(); });
                    if (!string.IsNullOrWhiteSpace(partyAPhone))
                        r.RelativeItem().Text(t => { t.Span("Điện thoại: "); t.Span(partyAPhone); });
                });

                if (!string.IsNullOrWhiteSpace(partyABankAcc))
                {
                    c.Item().Text(t =>
                    {
                        t.Span("- Tài khoản số: ");
                        t.Span(partyABankAcc).Bold();
                        if (!string.IsNullOrWhiteSpace(partyABankName))
                            t.Span($" tại {partyABankName}");
                        if (!string.IsNullOrWhiteSpace(partyABankBranch))
                            t.Span($" - {partyABankBranch}");
                    });
                }

                c.Item().Row(r =>
                {
                    r.RelativeItem(3).Text(t => { t.Span("- Đại diện bởi: "); t.Span(partyARep).Bold(); });
                    r.RelativeItem(2).Text(t => { t.Span("Chức vụ: "); t.Span(partyAPos); });
                });

                if (!string.IsNullOrWhiteSpace(partyAAuth))
                {
                    c.Item().Text(t => { t.Span("- Căn cứ đại diện: "); t.Span(partyAAuth).Italic(); });
                }
            });

            // BÊN B
            col.Item().PaddingTop(2).ShowEntire().Column(c =>
            {
                c.Spacing(1);
                string partyBLabel = isB2C
                    ? "BÊN B (BÊN MUA / KHÁCH HÀNG): "
                    : "BÊN B (BÊN MUA / ĐỐI TÁC): ";

                c.Item().Text(t =>
                {
                    t.Span(partyBLabel).Bold();
                    t.Span((_model.PartyBName ?? string.Empty).ToUpperInvariant()).Bold();
                });

                if (!string.IsNullOrWhiteSpace(_model.PartyBAddress))
                {
                    c.Item().Text(t =>
                    {
                        t.Span(isB2C ? "- Địa chỉ thường trú / Liên hệ: " : "- Địa chỉ trụ sở: ");
                        t.Span(_model.PartyBAddress);
                    });
                }

                if (!string.IsNullOrWhiteSpace(_model.PartyBDeliveryAddress) &&
                    !string.Equals(_model.PartyBDeliveryAddress, _model.PartyBAddress, StringComparison.OrdinalIgnoreCase))
                {
                    c.Item().Text(t => { t.Span("- Địa điểm giao nhận: "); t.Span(_model.PartyBDeliveryAddress); });
                }

                if (!string.IsNullOrWhiteSpace(_model.PartyBTaxCode) ||
                    !string.IsNullOrWhiteSpace(_model.PartyBIdCardNumber) ||
                    !string.IsNullOrWhiteSpace(_model.PartyBPhone))
                {
                    c.Item().Row(r =>
                    {
                        if (!string.IsNullOrWhiteSpace(_model.PartyBTaxCode))
                            r.RelativeItem().Text(t => { t.Span("- Mã số thuế: "); t.Span(_model.PartyBTaxCode).Bold(); });
                        else if (!string.IsNullOrWhiteSpace(_model.PartyBIdCardNumber))
                            r.RelativeItem().Text(t => { t.Span("- Số CCCD/CMND: "); t.Span(_model.PartyBIdCardNumber).Bold(); });

                        if (!string.IsNullOrWhiteSpace(_model.PartyBPhone))
                            r.RelativeItem().Text(t => { t.Span("Điện thoại: "); t.Span(_model.PartyBPhone); });
                    });
                }

                if (!string.IsNullOrWhiteSpace(_model.PartyBBankAccount))
                {
                    c.Item().Text(t =>
                    {
                        t.Span("- Tài khoản số: ");
                        t.Span(_model.PartyBBankAccount).Bold();
                        if (!string.IsNullOrWhiteSpace(_model.PartyBBankName))
                            t.Span($" tại {_model.PartyBBankName}");
                    });
                }

                if (!isB2C && !string.IsNullOrWhiteSpace(_model.PartyBRepresentative))
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem(3).Text(t => { t.Span("- Đại diện bởi: "); t.Span(_model.PartyBRepresentative).Bold(); });
                        if (!string.IsNullOrWhiteSpace(_model.PartyBPosition))
                            r.RelativeItem(2).Text(t => { t.Span("Chức vụ: "); t.Span(_model.PartyBPosition); });
                    });

                    if (!string.IsNullOrWhiteSpace(_model.PartyBAuthority))
                    {
                        c.Item().Text(t => { t.Span("- Căn cứ đại diện: "); t.Span(_model.PartyBAuthority).Italic(); });
                    }
                }
            });

            // BÊN C (NẾU HỢP ĐỒNG 3 BÊN)
            if (hasThirdParty && thirdParty != null)
            {
                col.Item().PaddingTop(4).ShowEntire().Column(c =>
                {
                    c.Spacing(2);
                    c.Item().Text(t =>
                    {
                        t.Span($"{thirdParty.PartyLabel}: ").Bold();
                        t.Span(thirdParty.Name.ToUpperInvariant()).Bold();
                    });
                    if (!string.IsNullOrWhiteSpace(thirdParty.Address))
                        c.Item().Text(t => { t.Span("- Địa chỉ: "); t.Span(thirdParty.Address); });
                    if (!string.IsNullOrWhiteSpace(thirdParty.TaxCode))
                        c.Item().Text(t => { t.Span("- Mã số thuế: "); t.Span(thirdParty.TaxCode).Bold(); });
                    if (!string.IsNullOrWhiteSpace(thirdParty.Representative))
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem(3).Text(t => { t.Span("- Đại diện bởi: "); t.Span(thirdParty.Representative).Bold(); });
                            if (!string.IsNullOrWhiteSpace(thirdParty.Position))
                                r.RelativeItem(2).Text(t => { t.Span("Chức vụ: "); t.Span(thirdParty.Position); });
                        });
                    }
                });
            }

            col.Item().PaddingTop(4).Text(hasThirdParty
                ? "Các bên cùng thống nhất thỏa thuận và ký kết hợp đồng với các điều khoản cụ thể sau đây:"
                : "Sau khi bàn bạc, hai bên cùng thống nhất thỏa thuận và ký kết hợp đồng với các điều khoản cụ thể sau đây:")
                .Italic().Justify();

            // ==========================================
            // 4. BẢNG KÊ HÀNG HÓA & TỔNG KẾT TÀI CHÍNH (NẾU CÓ)
            // ==========================================
            if (_model.Items != null && _model.Items.Count > 0)
            {
                col.Item().PaddingTop(6).Element(ComposeItemsTable);
            }

            if ((_model.Items != null && _model.Items.Count > 0) || _model.TotalAmount > 0)
            {
                col.Item().ShowEntire().PaddingTop(4).Column(c =>
                {
                    c.Spacing(2);
                    if (_model.SubTotal > 0 && (_model.TaxAmount > 0 || _model.TotalDiscount > 0))
                    {
                        c.Item().Row(r =>
                        {
                            r.RelativeItem().Text(t =>
                            {
                                t.Span("- Cộng tiền hàng (chưa thuế): ");
                                t.Span($"{_model.SubTotal:N0} {_model.CurrencyCode}").Bold();
                            });
                            if (_model.TotalDiscount > 0)
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span("Chiết khấu: ");
                                    t.Span($"{_model.TotalDiscount:N0} {_model.CurrencyCode}").Bold();
                                });
                            }
                            if (_model.TaxAmount > 0)
                            {
                                r.RelativeItem().Text(t =>
                                {
                                    t.Span($"Thuế GTGT ({_model.TaxRate:0.#}%): ");
                                    t.Span($"{_model.TaxAmount:N0} {_model.CurrencyCode}").Bold();
                                });
                            }
                        });
                    }

                    c.Item().Row(r =>
                    {
                        r.RelativeItem(3).Text(t =>
                        {
                            t.Span("- Tổng giá trị hợp đồng thanh toán: ").Bold();
                            t.Span($"{_model.TotalAmount:N0} {_model.CurrencyCode}").Bold();
                        });
                        if (_model.DepositAmount > 0)
                        {
                            r.RelativeItem(2).Text(t =>
                            {
                                t.Span("Tiền đặt cọc: ").Bold();
                                t.Span($"{_model.DepositAmount:N0} {_model.CurrencyCode}");
                            });
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(_model.AmountInWords))
                    {
                        c.Item().Text(t =>
                        {
                            t.Span("- Số tiền viết bằng chữ: ").Bold();
                            t.Span(_model.AmountInWords).Italic().Bold();
                        });
                    }
                });
            }

            // ==========================================
            // 5. CÁC ĐIỀU KHOẢN HỢP ĐỒNG (CHỐNG RỚT TIÊU ĐỀ LẺ LOI + TRẢI TRANG TỰ NHIÊN + BẢO TOÀN ĐỊNH DẠNG)
            // ==========================================
            if (_model.Clauses != null && _model.Clauses.Count > 0)
            {
                col.Item().PaddingTop(2).Column(clauseCol =>
                {
                    foreach (var clause in _model.Clauses)
                    {
                        var num = (clause.ClauseNumber ?? string.Empty).Trim().TrimEnd('.', ':');
                        var title = (clause.Title ?? string.Empty).Trim();
                        var heading = string.IsNullOrWhiteSpace(title)
                            ? $"{num}."
                            : $"{num}. {title}";

                        clauseCol.RenderLegalClause(
                            heading,
                            clause.Content,
                            headingFontSize: 12.5f,
                            bodyFontSize: baseFontSize,
                            lineHeight: lineHeight);
                    }
                });
            }
            else
            {
                // Các điều khoản bổ sung nếu không có Clauses
                if (!string.IsNullOrWhiteSpace(_model.PaymentTerms))
                {
                    col.RenderLegalClause("Điều khoản thanh toán cụ thể", _model.PaymentTerms, 12.5f, baseFontSize, lineHeight);
                }
                if (!string.IsNullOrWhiteSpace(_model.DeliveryTerms))
                {
                    col.RenderLegalClause("Điều kiện và địa điểm giao nhận", _model.DeliveryTerms, 12.5f, baseFontSize, lineHeight);
                }
                if (!string.IsNullOrWhiteSpace(_model.InstallationTerms))
                {
                    col.RenderLegalClause("Điều kiện thi công lắp đặt và nghiệm thu", _model.InstallationTerms, 12.5f, baseFontSize, lineHeight);
                }
                if (!string.IsNullOrWhiteSpace(_model.WarrantyTerms))
                {
                    col.RenderLegalClause("Chính sách bảo hành và hỗ trợ kỹ thuật", _model.WarrantyTerms, 12.5f, baseFontSize, lineHeight);
                }
                if (!string.IsNullOrWhiteSpace(_model.GeneralTerms))
                {
                    col.RenderLegalClause("Điều khoản chung bổ sung", _model.GeneralTerms, 12.5f, baseFontSize, lineHeight);
                }
            }

            // ==========================================
            // 6. ĐOẠN KẾT & KHỐI CHỮ KÝ CÁC BÊN (SHOW ENTIRE ĐỂ KHÔNG BỊ TÁCH TRANG)
            // ==========================================
            bool alreadyHasCopiesClause = _model.Clauses != null && _model.Clauses.Any(c =>
                (c.Title != null && c.Title.Contains("số bản", StringComparison.OrdinalIgnoreCase)) ||
                (c.Content != null && c.Content.Contains("lập thành", StringComparison.OrdinalIgnoreCase)));

            if (!alreadyHasCopiesClause)
            {
                int copies = hasThirdParty ? 3 : (isB2C ? 2 : 4);
                int eachCopies = hasThirdParty ? 1 : (isB2C ? 1 : 2);
                string copiesText = hasThirdParty
                    ? $"Hợp đồng này có hiệu lực kể từ ngày ký và được lập thành 0{copies} bản chính bằng tiếng Việt có giá trị pháp lý như nhau, mỗi bên giữ 0{eachCopies} bản để cùng thực hiện."
                    : $"Hợp đồng này có hiệu lực kể từ ngày ký và được lập thành 0{copies} bản chính bằng tiếng Việt có giá trị pháp lý như nhau, Bên A giữ 0{eachCopies} bản, Bên B giữ 0{eachCopies} bản để cùng thực hiện.";

                col.Item().ShowEntire().PaddingTop(3).Text(copiesText).Italic().Justify();
            }

            col.Item().ShowEntire().PaddingTop(6).Element(sigContainer =>
                ComposeSignatures(sigContainer, isB2C, hasThirdParty, thirdParty));
        });
    }

    private void ComposeItemsTable(IContainer container)
    {
        // Cỡ chữ bảng dữ liệu 10.5pt đúng chuẩn (10.5 - 11.5pt), lặp lại Header khi qua trang, mỗi dòng ShowEntire()
        const float tableHeaderFontSize = 10.5f;
        const float tableCellFontSize = 10.5f;

        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(28); // STT
                cols.ConstantColumn(68); // Mã hàng
                cols.RelativeColumn();   // Tên hàng hóa, quy cách
                cols.ConstantColumn(40); // ĐVT
                cols.ConstantColumn(38); // SL
                cols.ConstantColumn(74); // Đơn giá
                cols.ConstantColumn(80); // Thành tiền
            });

            table.Header(header =>
            {
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text("STT").Bold().FontSize(tableHeaderFontSize);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text("Mã hàng").Bold().FontSize(tableHeaderFontSize);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).AlignCenter().AlignMiddle().Text("Tên hàng hóa, quy cách").Bold().FontSize(tableHeaderFontSize);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text("ĐVT").Bold().FontSize(tableHeaderFontSize);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text("Số lượng").Bold().FontSize(tableHeaderFontSize);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).AlignCenter().AlignMiddle().Text("Đơn giá").Bold().FontSize(tableHeaderFontSize);
                header.Cell().Border(0.5f).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).PaddingVertical(4).PaddingHorizontal(4).AlignCenter().AlignMiddle().Text("Thành tiền").Bold().FontSize(tableHeaderFontSize);
            });

            int index = 1;
            foreach (var item in _model.Items)
            {
                var productDisplay = string.IsNullOrWhiteSpace(item.Specifications)
                    ? item.ProductName
                    : $"{item.ProductName} ({item.Specifications})";

                table.Cell().ShowEntire().Border(0.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(3.5f).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text(index++.ToString()).FontSize(tableCellFontSize);
                table.Cell().ShowEntire().Border(0.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(3.5f).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text(item.ProductCode ?? "-").FontSize(tableCellFontSize);
                table.Cell().ShowEntire().Border(0.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(3.5f).PaddingHorizontal(4).AlignLeft().AlignMiddle().Text(productDisplay).FontSize(tableCellFontSize);
                table.Cell().ShowEntire().Border(0.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(3.5f).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text(item.Unit ?? "Bộ").FontSize(tableCellFontSize);
                table.Cell().ShowEntire().Border(0.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(3.5f).PaddingHorizontal(3).AlignCenter().AlignMiddle().Text($"{item.Quantity:N0}").FontSize(tableCellFontSize);
                table.Cell().ShowEntire().Border(0.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(3.5f).PaddingHorizontal(4).AlignRight().AlignMiddle().Text($"{item.UnitPrice:N0}").FontSize(tableCellFontSize);
                table.Cell().ShowEntire().Border(0.5f).BorderColor(Colors.Grey.Darken2).PaddingVertical(3.5f).PaddingHorizontal(4).AlignRight().AlignMiddle().Text($"{item.LineTotal:N0}").FontSize(tableCellFontSize).Bold();
            }
        });
    }

    private void ComposeSignatures(
        IContainer container,
        bool isB2C,
        bool hasThirdParty,
        ContractThirdPartyInfo? thirdParty)
    {
        var partyARep = !string.IsNullOrWhiteSpace(_model.PartyARepresentative)
            ? _model.PartyARepresentative
            : (_partyASettings?.RepresentativeName ?? "Trần Thị Mai Phương");

        var partyBDisplayName = (!isB2C && !string.IsNullOrWhiteSpace(_model.PartyBRepresentative))
            ? _model.PartyBRepresentative!
            : _model.PartyBName;

        bool showSignedA = _includeSignatures && _model.PartyASignedAt.HasValue;
        bool showSignedB = _includeSignatures && _model.PartyBSignedAt.HasValue;

        container.Column(sigRoot =>
        {
            sigRoot.Item().Row(row =>
            {
                // Cột 1: ĐẠI DIỆN BÊN A
                row.RelativeItem().PaddingHorizontal(4).AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("ĐẠI DIỆN BÊN A").Bold().FontSize(12.5f);
                    c.Item().PaddingTop(1).AlignCenter().Text("(Ký, ghi rõ họ tên và đóng dấu)").FontSize(10f).Italic();

                    if (showSignedA)
                    {
                        c.Item().PaddingTop(6).Element(RenderSignatureBlockPartyA);
                    }
                    else
                    {
                        c.Item().Height(46);
                        if (!string.IsNullOrWhiteSpace(partyARep))
                            c.Item().AlignCenter().Text(partyARep).Bold().FontSize(12f);
                    }
                });

                // Cột 2: ĐẠI DIỆN BÊN B
                row.RelativeItem().PaddingHorizontal(4).AlignCenter().Column(c =>
                {
                    string sigTitleB = isB2C ? "ĐẠI DIỆN BÊN B (KHÁCH HÀNG)" : "ĐẠI DIỆN BÊN B";
                    string sigNoteB = isB2C ? "(Ký và ghi rõ họ tên)" : "(Ký, ghi rõ họ tên và đóng dấu)";

                    c.Item().AlignCenter().Text(sigTitleB).Bold().FontSize(12.5f);
                    c.Item().PaddingTop(1).AlignCenter().Text(sigNoteB).FontSize(10f).Italic();

                    if (showSignedB)
                    {
                        c.Item().PaddingTop(6).Element(RenderSignatureBlockPartyB);
                    }
                    else
                    {
                        c.Item().Height(46);
                        if (!string.IsNullOrWhiteSpace(partyBDisplayName))
                            c.Item().AlignCenter().Text(partyBDisplayName).Bold().FontSize(12f);
                    }
                });

                // Cột 3 (nếu Hợp đồng có 3 bên ký): ĐẠI DIỆN BÊN C
                if (hasThirdParty && thirdParty != null)
                {
                    row.RelativeItem().PaddingHorizontal(4).AlignCenter().Column(c =>
                    {
                        c.Item().AlignCenter().Text(thirdParty.HeadingTitle).Bold().FontSize(12.5f);
                        c.Item().PaddingTop(1).AlignCenter().Text("(Ký, ghi rõ họ tên và đóng dấu)").FontSize(10f).Italic();
                        c.Item().Height(46);
                        var repC = !string.IsNullOrWhiteSpace(thirdParty.Representative)
                            ? thirdParty.Representative
                            : thirdParty.Name;
                        if (!string.IsNullOrWhiteSpace(repC))
                            c.Item().AlignCenter().Text(repC).Bold().FontSize(12f);
                    });
                }
            });

            // Chỉ hiển thị chứng thực chữ ký số & mã băm SHA-256 ở Bản đã ký số (khi đã thực sự ký)
            if (_includeSignatures && (showSignedA || showSignedB) && !string.IsNullOrWhiteSpace(_model.DocumentHash))
            {
                sigRoot.Item().PaddingTop(10).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Background(Colors.Grey.Lighten5).PaddingVertical(4).PaddingHorizontal(8).Row(r =>
                {
                    r.RelativeItem().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(9.5f).FontColor(Colors.Grey.Darken3));
                        t.Span("Chứng thực chữ ký điện tử & toàn vẹn dữ liệu (SHA-256): ").Bold();
                        t.Span(_model.DocumentHash);
                    });
                });
            }
        });
    }

    private void RenderSignatureBlockPartyA(IContainer container)
    {
        var signerName = !string.IsNullOrWhiteSpace(_model.PartyASignedBy)
            ? _model.PartyASignedBy
            : (!string.IsNullOrWhiteSpace(_model.PartyARepresentative) ? _model.PartyARepresentative : "Trần Thị Mai Phương");

        container.Border(0.75f).BorderColor(Colors.Blue.Darken2).Background(Colors.Blue.Lighten5).Padding(6).Column(c =>
        {
            c.Item().AlignCenter().Text("ĐÃ KÝ SỐ / ĐIỆN TỬ HỢP LỆ").Bold().FontSize(9.5f).FontColor(Colors.Blue.Darken3);
            if (!string.IsNullOrWhiteSpace(_model.PartyASignatureData) && _model.PartyASignatureData.StartsWith("data:image"))
            {
                try
                {
                    var base64 = _model.PartyASignatureData.Substring(_model.PartyASignatureData.IndexOf(',') + 1);
                    var bytes = Convert.FromBase64String(base64);
                    c.Item().PaddingVertical(2).AlignCenter().Height(42).Image(bytes).FitArea();
                }
                catch { }
            }
            c.Item().AlignCenter().Text(signerName).Bold().FontSize(11f).FontColor(Colors.Black);
            c.Item().AlignCenter().Text($"Ký ngày: {_model.PartyASignedAt:dd/MM/yyyy HH:mm}").FontSize(9.5f).Italic().FontColor(Colors.Grey.Darken3);
        });
    }

    private void RenderSignatureBlockPartyB(IContainer container)
    {
        var signerName = !string.IsNullOrWhiteSpace(_model.PartyBSignedBy)
            ? _model.PartyBSignedBy
            : (!string.IsNullOrWhiteSpace(_model.PartyBRepresentative) ? _model.PartyBRepresentative : _model.PartyBName);

        container.Border(0.75f).BorderColor(Colors.Green.Darken2).Background(Colors.Green.Lighten5).Padding(6).Column(c =>
        {
            c.Item().AlignCenter().Text("ĐÃ KÝ SỐ / ĐIỆN TỬ HỢP LỆ").Bold().FontSize(9.5f).FontColor(Colors.Green.Darken3);
            if (!string.IsNullOrWhiteSpace(_model.PartyBSignatureData) && _model.PartyBSignatureData.StartsWith("data:image"))
            {
                try
                {
                    var base64 = _model.PartyBSignatureData.Substring(_model.PartyBSignatureData.IndexOf(',') + 1);
                    var bytes = Convert.FromBase64String(base64);
                    c.Item().PaddingVertical(2).AlignCenter().Height(42).Image(bytes).FitArea();
                }
                catch { }
            }
            c.Item().AlignCenter().Text(signerName).Bold().FontSize(11f).FontColor(Colors.Black);
            c.Item().AlignCenter().Text($"Ký ngày: {_model.PartyBSignedAt:dd/MM/yyyy HH:mm}").FontSize(9.5f).Italic().FontColor(Colors.Grey.Darken3);
        });
    }

    private void ComposeFooter(IContainer container)
    {
        // Footer chuẩn: Bên trái là Số hợp đồng, Bên phải là Trang X / Y, cỡ chữ 9 - 9.5pt, không có URL hay chữ thừa
        const float footerFontSize = 9.2f;

        container.Column(col =>
        {
            col.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
            col.Item().PaddingTop(3).Row(row =>
            {
                row.RelativeItem().Text(_model.ContractNumber ?? string.Empty)
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
