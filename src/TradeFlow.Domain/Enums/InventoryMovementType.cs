namespace TradeFlow.Domain.Enums;

/// <summary>
/// Loại biến động xuất nhập tồn kho (thẻ kho)
/// </summary>
public enum InventoryMovementType
{
    /// <summary>Nhập kho mua hàng từ nhà cung cấp</summary>
    InboundPurchase = 1,

    /// <summary>Nhập kho điều chỉnh kiểm kê thừa (sau duyệt kiểm kê)</summary>
    InboundStocktakeSurplus = 2,

    /// <summary>Nhập kho hàng khách trả lại (đã kiểm tra hàng tốt)</summary>
    InboundCustomerReturn = 3,

    /// <summary>Nhập kho chuyển nội bộ</summary>
    InboundTransfer = 4,

    /// <summary>Xuất kho bán hàng</summary>
    OutboundSale = 5,

    /// <summary>Xuất kho điều chỉnh kiểm kê thiếu (sau duyệt kiểm kê)</summary>
    OutboundStocktakeShortage = 6,

    /// <summary>Xuất kho trả hàng nhà cung cấp</summary>
    OutboundSupplierReturn = 7,

    /// <summary>Xuất hủy hàng hỏng / mất phẩm chất</summary>
    OutboundScrap = 8,

    /// <summary>Xuất kho chuyển nội bộ</summary>
    OutboundTransfer = 9,

    /// <summary>Chuyển trạng thái vào khu cách ly</summary>
    QuarantineTransferIn = 10,

    /// <summary>Xử lý / giải tỏa khỏi khu cách ly (phục hồi hoặc thanh lý)</summary>
    QuarantineDisposition = 11,

    /// <summary>Điều chỉnh số dư ban đầu</summary>
    InitialBalance = 12
}
