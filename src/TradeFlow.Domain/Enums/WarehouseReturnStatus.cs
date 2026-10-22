namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái quy trình tiếp nhận và xử lý hàng trả lại
/// </summary>
public enum WarehouseReturnStatus
{
    /// <summary>Chờ tiếp nhận tại cửa kho</summary>
    PendingIntake = 1,

    /// <summary>Đã tiếp nhận vật lý, chờ kiểm tra chất lượng</summary>
    ReceivedAwaitingInspection = 2,

    /// <summary>Đã kiểm tra chất lượng, chờ phê duyệt phương án xử lý</summary>
    Inspected = 3,

    /// <summary>Đã duyệt phương án xử lý (nhập hàng tốt, cách ly hàng lỗi, từ chối...)</summary>
    Approved = 4,

    /// <summary>Đã hoàn tất (chứng từ kho đã ghi nhận thành công)</summary>
    Completed = 5,

    /// <summary>Từ chối nhận hàng (không đúng chính sách đổi trả / không đạt)</summary>
    Rejected = 6,

    /// <summary>Đã hủy bỏ</summary>
    Cancelled = 7
}
