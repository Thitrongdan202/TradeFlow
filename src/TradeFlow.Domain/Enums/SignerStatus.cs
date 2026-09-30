namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái chứng thư / danh tính người ký số
/// </summary>
public enum SignerStatus
{
    /// <summary>Đang hoạt động</summary>
    Active = 1,

    /// <summary>Tạm dừng</summary>
    Suspended = 2,

    /// <summary>Đã thu hồi</summary>
    Revoked = 3,

    /// <summary>Đã hết hạn</summary>
    Expired = 4
}

public static class SignerStatusExtensions
{
    public static string ToVietnamese(this SignerStatus status) => status switch
    {
        SignerStatus.Active => "Đang hoạt động",
        SignerStatus.Suspended => "Tạm dừng",
        SignerStatus.Revoked => "Đã thu hồi",
        SignerStatus.Expired => "Đã hết hạn",
        _ => "Không xác định"
    };

    public static string ToBadgeClass(this SignerStatus status) => status switch
    {
        SignerStatus.Active => "tf-badge tf-badge-success",
        SignerStatus.Suspended => "tf-badge tf-badge-warning",
        SignerStatus.Expired => "tf-badge tf-badge-warning",
        _ => "tf-badge tf-badge-error"
    };
}
