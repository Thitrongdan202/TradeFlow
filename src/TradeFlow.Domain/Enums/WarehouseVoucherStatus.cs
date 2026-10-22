namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái của chứng từ kho
/// </summary>
public enum WarehouseVoucherStatus
{
    /// <summary>Bản nháp</summary>
    Draft = 1,

    /// <summary>Đã phê duyệt và ghi sổ kho</summary>
    Approved = 2,

    /// <summary>Đã hủy bỏ</summary>
    Cancelled = 3
}
