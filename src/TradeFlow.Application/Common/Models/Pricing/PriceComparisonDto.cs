using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Pricing;

public class PriceComparisonResultDto
{
    public PriceListDto BasePriceList { get; set; } = new();
    public PriceListDto TargetPriceList { get; set; } = new();
    public List<PriceComparisonItemDto> Items { get; set; } = new();
}

public class PriceComparisonItemDto
{
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string NewCode { get; set; } = string.Empty;
    public string? LegacyCode { get; set; }
    public string? Group { get; set; }
    public string? ImageStorageRef { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal? TargetPrice { get; set; }
    public decimal? PriceDifference => (TargetPrice.HasValue && BasePrice.HasValue) ? TargetPrice.Value - BasePrice.Value : null;
    public decimal? PercentageDifference => (BasePrice.HasValue && BasePrice.Value > 0 && TargetPrice.HasValue)
        ? Math.Round(((TargetPrice.Value - BasePrice.Value) / BasePrice.Value) * 100m, 2)
        : null;
    public PriceChangeStatus ChangeStatus { get; set; }
}