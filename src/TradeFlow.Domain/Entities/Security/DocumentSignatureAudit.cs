using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Security;

/// <summary>
/// Nhật ký kiểm tra và bằng chứng pháp lý của các thao tác ký số chứng từ / hóa đơn
/// </summary>
public class DocumentSignatureAudit : AuditableEntity<int>
{
    public DocumentSignatureAudit() { }

    /// <summary>Loại chứng từ (ví dụ: Invoice, SalesOrder, Quotation)</summary>
    public string DocumentType { get; set; } = "Invoice";

    /// <summary>ID của chứng từ</summary>
    public int DocumentId { get; set; }

    /// <summary>Số hiệu chứng từ (ví dụ: INV-2026-0001 hoặc 00000001)</summary>
    public string DocumentNumber { get; set; } = string.Empty;

    /// <summary>ID danh tính người ký số (SignerIdentity.Id)</summary>
    public int? SignerIdentityId { get; set; }

    /// <summary>ID người dùng thực hiện (ApplicationUser.Id)</summary>
    public string? SignerUserId { get; set; }

    /// <summary>Họ tên người ký số</summary>
    public string SignerName { get; set; } = string.Empty;

    /// <summary>Chức vụ người ký</summary>
    public string SignerPosition { get; set; } = string.Empty;

    /// <summary>Vai trò thẩm quyền ký</summary>
    public SignerRole SignerRole { get; set; } = SignerRole.AuthorizedSigner;

    /// <summary>Thời điểm ký số (UTC)</summary>
    public DateTime SigningTime { get; set; } = DateTime.UtcNow;

    /// <summary>Trạng thái chữ ký tại thời điểm ghi nhận</summary>
    public DigitalSignatureStatus SignatureStatus { get; set; } = DigitalSignatureStatus.Signed;

    /// <summary>Mã băm chuẩn tắc của nội dung chứng từ tại thời điểm ký (Canonical Digest)</summary>
    public string DocumentHash { get; set; } = string.Empty;

    /// <summary>Giá trị chữ ký điện tử (Base64 PKCS#1 Signature Value)</summary>
    public string SignatureValue { get; set; } = string.Empty;

    /// <summary>Chủ thể chứng thư (Certificate Subject)</summary>
    public string? CertificateSubject { get; set; }

    /// <summary>Số sê-ri chứng thư</summary>
    public string? CertificateSerialNumber { get; set; }

    /// <summary>Loại phương thức ký số đã sử dụng</summary>
    public SigningProviderType ProviderType { get; set; } = SigningProviderType.SoftwareRsa;

    /// <summary>Địa chỉ IP thực hiện thao tác</summary>
    public string? IpAddress { get; set; }

    /// <summary>Thông tin trình duyệt / máy khách (User Agent)</summary>
    public string? UserAgent { get; set; }

    /// <summary>Kết quả kiểm tra xác thực gần nhất</summary>
    public string? VerificationResult { get; set; }

    /// <summary>Thời điểm kiểm tra xác thực gần nhất (UTC)</summary>
    public DateTime? LastVerifiedAt { get; set; }

    /// <summary>Người kiểm tra xác thực</summary>
    public string? LastVerifiedBy { get; set; }

    /// <summary>Thông báo lỗi (nếu ký hoặc xác thực thất bại)</summary>
    public string? ErrorMessage { get; set; }
}
