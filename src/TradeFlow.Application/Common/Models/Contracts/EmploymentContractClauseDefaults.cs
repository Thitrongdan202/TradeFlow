using System;
using System.Collections.Generic;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Contracts;

/// <summary>
/// Trình tạo điều khoản mẫu chuẩn hóa cho Hợp đồng lao động theo Bộ luật Lao động
/// (Văn bản hợp nhất số 18/VBHN-VPQH ngày 12/02/2026 & Điều 21 BLLĐ)
/// </summary>
public static class EmploymentContractClauseDefaults
{
    public static List<EmploymentContractClauseDto> GenerateDefaultClauses(EmploymentContractDto contract)
    {
        var list = new List<EmploymentContractClauseDto>();
        int order = 1;

        // ==========================================
        // ĐIỀU 1: CÔNG VIỆC, ĐỊA ĐIỂM VÀ THỜI HẠN (Điểm c, d Khoản 1 Điều 21 BLLĐ)
        // ==========================================
        string article1Content;
        if (contract.ContractType == EmploymentContractType.Internship)
        {
            article1Content = $"1.1. Chương trình thực tập: {contract.InternshipProgramme ?? "Thực tập nghiệp vụ và rèn luyện kỹ năng thực tiễn chuyên ngành"}.\n" +
                              $"1.2. Vị trí thực tập / bộ phận: {contract.JobTitle} - Phòng ban: {contract.DepartmentName}.\n" +
                              $"1.3. Người hướng dẫn chuyên môn: {contract.TrainingSupervisor ?? "Trưởng bộ phận chỉ định"}.\n" +
                              $"1.4. Thời hạn thực tập: Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "theo thỏa thuận tiếp nhận")}.\n" +
                              $"1.5. Mục tiêu thực tập: {contract.TrainingObjectives ?? "Làm quen môi trường tác nghiệp thực tế, nắm vững quy trình và nâng cao kỹ năng nghề nghiệp"}.\n" +
                              $"1.6. Địa điểm thực tập: {contract.Workplace}.";
        }
        else if (contract.ContractType == EmploymentContractType.VocationalTraining)
        {
            article1Content = $"1.1. Nghề đào tạo / vị trí tập nghề: {contract.JobTitle} tại {contract.DepartmentName} (Căn cứ Điều 61 Bộ luật Lao động).\n" +
                              $"1.2. Người dạy nghề / hướng dẫn kỹ thuật: {contract.TrainingSupervisor ?? "Chuyên gia đào tạo của Công ty"}.\n" +
                              $"1.3. Thời hạn đào tạo nghề: Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "theo đề cương đào tạo")}.\n" +
                              $"1.4. Mục tiêu đào tạo: {contract.TrainingObjectives ?? "Nắm vững kỹ năng thao tác chuẩn, quy chuẩn an toàn và vượt qua kỳ kiểm tra đánh giá tay nghề nội bộ"}.\n" +
                              $"1.5. Địa điểm đào tạo và thực hành: {contract.Workplace}.";
        }
        else if (contract.ContractType == EmploymentContractType.Probation)
        {
            var duration = contract.ProbationDurationDays.HasValue ? $"{contract.ProbationDurationDays.Value} ngày" : "theo quy định tại Điều 25 BLLĐ";
            article1Content = $"1.1. Loại hợp đồng: Hợp đồng thử việc theo Điều 24 & Điều 25 Bộ luật Lao động.\n" +
                              $"1.2. Chức danh / Vị trí thử việc: {contract.JobTitle} thuộc {contract.DepartmentName}.\n" +
                              $"1.3. Thời hạn thử việc: Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "")} (Thời lượng: {duration}).\n" +
                              $"1.4. Địa điểm làm việc: {contract.Workplace}.\n" +
                              $"1.5. Nhiệm vụ chính: {(!string.IsNullOrWhiteSpace(contract.JobDescription) ? contract.JobDescription : "Thực hiện các công việc chuyên môn theo bản mô tả công việc và phân công của Quản lý trực tiếp")}.";
        }
        else
        {
            var termStr = contract.ContractType == EmploymentContractType.IndefiniteTerm
                ? $"Không xác định thời hạn (Điểm a Khoản 1 Điều 20 Bộ luật Lao động), bắt đầu có hiệu lực từ ngày {contract.StartDate:dd/MM/yyyy}."
                : $"Xác định thời hạn (Điểm b Khoản 1 Điều 20 Bộ luật Lao động): Từ ngày {contract.StartDate:dd/MM/yyyy} đến ngày {(contract.EndDate.HasValue ? contract.EndDate.Value.ToString("dd/MM/yyyy") : "theo thỏa thuận")} (thời hạn không quá 36 tháng).";

            article1Content = $"1.1. Loại hợp đồng lao động: {contract.ContractTypeName}.\n" +
                              $"1.2. Thời hạn hợp đồng: {termStr}\n" +
                              $"1.3. Chức danh / Vị trí công việc: {contract.JobTitle} thuộc phòng ban / bộ phận: {contract.DepartmentName}.\n" +
                              $"1.4. Địa điểm làm việc: {contract.Workplace}.\n" +
                              $"1.5. Nhiệm vụ và trách nhiệm chính: {(!string.IsNullOrWhiteSpace(contract.JobDescription) ? contract.JobDescription : "Chịu trách nhiệm thực hiện các nhiệm vụ chuyên môn theo chức danh và phân công của Lãnh đạo đơn vị")}.";
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

        // ==========================================
        // ĐIỀU 2: THỜI GIỜ LÀM VIỆC & NGHỈ NGƠI (Điểm g Khoản 1 Điều 21 BLLĐ)
        // ==========================================
        string article2Content = $"2.1. Thời giờ làm việc: {contract.WorkingHoursPerWeek} giờ/tuần (không quá 48 giờ/tuần theo Điều 105 Bộ luật Lao động). Lịch làm việc: {(!string.IsNullOrWhiteSpace(contract.WorkingSchedule) ? contract.WorkingSchedule : "Từ Thứ Hai đến Thứ Bảy, sáng 08h00 - 12h00, chiều 13h30 - 17h30")}.\n" +
                                 $"2.2. Chế độ nghỉ ngơi: Được nghỉ hàng tuần theo quy chế công tác; nghỉ các ngày Lễ, Tết hưởng nguyên lương theo quy định tại Điều 112 Bộ luật Lao động.\n" +
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

        // ==========================================
        // ĐIỀU 3: TIỀN LƯƠNG, PHỤ CẤP VÀ KỲ HẠN TRẢ LƯƠNG (Điểm đ, e Khoản 1 Điều 21 BLLĐ)
        // ==========================================
        string article3Content;
        if (!string.IsNullOrWhiteSpace(contract.SalaryClauseText))
        {
            article3Content = contract.SalaryClauseText;
        }
        else if (contract.ContractType == EmploymentContractType.Internship)
        {
            article3Content = $"3.1. Khoản hỗ trợ kinh phí thực tập: {(contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0 ? $"{contract.BasicSalary.Value:N0} đ/tháng" : "Khoản thù lao phụ cấp hỗ trợ theo thỏa thuận tiếp nhận thực tập của Công ty")}.\n" +
                              $"3.2. Kỳ hạn và phương thức chi trả: Chi trả định kỳ {contract.PaymentPeriod} qua {contract.PaymentMethod}.\n" +
                              $"3.3. Các hỗ trợ phúc lợi khác: Ăn trưa và trang bị phương tiện làm việc tại văn phòng/showroom.";
        }
        else if (contract.ContractType == EmploymentContractType.VocationalTraining)
        {
            article3Content = $"3.1. Tiền công trong thời gian học nghề, tập nghề: {(contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0 ? $"{contract.BasicSalary.Value:N0} đ/tháng" : "Hưởng thù lao học việc theo thỏa thuận phù hợp với Khoản 5 Điều 61 Bộ luật Lao động")}.\n" +
                              $"3.2. Chi phí đào tạo: Doanh nghiệp đài thọ chi phí tài liệu và trang thiết bị thực hành.\n" +
                              $"3.3. Kỳ hạn và phương thức thanh toán: Chi trả định kỳ {contract.PaymentPeriod} qua {contract.PaymentMethod}.";
        }
        else
        {
            string salaryStr = (contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0)
                ? $"{contract.BasicSalary.Value:N0} đồng/tháng ({contract.SalaryType})"
                : "Theo quy chế lương thưởng và thỏa thuận phụ lục hợp đồng";

            string probNote = string.Empty;
            if (contract.ContractType == EmploymentContractType.Probation && contract.ProbationSalaryPercentage.HasValue && contract.BasicSalary.HasValue && contract.BasicSalary.Value > 0)
            {
                var probAmount = contract.BasicSalary.Value * (contract.ProbationSalaryPercentage.Value / 100m);
                probNote = $"\n   * Tiền lương trong thời gian thử việc bằng {contract.ProbationSalaryPercentage.Value}% lương chính thức, tương đương {probAmount:N0} đ/tháng theo Điều 26 BLLĐ.";
            }

            string allowanceStr = $"Phụ cấp trách nhiệm: {(contract.PositionAllowance.HasValue ? $"{contract.PositionAllowance.Value:N0} đ/tháng" : "Theo quy chế")}; Tiền ăn trưa: {(contract.LunchAllowance.HasValue ? $"{contract.LunchAllowance.Value:N0} đ/tháng" : "Theo quy chế")}; Phụ cấp khác: {(contract.OtherAllowances.HasValue ? $"{contract.OtherAllowances.Value:N0} đ/tháng" : "Theo quy chế")}.";

            article3Content = $"3.1. Mức lương chính theo công việc hoặc chức danh: {salaryStr}.{probNote}\n" +
                              $"3.2. Phụ cấp lương và các khoản bổ sung khác: {allowanceStr}\n" +
                              $"3.3. Hình thức và kỳ hạn trả lương: Chi trả định kỳ {contract.PaymentPeriod} bằng hình thức {contract.PaymentMethod} (tuân thủ Điều 95, 96, 97 BLLĐ).\n" +
                              $"3.4. Chế độ nâng bậc, nâng lương: {contract.SalaryReviewPolicy} (xét duyệt định kỳ hàng năm căn cứ hiệu quả công việc và quy chế nâng lương Doanh nghiệp theo Điều 103 BLLĐ).";
        }

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_3",
            ArticleNumber = "Điều 3",
            Title = "Tiền lương, phụ cấp lương và chế độ nâng lương",
            Content = article3Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // ==========================================
        // ĐIỀU 4: TRANG BỊ BẢO HỘ LAO ĐỘNG (Điểm h Khoản 1 Điều 21 BLLĐ)
        // ==========================================
        string article4Content = contract.ContractType == EmploymentContractType.Internship || contract.ContractType == EmploymentContractType.VocationalTraining
            ? $"4.1. Trang bị an toàn: Người học việc/thực tập sinh được hướng dẫn nội quy an toàn lao động và trang bị đầy đủ phương tiện bảo hộ phù hợp với vị trí thực hành.\n" +
              $"4.2. Chấp hành an toàn vệ sinh lao động: Tuân thủ nghiêm ngặt các quy định về an toàn lao động, phòng chống cháy nổ tại cơ sở."
            : $"4.1. Trang bị bảo hộ lao động: Người sử dụng lao động trang bị đầy đủ và kịp thời các phương tiện bảo hộ lao động đạt tiêu chuẩn kỹ thuật an toàn: {contract.LaborProtection}.\n" +
              $"4.2. Huấn luyện an toàn, vệ sinh lao động: Người lao động được tham gia các khóa huấn luyện về an toàn, vệ sinh lao động định kỳ và khám sức khỏe định kỳ theo quy định của Luật An toàn, vệ sinh lao động.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_4",
            ArticleNumber = "Điều 4",
            Title = "Trang bị bảo hộ lao động và an toàn vệ sinh lao động",
            Content = article4Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // ==========================================
        // ĐIỀU 5: BẢO HIỂM XÃ HỘI, BHYT VÀ BHTN (Điểm i Khoản 1 Điều 21 BLLĐ)
        // ==========================================
        string article5Content = contract.ContractType == EmploymentContractType.Internship || contract.ContractType == EmploymentContractType.VocationalTraining
            ? $"5.1. Chế độ bảo hiểm: Người sử dụng lao động thực hiện bảo hiểm tai nạn lao động theo quy định của pháp luật đối với học việc, tập nghề, thực tập sinh.\n" +
              $"5.2. Quyền lợi y tế: Được sơ cứu, cấp cứu kịp thời và thanh toán chi phí y tế nếu xảy ra tai nạn trong quá trình học nghề, thực tập tại Công ty."
            : $"5.1. Nghĩa vụ đóng bảo hiểm: Hai bên thực hiện nghĩa vụ tham gia Bảo hiểm xã hội, Bảo hiểm y tế, Bảo hiểm thất nghiệp theo đúng quy định tại Điều 168 Bộ luật Lao động và pháp luật bảo hiểm hiện hành (tổng mức đóng {contract.SocialInsuranceRate}%: NSDLĐ đóng 21.5%, NLĐ đóng 10.5%). {(contract.ContractType == EmploymentContractType.Probation ? "Đối với thời gian thử việc, khoản tiền bảo hiểm được tính gộp và chi trả trực tiếp cùng kỳ lương cho người lao động." : "")}\n" +
              $"5.2. Quyền lợi bảo hiểm: Người lao động được hưởng đầy đủ các chế độ ốm đau, thai sản, tai nạn lao động, hưu trí, tử tuất và trợ cấp thất nghiệp theo quy định của pháp luật.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_5",
            ArticleNumber = "Điều 5",
            Title = "Bảo hiểm xã hội, bảo hiểm y tế và bảo hiểm thất nghiệp",
            Content = article5Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // ==========================================
        // ĐIỀU 6: ĐÀO TẠO, BỒI DƯỠNG NÂNG CAO KỸ NĂNG NGHỀ (Điểm k Khoản 1 Điều 21 BLLĐ)
        // ==========================================
        string trainingClause = !string.IsNullOrWhiteSpace(contract.TrainingClauseDetails)
            ? contract.TrainingClauseDetails
            : "6.1. Trách nhiệm đào tạo: Người sử dụng lao động có trách nhiệm tạo điều kiện để người lao động được đào tạo, bồi dưỡng nâng cao trình độ, kỹ năng nghề phù hợp với vị trí công tác và nhu cầu phát triển của Doanh nghiệp theo quy định tại Điều 60 Bộ luật Lao động.\n" +
              "6.2. Hợp đồng đào tạo nghề: Trường hợp người lao động được cử tham gia các khóa đào tạo nghề, bồi dưỡng nghiệp vụ chuyên sâu bằng kinh phí của Doanh nghiệp, hai bên ký Hợp đồng đào tạo nghề theo Điều 62 Bộ luật Lao động. Người lao động cam kết thời gian làm việc sau đào tạo và bồi hoàn chi phí đào tạo hợp lệ nếu tự ý chấm dứt hợp đồng trái cam kết.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_6",
            ArticleNumber = "Điều 6",
            Title = "Đào tạo, bồi dưỡng, nâng cao trình độ và kỹ năng nghề",
            Content = trainingClause,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // ==========================================
        // ĐIỀU 7: QUYỀN VÀ NGHĨA VỤ CỦA CÁC BÊN
        // ==========================================
        string article7Content = $"7.1. Quyền và nghĩa vụ của Người sử dụng lao động (Bên A):\n" +
                                 $"   - Quản lý, điều hành và bố trí công việc phù hợp với năng lực chuyên môn và thỏa thuận;\n" +
                                 $"   - Đảm bảo thanh toán đầy đủ, đúng hạn tiền lương và các chế độ phúc lợi đã cam kết;\n" +
                                 $"   - Ban hành và yêu cầu tuân thủ Nội quy lao động, quy trình an toàn và tác nghiệp của Doanh nghiệp;\n" +
                                 $"   - Thực hiện đầy đủ các quyền và nghĩa vụ khác theo quy định tại Điều 6 Bộ luật Lao động.\n" +
                                 $"7.2. Quyền và nghĩa vụ của Người lao động (Bên B):\n" +
                                 $"   - Chấp hành nghiêm chỉnh kỷ luật lao động, nội quy an toàn và quy định bảo mật của Doanh nghiệp;\n" +
                                 $"   - Thực hiện công việc được giao với tinh thần trách nhiệm, bảo quản tài sản và trang thiết bị làm việc;\n" +
                                 $"   - Được bảo đảm các quyền về việc làm, tiền lương, nghỉ ngơi, bảo hiểm và bảo hộ lao động theo Điều 5 Bộ luật Lao động.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_7",
            ArticleNumber = "Điều 7",
            Title = "Quyền và nghĩa vụ của các bên",
            Content = article7Content,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order++
        });

        // ==========================================
        // ĐIỀU 8: BẢO VỆ BÍ MẬT KINH DOANH & BẢO MẬT DỮ LIỆU (Khoản 2 Điều 21 BLLĐ)
        // ==========================================
        if (contract.HasNonDisclosureAgreement || !string.IsNullOrWhiteSpace(contract.NdaClauseDetails))
        {
            string article8Content = !string.IsNullOrWhiteSpace(contract.NdaClauseDetails)
                ? contract.NdaClauseDetails
                : "8.1. Thỏa thuận bảo vệ bí mật kinh doanh: Căn cứ Khoản 2 Điều 21 Bộ luật Lao động, Người lao động cam kết giữ bí mật toàn bộ thông tin công nghệ, công thức giá, danh sách đối tác khách hàng và bí mật kinh doanh của Doanh nghiệp.\n" +
                  "8.2. Không tiết lộ hoặc chuyển giao dữ liệu doanh nghiệp cho bất kỳ bên thứ ba nào khi chưa có sự đồng thuận bằng văn bản của Người đại diện theo pháp luật.\n" +
                  "8.3. Thời hạn bảo mật: Cam kết bảo mật có hiệu lực trong suốt thời gian làm việc và tối thiểu 02 năm kể từ ngày chấm dứt quan hệ lao động. Trường hợp vi phạm gây thiệt hại thực tế, người lao động có trách nhiệm bồi thường theo quy định của pháp luật.";

            list.Add(new EmploymentContractClauseDto
            {
                Id = "clause_8",
                ArticleNumber = $"Điều {order}",
                Title = "Thỏa thuận bảo vệ bí mật kinh doanh và bảo mật dữ liệu",
                Content = article8Content,
                IsMandatory = false,
                IsOptional = true,
                DisplayOrder = order++
            });
        }

        // ==========================================
        // ĐIỀU KHOẢN PHÚC LỢI NỘI BỘ (NẾU CÓ)
        // ==========================================
        if (contract.HasSpecialBenefits || !string.IsNullOrWhiteSpace(contract.SpecialBenefitsDetails))
        {
            string articleBenefitsContent = !string.IsNullOrWhiteSpace(contract.SpecialBenefitsDetails)
                ? contract.SpecialBenefitsDetails
                : "Được xét thưởng lương tháng 13, thưởng năng suất KPI theo quy chế tài chính; tham gia các hoạt động teambuilding, nghỉ dưỡng hàng năm do Công ty tổ chức; hưởng chính sách trợ cấp hiếu hỷ, sinh nhật và quà tặng lễ tết.";

            list.Add(new EmploymentContractClauseDto
            {
                Id = "clause_benefits",
                ArticleNumber = $"Điều {order}",
                Title = "Chế độ phúc lợi nội bộ Doanh nghiệp",
                Content = articleBenefitsContent,
                IsMandatory = false,
                IsOptional = true,
                DisplayOrder = order++
            });
        }

        // ==========================================
        // THỎA THUẬN TÙY CHỌN BỔ SUNG (NẾU CÓ)
        // ==========================================
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

        // ==========================================
        // ĐIỀU KHOẢN THI HÀNH, HIỆU LỰC VÀ SỐ BẢN HỢP ĐỒNG (Điều 23 & Khoản 1 Điều 14 BLLĐ)
        // ==========================================
        string finalArticleContent = $"Hợp đồng có hiệu lực thi hành kể từ ngày hai bên hoàn tất việc ký kết hợp pháp (theo Điều 23 Bộ luật Lao động).\n" +
                                     $"Hợp đồng lao động bằng văn bản này được lập thành 02 (hai) bản có giá trị pháp lý như nhau, Người sử dụng lao động giữ 01 (một) bản, Người lao động giữ 01 (một) bản để theo dõi và thực hiện (theo Khoản 1 Điều 14 Bộ luật Lao động).\n" +
                                     $"Trường hợp giao kết dưới hình thức thông điệp dữ liệu điện tử theo quy định tại Khoản 1 Điều 14 Bộ luật Lao động và Luật Giao dịch điện tử số 20/2023/QH15, tệp dữ liệu số lưu trữ trên hệ thống TradeFlow có giá trị toàn vẹn và có giá trị chứng cứ pháp lý.";

        list.Add(new EmploymentContractClauseDto
        {
            Id = "clause_final",
            ArticleNumber = $"Điều {order}",
            Title = "Điều khoản thi hành, hiệu lực và số bản hợp đồng",
            Content = finalArticleContent,
            IsMandatory = true,
            IsOptional = false,
            DisplayOrder = order
        });

        return list;
    }
}
