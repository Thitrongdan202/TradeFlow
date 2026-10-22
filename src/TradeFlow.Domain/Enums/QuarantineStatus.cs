namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái xử lý hàng lỗi / hàng cách ly
/// </summary>
public enum QuarantineStatus
{
    /// <summary>Đang cách ly lưu kho (không được bán)</summary>
    InQuarantine = 1,

    /// <summary>Đang sửa chữa / bảo hành kỹ thuật</summary>
    Repairing = 2,

    /// <summary>Đã khắc phục / phục hồi thành hàng sử dụng được hoặc hàng tốt</summary>
    Restored = 3,

    /// <summary>Đã xuất trả nhà cung cấp để bảo hành / đổi mới</summary>
    ReturnedToSupplier = 4,

    /// <summary>Đã xuất tiêu hủy / thanh lý phế liệu (có phê duyệt cấp cao)</summary>
    Scrapped = 5
}
