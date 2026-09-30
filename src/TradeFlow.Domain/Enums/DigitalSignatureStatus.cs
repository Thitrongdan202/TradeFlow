namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái chữ ký điện tử của hóa đơn và tài liệu
/// </summary>
public enum DigitalSignatureStatus
{
    /// <summary>Chưa ký</summary>
    Unsigned = 0,

    /// <summary>Đã ký</summary>
    Signed = 1,

    /// <summary>Chữ ký không hợp lệ</summary>
    Invalid = 2,

    /// <summary>Đang chờ ký</summary>
    Pending = 3,

    /// <summary>Chữ ký hợp lệ</summary>
    Valid = 4,

    /// <summary>Chứng thư hết hạn</summary>
    CertificateExpired = 5,

    /// <summary>Chữ ký đã bị thu hồi</summary>
    Revoked = 6,

    /// <summary>Tài liệu đã bị thay đổi sau khi ký</summary>
    Tampered = 7
}

public static class DigitalSignatureStatusExtensions
{
    public static string ToVietnamese(this DigitalSignatureStatus status) => status switch
    {
        DigitalSignatureStatus.Unsigned => "Chưa ký",
        DigitalSignatureStatus.Pending => "Đang chờ ký",
        DigitalSignatureStatus.Signed => "Đã ký",
        DigitalSignatureStatus.Valid => "Chữ ký hợp lệ",
        DigitalSignatureStatus.Invalid => "Chữ ký không hợp lệ",
        DigitalSignatureStatus.CertificateExpired => "Chứng thư hết hạn",
        DigitalSignatureStatus.Revoked => "Chữ ký đã bị thu hồi",
        DigitalSignatureStatus.Tampered => "Tài liệu đã bị thay đổi sau khi ký",
        _ => "Chưa ký"
    };

    public static string ToBadgeClass(this DigitalSignatureStatus status) => status switch
    {
        DigitalSignatureStatus.Valid or DigitalSignatureStatus.Signed => "tf-badge tf-badge-success",
        DigitalSignatureStatus.Pending => "tf-badge tf-badge-warning",
        DigitalSignatureStatus.Unsigned => "tf-badge tf-badge-gray",
        DigitalSignatureStatus.CertificateExpired => "tf-badge tf-badge-warning",
        _ => "tf-badge tf-badge-error"
    };
}

