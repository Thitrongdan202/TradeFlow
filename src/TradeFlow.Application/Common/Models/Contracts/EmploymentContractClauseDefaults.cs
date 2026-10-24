using System;
using System.Collections.Generic;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Contracts;

public static class EmploymentContractClauseDefaults
{
    public static List<EmploymentContractClauseDto> GenerateDefaultClauses(EmploymentContractDto contract)
    {
        var list = new List<EmploymentContractClauseDto>();
        int order = 1;

        // Điều 1: Công việc, địa điểm làm việc và thời hạn hợp đồng
        string article1Content;
        if (contract.ContractType == EmploymentContractType.Internship)
        {
            article1Content = $"1.1. Chương trình thực tập: {contract.InternshipProgramme ?? "Thực tập nghiệp vụ và rèn luyện kỹ năng thực tiễn"}.\n" +
                              $"1.2. Vị trí thực tập / bộ phận: {contract.JobTitle} - {contract.DepartmentName}.\n" +
                              $"1.3. Cán bộ hướng dẫn trực tiếp: {contract.TrainingSupervisor ?? "Trưởng bộ phận chỉ định"}.\n" +
                              $"1.4. Thời hạn thực tập: Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "theo thỏa thuận")}.\n" +
                              $"1.5. Mục tiêu thực tập: {contract.TrainingObjectives ?? "Làm quen môi trường làm việc thực tế và nâng cao kỹ năng nghề nghiệp"}.\n" +
                              $"1.6. Địa điểm thực tập: {contract.Workplace}.";
        }
        else if (contract.ContractType == EmploymentContractType.VocationalTraining)
        {
            article1Content = $"1.1. Nghề đào tạo / vị trí tập nghề: {contract.JobTitle} tại {contract.DepartmentName} (Căn cứ Điều 61 Bộ luật Lao động).\n" +
                              $"1.2. Người dạy nghề / hướng dẫn: {contract.TrainingSupervisor ?? "Chuyên gia đào tạo của Công ty"}.\n" +
                              $"1.3. Thời hạn đào tạo nghề: Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "theo đề cương")}.\n" +
                              $"1.4. Đề cương & mục tiêu đào tạo: {contract.TrainingObjectives ?? "Đạt chứng chỉ kỹ năng nghề nội bộ và nắm vững quy trình tác nghiệp"}.\n" +
                              $"1.5. Địa điểm đào tạo và thực hành: {contract.Workplace}.";
        }
        else if (contract.ContractType == EmploymentContractType.Probation)
        {
            var duration = contract.ProbationDurationDays.HasValue ? $"{contract.ProbationDurationDays.Value} ngày" : "theo luật định";
            article1Content = $"1.1. Loại hợp đồng: Hợp đồng thử việc theo Điều 24, 25 Bộ luật Lao động.\n" +
                              $"1.2. Chức danh / Công việc thử việc: {contract.JobTitle} thuộc {contract.DepartmentName}.\n" +
                              $"1.3. Thời hạn thử việc: Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "")} (Thời gian: {duration}).\n" +
                              $"1.4. Địa điểm làm việc: {contract.Workplace}.\n" +
                              $"1.5. Nhiệm vụ chính: {(!string.IsNullOrWhiteSpace(contract.JobDescription) ? contract.JobDescription : "Thực hiện các công việc chuyên môn theo bản mô tả công việc và phân công của Quản lý trực tiếp")}.";
        }
        else
        {
            var termStr = contract.ContractType == EmploymentContractType.IndefiniteTerm
                ? $"Không xác định thời hạn, bắt đầu có hiệu lực từ ngày {contract.StartDate:dd/MM/yyyy}."
                : $"Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "theo thỏa thuận")} (theo Điều 20 Bộ luật Lao động).";

            article1Content = $"1.1. Loại hợp đồng: {contract.ContractTypeName}.\n" +
                              $"1.2. Thời hạn hợp đồng: {termStr}\n" +
                              $"1.3. Chức danh / Vị trí công việc: {contract.JobTitle} thuộc {contract.DepartmentName}.\n" +
                              $"1.4. Địa điểm làm việc: {contract.Workplace}.\n" +
                              $"1.5. Mô tả nhiệm vụ chính: {(!string.IsNullOrWhiteSpace(contract.JobDescription) ? contract.JobDescription : "Chịu trách nhiệm thực hiện các nhiệm vụ chuyên môn theo chức danh và phân công của Lãnh đạo đơn vị")}.";
        }

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_1",
            ArticleNumber = "Điều 1",
            Title = (contract.ContractType == EmploymentContractType.Internship || contract.ContractType == EmploymentContractType.VocationalTraining)
                ? "Chương trình, vị trí đào tạo và thời hạn"
                : "Công việc, địa điểm làm việc và thời hạn hợp đồng",
            Content = article1Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // Điều 2: Thời giờ làm việc và nghỉ ngơi
        string article2Content = $"2.1. Thời giờ làm việc: {contract.WorkingHoursPerWeek} giờ/tuần. Lịch làm việc: {(!string.IsNullOrWhiteSpace(contract.WorkingSchedule) ? contract.WorkingSchedule : "Từ Thứ Hai đến Thứ Bảy, sáng 08h00 - 12h00, chiều 13h30 - 17h30")}.\n" +
                                 $"2.2. Chế độ nghỉ ngơi: Nghỉ hàng tuần theo lịch công tác; nghỉ ngày Lễ, Tết hưởng nguyên lương theo quy định tại Điều 112 Bộ luật Lao động.\n" +
                                 $"2.3. Nghỉ phép năm: Được hưởng {contract.AnnualLeaveDays} ngày nghỉ phép năm hưởng nguyên lương theo quy định tại Điều 113 Bộ luật Lao động.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_2",
            ArticleNumber = "Điều 2",
            Title = "Thời giờ làm việc và thời giờ nghỉ ngơi",
            Content = article2Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // Điều 3: Tiền lương, phụ cấp và hình thức chi trả
        string article3Content;
        if (!string.IsNullOrWhiteSpace(contract.SalaryClauseText))
        {
            article3Content = contract.SalaryClauseText;
        }
        else if (contract.ContractType == EmploymentContractType.Internship)
        {
            article3Content = $"3.1. Hỗ trợ kinh phí thực tập: {(contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0 ? $"{contract.BasicSalary.Value:N0} đ/tháng" : "Khoản phụ cấp hỗ trợ thù lao theo thỏa thuận và quy chế đào tạo sinh viên của Công ty")}.\n" +
                              $"3.2. Hình thức và kỳ hạn thanh toán: {contract.PaymentPeriod}. Hình thức: {contract.PaymentMethod}.\n" +
                              $"3.3. Các hỗ trợ khác: Ăn trưa và trang bị phương tiện làm việc tại văn phòng/showroom.";
        }
        else if (contract.ContractType == EmploymentContractType.VocationalTraining)
        {
            article3Content = $"3.1. Tiền công trong thời gian học nghề, tập nghề: {(contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0 ? $"{contract.BasicSalary.Value:N0} đ/tháng" : "Hưởng thù lao học việc theo thỏa thuận phù hợp với quy định tại Khoản 5 Điều 61 Bộ luật Lao động")}.\n" +
                              $"3.2. Chi phí đào tạo: Công ty tài trợ toàn bộ chi phí giáo trình và trang thiết bị thực hành.\n" +
                              $"3.3. Kỳ hạn thanh toán: {contract.PaymentPeriod}. Hình thức: {contract.PaymentMethod}.";
        }
        else
        {
            string salaryStr = (contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0)
                ? $"{contract.BasicSalary.Value:N0} đồng/tháng ({contract.SalaryType})"
                : "Thỏa thuận theo phụ lục chi tiết và quy chế nội bộ Công ty";

            string probNote = string.Empty;
            if (contract.ContractType == EmploymentContractType.Probation && contract.ProbationSalaryPercentage.HasValue && contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0)
            {
                var probAmount = contract.BasicSalary.Value * (contract.ProbationSalaryPercentage.Value / 100m);
                probNote = $"\n   * Mức lương thử việc: {contract.ProbationSalaryPercentage.Value}% lương chính, tương đương {probAmount:N0} đ/tháng theo Điều 26 BLLĐ.";
            }

            string allowanceStr = $"Phụ cấp chức vụ: {(contract.PositionAllowance.HasValue ? $"{contract.PositionAllowance.Value:N0} đ/tháng" : "Theo quy chế")}; Phụ cấp cơm trưa: {(contract.LunchAllowance.HasValue ? $"{contract.LunchAllowance.Value:N0} đ/tháng" : "Theo quy chế")}; Phụ cấp khác: {(contract.OtherAllowances.HasValue ? $"{contract.OtherAllowances.Value:N0} đ/tháng" : "Theo quy chế")}.";

            article3Content = $"3.1. Mức lương cơ bản: {salaryStr}.{probNote}\n" +
                              $"3.2. Các khoản phụ cấp: {allowanceStr}\n" +
                              $"3.3. Kỳ hạn thanh toán: {contract.PaymentPeriod}. Hình thức: {contract.PaymentMethod}.\n" +
                              $"3.4. Chế độ nâng lương: {contract.SalaryReviewPolicy}.";
        }

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_3",
            ArticleNumber = "Điều 3",
            Title = "Tiền lương, phụ cấp và hình thức chi trả",
            Content = article3Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // Điều 4: Bảo hộ lao động và bảo hiểm xã hội
        string article4Content = contract.ContractType == EmploymentContractType.Internship || contract.ContractType == EmploymentContractType.VocationalTraining
            ? $"4.1. Trang thiết bị an toàn: Người học việc/thực tập sinh được trang bị phương tiện bảo hộ lao động và hướng dẫn an toàn vệ sinh lao động.\n" +
              $"4.2. Bảo hiểm: Đối với thực tập sinh/học nghề, thực hiện bảo hiểm tai nạn lao động theo quy định chuyên ngành."
            : $"4.1. Trang bị bảo hộ lao động: {contract.LaborProtection}.\n" +
              $"4.2. Bảo hiểm xã hội, BHYT, BHTN: Hai bên thực hiện nghĩa vụ đóng bảo hiểm theo tỷ lệ luật định (tổng cộng {contract.SocialInsuranceRate}%: NSDLĐ đóng 21.5%, NLĐ đóng 10.5%). {(contract.ContractType == EmploymentContractType.Probation ? "Đối với hợp đồng thử việc, tiền bảo hiểm được tính gộp vào tiền lương chi trả trực tiếp cho người lao động." : "")}";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_4",
            ArticleNumber = "Điều 4",
            Title = "Bảo hộ lao động và chế độ bảo hiểm",
            Content = article4Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // Điều 5: Quyền và nghĩa vụ của người sử dụng lao động và người lao động
        string article5Content = $"5.1. Quyền và nghĩa vụ của Bên A (Người sử dụng lao động):\n" +
                                 $"   - Quản lý, điều hành và bố trí công việc phù hợp với năng lực chuyên môn và thỏa thuận.\n" +
                                 $"   - Đảm bảo thanh toán đầy đủ, đúng hạn tiền lương và các chế độ phúc lợi đã cam kết.\n" +
                                 $"   - Ban hành và yêu cầu tuân thủ Nội quy lao động, quy trình tác nghiệp của Doanh nghiệp.\n" +
                                 $"5.2. Quyền và nghĩa vụ của Bên B (Người lao động / Thực tập sinh):\n" +
                                 $"   - Chấp hành nghiêm chỉnh kỷ luật lao động, nội quy an toàn và bảo mật thông tin.\n" +
                                 $"   - Hoàn thành nhiệm vụ công việc được giao với tinh thần trách nhiệm và tính chuyên nghiệp cao.\n" +
                                 $"   - Được hưởng đầy đủ các quyền lợi về lương thưởng, bảo hiểm và bảo hộ lao động theo pháp luật.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_5",
            ArticleNumber = "Điều 5",
            Title = "Quyền và nghĩa vụ của các bên",
            Content = article5Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // Điều 6: Thỏa thuận bảo mật thông tin & Bí mật kinh doanh (Tùy chọn)
        string article6Content = !string.IsNullOrWhiteSpace(contract.NdaClauseDetails)
            ? contract.NdaClauseDetails
            : "6.1. Người lao động cam kết giữ bí mật toàn bộ thông tin kinh doanh, danh sách khách hàng, công thức giá, bí mật công nghệ và tài sản trí tuệ của Doanh nghiệp.\n" +
              "6.2. Không sao chép, cung cấp hoặc tiết lộ dữ liệu doanh nghiệp cho bất kỳ bên thứ ba nào khi chưa có văn bản đồng thuận của Lãnh đạo.\n" +
              "6.3. Cam kết bảo mật có hiệu lực trong suốt thời gian làm việc và tối thiểu 02 năm kể từ ngày chấm dứt quan hệ lao động.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_6",
            ArticleNumber = "Điều 6",
            Title = "Thỏa thuận bảo mật thông tin & Bí mật kinh doanh (NDA)",
            Content = article6Content,
            IsMandatory = false,
            IsOptional = true,
            DisplayOrder = order++
        });

        // Điều 7: Cam kết đào tạo nghề & Bồi hoàn chi phí (Tùy chọn)
        if (contract.HasTrainingReimbursement || contract.ContractType == EmploymentContractType.VocationalTraining)
        {
            string article7Content = !string.IsNullOrWhiteSpace(contract.TrainingClauseDetails)
                ? contract.TrainingClauseDetails
                : "7.1. Trường hợp người lao động được Doanh nghiệp tài trợ tham gia các khóa đào tạo chuyên sâu hoặc học nghề, người lao động cam kết phục vụ tối thiểu 24 tháng sau khóa học.\n" +
                  "7.2. Trường hợp tự ý chấm dứt hợp đồng trước thời hạn cam kết hoặc bị sa thải do kỷ luật, người lao động có trách nhiệm bồi hoàn chi phí đào tạo theo quy định tại Điều 62 Bộ luật Lao động.";

            list.Add(new EmploymentContractClauseDto
            {
                Id = "clause_7",
                ArticleNumber = "Điều 7",
                Title = "Cam kết đào tạo & Bồi hoàn chi phí đào tạo",
                Content = article7Content,
                IsMandatory = false,
                IsOptional = true,
                DisplayOrder = order++
            });
        }

        // Điều 8: Chế độ phúc lợi nội bộ LACASA (Tùy chọn)
        if (contract.HasSpecialBenefits || !string.IsNullOrWhiteSpace(contract.SpecialBenefitsDetails))
        {
            string article8Content = !string.IsNullOrWhiteSpace(contract.SpecialBenefitsDetails)
                ? contract.SpecialBenefitsDetails
                : "8.1. Được xét thưởng lương tháng 13, thưởng năng suất KPI theo quy chế tài chính của Công ty.\n" +
                  "8.2. Tham gia các hoạt động teambuilding, nghỉ dưỡng hàng năm do Công ty tổ chức.\n" +
                  "8.3. Hưởng các chính sách trợ cấp hiếu hỷ, sinh nhật và quà tặng lễ tết.";

            list.Add(new EmploymentContractClauseDto
            {
                Id = "clause_8",
                ArticleNumber = "Điều 8",
                Title = "Chế độ phúc lợi nội bộ Doanh nghiệp",
                Content = article8Content,
                IsMandatory = false,
                IsOptional = true,
                DisplayOrder = order++
            });
        }

        // Điều khoản tùy biến bổ sung (nếu có)
        if (!string.IsNullOrWhiteSpace(contract.CustomTerms))
        {
            list.Add(new EmploymentContractClauseDto
            {
                Id = "clause_custom",
                ArticleNumber = $"Điều {order}",
                Title = "Thỏa thuận bổ sung đặc thù",
                Content = contract.CustomTerms,
                IsMandatory = false,
                IsOptional = true,
                DisplayOrder = order++
            });
        }

        // Điều khoản thi hành & Hiệu lực pháp lý
        string finalArticleContent = $"Hợp đồng có hiệu lực thi hành kể từ ngày hai bên hoàn tất việc ký kết hợp pháp.\n" +
                                     $"Hợp đồng được lập thành 02 bản có giá trị pháp lý như nhau, mỗi bên giữ 01 bản để theo dõi và thực hiện.\n" +
                                     $"Trường hợp giao kết bằng hợp đồng điện tử theo Nghị định 337/2025/NĐ-CP, tệp dữ liệu số lưu trữ trên hệ thống TradeFlow có giá trị toàn vẹn và có giá trị chứng cứ pháp lý.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_final",
            ArticleNumber = $"Điều {order}",
            Title = "Điều khoản thi hành & Hiệu lực pháp lý",
            Content = finalArticleContent,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order
        });

        return list;
    }
}
