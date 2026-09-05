using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

public class Currency : AuditableEntity<int>
{
    public Currency() { }

    public string Code { get; set; } = string.Empty;       // Mã tiền tệ (VND, USD)
    public string Name { get; set; } = string.Empty;       // Tên tiền tệ
    public string? Symbol { get; set; }                     // Ký hiệu (₫, $)
    public decimal ExchangeRate { get; set; } = 1;          // Tỷ giá so với VND
    public bool IsDefault { get; set; }                     // Đây là tiền tệ mặc định?
    public bool IsActive { get; set; } = true;
}
