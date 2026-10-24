namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái vòng đời hợp đồng lao động
/// </summary>
public enum EmploymentContractStatus
{
    /// <summary>0 - Bản nháp đang soạn thảo, chưa ký</summary>
    Draft = 0,

    /// <summary>1 - Đã hoàn thiện nội dung, đang chờ hai bên ký kết</summary>
    PendingSignature = 1,

    /// <summary>2 - Đang có hiệu lực thi hành</summary>
    Active = 2,

    /// <summary>3 - Sắp hết hạn (còn <= 30 ngày tính đến ngày kết thúc)</summary>
    ExpiringSoon = 3,

    /// <summary>4 - Đã hết hạn hợp đồng nhưng chưa gia hạn hoặc ký mới</summary>
    Expired = 4,

    /// <summary>5 - Đã chấm dứt theo quy định (thôi việc, thỏa thuận chấm dứt, sa thải...)</summary>
    Terminated = 5,

    /// <summary>6 - Đã hủy (bản soạn thảo sai hoặc bị hủy bỏ trước hiệu lực)</summary>
    Cancelled = 6
}
