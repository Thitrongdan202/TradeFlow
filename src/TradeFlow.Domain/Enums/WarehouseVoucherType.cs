namespace TradeFlow.Domain.Enums;

/// <summary>
/// Loại chứng từ / phiếu kho theo chế độ kế toán Việt Nam
/// </summary>
public enum WarehouseVoucherType
{
    /// <summary>Phiếu nhập kho (Mẫu 01-VT)</summary>
    InboundReceipt = 1,

    /// <summary>Phiếu xuất kho (Mẫu 02-VT)</summary>
    OutboundIssue = 2,

    /// <summary>Biên bản kiểm nghiệm vật tư, hàng hóa (Mẫu 03-VT)</summary>
    InspectionRecord = 3,

    /// <summary>Biên bản tổng hợp kiểm kê vật tư, hàng hóa (Mẫu 05-VT)</summary>
    StocktakeReport = 4
}
