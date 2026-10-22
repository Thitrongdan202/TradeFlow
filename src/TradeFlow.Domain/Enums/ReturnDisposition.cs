namespace TradeFlow.Domain.Enums;

/// <summary>
/// Phương án xử lý / bố trí hàng trả lại sau kiểm tra chất lượng
/// </summary>
public enum ReturnDisposition
{
    /// <summary>Chờ kiểm tra phân loại</summary>
    PendingInspection = 1,

    /// <summary>Hàng tốt: Nhập lại kho khả dụng bán hàng</summary>
    RestockSaleable = 2,

    /// <summary>Hàng đã qua sử dụng / trầy xước nhẹ: Chuyển kho hàng cũ / trưng bày</summary>
    TransferUsable = 3,

    /// <summary>Hàng hỏng / lỗi: Đưa vào kho/khu vực cách ly (loại khỏi tồn khả dụng)</summary>
    QuarantineDefective = 4,

    /// <summary>Chuyển trạm bảo hành / sửa chữa kỹ thuật</summary>
    RepairService = 5,

    /// <summary>Từ chối nhận (trả lại cho khách hàng)</summary>
    RejectReturn = 6,

    /// <summary>Xuất trả lại nhà cung cấp (đổi trả theo chính sách hãng)</summary>
    ReturnToSupplier = 7
}
