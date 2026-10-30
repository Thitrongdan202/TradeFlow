using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Currency;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.MasterData;

public class CurrencyServiceTests
{
    private TradeFlowDbContext CreateDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var context = new TradeFlowDbContext(options);
        return context;
    }

    private void SeedCurrencies(TradeFlowDbContext context)
    {
        context.Currencies.AddRange(
            new Currency { Code = "VND", Name = "Việt Nam đồng", Symbol = "₫", ExchangeRate = 1m, DecimalPlaces = 0, IsDefault = true, IsActive = true, RateSource = "Gốc" },
            new Currency { Code = "USD", Name = "Đô la Mỹ", Symbol = "$", ExchangeRate = 25400m, DecimalPlaces = 2, IsDefault = false, IsActive = true, RateSource = "Thủ công" },
            new Currency { Code = "EUR", Name = "Euro", Symbol = "€", ExchangeRate = 28000m, DecimalPlaces = 2, IsDefault = false, IsActive = true, RateSource = "Thủ công" },
            new Currency { Code = "JPY", Name = "Yên Nhật", Symbol = "¥", ExchangeRate = 165m, DecimalPlaces = 0, IsDefault = false, IsActive = true, RateSource = "Thủ công" },
            new Currency { Code = "GBP", Name = "Bảng Anh", Symbol = "£", ExchangeRate = 32500m, DecimalPlaces = 2, IsDefault = false, IsActive = true, RateSource = "Thủ công", IsManualOverride = true }
        );
        context.SaveChanges();
    }

    [Fact]
    public async Task ConvertAmountAsync_FromUsdToVnd_CalculatesExactAmount()
    {
        // Arrange
        using var context = CreateDbContext(nameof(ConvertAmountAsync_FromUsdToVnd_CalculatesExactAmount));
        SeedCurrencies(context);

        var mockProvider = new Mock<IExchangeRateProvider>();
        var mockAudit = new Mock<IAuditService>();
        var mockLogger = new Mock<ILogger<CurrencyService>>();

        var service = new CurrencyService(context, mockProvider.Object, mockAudit.Object, mockLogger.Object);

        // Act (100 USD * 25,400 = 2,540,000 VND)
        var result = await service.ConvertAmountAsync(100m, "USD", "VND");

        // Assert
        result.Should().NotBeNull();
        result.OriginalAmount.Should().Be(100m);
        result.ConvertedAmount.Should().Be(2540000m);
        result.FromCurrencyCode.Should().Be("USD");
        result.ToCurrencyCode.Should().Be("VND");
        result.TargetDecimalPlaces.Should().Be(0);
        result.FormattedResult.Should().Contain("2.540.000");
    }

    [Fact]
    public async Task ConvertAmountAsync_FromVndToUsd_CalculatesExactAmountWithTwoDecimals()
    {
        // Arrange
        using var context = CreateDbContext(nameof(ConvertAmountAsync_FromVndToUsd_CalculatesExactAmountWithTwoDecimals));
        SeedCurrencies(context);

        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());

        // Act (254,000 VND / 25,400 = 10.00 USD)
        var result = await service.ConvertAmountAsync(254000m, "VND", "USD");

        // Assert
        result.ConvertedAmount.Should().Be(10.00m);
        result.TargetDecimalPlaces.Should().Be(2);
        result.FormattedResult.Should().Be("10,00");
    }

    [Fact]
    public async Task ConvertAmountAsync_CrossCurrency_EurToUsd_CalculatesCorrectly()
    {
        // Arrange
        using var context = CreateDbContext(nameof(ConvertAmountAsync_CrossCurrency_EurToUsd_CalculatesCorrectly));
        SeedCurrencies(context);

        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());

        // Act: 100 EUR * (28000 / 25400) = ~110.24 USD
        var result = await service.ConvertAmountAsync(100m, "EUR", "USD");

        // Assert
        result.ConvertedAmount.Should().Be(110.24m);
        result.TargetDecimalPlaces.Should().Be(2);
    }

    [Fact]
    public async Task ConvertAmountAsync_SameCurrency_ReturnsIdenticalAmount()
    {
        // Arrange
        using var context = CreateDbContext(nameof(ConvertAmountAsync_SameCurrency_ReturnsIdenticalAmount));
        SeedCurrencies(context);

        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());

        // Act
        var result = await service.ConvertAmountAsync(500m, "USD", "USD");

        // Assert
        result.ConvertedAmount.Should().Be(500.00m);
    }

    [Fact]
    public async Task ConvertAmountAsync_EmptyOrInvalidCodes_ThrowsArgumentException()
    {
        using var context = CreateDbContext(nameof(ConvertAmountAsync_EmptyOrInvalidCodes_ThrowsArgumentException));
        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());

        await Assert.ThrowsAsync<ArgumentException>(() => service.ConvertAmountAsync(100m, "", "VND"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ConvertAmountAsync(100m, "USD", ""));
    }

    [Fact]
    public async Task ConvertAmountAsync_NonExistentCurrency_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext(nameof(ConvertAmountAsync_NonExistentCurrency_ThrowsInvalidOperationException));
        SeedCurrencies(context);

        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertAmountAsync(100m, "XYZ", "VND"));
    }

    [Fact]
    public async Task UpdateManualRateAsync_ValidRate_UpdatesEntityAndCreatesHistory()
    {
        // Arrange
        using var context = CreateDbContext(nameof(UpdateManualRateAsync_ValidRate_UpdatesEntityAndCreatesHistory));
        SeedCurrencies(context);

        var mockAudit = new Mock<IAuditService>();
        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), mockAudit.Object, Mock.Of<ILogger<CurrencyService>>());

        var usd = await context.Currencies.FirstAsync(c => c.Code == "USD");

        var request = new UpdateManualRateRequest
        {
            CurrencyId = usd.Id,
            CurrencyCode = "USD",
            NewRate = 25550m,
            EffectiveAt = DateTime.UtcNow,
            Notes = "Cập nhật theo Vietcombank",
            IsManualOverride = true
        };

        // Act
        var success = await service.UpdateManualRateAsync(request, "admin");

        // Assert
        success.Should().BeTrue();

        var updatedUsd = await context.Currencies.FirstAsync(c => c.Code == "USD");
        updatedUsd.ExchangeRate.Should().Be(25550m);
        updatedUsd.RateSource.Should().Be("Thủ công");
        updatedUsd.IsManualOverride.Should().BeTrue();
        updatedUsd.Notes.Should().Be("Cập nhật theo Vietcombank");

        var history = await context.ExchangeRateHistories.FirstOrDefaultAsync(h => h.CurrencyCode == "USD");
        history.Should().NotBeNull();
        history!.Rate.Should().Be(25550m);
        history.PreviousRate.Should().Be(25400m);
        history.Source.Should().Be("Thủ công");
        history.ChangedBy.Should().Be("admin");
    }

    [Fact]
    public async Task UpdateManualRateAsync_BaseCurrencyVnd_ThrowsInvalidOperationException()
    {
        // Arrange
        using var context = CreateDbContext(nameof(UpdateManualRateAsync_BaseCurrencyVnd_ThrowsInvalidOperationException));
        SeedCurrencies(context);

        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());
        var vnd = await context.Currencies.FirstAsync(c => c.Code == "VND");

        var request = new UpdateManualRateRequest
        {
            CurrencyId = vnd.Id,
            CurrencyCode = "VND",
            NewRate = 2m
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateManualRateAsync(request, "admin"));
    }

    [Fact]
    public async Task UpdateManualRateAsync_ZeroOrNegativeRate_ThrowsArgumentException()
    {
        using var context = CreateDbContext(nameof(UpdateManualRateAsync_ZeroOrNegativeRate_ThrowsArgumentException));
        SeedCurrencies(context);

        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());
        var usd = await context.Currencies.FirstAsync(c => c.Code == "USD");

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateManualRateAsync(new UpdateManualRateRequest { CurrencyId = usd.Id, NewRate = 0m }, "admin"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateManualRateAsync(new UpdateManualRateRequest { CurrencyId = usd.Id, NewRate = -5m }, "admin"));
    }

    [Fact]
    public async Task ToggleCurrencyStatusAsync_BaseCurrencyVnd_ThrowsInvalidOperationException()
    {
        using var context = CreateDbContext(nameof(ToggleCurrencyStatusAsync_BaseCurrencyVnd_ThrowsInvalidOperationException));
        SeedCurrencies(context);

        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());
        var vnd = await context.Currencies.FirstAsync(c => c.Code == "VND");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ToggleCurrencyStatusAsync(vnd.Id, "admin"));
    }

    [Fact]
    public async Task FetchLatestRatesPreviewAsync_ManualOverride_SetsSelectedToFalseByDefault()
    {
        // Arrange
        using var context = CreateDbContext(nameof(FetchLatestRatesPreviewAsync_ManualOverride_SetsSelectedToFalseByDefault));
        SeedCurrencies(context);

        var mockProvider = new Mock<IExchangeRateProvider>();
        mockProvider.Setup(p => p.ProviderName).Returns("ExchangeRate-API");
        mockProvider.Setup(p => p.FetchRatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRateFetchResult
            {
                Success = true,
                RatesToVnd = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                {
                    ["USD"] = 25600m,
                    ["EUR"] = 28500m,
                    ["GBP"] = 33000m
                }
            });

        var service = new CurrencyService(context, mockProvider.Object, Mock.Of<IAuditService>(), Mock.Of<ILogger<CurrencyService>>());

        // Act
        var preview = await service.FetchLatestRatesPreviewAsync();

        // Assert
        preview.Success.Should().BeTrue();
        preview.Items.Should().NotBeEmpty();

        // USD is active and not manually overridden -> selected by default
        var usdItem = preview.Items.First(x => x.CurrencyCode == "USD");
        usdItem.IsSelected.Should().BeTrue();
        usdItem.NewRate.Should().Be(25600m);

        // GBP has IsManualOverride = true -> NOT selected by default to protect manual rates!
        var gbpItem = preview.Items.First(x => x.CurrencyCode == "GBP");
        gbpItem.IsManualOverride.Should().BeTrue();
        gbpItem.IsSelected.Should().BeFalse();
        gbpItem.StatusMessage.Should().Contain("cố định thủ công");
    }

    [Fact]
    public async Task ApplyExchangeRatesAsync_AppliesRatesAndRecordsHistories()
    {
        // Arrange
        using var context = CreateDbContext(nameof(ApplyExchangeRatesAsync_AppliesRatesAndRecordsHistories));
        SeedCurrencies(context);

        var mockAudit = new Mock<IAuditService>();
        var service = new CurrencyService(context, Mock.Of<IExchangeRateProvider>(), mockAudit.Object, Mock.Of<ILogger<CurrencyService>>());

        var request = new ApplyExchangeRatesRequest
        {
            RatesToApply = new List<ApplyRateItemDto>
            {
                new ApplyRateItemDto { CurrencyCode = "USD", Rate = 25650m, KeepManualOverride = false },
                new ApplyRateItemDto { CurrencyCode = "EUR", Rate = 28600m, KeepManualOverride = false }
            },
            EffectiveAt = DateTime.UtcNow
        };

        // Act
        var result = await service.ApplyExchangeRatesAsync(request, "admin");

        // Assert
        result.Success.Should().BeTrue();
        result.UpdatedCount.Should().Be(2);

        var usd = await context.Currencies.FirstAsync(c => c.Code == "USD");
        usd.ExchangeRate.Should().Be(25650m);
        usd.RateSource.Should().Be("API ExchangeRate");

        var eur = await context.Currencies.FirstAsync(c => c.Code == "EUR");
        eur.ExchangeRate.Should().Be(28600m);

        var histories = await context.ExchangeRateHistories.ToListAsync();
        histories.Should().HaveCount(2);
        histories.Should().Contain(h => h.CurrencyCode == "USD" && h.Rate == 25650m);
    }
}
