using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Security;

/// <summary>
/// Cấu hình chính sách an ninh và chữ ký số của doanh nghiệp
/// </summary>
public class CompanySecuritySettings : AuditableEntity<int>
{
    public CompanySecuritySettings() { }

    /// <summary>Bắt buộc nhập mã PIN khi thực hiện ký số</summary>
    public bool RequireSignerPin { get; set; } = true;

    /// <summary>Độ dài tối thiểu của mã PIN người ký</summary>
    public int PinMinLength { get; set; } = 6;

    /// <summary>Thời gian hiệu lực của mã kích hoạt người ký (phút)</summary>
    public int EnrollmentCodeExpirationMinutes { get; set; } = 30;

    /// <summary>Số ngày cảnh báo trước khi chứng thư số hết hạn</summary>
    public int CertExpirationWarningDays { get; set; } = 30;

    /// <summary>Cho phép Quản trị viên hệ thống cấp mã kích hoạt người ký</summary>
    public bool AllowAdminSignerEnrollment { get; set; } = true;

    /// <summary>Tự động thu hồi chứng thư và quyền ký khi nhân viên thôi việc</summary>
    public bool AutoRevokeOnTermination { get; set; } = true;

    /// <summary>Số lần thử ký/nhập PIN sai tối đa trước khi tạm khóa</summary>
    public int MaxFailedSignAttempts { get; set; } = 5;

    /// <summary>Phương thức ký mặc định của hệ thống</summary>
    public SigningProviderType DefaultSigningProvider { get; set; } = SigningProviderType.SoftwareRsa;

    /// <summary>Phương pháp chuẩn hóa XMLDSig</summary>
    public string XmlDsigCanonicalizationMethod { get; set; } = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";

    /// <summary>Thuật toán ký XMLDSig</summary>
    public string XmlDsigSignatureMethod { get; set; } = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

    /// <summary>Thuật toán băm XMLDSig</summary>
    public string XmlDsigDigestMethod { get; set; } = "http://www.w3.org/2001/04/xmlenc#sha256";
}
