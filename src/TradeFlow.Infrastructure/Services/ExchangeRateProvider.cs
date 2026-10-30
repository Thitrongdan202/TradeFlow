using System.Text.Json;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;

namespace TradeFlow.Infrastructure.Services;

public class ExchangeRateProvider : IExchangeRateProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ExchangeRateProvider> _logger;

    private const string UsdEndpoint = "https://open.er-api.com/v6/latest/USD";
    private const string VndEndpoint = "https://open.er-api.com/v6/latest/VND";
    private const int MaxResponseBytes = 256 * 1024; // 256 KB max payload limit

    public ExchangeRateProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<ExchangeRateProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public string ProviderName => "ExchangeRate-API (HTTPS)";

    public async Task<ExchangeRateFetchResult> FetchRatesAsync(CancellationToken cancellationToken = default)
    {
        var result = new ExchangeRateFetchResult
        {
            ProviderName = ProviderName,
            BaseCurrency = "VND",
            FetchedAtUtc = DateTime.UtcNow
        };

        // Attempt 1: Fetch via USD pivot endpoint (highest precision across all currency pairs)
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var (success, rates) = await FetchFromUsdEndpointAsync(cts.Token);
            if (success && rates.Count > 0)
            {
                result.Success = true;
                result.RatesToVnd = rates;
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "USD-base exchange rate endpoint failed, attempting fallback to VND endpoint.");
        }

        // Attempt 2: Fallback to direct VND endpoint
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(8));

            var (success, rates) = await FetchFromVndEndpointAsync(cts.Token);
            if (success && rates.Count > 0)
            {
                result.Success = true;
                result.RatesToVnd = rates;
                return result;
            }

            result.Success = false;
            result.ErrorMessage = "Không thể lấy dữ liệu tỷ giá hợp lệ từ cả hai cổng cung cấp.";
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Exchange rate request timed out after 8 seconds.");
            result.Success = false;
            result.ErrorMessage = "Hết thời gian chờ phản hồi từ máy chủ tỷ giá (Timeout > 8 giây). Vui lòng thử lại sau.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch exchange rates from external provider.");
            result.Success = false;
            result.ErrorMessage = $"Lỗi kết nối dịch vụ tỷ giá: {ex.Message}";
        }

        return result;
    }

    private async Task<(bool Success, Dictionary<string, decimal> Rates)> FetchFromUsdEndpointAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ExchangeRateProvider");
        using var response = await client.GetAsync(UsdEndpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ExchangeRate API USD endpoint returned status {StatusCode}", response.StatusCode);
            return (false, new());
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        byte[] buffer = new byte[8192];
        int totalBytesRead = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            totalBytesRead += read;
            if (totalBytesRead > MaxResponseBytes)
            {
                throw new InvalidOperationException($"Phản hồi từ máy chủ tỷ giá vượt quá giới hạn an toàn {MaxResponseBytes / 1024} KB.");
            }
            memoryStream.Write(buffer, 0, read);
        }

        memoryStream.Position = 0;
        using var doc = await JsonDocument.ParseAsync(memoryStream, cancellationToken: cancellationToken);
        var root = doc.RootElement;

        if (root.TryGetProperty("result", out var resultProp) && resultProp.GetString() != "success")
        {
            return (false, new());
        }

        if (!root.TryGetProperty("rates", out var ratesProp) || ratesProp.ValueKind != JsonValueKind.Object)
        {
            return (false, new());
        }

        if (!ratesProp.TryGetProperty("VND", out var vndProp) || !vndProp.TryGetDecimal(out var vndPerUsd) || vndPerUsd <= 0)
        {
            _logger.LogWarning("ExchangeRate API USD response did not contain valid VND rate.");
            return (false, new());
        }

        var ratesToVnd = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["VND"] = 1.0m,
            ["USD"] = Math.Round(vndPerUsd, 4)
        };

        foreach (var prop in ratesProp.EnumerateObject())
        {
            string code = prop.Name.ToUpperInvariant();
            if (code == "VND" || code == "USD") continue;

            if (prop.Value.TryGetDecimal(out var foreignPerUsd) && foreignPerUsd > 0)
            {
                // Rate to VND = VND_per_USD / Foreign_per_USD
                // e.g. EUR: 25400 / 0.9 = ~28222 VND
                decimal rateToVnd = vndPerUsd / foreignPerUsd;
                if (rateToVnd > 0 && rateToVnd < 100_000_000_000m)
                {
                    ratesToVnd[code] = Math.Round(rateToVnd, 6);
                }
            }
        }

        return (true, ratesToVnd);
    }

    private async Task<(bool Success, Dictionary<string, decimal> Rates)> FetchFromVndEndpointAsync(CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("ExchangeRateProvider");
        using var response = await client.GetAsync(VndEndpoint, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("ExchangeRate API VND endpoint returned status {StatusCode}", response.StatusCode);
            return (false, new());
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        byte[] buffer = new byte[8192];
        int totalBytesRead = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            totalBytesRead += read;
            if (totalBytesRead > MaxResponseBytes)
            {
                throw new InvalidOperationException($"Phản hồi từ máy chủ tỷ giá vượt quá giới hạn an toàn {MaxResponseBytes / 1024} KB.");
            }
            memoryStream.Write(buffer, 0, read);
        }

        memoryStream.Position = 0;
        using var doc = await JsonDocument.ParseAsync(memoryStream, cancellationToken: cancellationToken);
        var root = doc.RootElement;

        if (root.TryGetProperty("result", out var resultProp) && resultProp.GetString() != "success")
        {
            return (false, new());
        }

        if (!root.TryGetProperty("rates", out var ratesProp) || ratesProp.ValueKind != JsonValueKind.Object)
        {
            return (false, new());
        }

        var ratesToVnd = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["VND"] = 1.0m
        };

        foreach (var prop in ratesProp.EnumerateObject())
        {
            string code = prop.Name.ToUpperInvariant();
            if (code == "VND") continue;

            if (prop.Value.TryGetDecimal(out var foreignPerVnd) && foreignPerVnd > 0)
            {
                // Rate to VND = 1.0 / foreignPerVnd
                decimal rateToVnd = 1.0m / foreignPerVnd;
                if (rateToVnd > 0 && rateToVnd < 100_000_000_000m)
                {
                    ratesToVnd[code] = Math.Round(rateToVnd, 6);
                }
            }
        }

        return (true, ratesToVnd);
    }
}
