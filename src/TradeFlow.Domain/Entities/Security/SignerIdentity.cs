using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Security;

/// <summary>
/// Danh tính người ký số được ủy quyền trong hệ thống
/// </summary>
public class SignerIdentity : AuditableEntity<int>
{
    public SignerIdentity() { }

    /// <summary>ID người dùng gắn liền với chứng thư (ApplicationUser.Id)</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Tên tài khoản người dùng</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>Họ và tên đầy đủ của người ký</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Chức vụ (e.g. Giám đốc điều hành, Phó giám đốc, Kế toán trưởng)</summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>Vai trò thẩm quyền ký</summary>
    public SignerRole SignerRole { get; set; } = SignerRole.AuthorizedSigner;

    /// <summary>Trạng thái chứng thư ký</summary>
    public SignerStatus Status { get; set; } = SignerStatus.Active;

    /// <summary>Loại nhà cung cấp ký số</summary>
    public SigningProviderType ProviderType { get; set; } = SigningProviderType.SoftwareRsa;

    /// <summary>Số sê-ri chứng thư số</summary>
    public string CertificateSerialNumber { get; set; } = string.Empty;

    /// <summary>Chủ thể chứng thư (Subject Distinguished Name)</summary>
    public string CertificateSubject { get; set; } = string.Empty;

    /// <summary>Đơn vị cấp chứng thư (Issuer)</summary>
    public string CertificateIssuer { get; set; } = "TradeFlow Security CA";

    /// <summary>Mã băm chứng thư (Thumbprint)</summary>
    public string CertificateThumbprint { get; set; } = string.Empty;

    /// <summary>Ngày bắt đầu hiệu lực (UTC)</summary>
    public DateTime ValidFrom { get; set; } = DateTime.UtcNow;

    /// <summary>Ngày hết hạn hiệu lực (UTC)</summary>
    public DateTime ValidTo { get; set; } = DateTime.UtcNow.AddYears(1);

    /// <summary>Khóa riêng tư đã được mã hóa bằng mã PIN của người ký (AES-GCM). KHÔNG BAO GIỜ lưu plaintext!</summary>
    public string? EncryptedPrivateKey { get; set; }

    /// <summary>Muối (Salt) dẫn xuất khóa PBKDF2 từ mã PIN của người ký</summary>
    public string? KeySalt { get; set; }

    /// <summary>Mã băm kiểm tra nhanh mã PIN (SHA-256 kèm Salt)</summary>
    public string? PinVerificationHash { get; set; }

    /// <summary>Khóa công khai định dạng XML (RSAKeyValue)</summary>
    public string? PublicKeyXml { get; set; }

    /// <summary>Khóa công khai định dạng PEM</summary>
    public string? PublicKeyPem { get; set; }

    /// <summary>Mã băm SHA-256 của mã kích hoạt đã dùng</summary>
    public string? EnrollmentCodeHash { get; set; }

    /// <summary>Người cấp quyền kích hoạt</summary>
    public string EnrolledBy { get; set; } = string.Empty;

    /// <summary>Thời điểm kích hoạt</summary>
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    /// <summary>Thời điểm bị thu hồi (nếu có)</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Người thực hiện thu hồi</summary>
    public string? RevokedBy { get; set; }

    /// <summary>Lý do thu hồi</summary>
    public string? RevocationReason { get; set; }

    /// <summary>Ghi chú bổ sung</summary>
    public string? Notes { get; set; }
}
