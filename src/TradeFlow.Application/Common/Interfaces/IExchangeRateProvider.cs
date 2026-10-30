namespace TradeFlow.Application.Common.Interfaces;

public class ExchangeRateFetchResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = "VND";
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Rates normalized to: 1 Foreign Currency = X VND.
    /// Example: "USD" -> 25400m
    /// </summary>
    public Dictionary<string, decimal> RatesToVnd { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public interface IExchangeRateProvider
{
    string ProviderName { get; }
    Task<ExchangeRateFetchResult> FetchRatesAsync(CancellationToken cancellationToken = default);
}
