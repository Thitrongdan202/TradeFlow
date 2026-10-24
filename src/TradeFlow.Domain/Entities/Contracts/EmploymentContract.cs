using TradeFlow.Domain.Common;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Contracts;

/// <summary>
/// Hợp đồng lao động và phụ lục theo Bộ luật Lao động số 45/2019/QH14 và VB hợp nhất 18/VBHN-VPQH.
/// Quản lý tách biệt với Hợp đồng thương mại để bảo đảm quyền riêng tư, bảo vệ dữ liệu cá nhân theo Luật 91/2025/QH15.
/// </summary>
public class EmploymentContract : AuditableEntity<int>
{
    public EmploymentContract() { }

    // ==========================================
    // 1. THÔNG TIN ĐỊNH DANH HỢP ĐỒNG
    // ==========================================

    /// <summary>Số hợp đồng độc nhất (VD: HDLD-2026-0001, HDTV-2026-0001)</summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>Tiêu đề hợp đồng</summary>
    public string Title { get; set; } = "HỢP ĐỒNG LAO ĐỘNG";

    /// <summary>Loại hợp đồng lao động (Không xác định thời hạn, Xác định thời hạn, Thử việc)</summary>
    public EmploymentContractType ContractType { get; set; } = EmploymentContractType.FixedTerm;

    /// <summary>Trạng thái vòng đời hợp đồng</summary>
    public EmploymentContractStatus Status { get; set; } = EmploymentContractStatus.Draft;

    /// <summary>Ngày ký kết / giao kết hợp đồng</summary>
    public DateTime ContractDate { get; set; } = DateTime.UtcNow;

    /// <summary>Địa điểm ký kết (VD: Cần Thơ, TP. Hồ Chí Minh)</summary>
    public string ContractPlace { get; set; } = "Cần Thơ";

    /// <summary>Ngày bắt đầu làm việc / có hiệu lực</summary>
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    /// <summary>Ngày kết thúc hợp đồng (đối với HĐ xác định thời hạn và thử việc)</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Thời gian thử việc (tính bằng ngày: 6, 30, 60, 180 theo Điều 25 BLLĐ)</summary>
    public int? ProbationDurationDays { get; set; }

    /// <summary>Tỷ lệ hưởng lương thử việc (% - tối thiểu 85% theo Điều 26 BLLĐ)</summary>
    public decimal? ProbationSalaryPercentage { get; set; } = 85m;

    // ==========================================
    // 2. NGƯỜI SỬ DỤNG LAO ĐỘNG (BÊN A) - SNAPSHOT
    // ==========================================

    public string EmployerName { get; set; } = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM";
    public string EmployerTaxCode { get; set; } = "0316899999";
    public string EmployerAddress { get; set; } = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ";
    public string? EmployerPhone { get; set; } = "0909 123 456";
    public string EmployerRepresentativeName { get; set; } = "Nguyễn Văn An";
    public string EmployerRepresentativePosition { get; set; } = "Giám đốc";
    public string? EmployerRepresentativeAuthority { get; set; } = "Đại diện theo pháp luật";

    // ==========================================
    // 3. NGƯỜI LAO ĐỘNG (BÊN B) - SNAPSHOT & LIÊN KẾT
    // ==========================================

    /// <summary>ID nhân viên liên kết trong MasterData</summary>
    public int EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public string EmployeeCode { get; set; } = string.Empty;
    public string EmployeeFullName { get; set; } = string.Empty;
    public DateTime? EmployeeDateOfBirth { get; set; }
    public string? EmployeeGender { get; set; }
    public string? EmployeeNationalId { get; set; }
    public DateTime? EmployeeNationalIdIssueDate { get; set; }
    public string? EmployeeNationalIdIssuePlace { get; set; }
    public string? EmployeeAddress { get; set; }
    public string? EmployeePhone { get; set; }
    public string? EmployeeEmail { get; set; }
    public string? EmployeeBankInformation { get; set; }

    // ==========================================
    // 4. ĐIỀU 21 BLLĐ: CÔNG VIỆC, ĐỊA ĐIỂM, THỜI GIỜ
    // ==========================================

    /// <summary>Chức danh / Vị trí công việc</summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Phòng ban / Đơn vị công tác</summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>Địa điểm làm việc cụ thể</summary>
    public string Workplace { get; set; } = "Văn phòng & Showroom LACASA, Số 123 Đường Nguyễn Văn Cừ, TP. Cần Thơ";

    /// <summary>Mô tả tóm tắt nội dung công việc và trách nhiệm chính</summary>
    public string JobDescription { get; set; } = string.Empty;

    /// <summary>Số giờ làm việc chuẩn trong tuần (không quá 48h/tuần theo Điều 105 BLLĐ)</summary>
    public int WorkingHoursPerWeek { get; set; } = 48;

    /// <summary>Lịch làm việc chi tiết</summary>
    public string WorkingSchedule { get; set; } = "Từ Thứ Hai đến Thứ Bảy: Sáng 08:00 - 12:00, Chiều 13:00 - 17:00 (Nghỉ Chủ Nhật)";

    /// <summary>Số ngày nghỉ phép năm hưởng nguyên lương (tối thiểu 12 ngày theo Điều 113 BLLĐ)</summary>
    public int AnnualLeaveDays { get; set; } = 12;

    // ==========================================
    // 5. TIỀN LƯƠNG, PHỤ CẤP VÀ CHẾ ĐỘ PHÚC LỢI
    // ==========================================

    /// <summary>Mức lương chính theo tháng (VND)</summary>
    public decimal BasicSalary { get; set; }

    /// <summary>Hình thức trả lương (Lương thời gian, Lương khoán, Lương sản phẩm)</summary>
    public string SalaryType { get; set; } = "Lương thời gian (Lương tháng)";

    /// <summary>Kỳ hạn trả lương</summary>
    public string PaymentPeriod { get; set; } = "Chi trả định kỳ 01 lần/tháng vào ngày 05 đến 10 của tháng kế tiếp";

    /// <summary>Phương thức thanh toán</summary>
    public string PaymentMethod { get; set; } = "Chuyển khoản qua tài khoản ngân hàng của Người lao động";

    /// <summary>Phụ cấp chức vụ / trách nhiệm (VND/tháng)</summary>
    public decimal PositionAllowance { get; set; } = 0;

    /// <summary>Phụ cấp tiền ăn trưa (VND/tháng)</summary>
    public decimal LunchAllowance { get; set; } = 730000;

    /// <summary>Các khoản phụ cấp và bổ sung khác (xăng xe, điện thoại... VND/tháng)</summary>
    public decimal OtherAllowances { get; set; } = 0;

    /// <summary>Chính sách xét nâng bậc / nâng lương</summary>
    public string SalaryReviewPolicy { get; set; } = "Được xem xét nâng lương định kỳ 01 năm/lần dựa trên hiệu quả công việc và kết quả kinh doanh của Công ty";

    /// <summary>Tỷ lệ đóng BHXH, BHYT, BHTN theo luật định (%)</summary>
    public decimal SocialInsuranceRate { get; set; } = 32m;

    /// <summary>Trang bị bảo hộ lao động</summary>
    public string LaborProtection { get; set; } = "Được trang bị đồng phục công ty, thiết bị và công cụ bảo hộ lao động đạt chuẩn an toàn vệ sinh lao động theo vị trí làm việc";

    // ==========================================
    // 6. ĐIỀU KHOẢN TÙY CHỌN HỢP PHÁP
    // ==========================================

    /// <summary>Bật/tắt thỏa thuận bảo mật thông tin & bí mật thương mại (NDA)</summary>
    public bool HasNonDisclosureAgreement { get; set; } = true;

    /// <summary>Nội dung chi tiết cam kết bảo mật</summary>
    public string? NdaClauseDetails { get; set; } = "Người lao động cam kết tuyệt đối bảo mật thông tin khách hàng, chính sách giá, dữ liệu kinh doanh và bí quyết kỹ thuật của LACASA trong và sau quá trình làm việc";

    /// <summary>Bật/tắt cam kết đào tạo và hoàn trả chi phí đào tạo</summary>
    public bool HasTrainingReimbursement { get; set; } = false;

    /// <summary>Nội dung cam kết bồi hoàn chi phí đào tạo</summary>
    public string? TrainingClauseDetails { get; set; }

    /// <summary>Bật/tắt chế độ phúc lợi riêng của công ty</summary>
    public bool HasSpecialBenefits { get; set; } = true;

    /// <summary>Nội dung chi tiết chế độ phúc lợi</summary>
    public string? SpecialBenefitsDetails { get; set; } = "Thưởng lễ, Tết theo quy chế tài chính; Chế độ du lịch nghỉ mát thường niên; Khám sức khỏe định kỳ hàng năm";

    /// <summary>Điều khoản riêng biệt bổ sung khác (nếu có)</summary>
    public string? CustomTerms { get; set; }

    // ==========================================
    // 7. PHƯƠNG THỨC KÝ & XÁC THỰC PHÁP LÝ
    // ==========================================

    public EmploymentSigningMethod SigningMethod { get; set; } = EmploymentSigningMethod.None;

    public DateTime? EmployeeSignedAt { get; set; }
    public string? EmployeeSignatureData { get; set; }

    public DateTime? EmployerSignedAt { get; set; }
    public string? EmployerSignatureData { get; set; }

    /// <summary>Mã băm SHA-256 xác thực tính toàn vẹn nội dung hợp đồng điện tử</summary>
    public string? DigitalSignatureHash { get; set; }

    /// <summary>Mã xác thực nền tảng hợp đồng điện tử quốc gia theo NĐ 337/2025/NĐ-CP (nếu có)</summary>
    public string? NationalPlatformReference { get; set; }

    /// <summary>Đường dẫn tài liệu scan văn bản giấy có dấu mộc và chữ ký tay</summary>
    public string? PaperScanAttachmentPath { get; set; }

    // ==========================================
    // 8. LỊCH SỬ GIA HẠN, CHẤM DỨT, HỦY BỎ
    // ==========================================

    /// <summary>Hợp đồng gốc trước đó nếu đây là hợp đồng gia hạn hoặc tái ký</summary>
    public int? ParentContractId { get; set; }
    public EmploymentContract? ParentContract { get; set; }

    /// <summary>Số lần gia hạn hợp đồng (theo Điều 20: tối đa 01 lần đối với HĐ xác định thời hạn)</summary>
    public int RenewalCount { get; set; } = 0;

    /// <summary>Ngày chấm dứt hợp đồng</summary>
    public DateTime? TerminationDate { get; set; }

    /// <summary>Lý do chấm dứt (thỏa thuận hai bên, hết hạn không tái ký, đơn phương chấm dứt hợp pháp...)</summary>
    public string? TerminationReason { get; set; }

    /// <summary>Lý do hủy hợp đồng</summary>
    public string? CancellationReason { get; set; }

    /// <summary>Ghi chú nội bộ bộ phận nhân sự</summary>
    public string? Notes { get; set; }
}
