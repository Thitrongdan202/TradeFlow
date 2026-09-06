namespace TradeFlow.Domain.Enums;

/// <summary>Trạng thái đối soát khớp mã sản phẩm khi nhập bảng giá từ Excel</summary>
public enum PriceMatchStatus
{
    /// <summary>Đã khớp chính xác với sản phẩm trong danh mục hệ thống</summary>
    Matched = 1,

    /// <summary>Cần người dùng xem xét kiểm tra (chưa khớp chắc chắn)</summary>
    ReviewRequired = 2,

    /// <summary>Sản phẩm mới hoàn toàn (chưa có trong hệ thống)</summary>
    NewProduct = 3
}