namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái chu kỳ sống của hợp đồng thương mại
/// </summary>
public enum ContractStatus
{
    /// <summary>Bản nháp - đang biên soạn, có thể sửa đổi</summary>
    Draft = 0,

    /// <summary>Chờ ký - đã chốt nội dung (Finalized), chờ các bên ký duyệt</summary>
    PendingSignature = 1,

    /// <summary>Đã ký - đã được ký kết đầy đủ theo quy trình</summary>
    Signed = 2,

    /// <summary>Đã in - đã xuất bản in trên giấy để ký tay truyền thống</summary>
    Printed = 3,

    /// <summary>Đã thanh lý - hợp đồng đã thực hiện xong và lập biên bản thanh lý</summary>
    Liquidated = 4,

    /// <summary>Đã hủy - hợp đồng bị hủy bỏ hoặc chấm dứt trước hạn</summary>
    Cancelled = 5
}
