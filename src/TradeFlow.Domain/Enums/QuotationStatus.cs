namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái của Báo giá thương mại
/// </summary>
public enum QuotationStatus
{
    /// <summary>Bản nháp</summary>
    Draft = 0,

    /// <summary>Đã gửi cho khách hàng</summary>
    Sent = 1,

    /// <summary>Khách hàng đã chấp thuận</summary>
    Accepted = 2,

    /// <summary>Khách hàng từ chối</summary>
    Rejected = 3,

    /// <summary>Hết hạn hiệu lực</summary>
    Expired = 4,

    /// <summary>Đã chuyển thành Đơn bán hàng</summary>
    Converted = 5,

    /// <summary>Đã hủy</summary>
    Cancelled = 99
}
