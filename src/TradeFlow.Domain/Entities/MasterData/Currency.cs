using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

public class Currency : AuditableEntity<int>
{
    public Currency() { }

    public string Code { get; set; } = string.Empty;       // Mã tiền tệ (VND, USD, EUR...)
    public string Name { get; set; } = string.Empty;       // Tên tiền tệ (Việt Nam đồng, Đô la Mỹ...)
    public string? Symbol { get; set; }                     // Ký hiệu (₫, $, €...)
    public decimal ExchangeRate { get; set; } = 1;          // Tỷ giá quy đổi so với VND (1 đơn vị ngoại tệ = X VND)
    public int DecimalPlaces { get; set; } = 2;             // Số chữ số thập phân hiển thị (0 cho VND, JPY; 2 cho USD, EUR...)
    public bool IsDefault { get; set; }                     // Tiền tệ cơ sở mặc định (VND = true)
    public bool IsActive { get; set; } = true;              // Trạng thái hoạt động
    public string RateSource { get; set; } = "Thủ công";   // Nguồn tỷ giá ("Gốc", "Thủ công", "API ExchangeRate")
    public DateTime EffectiveAt { get; set; } = DateTime.UtcNow; // Thời điểm áp dụng tỷ giá
    public DateTime? LastRefreshedAt { get; set; }         // Thời điểm cập nhật trực tuyến gần nhất
    public string? Notes { get; set; }                     // Ghi chú / Căn cứ tỷ giá
    public bool IsManualOverride { get; set; } = false;    // Cố định tỷ giá thủ công (không tự động ghi đè từ API)
}
