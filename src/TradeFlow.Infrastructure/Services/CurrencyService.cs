using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Currency;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class CurrencyService : ICurrencyService
{
    private readonly TradeFlowDbContext _context;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly IAuditService _auditService;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(
        TradeFlowDbContext context,
        IExchangeRateProvider exchangeRateProvider,
        IAuditService auditService,
        ILogger<CurrencyService> logger)
    {
        _context = context;
        _exchangeRateProvider = exchangeRateProvider;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<CurrencyDto>> GetAllCurrenciesAsync(bool onlyActive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Currencies.AsNoTracking();
        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        var items = await query
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<CurrencyDto?> GetCurrencyByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var entity = await _context.Currencies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant(), cancellationToken);

        return entity == null ? null : MapToDto(entity);
    }

    public async Task<CurrencyConversionResult> ConvertAmountAsync(
        decimal amount,
        string fromCurrencyCode,
        string toCurrencyCode,
        CancellationToken cancellationToken = default)
    {
        var fromCode = (fromCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        var toCode = (toCurrencyCode ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(fromCode) || string.IsNullOrEmpty(toCode))
        {
            throw new ArgumentException("Mã tiền tệ nguồn và đích không được để trống.");
        }

        var currencies = await _context.Currencies
            .AsNoTracking()
            .Where(c => c.Code == fromCode || c.Code == toCode)
            .ToDictionaryAsync(c => c.Code, cancellationToken);

        if (!currencies.TryGetValue(fromCode, out var fromCurrency))
        {
            throw new InvalidOperationException($"Không tìm thấy đồng tiền nguồn '{fromCode}' trong hệ thống.");
        }

        if (!currencies.TryGetValue(toCode, out var toCurrency))
        {
            throw new InvalidOperationException($"Không tìm thấy đồng tiền đích '{toCode}' trong hệ thống.");
        }

        if (fromCurrency.ExchangeRate <= 0)
        {
            throw new InvalidOperationException($"Tỷ giá của đồng tiền nguồn '{fromCode}' không hợp lệ ({fromCurrency.ExchangeRate}).");
        }

        if (toCurrency.ExchangeRate <= 0)
        {
            throw new InvalidOperationException($"Tỷ giá của đồng tiền đích '{toCode}' không hợp lệ ({toCurrency.ExchangeRate}).");
        }

        // Quy đổi qua VND cơ sở:
        // 1. Chuyển tiền nguồn sang VND: vndAmount = amount * fromRate
        // 2. Chuyển VND sang tiền đích: destAmount = vndAmount / toRate
        decimal fromRate = fromCurrency.ExchangeRate;
        decimal toRate = toCurrency.ExchangeRate;
        decimal crossRate = toRate > 0 ? fromRate / toRate : 1m;
        decimal vndAmount = amount * fromRate;
        decimal convertedAmount = toRate > 0 ? vndAmount / toRate : 0m;

        int targetDecimals = toCurrency.DecimalPlaces;
        convertedAmount = Math.Round(convertedAmount, targetDecimals, MidpointRounding.AwayFromZero);

        string formatPattern = targetDecimals > 0 ? $"N{targetDecimals}" : "N0";
        var viCulture = CultureInfo.GetCultureInfo("vi-VN");
        string formattedAmount = convertedAmount.ToString(formatPattern, viCulture);

        string formula;
        if (fromCode == toCode)
        {
            formula = $"Không đổi (1 {fromCode} = 1 {toCode})";
        }
        else if (toCode == "VND")
        {
            formula = $"{amount.ToString("N2", viCulture)} {fromCode} × {fromRate.ToString("N2", viCulture)} = {formattedAmount} VND";
        }
        else if (fromCode == "VND")
        {
            formula = $"{amount.ToString("N2", viCulture)} VND ÷ {toRate.ToString("N2", viCulture)} = {formattedAmount} {toCode}";
        }
        else
        {
            formula = $"{amount.ToString("N2", viCulture)} {fromCode} × ({fromRate.ToString("N2", viCulture)} / {toRate.ToString("N2", viCulture)}) = {formattedAmount} {toCode}";
        }

        return new CurrencyConversionResult
        {
            OriginalAmount = amount,
            FromCurrencyCode = fromCode,
            ToCurrencyCode = toCode,
            FromRateToVnd = fromRate,
            ToRateToVnd = toRate,
            CrossRate = crossRate,
            ConvertedAmount = convertedAmount,
            TargetDecimalPlaces = targetDecimals,
            FormattedResult = formattedAmount,
            FormulaExplanation = formula
        };
    }

    public async Task<ExchangeRatePreviewDto> FetchLatestRatesPreviewAsync(CancellationToken cancellationToken = default)
    {
        var preview = new ExchangeRatePreviewDto
        {
            ProviderName = _exchangeRateProvider.ProviderName,
            FetchedAtUtc = DateTime.UtcNow
        };

        var fetchResult = await _exchangeRateProvider.FetchRatesAsync(cancellationToken);
        if (!fetchResult.Success)
        {
            preview.Success = false;
            preview.ErrorMessage = fetchResult.ErrorMessage ?? "Không thể lấy dữ liệu tỷ giá từ nhà cung cấp.";
            return preview;
        }

        var dbCurrencies = await _context.Currencies
            .AsNoTracking()
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);

        var items = new List<ExchangeRateComparisonItemDto>();

        foreach (var cur in dbCurrencies)
        {
            if (cur.IsDefault || cur.Code == "VND")
            {
                continue; // Tiền tệ cơ sở không thay đổi tỷ giá (luôn là 1.0)
            }

            if (fetchResult.RatesToVnd.TryGetValue(cur.Code, out var fetchedRate) && fetchedRate > 0)
            {
                var item = new ExchangeRateComparisonItemDto
                {
                    CurrencyCode = cur.Code,
                    CurrencyName = cur.Name,
                    Symbol = cur.Symbol,
                    CurrentRate = cur.ExchangeRate,
                    NewRate = fetchedRate,
                    IsManualOverride = cur.IsManualOverride,
                    // Nếu người dùng đã đánh dấu cố định thủ công thì mặc định không tích chọn
                    IsSelected = !cur.IsManualOverride && cur.IsActive,
                    IsActive = cur.IsActive,
                    StatusMessage = cur.IsManualOverride ? "Đã cố định thủ công (ưu tiên bảo lưu)" : null
                };
                items.Add(item);
            }
        }

        preview.Success = true;
        preview.Items = items;
        return preview;
    }

    public async Task<ApplyExchangeRatesResult> ApplyExchangeRatesAsync(
        ApplyExchangeRatesRequest request,
        string? updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (request?.RatesToApply == null || request.RatesToApply.Count == 0)
        {
            return new ApplyExchangeRatesResult { Success = true, UpdatedCount = 0, Message = "Không có tỷ giá nào được chọn để áp dụng." };
        }

        var now = DateTime.UtcNow;
        var effectiveAt = request.EffectiveAt > DateTime.MinValue ? request.EffectiveAt : now;
        var username = string.IsNullOrWhiteSpace(updatedBy) ? "System" : updatedBy.Trim();

        var codesToUpdate = request.RatesToApply.Select(r => r.CurrencyCode.ToUpperInvariant()).ToHashSet();
        var currencies = await _context.Currencies
            .Where(c => codesToUpdate.Contains(c.Code))
            .ToListAsync(cancellationToken);

        int count = 0;
        foreach (var item in request.RatesToApply)
        {
            var code = item.CurrencyCode.ToUpperInvariant();
            if (code == "VND") continue; // Không thay đổi đồng cơ sở

            var currency = currencies.FirstOrDefault(c => c.Code == code);
            if (currency == null) continue;

            if (item.Rate <= 0) continue;

            decimal oldRate = currency.ExchangeRate;
            currency.ExchangeRate = item.Rate;
            currency.RateSource = "API ExchangeRate";
            currency.EffectiveAt = effectiveAt;
            currency.LastRefreshedAt = now;
            currency.UpdatedAt = now;
            currency.UpdatedBy = username;

            if (!item.KeepManualOverride)
            {
                currency.IsManualOverride = false;
            }

            // Ghi nhận lịch sử biến động tỷ giá
            var history = new ExchangeRateHistory
            {
                CurrencyId = currency.Id,
                CurrencyCode = currency.Code,
                Rate = item.Rate,
                PreviousRate = oldRate,
                Source = "API ExchangeRate",
                EffectiveAt = effectiveAt,
                Notes = $"Cập nhật trực tuyến từ {_exchangeRateProvider.ProviderName}. Biến động: {(item.Rate - oldRate):+0.####;-0.####;0}",
                ChangedBy = username,
                CreatedAt = now
            };
            _context.ExchangeRateHistories.Add(history);
            count++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.CurrencyUpdated,
            username,
            "Currency",
            null,
            $"Cập nhật trực tuyến tỷ giá cho {count} đồng tiền từ {_exchangeRateProvider.ProviderName}.",
            cancellationToken: cancellationToken);

        return new ApplyExchangeRatesResult
        {
            Success = true,
            UpdatedCount = count,
            Message = $"Đã cập nhật tỷ giá thành công cho {count} loại tiền tệ."
        };
    }

    public async Task<bool> UpdateManualRateAsync(
        UpdateManualRateRequest request,
        string? updatedBy,
        CancellationToken cancellationToken = default)
    {
        if (request == null) return false;
        if (request.NewRate <= 0)
        {
            throw new ArgumentException("Tỷ giá phải là số dương lớn hơn 0.");
        }

        var currency = await _context.Currencies.FindAsync(new object[] { request.CurrencyId }, cancellationToken);
        if (currency == null)
        {
            throw new InvalidOperationException($"Không tìm thấy đồng tiền với ID {request.CurrencyId}.");
        }

        if (currency.IsDefault || currency.Code == "VND")
        {
            throw new InvalidOperationException("Không thể thay đổi tỷ giá của đồng tiền cơ sở (VND).");
        }

        var now = DateTime.UtcNow;
        var effectiveAt = request.EffectiveAt > DateTime.MinValue ? request.EffectiveAt : now;
        var username = string.IsNullOrWhiteSpace(updatedBy) ? "System" : updatedBy.Trim();

        decimal oldRate = currency.ExchangeRate;
        currency.ExchangeRate = request.NewRate;
        currency.RateSource = "Thủ công";
        currency.EffectiveAt = effectiveAt;
        currency.Notes = request.Notes;
        currency.IsManualOverride = request.IsManualOverride;
        currency.UpdatedAt = now;
        currency.UpdatedBy = username;

        // Lưu lịch sử
        var history = new ExchangeRateHistory
        {
            CurrencyId = currency.Id,
            CurrencyCode = currency.Code,
            Rate = request.NewRate,
            PreviousRate = oldRate,
            Source = "Thủ công",
            EffectiveAt = effectiveAt,
            Notes = request.Notes,
            ChangedBy = username,
            CreatedAt = now
        };
        _context.ExchangeRateHistories.Add(history);

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.CurrencyUpdated,
            username,
            "Currency",
            currency.Id.ToString(),
            $"Điều chỉnh tỷ giá thủ công {currency.Code}: {oldRate:N4} -> {request.NewRate:N4}. Lý do: {request.Notes}",
            cancellationToken: cancellationToken);

        return true;
    }

    public async Task<List<ExchangeRateHistoryDto>> GetRateHistoryAsync(string currencyCode, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)) return new();

        var code = currencyCode.Trim().ToUpperInvariant();
        var items = await _context.ExchangeRateHistories
            .AsNoTracking()
            .Where(h => h.CurrencyCode == code)
            .OrderByDescending(h => h.EffectiveAt)
            .ThenByDescending(h => h.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return items.Select(h => new ExchangeRateHistoryDto
        {
            Id = h.Id,
            CurrencyCode = h.CurrencyCode,
            Rate = h.Rate,
            PreviousRate = h.PreviousRate,
            Source = h.Source,
            EffectiveAt = h.EffectiveAt,
            Notes = h.Notes,
            ChangedBy = h.ChangedBy,
            CreatedAt = h.CreatedAt
        }).ToList();
    }

    public async Task<bool> ToggleCurrencyStatusAsync(int id, string? updatedBy, CancellationToken cancellationToken = default)
    {
        var currency = await _context.Currencies.FindAsync(new object[] { id }, cancellationToken);
        if (currency == null) return false;

        if (currency.IsDefault || currency.Code == "VND")
        {
            throw new InvalidOperationException("Không thể tạm dừng đồng tiền cơ sở mặc định (VND).");
        }

        currency.IsActive = !currency.IsActive;
        currency.UpdatedAt = DateTime.UtcNow;
        currency.UpdatedBy = updatedBy ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.CurrencyUpdated,
            updatedBy ?? "System",
            "Currency",
            currency.Id.ToString(),
            $"Thay đổi trạng thái {currency.Code} sang {(currency.IsActive ? "Hoạt động" : "Tạm dừng")}.",
            cancellationToken: cancellationToken);

        return true;
    }

    private static CurrencyDto MapToDto(Currency c) => new()
    {
        Id = c.Id,
        Code = c.Code,
        Name = c.Name,
        Symbol = c.Symbol,
        ExchangeRate = c.ExchangeRate,
        DecimalPlaces = c.DecimalPlaces,
        IsDefault = c.IsDefault,
        IsActive = c.IsActive,
        RateSource = c.RateSource,
        EffectiveAt = c.EffectiveAt,
        LastRefreshedAt = c.LastRefreshedAt,
        Notes = c.Notes,
        IsManualOverride = c.IsManualOverride,
        CreatedAt = c.CreatedAt,
        CreatedBy = c.CreatedBy,
        UpdatedAt = c.UpdatedAt,
        UpdatedBy = c.UpdatedBy
    };
}
