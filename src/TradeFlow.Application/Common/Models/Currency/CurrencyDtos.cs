namespace TradeFlow.Application.Common.Models.Currency;

public class CurrencyDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public int DecimalPlaces { get; set; } = 2;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string RateSource { get; set; } = "Thủ công";
    public DateTime EffectiveAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRefreshedAt { get; set; }
    public string? Notes { get; set; }
    public bool IsManualOverride { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public class CurrencyConversionResult
{
    public decimal OriginalAmount { get; set; }
    public string FromCurrencyCode { get; set; } = string.Empty;
    public string ToCurrencyCode { get; set; } = string.Empty;
    public decimal FromRateToVnd { get; set; }
    public decimal ToRateToVnd { get; set; }
    public decimal CrossRate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public int TargetDecimalPlaces { get; set; }
    public string FormattedResult { get; set; } = string.Empty;
    public string FormulaExplanation { get; set; } = string.Empty;
}

public class ExchangeRateComparisonItemDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public string CurrencyName { get; set; } = string.Empty;
    public string? Symbol { get; set; }
    public decimal CurrentRate { get; set; }
    public decimal NewRate { get; set; }
    public decimal RateDifference => NewRate - CurrentRate;
    public decimal PercentageChange => CurrentRate > 0 ? ((NewRate - CurrentRate) / CurrentRate) * 100m : 0m;
    public bool IsManualOverride { get; set; }
    public bool IsSelected { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? StatusMessage { get; set; }
}

public class ExchangeRatePreviewDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
    public List<ExchangeRateComparisonItemDto> Items { get; set; } = new();
}

public class ApplyExchangeRatesRequest
{
    public List<ApplyRateItemDto> RatesToApply { get; set; } = new();
    public DateTime EffectiveAt { get; set; } = DateTime.UtcNow;
}

public class ApplyRateItemDto
{
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public bool KeepManualOverride { get; set; }
}

public class ApplyExchangeRatesResult
{
    public bool Success { get; set; }
    public int UpdatedCount { get; set; }
    public string? Message { get; set; }
}

public class UpdateManualRateRequest
{
    public int CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal NewRate { get; set; }
    public DateTime EffectiveAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public bool IsManualOverride { get; set; } = true;
}

public class ExchangeRateHistoryDto
{
    public int Id { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal Rate { get; set; }
    public decimal? PreviousRate { get; set; }
    public string Source { get; set; } = "Thủ công";
    public DateTime EffectiveAt { get; set; }
    public string? Notes { get; set; }
    public string? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}
