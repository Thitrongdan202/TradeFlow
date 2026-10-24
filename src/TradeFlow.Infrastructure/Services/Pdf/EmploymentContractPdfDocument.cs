using System;
using System.IO;
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
            page.MarginTop(1.8f, Unit.Centimetre);
            page.MarginBottom(1.8f, Unit.Centimetre);
            page.MarginLeft(2.2f, Unit.Centimetre);
            page.MarginRight(1.8f, Unit.Centimetre);
            page.PageColor(Colors.White);

            // Phông chữ Times New Roman quy chuẩn thể thức văn bản pháp lý Việt Nam
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
            // Quốc hiệu và tiêu ngữ
            col.Item().Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").FontSize(11.5f).Bold();
                    c.Item().PaddingTop(1).Text("Độc lập - Tự do - Hạnh phúc").FontSize(11f).Bold();
                    c.Item().PaddingTop(3).AlignCenter().Width(130).LineHorizontal(0.75f).LineColor(Colors.Black);
                    c.Item().PaddingTop(6).Text($"{_contract.ContractPlace}, ngày {_contract.ContractDate:dd} tháng {_contract.ContractDate:MM} năm {_contract.ContractDate:yyyy}").FontSize(10f).Italic();
                });
            });

            // Tiêu đề văn bản
            col.Item().PaddingTop(10).AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text(_contract.Title.ToUpper()).FontSize(14).Bold();
                c.Item().AlignCenter().PaddingTop(2).Text($"Số: {_contract.ContractNumber}").FontSize(10.5f).Bold();
                c.Item().AlignCenter().PaddingTop(1).Text($"(Quy chuẩn theo Bộ luật Lao động số 45/2019/QH14 & VB hợp nhất 18/VBHN-VPQH)").FontSize(9f).Italic().FontColor(Colors.Grey.Darken1);
            });

            col.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingTop(8).Column(col =>
        {
            // Căn cứ pháp lý
            col.Item().Text(t =>
            {
                t.Span("- Căn cứ Bộ luật Lao động số 45/2019/QH14 và Văn bản hợp nhất số 18/VBHN-VPQH ngày 12/02/2026;\n").Italic().FontSize(9.5f);
                t.Span("- Căn cứ Nghị định số 145/2020/NĐ-CP và Nghị định số 337/2025/NĐ-CP về giao kết hợp đồng lao động điện tử;\n").Italic().FontSize(9.5f);
                t.Span("- Căn cứ nhu cầu sử dụng lao động và năng lực chuyên môn của hai bên;\n").Italic().FontSize(9.5f);
                t.Span("Hôm nay, hai bên chúng tôi gồm có:").FontSize(10f);
            });

            // ==========================================
            // BÊN A: NGƯỜI SỬ DỤNG LAO ĐỘNG
            // ==========================================
            col.Item().PaddingTop(6).Text("BÊN A - NGƯỜI SỬ DỤNG LAO ĐỘNG:").FontSize(10.5f).Bold();
            col.Item().PaddingLeft(10).Column(a =>
            {
                a.Item().Text(t => { t.Span("Tên doanh nghiệp: ").Bold(); t.Span(_contract.EmployerName.ToUpper()); });
                a.Item().Text(t => { t.Span("Mã số doanh nghiệp/MST: ").Bold(); t.Span(_contract.EmployerTaxCode); });
                a.Item().Text(t => { t.Span("Địa chỉ trụ sở chính: ").Bold(); t.Span(_contract.EmployerAddress); });
                a.Item().Text(t => { t.Span("Điện thoại: ").Bold(); t.Span(_contract.EmployerPhone ?? "Chưa cập nhật"); });
                a.Item().Text(t =>
                {
                    t.Span("Người đại diện: ").Bold();
                    t.Span(_contract.EmployerRepresentativeName);
                    t.Span("     Chức vụ: ").Bold();
                    t.Span(_contract.EmployerRepresentativePosition);
                    t.Span($" ({_contract.EmployerRepresentativeAuthority ?? "Đại diện theo pháp luật"})");
                });
            });

            // ==========================================
            // BÊN B: NGƯỜI LAO ĐỘNG
            // ==========================================
            col.Item().PaddingTop(8).Text("BÊN B - NGƯỜI LAO ĐỘNG:").FontSize(10.5f).Bold();
            col.Item().PaddingLeft(10).Column(b =>
            {
                b.Item().Text(t =>
                {
                    t.Span("Họ và tên: ").Bold();
                    t.Span(_contract.EmployeeFullName.ToUpper());
                    t.Span("     Mã nhân viên: ").Bold();
                    t.Span(_contract.EmployeeCode);
                });
                b.Item().Text(t =>
                {
                    t.Span("Ngày sinh: ").Bold();
                    t.Span(_contract.EmployeeDateOfBirth.HasValue ? _contract.EmployeeDateOfBirth.Value.ToString("dd/MM/yyyy") : "Chưa cập nhật");
                    t.Span("     Giới tính: ").Bold();
                    t.Span(_contract.EmployeeGender ?? "Không xác định");
                });
                b.Item().Text(t =>
                {
                    t.Span("Số CCCD/Hộ chiếu: ").Bold();
                    t.Span(_contract.EmployeeNationalId ?? "Chưa cập nhật");
                    if (_contract.EmployeeNationalIdIssueDate.HasValue)
                    {
                        t.Span("     Ngày cấp: ").Bold();
                        t.Span(_contract.EmployeeNationalIdIssueDate.Value.ToString("dd/MM/yyyy"));
                    }
                    if (!string.IsNullOrEmpty(_contract.EmployeeNationalIdIssuePlace))
                    {
                        t.Span("     Nơi cấp: ").Bold();
                        t.Span(_contract.EmployeeNationalIdIssuePlace);
                    }
                });
                b.Item().Text(t => { t.Span("Địa chỉ thường trú/Nơi cư trú: ").Bold(); t.Span(_contract.EmployeeAddress ?? "Chưa cập nhật"); });
                b.Item().Text(t =>
                {
                    t.Span("Điện thoại: ").Bold();
                    t.Span(_contract.EmployeePhone ?? "Chưa cập nhật");
                    t.Span("     Email: ").Bold();
                    t.Span(_contract.EmployeeEmail ?? "Chưa cập nhật");
                });
                if (!string.IsNullOrEmpty(_contract.EmployeeBankInformation))
                {
                    b.Item().Text(t => { t.Span("Tài khoản nhận lương: ").Bold(); t.Span(_contract.EmployeeBankInformation); });
                }
            });

            col.Item().PaddingTop(8).Text("Hai bên thống nhất ký kết Hợp đồng lao động với các điều khoản và điều kiện sau đây:").Italic();

            if (_contract.Clauses != null && _contract.Clauses.Any())
            {
                foreach (var clause in _contract.Clauses.OrderBy(c => c.DisplayOrder))
                {
                    col.Item().PaddingTop(8).Column(c =>
                    {
                        var heading = string.IsNullOrWhiteSpace(clause.ArticleNumber)
                            ? clause.Title
                            : $"{clause.ArticleNumber}. {clause.Title}";
                        c.Item().Text(heading).Bold();
                        c.Item().PaddingLeft(10).Text(clause.Content ?? string.Empty);
                    });
                }
            }
            else
            {
                // ==========================================
                // ĐIỀU 1: LOẠI HỢP ĐỒNG, CÔNG VIỆC, ĐỊA ĐIỂM
                // ==========================================
                col.Item().PaddingTop(8).Column(c =>
                {
                    c.Item().Text("Điều 1. Loại hợp đồng, thời hạn và địa điểm làm việc").Bold();
                    c.Item().PaddingLeft(10).Column(sub =>
                    {
                        sub.Item().Text(t =>
                        {
                            t.Span("1.1. Loại hợp đồng: ").Bold();
                            t.Span(_contract.ContractTypeName);
                        });
                        sub.Item().Text(t =>
                        {
                            t.Span("1.2. Thời hạn hợp đồng: ").Bold();
                            if (_contract.ContractType == EmploymentContractType.IndefiniteTerm)
                            {
                                t.Span($"Không xác định thời hạn, bắt đầu có hiệu lực từ ngày {_contract.StartDate:dd/MM/yyyy}.");
                            }
                            else
                            {
                                t.Span($"Từ ngày {_contract.StartDate:dd/MM/yyyy} đến ngày {_contract.EndDate:dd/MM/yyyy}");
                                if (_contract.ContractType == EmploymentContractType.Probation && _contract.ProbationDurationDays.HasValue)
                                {
                                    t.Span($" (Thời gian thử việc: {_contract.ProbationDurationDays.Value} ngày làm việc theo Điều 25 BLLĐ).");
                                }
                                else
                                {
                                    t.Span(".");
                                }
                            }
                        });
                        sub.Item().Text(t =>
                        {
                            t.Span("1.3. Chức danh / Vị trí công tác: ").Bold();
                            t.Span(_contract.JobTitle);
                            t.Span(" - Phòng ban: ").Bold();
                            t.Span(_contract.DepartmentName);
                        });
                        sub.Item().Text(t =>
                        {
                            t.Span("1.4. Địa điểm làm việc: ").Bold();
                            t.Span(_contract.Workplace);
                        });
                        if (!string.IsNullOrWhiteSpace(_contract.JobDescription))
                        {
                            sub.Item().Text(t =>
                            {
                                t.Span("1.5. Mô tả nhiệm vụ chính: ").Bold();
                                t.Span(_contract.JobDescription);
                            });
                        }
                    });
                });

                // ==========================================
                // ĐIỀU 2: THỜI GIỜ LÀM VIỆC & NGHỈ NGƠI
                // ==========================================
                col.Item().PaddingTop(8).Column(c =>
                {
                    c.Item().Text("Điều 2. Thời giờ làm việc và thời giờ nghỉ ngơi").Bold();
                    c.Item().PaddingLeft(10).Column(sub =>
                    {
                        sub.Item().Text(t =>
                        {
                            t.Span("2.1. Thời giờ làm việc: ").Bold();
                            t.Span($"{_contract.WorkingHoursPerWeek} giờ/tuần. {_contract.WorkingSchedule}");
                        });
                        sub.Item().Text(t =>
                        {
                            t.Span("2.2. Chế độ nghỉ ngơi: ").Bold();
                            t.Span($"Nghỉ hàng tuần theo lịch công tác; Nghỉ các ngày Lễ, Tết theo quy định của Bộ luật Lao động. Được hưởng {_contract.AnnualLeaveDays} ngày nghỉ phép năm hưởng nguyên lương theo quy định tại Điều 113 BLLĐ.");
                        });
                    });
                });

                // ==========================================
                // ĐIỀU 3: TIỀN LƯƠNG & CHẾ ĐỘ PHỤ CẤP
                // ==========================================
                col.Item().PaddingTop(8).Column(c =>
                {
                    c.Item().Text("Điều 3. Tiền lương, phụ cấp và hình thức chi trả").Bold();
                    c.Item().PaddingLeft(10).Column(sub =>
                    {
                        sub.Item().Text(t =>
                        {
                            t.Span("3.1. Thỏa thuận tiền lương / phụ cấp: ").Bold();
                            if (!string.IsNullOrWhiteSpace(_contract.SalaryClauseText))
                            {
                                t.Span(_contract.SalaryClauseText);
                            }
                            else if (_contract.BasicSalary.HasValue && _contract.BasicSalary.Value > 0)
                            {
                                t.Span($"{_contract.BasicSalary.Value:N0} đồng/tháng ({_contract.SalaryType}).");
                                if (_contract.ContractType == EmploymentContractType.Probation && _contract.ProbationSalaryPercentage.HasValue)
                                {
                                    var probSalary = _contract.BasicSalary.Value * (_contract.ProbationSalaryPercentage.Value / 100m);
                                    t.Span($" Lương trong thời gian thử việc bằng {_contract.ProbationSalaryPercentage.Value}% lương chính, tương đương {probSalary:N0} đồng/tháng theo Điều 26 BLLĐ.");
                                }
                            }
                            else if (_contract.ContractType == EmploymentContractType.Internship)
                            {
                                t.Span("Thỏa thuận phụ cấp hỗ trợ thực tập theo quy chế đào tạo của Công ty.");
                            }
                            else
                            {
                                t.Span("Thỏa thuận theo phụ lục chi tiết và quy chế nội bộ Công ty.");
                            }
                        });
                        if ((_contract.PositionAllowance ?? 0) > 0 || (_contract.LunchAllowance ?? 0) > 0 || (_contract.OtherAllowances ?? 0) > 0)
                        {
                            sub.Item().Text(t =>
                            {
                                t.Span("3.2. Các khoản phụ cấp: ").Bold();
                                t.Span($"Phụ cấp trách nhiệm: {(_contract.PositionAllowance ?? 0):N0} đ/tháng; Tiền ăn trưa: {(_contract.LunchAllowance ?? 0):N0} đ/tháng; Phụ cấp khác: {(_contract.OtherAllowances ?? 0):N0} đ/tháng.");
                            });
                        }
                        sub.Item().Text(t =>
                        {
                            t.Span("3.3. Kỳ hạn và phương thức thanh toán: ").Bold();
                            t.Span($"{_contract.PaymentPeriod}. Hình thức: {_contract.PaymentMethod}.");
                        });
                        sub.Item().Text(t =>
                        {
                            t.Span("3.4. Chế độ nâng lương: ").Bold();
                            t.Span(_contract.SalaryReviewPolicy);
                        });
                    });
                });

                // ==========================================
                // ĐIỀU 4: BẢO HỘ LAO ĐỘNG & BẢO HIỂM
                // ==========================================
                col.Item().PaddingTop(8).Column(c =>
                {
                    c.Item().Text("Điều 4. Bảo hộ lao động và chế độ bảo hiểm").Bold();
                    c.Item().PaddingLeft(10).Column(sub =>
                    {
                        sub.Item().Text(t =>
                        {
                            t.Span("4.1. Trang bị bảo hộ lao động: ").Bold();
                            t.Span(_contract.LaborProtection);
                        });
                        sub.Item().Text(t =>
                        {
                            t.Span("4.2. Bảo hiểm xã hội, BHYT, BHTN: ").Bold();
                            t.Span($"Hai bên thực hiện nghĩa vụ đóng BHXH, BHYT, BHTN theo tỷ lệ luật định (tổng cộng {_contract.SocialInsuranceRate}%: NSDLĐ đóng 21.5%, NLĐ đóng 10.5%). Đối với hợp đồng thử việc, tiền bảo hiểm được tính gộp vào tiền lương chi trả trực tiếp cho người lao động.");
                        });
                    });
                });

                // ==========================================
                // ĐIỀU 5: ĐIỀU KHOẢN TÙY CHỌN & BẢO MẬT (NẾU CÓ)
                // ==========================================
                if (_contract.HasNonDisclosureAgreement || _contract.HasTrainingReimbursement || _contract.HasSpecialBenefits || !string.IsNullOrWhiteSpace(_contract.CustomTerms))
                {
                    col.Item().PaddingTop(8).Column(c =>
                    {
                        c.Item().Text("Điều 5. Thỏa thuận bảo mật thông tin và chế độ bổ sung").Bold();
                        c.Item().PaddingLeft(10).Column(sub =>
                        {
                            if (_contract.HasNonDisclosureAgreement && !string.IsNullOrWhiteSpace(_contract.NdaClauseDetails))
                            {
                                sub.Item().Text(t =>
                                {
                                    t.Span("5.1. Bảo vệ bí mật kinh doanh & bảo mật dữ liệu: ").Bold();
                                    t.Span(_contract.NdaClauseDetails);
                                });
                            }
                            if (_contract.HasTrainingReimbursement && !string.IsNullOrWhiteSpace(_contract.TrainingClauseDetails))
                            {
                                sub.Item().Text(t =>
                                {
                                    t.Span("5.2. Cam kết đào tạo nghề: ").Bold();
                                    t.Span(_contract.TrainingClauseDetails);
                                });
                            }
                            if (_contract.HasSpecialBenefits && !string.IsNullOrWhiteSpace(_contract.SpecialBenefitsDetails))
                            {
                                sub.Item().Text(t =>
                                {
                                    t.Span("5.3. Phúc lợi nội bộ LACASA: ").Bold();
                                    t.Span(_contract.SpecialBenefitsDetails);
                                });
                            }
                            if (!string.IsNullOrWhiteSpace(_contract.CustomTerms))
                            {
                                sub.Item().Text(t =>
                                {
                                    t.Span("5.4. Thỏa thuận khác: ").Bold();
                                    t.Span(_contract.CustomTerms);
                                });
                            }
                        });
                    });
                }

                // ==========================================
                // ĐIỀU 6: ĐIỀU KHOẢN THI HÀNH & KÝ KẾT
                // ==========================================
                col.Item().PaddingTop(8).Column(c =>
                {
                    c.Item().Text("Điều 6. Điều khoản thi hành và phương thức giao kết").Bold();
                    c.Item().PaddingLeft(10).Column(sub =>
                    {
                        sub.Item().Text("6.1. Hợp đồng có hiệu lực thi hành kể từ ngày hai bên hoàn tất việc ký kết hợp pháp.");
                        sub.Item().Text("6.2. Hợp đồng được lập thành 02 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản để theo dõi và thực hiện. Trường hợp giao kết bằng hợp đồng điện tử theo Nghị định 337/2025/NĐ-CP, tệp dữ liệu số lưu trữ trên hệ thống TradeFlow có giá trị toàn vẹn và có giá trị chứng cứ pháp lý.");
                        if (!string.IsNullOrEmpty(_contract.DigitalSignatureHash))
                        {
                            sub.Item().Text($"6.3. Mã xác thực tính toàn vẹn (SHA-256): {_contract.DigitalSignatureHash}").FontSize(8.5f).FontColor(Colors.Grey.Darken2);
                        }
                    });
                });
            }

            // ==========================================
            // KHỐI CHỮ KÝ (GIỮ NGUYÊN KHỐI, KHÔNG NGẮT TRANG)
            // ==========================================
            col.Item().PaddingTop(16).ShowEntire().Row(sigRow =>
            {
                // Bên B (Người lao động)
                sigRow.RelativeItem().AlignCenter().Column(sigCol =>
                {
                    sigCol.Item().Text("NGƯỜI LAO ĐỘNG").FontSize(11).Bold();
                    sigCol.Item().Text("(Ký và ghi rõ họ tên)").FontSize(9.5f).Italic();

                    sigCol.Item().Height(65).AlignCenter().AlignMiddle().Element(sigBox =>
                    {
                        if (_contract.EmployeeSignedAt.HasValue)
                        {
                            sigBox.Column(s =>
                            {
                                s.Item().Text("[ĐÃ KÝ ĐIỆN TỬ]").FontSize(9).Bold().FontColor(Colors.Green.Darken2);
                                s.Item().Text($"{_contract.EmployeeSignedAt:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });

                    sigCol.Item().Text(_contract.EmployeeFullName).FontSize(10.5f).Bold();
                });

                // Bên A (Người sử dụng lao động)
                sigRow.RelativeItem().AlignCenter().Column(sigCol =>
                {
                    sigCol.Item().Text("ĐẠI DIỆN BÊN A").FontSize(11).Bold();
                    sigCol.Item().Text("(Ký, ghi rõ họ tên và đóng dấu)").FontSize(9.5f).Italic();

                    sigCol.Item().Height(65).AlignCenter().AlignMiddle().Element(sigBox =>
                    {
                        if (_contract.EmployerSignedAt.HasValue)
                        {
                            sigBox.Column(s =>
                            {
                                s.Item().Text("[ĐÃ KÝ ĐIỆN TỬ & ĐÓNG DẤU]").FontSize(9).Bold().FontColor(Colors.Green.Darken2);
                                s.Item().Text($"{_contract.EmployerSignedAt:dd/MM/yyyy HH:mm}").FontSize(8).FontColor(Colors.Grey.Darken1);
                            });
                        }
                    });

                    sigCol.Item().Text(_contract.EmployerRepresentativeName).FontSize(10.5f).Bold();
                    sigCol.Item().Text(_contract.EmployerRepresentativePosition).FontSize(9.5f).Italic();
                });
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text(t =>
            {
                t.Span("Hệ thống quản trị TradeFlow — Hợp đồng lao động chuẩn VB hợp nhất 18/VBHN-VPQH").FontSize(8f).FontColor(Colors.Grey.Darken1);
            });
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Trang ").FontSize(8f);
                t.CurrentPageNumber().FontSize(8f);
                t.Span(" / ").FontSize(8f);
                t.TotalPages().FontSize(8f);
            });
        });
    }
}
