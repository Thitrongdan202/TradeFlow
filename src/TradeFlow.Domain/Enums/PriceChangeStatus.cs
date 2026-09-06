namespace TradeFlow.Domain.Enums;

/// <summary>Trạng thái biến động giá so với kỳ trước</summary>
public enum PriceChangeStatus
{
    /// <summary>Tăng giá</summary>
    Increased = 1,

    /// <summary>Giảm giá</summary>
    Decreased = 2,

    /// <summary>Không đổi</summary>
    Unchanged = 3,

    /// <summary>Sản phẩm mới áp dụng</summary>
    NewProduct = 4,

    /// <summary>Ngừng áp dụng / Không còn trong bảng giá này</summary>
    Discontinued = 5
}