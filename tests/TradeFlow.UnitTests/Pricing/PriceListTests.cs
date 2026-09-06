using FluentAssertions;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Domain.Enums;
using Xunit;

namespace TradeFlow.UnitTests.Pricing;

public class PriceListTests
{
    [Fact]
    public void SystemCodeConstants_ContainsPriceList_WithPrefixBG()
    {
        SystemCodeConstants.Defaults.Should().ContainKey(SystemCodeConstants.PriceList);
        var (prefix, desc) = SystemCodeConstants.Defaults[SystemCodeConstants.PriceList];
        prefix.Should().Be("BG");
        desc.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PriceList_Entity_DefaultsAreCorrect()
    {
        var priceList = new PriceList();
        priceList.Status.Should().Be(PriceListStatus.Draft);
        priceList.Items.Should().NotBeNull().And.BeEmpty();
        priceList.Year.Should().Be(DateTime.UtcNow.Year);
    }

    [Fact]
    public void PriceListItem_Entity_DefaultsAreCorrect()
    {
        var item = new PriceListItem();
        item.CurrencyCode.Should().Be("VND");
        item.MatchStatus.Should().Be(PriceMatchStatus.Matched);
    }

    [Theory]
    [InlineData(100000, 120000, 20000, 20.0)]
    [InlineData(200000, 150000, -50000, -25.0)]
    [InlineData(150000, 150000, 0, 0.0)]
    [InlineData(1000000, 1150000, 150000, 15.0)]
    [InlineData(2500000, 2000000, -500000, -20.0)]
    public void PriceComparisonItemDto_DifferenceAndPercentage_CalculateCorrectly(
        decimal basePrice, decimal targetPrice, decimal expectedDiff, decimal expectedPct)
    {
        var item = new PriceComparisonItemDto
        {
            BasePrice = basePrice,
            TargetPrice = targetPrice
        };

        item.PriceDifference.Should().Be(expectedDiff);
        item.PercentageDifference.Should().Be(expectedPct);
    }

    [Fact]
    public void PriceComparisonItemDto_WhenBasePriceIsNull_DifferenceIsNull()
    {
        var item = new PriceComparisonItemDto
        {
            BasePrice = null,
            TargetPrice = 150000,
            ChangeStatus = PriceChangeStatus.NewProduct
        };

        item.PriceDifference.Should().BeNull();
        item.PercentageDifference.Should().BeNull();
        item.ChangeStatus.Should().Be(PriceChangeStatus.NewProduct);
    }

    [Fact]
    public void PriceComparisonItemDto_WhenTargetPriceIsNull_DifferenceIsNull()
    {
        var item = new PriceComparisonItemDto
        {
            BasePrice = 150000,
            TargetPrice = null,
            ChangeStatus = PriceChangeStatus.Discontinued
        };

        item.PriceDifference.Should().BeNull();
        item.PercentageDifference.Should().BeNull();
        item.ChangeStatus.Should().Be(PriceChangeStatus.Discontinued);
    }

    [Fact]
    public void PriceListStatus_Enum_ContainsExpectedValues()
    {
        Enum.IsDefined(typeof(PriceListStatus), PriceListStatus.Draft).Should().BeTrue();
        Enum.IsDefined(typeof(PriceListStatus), PriceListStatus.Active).Should().BeTrue();
        Enum.IsDefined(typeof(PriceListStatus), PriceListStatus.Expired).Should().BeTrue();
        Enum.IsDefined(typeof(PriceListStatus), PriceListStatus.Cancelled).Should().BeTrue();
    }

    [Fact]
    public void PriceMatchStatus_Enum_ContainsExpectedValues()
    {
        Enum.IsDefined(typeof(PriceMatchStatus), PriceMatchStatus.Matched).Should().BeTrue();
        Enum.IsDefined(typeof(PriceMatchStatus), PriceMatchStatus.ReviewRequired).Should().BeTrue();
        Enum.IsDefined(typeof(PriceMatchStatus), PriceMatchStatus.NewProduct).Should().BeTrue();
    }

    [Fact]
    public void PriceChangeStatus_Enum_ContainsExpectedValues()
    {
        Enum.IsDefined(typeof(PriceChangeStatus), PriceChangeStatus.Increased).Should().BeTrue();
        Enum.IsDefined(typeof(PriceChangeStatus), PriceChangeStatus.Decreased).Should().BeTrue();
        Enum.IsDefined(typeof(PriceChangeStatus), PriceChangeStatus.Unchanged).Should().BeTrue();
        Enum.IsDefined(typeof(PriceChangeStatus), PriceChangeStatus.NewProduct).Should().BeTrue();
        Enum.IsDefined(typeof(PriceChangeStatus), PriceChangeStatus.Discontinued).Should().BeTrue();
    }

    [Fact]
    public void AuditEventType_ContainsPricingEventTypes()
    {
        ((int)AuditEventType.PriceListCreated).Should().Be(90);
        ((int)AuditEventType.PriceListUpdated).Should().Be(91);
        ((int)AuditEventType.PriceListDeleted).Should().Be(92);
        ((int)AuditEventType.PriceListApproved).Should().Be(93);
        ((int)AuditEventType.PriceListImported).Should().Be(94);
        ((int)AuditEventType.PriceListExported).Should().Be(95);
        ((int)AuditEventType.PriceListOriginalDownloaded).Should().Be(96);
    }
}