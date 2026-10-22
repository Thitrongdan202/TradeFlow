namespace TradeFlow.Domain.Enums;

/// <summary>
/// Vòng đời trạng thái của đợt kiểm kê kho
/// </summary>
public enum StocktakeStatus
{
    /// <summary>Bản nháp: đang chuẩn bị đợt kiểm kê, chọn phạm vi</summary>
    Draft = 1,

    /// <summary>Đang kiểm đếm: đã chốt số liệu sổ sách snapshot, thủ kho đang đếm thực tế</summary>
    Counting = 2,

    /// <summary>Chờ duyệt: đã nhập số liệu đếm, gửi cấp quản lý phê duyệt chênh lệch</summary>
    PendingReview = 3,

    /// <summary>Cần kiểm tra lại / Bị từ chối: cấp quản lý yêu cầu đếm lại</summary>
    Rejected = 4,

    /// <summary>Đã duyệt: cấp quản lý đã phê duyệt kết quả và chênh lệch</summary>
    Approved = 5,

    /// <summary>Đã hoàn tất: đã tự động lập phiếu nhập/xuất điều chỉnh và ghi sổ kho thành công</summary>
    Completed = 6,

    /// <summary>Đã hủy bỏ</summary>
    Cancelled = 7
}
