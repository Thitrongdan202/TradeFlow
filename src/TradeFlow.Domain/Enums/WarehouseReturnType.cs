namespace TradeFlow.Domain.Enums;

/// <summary>
/// Chiều giao dịch hàng trả lại
/// </summary>
public enum WarehouseReturnType
{
    /// <summary>Khách trả hàng (Khách hàng trả lại hàng cho công ty - Nhập kho)</summary>
    CustomerReturn = 1,

    /// <summary>Trả hàng nhà cung cấp (Công ty xuất trả lại hàng cho nhà cung cấp - Xuất kho)</summary>
    SupplierReturn = 2
}
