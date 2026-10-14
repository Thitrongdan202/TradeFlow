namespace TradeFlow.Domain.Enums;

/// <summary>
/// Phương thức xác lập / ký kết hợp đồng
/// </summary>
public enum ContractExecutionMethod
{
    /// <summary>Chưa xác định</summary>
    NotSpecified = 0,

    /// <summary>Ký điện tử / Ký số trong hệ thống</summary>
    Electronic = 1,

    /// <summary>In văn bản giấy và ký tay / đóng dấu ướt</summary>
    PaperPrinted = 2,

    /// <summary>Kết hợp (Ký điện tử và in lưu trữ)</summary>
    Both = 3
}
