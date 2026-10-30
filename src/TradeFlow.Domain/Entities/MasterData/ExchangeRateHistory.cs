using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

public class ExchangeRateHistory : Entity<int>
{
    public int CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal? PreviousRate { get; set; }
    public string Source { get; set; } = "Thủ công"; // "Thủ công", "API ExchangeRate", "Hệ thống"
    public DateTime EffectiveAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Currency? Currency { get; set; }
}
