using System.Collections.Concurrent;
using FluentAssertions;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.MasterData;

public class SystemCodeGeneratorTests
{
    [Theory]
    [InlineData("SP", 1, "SP000001")]
    [InlineData("SP", 42, "SP000042")]
    [InlineData("KH", 1, "KH000001")]
    [InlineData("NCC", 123, "NCC000123")]
    [InlineData("KHO", 9, "KHO000009")]
    [InlineData("DM", 15, "DM000015")]
    [InlineData("DV", 8, "DV000008")]
    [InlineData("SP", 1000000, "SP1000000")]
    public void FormatCode_StandardPrefixAndNumber_FormatsCorrectly(string prefix, long number, string expected)
    {
        var result = SystemCodeGenerator.FormatCode(prefix, number, "{Prefix}{Number:D6}");
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatCode_NullOrEmptyPattern_FallsBackToDefault6Digits()
    {
        var result1 = SystemCodeGenerator.FormatCode("SP", 5, "");
        var result2 = SystemCodeGenerator.FormatCode("KH", 5, null!);

        result1.Should().Be("SP000005");
        result2.Should().Be("KH000005");
    }

    [Fact]
    public void SystemCodeConstants_Defaults_ContainsAllSixMasterDataKeysWithExactPrefixes()
    {
        var defaults = SystemCodeConstants.Defaults;

        defaults.Should().ContainKey(SystemCodeConstants.Product);
        defaults[SystemCodeConstants.Product].Prefix.Should().Be("SP");

        defaults.Should().ContainKey(SystemCodeConstants.Customer);
        defaults[SystemCodeConstants.Customer].Prefix.Should().Be("KH");

        defaults.Should().ContainKey(SystemCodeConstants.Supplier);
        defaults[SystemCodeConstants.Supplier].Prefix.Should().Be("NCC");

        defaults.Should().ContainKey(SystemCodeConstants.Warehouse);
        defaults[SystemCodeConstants.Warehouse].Prefix.Should().Be("KHO");

        defaults.Should().ContainKey(SystemCodeConstants.ProductCategory);
        defaults[SystemCodeConstants.ProductCategory].Prefix.Should().Be("DM");

        defaults.Should().ContainKey(SystemCodeConstants.UnitOfMeasure);
        defaults[SystemCodeConstants.UnitOfMeasure].Prefix.Should().Be("DV");
    }

    [Fact]
    public void ProductEntity_HasDistinctSystemCode_NewCode_AndLegacyCode()
    {
        var product = new Product
        {
            Code = "SP000001",
            NewCode = "TL2138",
            LegacyCode = "OLD-999",
            Name = "Bơm chìm 3 pha 2.2kW",
            CategoryId = 1,
            UnitId = 1,
            IsActive = true
        };

        product.Code.Should().Be("SP000001");
        product.NewCode.Should().Be("TL2138");
        product.LegacyCode.Should().Be("OLD-999");
        product.Images.Should().NotBeNull();
        product.Images.Should().BeEmpty();
    }

    [Fact]
    public void CustomerAndSupplierAndWarehouse_HaveSeparateBusinessCode()
    {
        var customer = new Customer
        {
            Code = "KH000001",
            BusinessCode = "CUST-VIP-01",
            Name = "Công ty Cơ điện ABC"
        };

        var supplier = new Supplier
        {
            Code = "NCC000001",
            BusinessCode = "SUP-JAPAN-01",
            Name = "Tập đoàn Thiết bị Công nghiệp XYZ"
        };

        var warehouse = new Warehouse
        {
            Code = "KHO000001",
            BusinessCode = "WH-HN-01",
            Name = "Kho tổng Long Biên"
        };

        customer.Code.Should().NotBe(customer.BusinessCode);
        supplier.Code.Should().NotBe(supplier.BusinessCode);
        warehouse.Code.Should().NotBe(warehouse.BusinessCode);
    }

    [Fact]
    public void ProductImageEntity_StoresMetadataWithoutDatabaseBinaryBlobs()
    {
        var image = new ProductImage
        {
            ProductId = 10,
            FileName = "pump_profile.jpg",
            StorageReference = "uploads/products/abcd1234_pump_profile.jpg",
            ContentType = "image/jpeg",
            FileSizeBytes = 102400,
            IsPrimary = true,
            SortOrder = 1
        };

        image.FileName.Should().Be("pump_profile.jpg");
        image.StorageReference.Should().Contain("uploads/products");
        image.FileSizeBytes.Should().Be(102400);
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task ConcurrencySimulation_ParallelCodeFormatting_GeneratesUniqueCodes()
    {
        var generatedCodes = new ConcurrentBag<string>();
        long sequenceCounter = 0;
        var lockObj = new object();

        var tasks = Enumerable.Range(1, 100).Select(_ => Task.Run(() =>
        {
            long current;
            lock (lockObj)
            {
                sequenceCounter++;
                current = sequenceCounter;
            }
            var code = SystemCodeGenerator.FormatCode("SP", current, "{Prefix}{Number:D6}");
            generatedCodes.Add(code);
        }));

        await Task.WhenAll(tasks);

        generatedCodes.Should().HaveCount(100);
        generatedCodes.Distinct().Should().HaveCount(100);
        generatedCodes.Should().Contain("SP000001");
        generatedCodes.Should().Contain("SP000100");
    }
}