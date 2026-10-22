namespace TradeFlow.Domain.Enums;

/// <summary>
/// Tình trạng / phẩm chất của hàng hóa trong kho
/// </summary>
public enum StockCondition
{
    /// <summary>Hàng tốt (đạt chuẩn chất lượng, khả dụng cho bán hàng)</summary>
    Saleable = 1,

    /// <summary>Kém phẩm chất / đã qua sử dụng nhưng còn sử dụng hoặc bán thanh lý được</summary>
    Usable = 2,

    /// <summary>Hỏng / mất phẩm chất / cách ly (không được phép bán)</summary>
    Defective = 3,

    /// <summary>Chưa xác định (đang chờ bộ phận kiểm tra đánh giá chất lượng)</summary>
    Undetermined = 4
}
