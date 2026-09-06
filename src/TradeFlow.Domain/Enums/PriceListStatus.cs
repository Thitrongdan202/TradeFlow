namespace TradeFlow.Domain.Enums;

/// <summary>Trạng thái hiệu lực của bảng giá</summary>
public enum PriceListStatus
{
    /// <summary>Bản nháp / Đang biên tập</summary>
    Draft = 1,

    /// <summary>Đang áp dụng</summary>
    Active = 2,

    /// <summary>Hết hiệu lực</summary>
    Expired = 3,

    /// <summary>Đã hủy bỏ</summary>
    Cancelled = 4
}