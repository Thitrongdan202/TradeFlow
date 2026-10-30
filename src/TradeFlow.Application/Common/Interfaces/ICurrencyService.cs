using TradeFlow.Application.Common.Models.Currency;

namespace TradeFlow.Application.Common.Interfaces;

public interface ICurrencyService
{
    Task<List<CurrencyDto>> GetAllCurrenciesAsync(bool onlyActive = false, CancellationToken cancellationToken = default);
    Task<CurrencyDto?> GetCurrencyByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task<CurrencyConversionResult> ConvertAmountAsync(decimal amount, string fromCurrencyCode, string toCurrencyCode, CancellationToken cancellationToken = default);
    Task<ExchangeRatePreviewDto> FetchLatestRatesPreviewAsync(CancellationToken cancellationToken = default);
    Task<ApplyExchangeRatesResult> ApplyExchangeRatesAsync(ApplyExchangeRatesRequest request, string? updatedBy, CancellationToken cancellationToken = default);
    Task<bool> UpdateManualRateAsync(UpdateManualRateRequest request, string? updatedBy, CancellationToken cancellationToken = default);
    Task<List<ExchangeRateHistoryDto>> GetRateHistoryAsync(string currencyCode, int limit = 50, CancellationToken cancellationToken = default);
    Task<bool> ToggleCurrencyStatusAsync(int id, string? updatedBy, CancellationToken cancellationToken = default);
}
