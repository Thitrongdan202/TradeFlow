using System.ComponentModel.DataAnnotations;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Contracts;

public class EmploymentContractDto
{
    public int Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public EmploymentContractType ContractType { get; set; }
    public string ContractTypeName => ContractType switch
    {
        EmploymentContractType.IndefiniteTerm => "Không xác định thời hạn",
        EmploymentContractType.FixedTerm => "Xác định thời hạn (tối đa 36 tháng)",
        EmploymentContractType.Probation => "Hợp đồng thử việc",
        _ => "Chưa xác định"
    };

    public EmploymentContractStatus Status { get; set; }
    public string StatusName => Status switch
    {
        EmploymentContractStatus.Draft => "Bản nháp",
        EmploymentContractStatus.PendingSignature => "Chờ ký kết",
        EmploymentContractStatus.Active => "Đang hiệu lực",
        EmploymentContractStatus.ExpiringSoon => "Sắp hết hạn",
        EmploymentContractStatus.Expired => "Đã hết hạn",
        EmploymentContractStatus.Terminated => "Đã chấm dứt",
        EmploymentContractStatus.Cancelled => "Đã hủy",
        _ => "Chưa xác định"
    };

    public DateTime ContractDate { get; set; }
    public string ContractPlace { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? ProbationDurationDays { get; set; }
    public decimal? ProbationSalaryPercentage { get; set; }

    // Bên A (Người sử dụng lao động)
    public string EmployerName { get; set; } = string.Empty;
    public string EmployerTaxCode { get; set; } = string.Empty;
    public string EmployerAddress { get; set; } = string.Empty;
    public string? EmployerPhone { get; set; }
    public string EmployerRepresentativeName { get; set; } = string.Empty;
    public string EmployerRepresentativePosition { get; set; } = string.Empty;
    public string? EmployerRepresentativeAuthority { get; set; }

    // Bên B (Người lao động)
    public int EmployeeId { get; set; }
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

    // Công việc & Chế độ
    public string JobTitle { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string Workplace { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    public int WorkingHoursPerWeek { get; set; } = 48;
    public string WorkingSchedule { get; set; } = string.Empty;
    public int AnnualLeaveDays { get; set; } = 12;

    // Tiền lương (Bảo mật theo Luật 91/2025/QH15)
    public decimal BasicSalary { get; set; }
    public string SalaryType { get; set; } = string.Empty;
    public string PaymentPeriod { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal PositionAllowance { get; set; }
    public decimal LunchAllowance { get; set; }
    public decimal OtherAllowances { get; set; }
    public string SalaryReviewPolicy { get; set; } = string.Empty;
    public decimal SocialInsuranceRate { get; set; } = 32m;
    public string LaborProtection { get; set; } = string.Empty;

    // Điều khoản tùy chọn
    public bool HasNonDisclosureAgreement { get; set; }
    public string? NdaClauseDetails { get; set; }
    public bool HasTrainingReimbursement { get; set; }
    public string? TrainingClauseDetails { get; set; }
    public bool HasSpecialBenefits { get; set; }
    public string? SpecialBenefitsDetails { get; set; }
    public string? CustomTerms { get; set; }

    // Ký kết
    public EmploymentSigningMethod SigningMethod { get; set; }
    public string SigningMethodName => SigningMethod switch
    {
        EmploymentSigningMethod.PaperSigned => "Ký giấy truyền thống (bản scan đính kèm)",
        EmploymentSigningMethod.InAppDrawnSignature => "Ký tay trực tiếp trên TradeFlow",
        EmploymentSigningMethod.InternalDigitalSignature => "Ký số nội bộ TradeFlow (SHA-256)",
        EmploymentSigningMethod.NationalPlatform => "Chứng thực nền tảng HĐĐT quốc gia (NĐ 337/2025)",
        _ => "Chưa ký kết"
    };

    public DateTime? EmployeeSignedAt { get; set; }
    public string? EmployeeSignatureData { get; set; }
    public DateTime? EmployerSignedAt { get; set; }
    public string? EmployerSignatureData { get; set; }
    public string? DigitalSignatureHash { get; set; }
    public string? NationalPlatformReference { get; set; }
    public string? PaperScanAttachmentPath { get; set; }

    // Gia hạn & Chấm dứt
    public int? ParentContractId { get; set; }
    public int RenewalCount { get; set; }
    public DateTime? TerminationDate { get; set; }
    public string? TerminationReason { get; set; }
    public string? CancellationReason { get; set; }
    public string? Notes { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // Quyền xem lương & thông tin nhạy cảm
    public bool CanViewSensitiveData { get; set; } = true;
}

public class CreateEmploymentContractDto
{
    [Required(ErrorMessage = "Vui lòng chọn nhân viên")]
    public int EmployeeId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại hợp đồng")]
    public EmploymentContractType ContractType { get; set; } = EmploymentContractType.FixedTerm;

    [Required(ErrorMessage = "Tiêu đề hợp đồng không được để trống")]
    [MaxLength(255)]
    public string Title { get; set; } = "HỢP ĐỒNG LAO ĐỘNG";

    public DateTime ContractDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string ContractPlace { get; set; } = "Cần Thơ";

    [Required(ErrorMessage = "Ngày bắt đầu làm việc không được để trống")]
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

    public DateTime? EndDate { get; set; }

    public int? ProbationDurationDays { get; set; }
    public decimal? ProbationSalaryPercentage { get; set; } = 85m;

    // Bên A override (nếu cần chỉnh so với cấu hình mặc định)
    public string? EmployerName { get; set; }
    public string? EmployerTaxCode { get; set; }
    public string? EmployerAddress { get; set; }
    public string? EmployerRepresentativeName { get; set; }
    public string? EmployerRepresentativePosition { get; set; }

    // Công việc
    [Required(ErrorMessage = "Chức danh công việc không được để trống")]
    [MaxLength(150)]
    public string JobTitle { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phòng ban không được để trống")]
    [MaxLength(150)]
    public string DepartmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Địa điểm làm việc không được để trống")]
    [MaxLength(500)]
    public string Workplace { get; set; } = "Showroom & Văn phòng LACASA, Số 123 Đường Nguyễn Văn Cừ, TP. Cần Thơ";

    [MaxLength(2000)]
    public string JobDescription { get; set; } = string.Empty;

    public int WorkingHoursPerWeek { get; set; } = 48;
    public string WorkingSchedule { get; set; } = "Từ Thứ Hai đến Thứ Bảy: Sáng 08:00 - 12:00, Chiều 13:00 - 17:00 (Nghỉ Chủ Nhật)";
    public int AnnualLeaveDays { get; set; } = 12;

    // Tiền lương & Phúc lợi
    [Range(0, 1000000000, ErrorMessage = "Mức lương cơ bản phải lớn hơn hoặc bằng 0")]
    public decimal BasicSalary { get; set; }

    public string SalaryType { get; set; } = "Lương thời gian (Lương tháng)";
    public string PaymentPeriod { get; set; } = "Chi trả định kỳ 01 lần/tháng vào ngày 05 đến 10 của tháng kế tiếp";
    public string PaymentMethod { get; set; } = "Chuyển khoản qua tài khoản ngân hàng của Người lao động";

    public decimal PositionAllowance { get; set; } = 0;
    public decimal LunchAllowance { get; set; } = 730000;
    public decimal OtherAllowances { get; set; } = 0;
    public string SalaryReviewPolicy { get; set; } = "Được xem xét nâng lương định kỳ 01 năm/lần dựa trên hiệu quả công việc và kết quả kinh doanh của Công ty";
    public decimal SocialInsuranceRate { get; set; } = 32m;
    public string LaborProtection { get; set; } = "Được trang bị đồng phục công ty, thiết bị và công cụ bảo hộ lao động đạt chuẩn an toàn vệ sinh lao động theo vị trí làm việc";

    // Điều khoản tùy chọn
    public bool HasNonDisclosureAgreement { get; set; } = true;
    public string? NdaClauseDetails { get; set; } = "Người lao động cam kết tuyệt đối bảo mật thông tin khách hàng, chính sách giá, dữ liệu kinh doanh và bí quyết kỹ thuật của LACASA trong và sau quá trình làm việc";

    public bool HasTrainingReimbursement { get; set; } = false;
    public string? TrainingClauseDetails { get; set; }

    public bool HasSpecialBenefits { get; set; } = true;
    public string? SpecialBenefitsDetails { get; set; } = "Thưởng lễ, Tết theo quy chế tài chính; Chế độ du lịch nghỉ mát thường niên; Khám sức khỏe định kỳ hàng năm";

    public string? CustomTerms { get; set; }
    public string? Notes { get; set; }
}

public class UpdateEmploymentContractDto : CreateEmploymentContractDto
{
    public int Id { get; set; }
}

public class RenewEmploymentContractDto
{
    public int ContractId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại hợp đồng gia hạn / tái ký")]
    public EmploymentContractType NewContractType { get; set; } = EmploymentContractType.FixedTerm;

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    [Range(0, 1000000000, ErrorMessage = "Mức lương cơ bản phải lớn hơn hoặc bằng 0")]
    public decimal BasicSalary { get; set; }

    public decimal PositionAllowance { get; set; }
    public decimal LunchAllowance { get; set; }
    public decimal OtherAllowances { get; set; }

    public string? Notes { get; set; }
}

public class SignEmploymentContractDto
{
    public int ContractId { get; set; }
    public EmploymentSigningMethod SigningMethod { get; set; }
    public string? SignatureData { get; set; }
    public string? SignerRole { get; set; } = "Employer"; // "Employer" or "Employee"
    public string? PaperScanAttachmentPath { get; set; }
    public string? NationalPlatformReference { get; set; }
}

public class EmploymentContractFilterDto
{
    public string? SearchTerm { get; set; }
    public EmploymentContractType? ContractType { get; set; }
    public EmploymentContractStatus? Status { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public DateTime? EndDateFrom { get; set; }
    public DateTime? EndDateTo { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
