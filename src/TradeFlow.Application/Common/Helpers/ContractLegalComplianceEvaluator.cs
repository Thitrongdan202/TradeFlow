using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TradeFlow.Application.Common.Models.Contracts;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Helpers;

public static class ContractLegalComplianceEvaluator
{
    public static ContractLegalComplianceReportDto EvaluateCommercialContract(
        string title,
        string templateCode,
        ContractPartyBType partyBType,
        string partyAName,
        string partyATaxCode,
        string partyAAddress,
        string partyARepresentative,
        string partyAPosition,
        string partyAAuthority,
        string partyBName,
        string partyBTaxCode,
        string partyBIdCardNumber,
        string partyBAddress,
        string partyBRepresentative,
        string partyBPosition,
        string partyBAuthority,
        decimal totalAmount,
        string amountInWords,
        IReadOnlyList<ContractItemDto>? items,
        IReadOnlyList<ContractClauseDto>? clauses,
        ContractExecutionMethod executionMethod)
    {
        var report = new ContractLegalComplianceReportDto();
        var itemsList = new List<LegalCheckItemDto>();

        // 1. Phân loại và hình thức giao dịch
        bool isB2C = partyBType == ContractPartyBType.Individual || templateCode.Equals("B2C_RETAIL", StringComparison.OrdinalIgnoreCase);

        itemsList.Add(new LegalCheckItemDto
        {
            Category = "Phân loại giao dịch",
            Title = "Xác định bản chất hợp đồng (B2B vs B2C)",
            Description = isB2C ? "Giao dịch bán lẻ với người tiêu dùng cá nhân (B2C)" : "Giao dịch thương mại giữa các tổ chức / thương nhân (B2B)",
            LegalReference = isB2C 
                ? "Luật Bảo vệ quyền lợi người tiêu dùng số 19/2023/QH15 & Nghị định 55/2024/NĐ-CP"
                : "Luật Thương mại số 36/2005/QH11 & Bộ luật Dân sự số 91/2015/QH13",
            Status = LegalCheckItemStatus.Passed,
            Details = isB2C 
                ? "Áp dụng cơ chế bảo vệ người tiêu dùng, chính sách đổi trả minh bạch, cỡ chữ in tối thiểu 12pt."
                : "Áp dụng quy định thương mại giữa các thương nhân, giới hạn phạt vi phạm tối đa 8% phần vi phạm (Đ301 LTM)."
        });

        // 2. Thẩm quyền và thông tin Bên A
        bool hasPartyAName = !string.IsNullOrWhiteSpace(partyAName);
        bool hasPartyATax = !string.IsNullOrWhiteSpace(partyATaxCode);
        bool hasPartyARep = !string.IsNullOrWhiteSpace(partyARepresentative);

        if (hasPartyAName && hasPartyATax && hasPartyARep)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Chủ thể Bên A",
                Title = "Thông tin pháp nhân & Đại diện Bên A (Đơn vị cung cấp)",
                Description = $"{partyAName} (MST: {partyATaxCode})",
                LegalReference = "Điều 117, Điều 134, Điều 138 Bộ luật Dân sự 2015",
                Status = LegalCheckItemStatus.Passed,
                Details = $"Người đại diện: {partyARepresentative} ({partyAPosition ?? "Đại diện"}). Căn cứ: {partyAAuthority ?? "Đại diện theo pháp luật"}."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Chủ thể Bên A",
                Title = "Thông tin pháp nhân Bên A chưa đầy đủ",
                Description = "Thiếu tên pháp nhân, MST hoặc người đại diện Bên A",
                LegalReference = "Điều 117 Bộ luật Dân sự 2015",
                Status = LegalCheckItemStatus.Failed,
                Details = "Cần cập nhật đầy đủ thông tin pháp nhân Bên A trong Thiết lập Bên A."
            });
        }

        // 3. Thông tin Bên B và Thẩm quyền
        bool hasPartyBName = !string.IsNullOrWhiteSpace(partyBName);
        if (!hasPartyBName)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Chủ thể Bên B",
                Title = "Chưa có thông tin định danh Bên B",
                Description = "Tên khách hàng / đối tác Bên B đang để trống",
                LegalReference = "Điều 398 Bộ luật Dân sự 2015",
                Status = LegalCheckItemStatus.Failed,
                Details = "Vui lòng nhập tên đối tác doanh nghiệp hoặc họ tên khách hàng cá nhân."
            });
        }
        else
        {
            if (isB2C)
            {
                bool hasIdCard = !string.IsNullOrWhiteSpace(partyBIdCardNumber);
                itemsList.Add(new LegalCheckItemDto
                {
                    Category = "Chủ thể Bên B",
                    Title = "Thông tin khách hàng cá nhân (B2C)",
                    Description = $"{partyBName}" + (hasIdCard ? $" (CCCD/ĐD: {partyBIdCardNumber})" : ""),
                    LegalReference = "Điều 10, Điều 28 Luật Bảo vệ quyền lợi người tiêu dùng 2023",
                    Status = hasIdCard ? LegalCheckItemStatus.Passed : LegalCheckItemStatus.Warning,
                    Details = hasIdCard 
                        ? "Khách hàng cá nhân trực tiếp giao kết và ký nhận văn bản."
                        : "Khuyến nghị bổ sung số CCCD/Hộ chiếu để xác lập quyền và nghĩa vụ hợp đồng chặt chẽ."
                });
            }
            else
            {
                bool hasTax = !string.IsNullOrWhiteSpace(partyBTaxCode);
                bool hasRep = !string.IsNullOrWhiteSpace(partyBRepresentative);
                itemsList.Add(new LegalCheckItemDto
                {
                    Category = "Chủ thể Bên B",
                    Title = "Thông tin đối tác doanh nghiệp (B2B)",
                    Description = $"{partyBName}" + (hasTax ? $" (MST: {partyBTaxCode})" : ""),
                    LegalReference = "Điều 134, Điều 141 Bộ luật Dân sự 2015 & Luật Doanh nghiệp 2020",
                    Status = (hasTax && hasRep) ? LegalCheckItemStatus.Passed : LegalCheckItemStatus.Warning,
                    Details = (hasTax && hasRep)
                        ? $"Đại diện: {partyBRepresentative} ({partyBPosition ?? "Đại diện"}). Căn cứ: {partyBAuthority ?? "Đại diện theo pháp luật hoặc Ủy quyền"}."
                        : "Khuyến nghị bổ sung MST và Người đại diện ký kết kèm chức danh của Bên B."
                });
            }
        }

        // 4. Danh mục hàng hóa và Đối tượng hợp đồng
        if (items != null && items.Count > 0)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung chủ yếu",
                Title = "Đối tượng và Danh mục thiết bị hàng hóa",
                Description = $"Gồm {items.Count} hạng mục thiết bị vật tư kèm thông số và đơn vị tính",
                LegalReference = "Điều 398 Bộ luật Dân sự 2015 & Điều 24 Luật Thương mại 2005",
                Status = LegalCheckItemStatus.Passed,
                Details = "Các sản phẩm có mã hàng, quy cách, số lượng, đơn giá và thuế VAT chi tiết."
            });
        }
        else if (templateCode.Equals("CONTRACT_LIQUIDATION", StringComparison.OrdinalIgnoreCase) ||
                 templateCode.Equals("PRINCIPLE_AGREEMENT", StringComparison.OrdinalIgnoreCase))
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung chủ yếu",
                Title = "Phạm vi hợp đồng khung / Biên bản thanh lý",
                Description = "Đối tượng hợp đồng xác định theo từng Đơn đặt hàng (PO) hoặc Quyết toán HĐ gốc",
                LegalReference = "Điều 403, Điều 422 Bộ luật Dân sự 2015",
                Status = LegalCheckItemStatus.Passed,
                Details = "Phù hợp với đặc thù hợp đồng nguyên tắc hoặc biên bản thanh lý."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung chủ yếu",
                Title = "Chưa có danh mục thiết bị / hàng hóa",
                Description = "Hợp đồng mua bán thiết bị cần có bảng kê danh mục sản phẩm cụ thể",
                LegalReference = "Điều 398 Bộ luật Dân sự 2015",
                Status = LegalCheckItemStatus.Warning,
                Details = "Vui lòng thêm ít nhất 01 thiết bị hoặc quy định rõ trong phụ lục chi tiết."
            });
        }

        // 5. Giá trị tài chính và Bằng chữ
        if (totalAmount > 0)
        {
            bool hasWords = !string.IsNullOrWhiteSpace(amountInWords);
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Giá trị & Thanh toán",
                Title = "Giá trị hợp đồng và số tiền bằng chữ",
                Description = $"{totalAmount:N0} VNĐ" + (hasWords ? $" ({amountInWords})" : ""),
                LegalReference = "Nghị định 30/2020/NĐ-CP về thể thức văn bản & Luật Kế toán 2015",
                Status = hasWords ? LegalCheckItemStatus.Passed : LegalCheckItemStatus.Warning,
                Details = hasWords ? "Số tiền bằng số và bằng chữ được thể hiện đầy đủ, thống nhất." : "Cần bổ sung số tiền viết bằng chữ."
            });
        }

        // 6. Điều khoản Phạt vi phạm (Điều 301 Luật Thương mại 2005)
        var penaltyClause = clauses?.FirstOrDefault(c => 
            c.Title.Contains("Phạt", StringComparison.OrdinalIgnoreCase) || 
            c.Content.Contains("phạt vi phạm", StringComparison.OrdinalIgnoreCase));

        if (!isB2C && penaltyClause != null)
        {
            bool has8PercentObligation = penaltyClause.Content.Contains("8%", StringComparison.OrdinalIgnoreCase) && 
                                         penaltyClause.Content.Contains("nghĩa vụ", StringComparison.OrdinalIgnoreCase);
            bool mentionsAllContract = penaltyClause.Content.Contains("8% giá trị hợp đồng", StringComparison.OrdinalIgnoreCase) ||
                                       penaltyClause.Content.Contains("8% tổng giá trị", StringComparison.OrdinalIgnoreCase);

            if (mentionsAllContract && !penaltyClause.Content.Contains("phần nghĩa vụ", StringComparison.OrdinalIgnoreCase))
            {
                itemsList.Add(new LegalCheckItemDto
                {
                    Category = "Chế tài & Bồi thường",
                    Title = "Rủi ro: Phạt vi phạm tính trên toàn bộ giá trị hợp đồng",
                    Description = "Điều khoản phạt 8% trên toàn bộ giá trị hợp đồng có nguy cơ vi phạm Điều 301 LTM 2005",
                    LegalReference = "Điều 301 Luật Thương mại 2005 (Mức phạt tối đa 8% giá trị PHẦN NGHĨA VỤ bị vi phạm)",
                    Status = LegalCheckItemStatus.Warning,
                    Details = "Quy định Điều 301 giới hạn mức phạt tối đa 8% trên giá trị phần nghĩa vụ bị vi phạm. Cần sửa cụm từ 'giá trị hợp đồng' thành 'giá trị phần nghĩa vụ bị vi phạm'."
                });
            }
            else
            {
                itemsList.Add(new LegalCheckItemDto
                {
                    Category = "Chế tài & Bồi thường",
                    Title = "Điều khoản phạt vi phạm hợp đồng (B2B)",
                    Description = "Quy định mức phạt vi phạm nghĩa vụ tuân thủ Điều 301 Luật Thương mại 2005",
                    LegalReference = "Điều 301, 302, 306 Luật Thương mại 2005",
                    Status = LegalCheckItemStatus.Passed,
                    Details = "Mức phạt áp dụng trên giá trị phần nghĩa vụ vi phạm kèm bồi thường thiệt hại thực tế và lãi chậm thanh toán."
                });
            }
        }
        else if (isB2C)
        {
            // Kiểm tra điều khoản không được phép theo Điều 25 Luật BVQLNTD 2023
            bool hasIllegalExclusion = clauses != null && clauses.Any(c => 
                c.Content.Contains("không chịu bất kỳ trách nhiệm", StringComparison.OrdinalIgnoreCase) ||
                c.Content.Contains("không được khiếu nại", StringComparison.OrdinalIgnoreCase));

            if (hasIllegalExclusion)
            {
                itemsList.Add(new LegalCheckItemDto
                {
                    Category = "Bảo vệ người tiêu dùng",
                    Title = "Cảnh báo điều khoản không được phép trong hợp đồng tiêu dùng",
                    Description = "Phát hiện điều khoản hạn chế khiếu nại hoặc loại trừ trách nhiệm của bên bán",
                    LegalReference = "Điều 25 Luật Bảo vệ quyền lợi người tiêu dùng 2023",
                    Status = LegalCheckItemStatus.Failed,
                    Details = "Điều 25 Luật BVQLNTD 2023 tuyên bố vô hiệu đối với các điều khoản loại trừ trách nhiệm của tổ chức kinh doanh hoặc hạn chế quyền khiếu nại của người tiêu dùng."
                });
            }
            else
            {
                itemsList.Add(new LegalCheckItemDto
                {
                    Category = "Bảo vệ người tiêu dùng",
                    Title = "Tuân thủ điều khoản hợp đồng giao kết với người tiêu dùng",
                    Description = "Không chứa điều khoản loại trừ trách nhiệm hoặc xâm phạm quyền người tiêu dùng",
                    LegalReference = "Điều 25, Điều 28 Luật Bảo vệ quyền lợi người tiêu dùng 2023",
                    Status = LegalCheckItemStatus.Passed,
                    Details = "Đảm bảo quyền đổi trả khi có lỗi sản xuất, bảo hành chính hãng và hỗ trợ giải quyết thỏa đáng."
                });
            }
        }

        // 7. Phương thức giao kết và Hiệu lực
        itemsList.Add(new LegalCheckItemDto
        {
            Category = "Giao kết & Chữ ký",
            Title = "Hình thức giao kết và Xác thực",
            Description = executionMethod switch
            {
                ContractExecutionMethod.Electronic => "Hợp đồng điện tử với chữ ký điện tử / chữ ký số",
                ContractExecutionMethod.PaperPrinted => "Hợp đồng in giấy ký tay trực tiếp và đóng dấu",
                _ => "Bản dự thảo đang hoàn thiện (chưa giao kết chính thức)"
            },
            LegalReference = "Luật Giao dịch điện tử số 20/2023/QH15 & Điều 119 Bộ luật Dân sự 2015",
            Status = LegalCheckItemStatus.Passed,
            Details = "Lưu ý: Việc xem trước PDF hoặc lưu bản nháp trong TradeFlow chỉ là thao tác soạn thảo nội bộ, không thay thế cho hành vi ký kết văn bản hợp pháp của các bên."
        });

        // 8. Đánh giá trạng thái tổng thể
        report.Items = itemsList;
        if (itemsList.Any(x => x.Status == LegalCheckItemStatus.Failed))
        {
            report.OverallStatus = itemsList.Any(x => x.Category.Contains("Bảo vệ") || x.Category.Contains("Chế tài")) 
                ? LegalComplianceStatus.PotentialLegalRisk 
                : LegalComplianceStatus.MissingInfo;
        }
        else if (itemsList.Any(x => x.Status == LegalCheckItemStatus.Warning))
        {
            report.OverallStatus = LegalComplianceStatus.RequiresLegalReview;
        }
        else
        {
            report.OverallStatus = LegalComplianceStatus.DataComplete;
        }

        return report;
    }

        public static ContractLegalComplianceReportDto EvaluateEmploymentContract(EmploymentContractDto dto)
    {
        return EvaluateEmploymentContract(
            dto.ContractType,
            dto.EmployerName ?? string.Empty,
            dto.EmployerTaxCode ?? string.Empty,
            dto.EmployerAddress ?? string.Empty,
            dto.EmployerRepresentativeName ?? string.Empty,
            dto.EmployerRepresentativePosition ?? string.Empty,
            dto.EmployerRepresentativeAuthority ?? string.Empty,
            dto.EmployeeFullName ?? string.Empty,
            dto.EmployeeNationalId ?? string.Empty,
            dto.EmployeeDateOfBirth,
            dto.EmployeeAddress ?? string.Empty,
            dto.StartDate,
            dto.EndDate,
            dto.JobTitle ?? string.Empty,
            dto.Workplace ?? string.Empty,
            dto.BasicSalary,
            dto.SalaryType ?? "Lương gộp (Gross)",
            dto.WorkingHoursPerWeek,
            dto.AnnualLeaveDays,
            dto.SocialInsuranceRate,
            dto.Clauses,
            dto.Status
        );
    }

    public static ContractLegalComplianceReportDto EvaluateEmploymentContract(
        EmploymentContractType contractType,
        string employerName,
        string employerTaxCode,
        string employerAddress,
        string employerRepresentative,
        string employerPosition,
        string employerAuthority,
        string employeeFullName,
        string employeeNationalId,
        DateTime? employeeDob,
        string employeeAddress,
        DateTime startDate,
        DateTime? endDate,
        string jobTitle,
        string workplace,
        decimal? basicSalary,
        string salaryType,
        int workingHoursPerWeek,
        int annualLeaveDays,
        decimal socialInsuranceRate,
        IReadOnlyList<EmploymentContractClauseDto>? clauses,
        EmploymentContractStatus status)
    {
        var report = new ContractLegalComplianceReportDto();
        var itemsList = new List<LegalCheckItemDto>();

        // 1. Loại hợp đồng theo Điều 20 Bộ luật Lao động (VBHN 18/VBHN-VPQH)
        if (contractType == EmploymentContractType.FixedTerm)
        {
            if (!endDate.HasValue)
            {
                itemsList.Add(new LegalCheckItemDto
                {
                    Category = "Loại hợp đồng",
                    Title = "Thiếu ngày kết thúc hợp đồng xác định thời hạn",
                    Description = "Hợp đồng lao động xác định thời hạn bắt buộc phải có ngày kết thúc",
                    LegalReference = "Điểm b Khoản 1 Điều 20 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                    Status = LegalCheckItemStatus.Failed,
                    Details = "Vui lòng nhập ngày kết thúc hợp đồng."
                });
            }
            else
            {
                var months = ((endDate.Value.Year - startDate.Year) * 12) + endDate.Value.Month - startDate.Month;
                if (months > 36)
                {
                    itemsList.Add(new LegalCheckItemDto
                    {
                        Category = "Loại hợp đồng",
                        Title = "Thời hạn hợp đồng vượt quá 36 tháng",
                        Description = $"Thời hạn hợp đồng hiện tại là {months} tháng, vượt giới hạn 36 tháng",
                        LegalReference = "Điểm b Khoản 1 Điều 20 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                        Status = LegalCheckItemStatus.Failed,
                        Details = "Khoản 1 Điều 20 quy định HĐLĐ xác định thời hạn có thời hạn không quá 36 tháng kể từ thời điểm có hiệu lực."
                    });
                }
                else
                {
                    itemsList.Add(new LegalCheckItemDto
                    {
                        Category = "Loại hợp đồng",
                        Title = "Hợp đồng lao động xác định thời hạn hợp lệ",
                        Description = $"Thời hạn từ {startDate:dd/MM/yyyy} đến {endDate.Value:dd/MM/yyyy} ({months} tháng)",
                        LegalReference = "Điểm b Khoản 1 Điều 20 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                        Status = LegalCheckItemStatus.Passed,
                        Details = "Thời hạn không quá 36 tháng theo đúng quy định luật định."
                    });
                }
            }
        }
        else if (contractType == EmploymentContractType.IndefiniteTerm)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Loại hợp đồng",
                Title = "Hợp đồng lao động không xác định thời hạn",
                Description = $"Bắt đầu có hiệu lực từ ngày {startDate:dd/MM/yyyy}",
                LegalReference = "Điểm a Khoản 1 Điều 20 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Passed,
                Details = "Hai bên không xác định thời hạn và thời điểm chấm dứt hiệu lực."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Loại hợp đồng",
                Title = contractType switch
                {
                    EmploymentContractType.Probation => "Hợp đồng thử việc (Điều 24 BLLĐ)",
                    EmploymentContractType.VocationalTraining => "Hợp đồng đào tạo nghề (Điều 61 BLLĐ)",
                    EmploymentContractType.Internship => "Thỏa thuận thực tập nghiệp vụ",
                    _ => "Văn bản quan hệ lao động"
                },
                Description = $"Bắt đầu từ {startDate:dd/MM/yyyy}" + (endDate.HasValue ? $" đến {endDate.Value:dd/MM/yyyy}" : ""),
                LegalReference = "Bộ luật Lao động (VBHN 18/VBHN-VPQH ngày 12/02/2026)",
                Status = LegalCheckItemStatus.Passed,
                Details = "Phù hợp với đặc thù giao dịch học nghề, thử việc hoặc thực tập."
            });
        }

        // 2. Thẩm quyền ký phía Người sử dụng lao động (Điều 18 BLLĐ)
        bool hasEmployer = !string.IsNullOrWhiteSpace(employerName) && !string.IsNullOrWhiteSpace(employerTaxCode);
        bool hasRep = !string.IsNullOrWhiteSpace(employerRepresentative);

        if (hasEmployer && hasRep)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Chủ thể giao kết",
                Title = "Thẩm quyền giao kết phía Người sử dụng lao động",
                Description = $"{employerName} — Đại diện: {employerRepresentative} ({employerPosition ?? "Lãnh đạo"})",
                LegalReference = "Điều 18 Bộ luật Lao động (VBHN 18/VBHN-VPQH ngày 12/02/2026)",
                Status = LegalCheckItemStatus.Passed,
                Details = $"Căn cứ thẩm quyền: {employerAuthority ?? "Đại diện theo pháp luật hoặc giấy ủy quyền hợp lệ"}."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Chủ thể giao kết",
                Title = "Thiếu thông tin người có thẩm quyền giao kết phía NSDLĐ",
                Description = "Chưa có tên đại diện hoặc thông tin pháp nhân người sử dụng lao động",
                LegalReference = "Điều 18, Điều 21 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Failed,
                Details = "Người giao kết phải là người đại diện theo pháp luật hoặc người được ủy quyền hợp pháp bằng văn bản."
            });
        }

        // 3. Thông tin Người lao động (Điểm b Khoản 1 Điều 21 BLLĐ)
        bool hasEmployee = !string.IsNullOrWhiteSpace(employeeFullName);
        bool hasNatId = !string.IsNullOrWhiteSpace(employeeNationalId);
        bool hasDob = employeeDob.HasValue;
        bool hasAddr = !string.IsNullOrWhiteSpace(employeeAddress);

        if (hasEmployee && hasNatId && hasDob && hasAddr)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Chủ thể giao kết",
                Title = "Thông tin định danh Người lao động",
                Description = $"{employeeFullName} (Sinh ngày: {employeeDob:dd/MM/yyyy}, CCCD/Hộ chiếu: {employeeNationalId})",
                LegalReference = "Điểm b Khoản 1 Điều 21 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Passed,
                Details = "Đầy đủ họ tên, ngày sinh, nơi cư trú và số giấy tờ định danh cá nhân."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Chủ thể giao kết",
                Title = "Thiếu thông tin bắt buộc của Người lao động",
                Description = "Chưa đầy đủ họ tên, ngày sinh, CCCD hoặc địa chỉ cư trú của NLĐ",
                LegalReference = "Điểm b Khoản 1 Điều 21 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Warning,
                Details = "Cần cập nhật hồ sơ nhân viên để đảm bảo giá trị pháp lý của văn bản giao kết."
            });
        }

        // 4. Công việc và địa điểm làm việc (Điểm c Khoản 1 Điều 21 BLLĐ)
        bool hasJob = !string.IsNullOrWhiteSpace(jobTitle);
        bool hasWorkplace = !string.IsNullOrWhiteSpace(workplace);
        if (hasJob && hasWorkplace)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung bắt buộc",
                Title = "Công việc và địa điểm làm việc",
                Description = $"Vị trí: {jobTitle} — Địa điểm: {workplace}",
                LegalReference = "Điểm c Khoản 1 Điều 21 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Passed,
                Details = "Xác định rõ ràng chức danh công việc và địa điểm thực hiện hợp đồng."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung bắt buộc",
                Title = "Chưa có chức danh công việc hoặc địa điểm làm việc",
                Description = "Điểm c Khoản 1 Điều 21 quy định bắt buộc phải có công việc và địa điểm làm việc",
                LegalReference = "Điểm c Khoản 1 Điều 21 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Failed,
                Details = "Vui lòng nhập vị trí công việc và địa điểm làm việc cụ thể."
            });
        }

        // 5. Tiền lương, phụ cấp và hình thức trả lương (Điểm đ Khoản 1 Điều 21 BLLĐ)
        bool hasSalary = basicSalary.HasValue && basicSalary.Value > 0;
        if (hasSalary)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung bắt buộc",
                Title = "Mức lương, phụ cấp và hình thức chi trả",
                Description = $"Lương chính: {basicSalary:N0} đ/tháng ({salaryType ?? "Gross"})",
                LegalReference = "Điểm đ Khoản 1 Điều 21, Điều 90, 95 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Passed,
                Details = "Đã thỏa thuận mức lương cụ thể, hình thức và kỳ hạn trả lương minh bạch."
            });
        }
        else if (contractType == EmploymentContractType.Internship)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung bắt buộc",
                Title = "Khoản hỗ trợ kinh phí thực tập",
                Description = "Áp dụng mức phụ cấp hỗ trợ sinh viên thực tập",
                LegalReference = "Quy chế đào tạo & Luật Giáo dục nghề nghiệp",
                Status = LegalCheckItemStatus.Passed,
                Details = "Phụ cấp thực tập được chi trả theo thỏa thuận tiếp nhận."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Nội dung bắt buộc",
                Title = "Chưa ghi nhận mức lương chính thức",
                Description = "Mức lương là nội dung chủ yếu bắt buộc theo Điều 21 BLLĐ",
                LegalReference = "Điểm đ Khoản 1 Điều 21 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Warning,
                Details = "Khuyến nghị điền số tiền lương cụ thể thay vì chỉ ghi thỏa thuận chung chung."
            });
        }

        // 6. Thời giờ làm việc, nghỉ ngơi & Bảo hiểm (Điểm g, i Khoản 1 Điều 21 BLLĐ)
        itemsList.Add(new LegalCheckItemDto
        {
            Category = "Nội dung bắt buộc",
            Title = "Thời giờ làm việc, thời giờ nghỉ ngơi và chế độ Bảo hiểm",
            Description = $"{workingHoursPerWeek} giờ/tuần; Phép năm: {annualLeaveDays} ngày; Tỷ lệ BHXH-BHYT-BHTN: {socialInsuranceRate}%",
            LegalReference = "Điểm g, Điểm i Khoản 1 Điều 21, Điều 105, 112, 113, 168 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
            Status = (workingHoursPerWeek <= 48 && annualLeaveDays >= 12) ? LegalCheckItemStatus.Passed : LegalCheckItemStatus.Warning,
            Details = "Thời giờ làm việc bình thường không quá 48 giờ/tuần (Đ105); nghỉ phép năm tối thiểu 12 ngày làm việc (Đ113)."
        });

        // 7. Bảo mật và Không xử phạt trái luật
        bool hasIllegalPenalty = clauses != null && clauses.Any(c => 
            c.Content != null && (
                c.Content.Contains("phạt tiền", StringComparison.OrdinalIgnoreCase) ||
                c.Content.Contains("cắt lương", StringComparison.OrdinalIgnoreCase) ||
                c.Content.Contains("trừ lương trái luật", StringComparison.OrdinalIgnoreCase)
            ));

        if (hasIllegalPenalty)
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Tuân thủ kỷ luật lao động",
                Title = "Phát hiện điều khoản phạt tiền / trừ lương trái luật",
                Description = "Pháp luật lao động nghiêm cấm dùng hình thức phạt tiền, cắt lương thay xử lý kỷ luật",
                LegalReference = "Khoản 2 Điều 127 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Failed,
                Details = "Điều 127 nghiêm cấm: Phạt tiền, cắt lương thay việc xử lý kỷ luật lao động. Cần xóa hoặc điều chỉnh điều khoản phạt tiền NLĐ."
            });
        }
        else
        {
            itemsList.Add(new LegalCheckItemDto
            {
                Category = "Tuân thủ kỷ luật lao động",
                Title = "Quy định trách nhiệm vật chất & bảo mật thông tin",
                Description = "Thỏa thuận bảo vệ bí mật kinh doanh và bồi thường chi phí đào tạo phù hợp luật định",
                LegalReference = "Khoản 2 Điều 21 & Điều 62 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
                Status = LegalCheckItemStatus.Passed,
                Details = "Tuân thủ quyền bảo vệ bí mật công nghệ mà không xâm phạm quyền lợi hợp pháp của người lao động."
            });
        }

        // 8. Lập thành 02 bản chính
        itemsList.Add(new LegalCheckItemDto
        {
            Category = "Giao kết & Số bản",
            Title = "Số lượng bản hợp đồng và Quyền lưu giữ",
            Description = "Hợp đồng lao động bằng văn bản được lập thành 02 bản (NSLĐ giữ 01 bản, NLĐ giữ 01 bản)",
            LegalReference = "Khoản 1 Điều 14 Bộ luật Lao động (VBHN 18/VBHN-VPQH)",
            Status = LegalCheckItemStatus.Passed,
            Details = "Người sử dụng lao động có nghĩa vụ giao 01 bản chính cho người lao động ngay sau khi ký kết."
        });

        report.Items = itemsList;
        if (itemsList.Any(x => x.Status == LegalCheckItemStatus.Failed))
        {
            report.OverallStatus = itemsList.Any(x => x.Category.Contains("kỷ luật")) 
                ? LegalComplianceStatus.PotentialLegalRisk 
                : LegalComplianceStatus.MissingInfo;
        }
        else if (itemsList.Any(x => x.Status == LegalCheckItemStatus.Warning))
        {
            report.OverallStatus = LegalComplianceStatus.RequiresLegalReview;
        }
        else
        {
            report.OverallStatus = LegalComplianceStatus.DataComplete;
        }

        return report;
    }
}
