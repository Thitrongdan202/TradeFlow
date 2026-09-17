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

public class MultiPeriodPriceComparisonDto
{
    /// <summary>Cơ sở so sánh giá (Chưa VAT)</summary>
    public string PriceBasis { get; set; } = "Chưa VAT";
    public List<PriceListPeriodColumnDto> Periods { get; set; } = new();
    public List<MultiPeriodComparisonItemDto> Items { get; set; } = new();
}

public class PriceListPeriodColumnDto
{
    public int PriceListId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PeriodLabel { get; set; } = string.Empty; // e.g. "Q1/2026", "Q2/2026", "2026"
    public int Year { get; set; }
    public int? Quarter { get; set; }
    public int? Month { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public PriceListStatus Status { get; set; }
}

public class MultiPeriodComparisonItemDto
{
    public int? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string NewCode { get; set; } = string.Empty;
    public string? LegacyCode { get; set; }
    public string? Group { get; set; }
    public string? ImageStorageRef { get; set; }
    /// <summary>Giá theo từng kỳ (Key: PriceListId, Value: UnitPrice Chưa VAT)</summary>
    public Dictionary<int, decimal?> PeriodPrices { get; set; } = new();
    /// <summary>Giá hiện tại hiệu lực (Chưa VAT)</summary>
    public decimal? CurrentPrice { get; set; }
}