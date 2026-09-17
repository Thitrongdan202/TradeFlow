using System.Xml.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Pricing;

public class PriceListVatAndComparisonTests
{
    private TradeFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradeFlowDbContext(options);

        // Seed CompanySettings
        var company = new CompanySettings("LACASA")
        {
            TaxCode = "0319205643",
            Address = "13 Nguyễn Văn Mai, Phường Xuân Hòa, TP Hồ Chí Minh",
            Phone = "0901234567",
            Email = "contact@lacasa.vn",
            BankAccount = "123456789",
            BankName = "Vietcombank - CN Tân Bình",
            Website = "https://lacasa.vn"
        };
        context.CompanySettings.Add(company);
        context.SaveChanges();

        return context;
    }

    private ExcelPricingService CreateExcelPricingService(TradeFlowDbContext context, IVatRuleEngine? vatRuleEngine = null)
    {
        var mockFileStorage = new Mock<IFileStorageService>();
        var mockCodeGenerator = new Mock<ISystemCodeGenerator>();
        mockCodeGenerator.Setup(c => c.GenerateCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string prefix, CancellationToken _) => $"{prefix}{DateTime.UtcNow.Ticks % 1000000:D6}");

        var mockAudit = new Mock<IAuditService>();
        var engine = vatRuleEngine ?? new VatRuleEngine();
        var logger = NullLogger<ExcelPricingService>.Instance;

        return new ExcelPricingService(context, mockFileStorage.Object, mockCodeGenerator.Object, mockAudit.Object, engine, logger);
    }

    private PriceListService CreatePriceListService(TradeFlowDbContext context)
    {
        var mockCodeGenerator = new Mock<ISystemCodeGenerator>();
        mockCodeGenerator.Setup(c => c.GenerateCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string prefix, CancellationToken _) => $"BG{DateTime.UtcNow.Ticks % 1000000:D6}");

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(u => u.UserName).Returns("admin");

        var mockAudit = new Mock<IAuditService>();
        var mockFileStorage = new Mock<IFileStorageService>();

        return new PriceListService(context, mockCodeGenerator.Object, mockCurrentUser.Object, mockAudit.Object, mockFileStorage.Object);
    }

    // =========================================================================
    // 1. Import existing PriceList
    // =========================================================================
    [Fact]
    public async Task Scenario01_ImportPriceList_CreatesPriceListAndItemsSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var excelService = CreateExcelPricingService(context);

        var dto = new ExcelImportCommitRequest
        {
            Name = "Bảng giá Thiết bị vệ sinh Q2/2026",
            Year = 2026,
            Quarter = 2,
            QuotationDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc),
            AutoCreateProducts = true,
            Items = new List<ExcelParsedItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    Group = "Bàn cầu",
                    NewCode = "TL2138",
                    LegacyCode = "BC-01",
                    ProductInfo = "Bồn cầu 1 khối cao cấp LACASA",
                    UnitPrice = 1150000,
                    VatRate = 8,
                    MatchStatus = PriceMatchStatus.NewProduct
                },
                new()
                {
                    SortOrder = 2,
                    Group = "Lavabo",
                    NewCode = "LV5010",
                    LegacyCode = "LV-01",
                    ProductInfo = "Chậu rửa đặt bàn oval",
                    UnitPrice = 650000,
                    VatRate = 8,
                    MatchStatus = PriceMatchStatus.NewProduct
                }
            }
        };

        var plDto = await excelService.CommitImportAsync(dto, "admin");

        plDto.Should().NotBeNull();
        plDto.Id.Should().BeGreaterThan(0);
        var pl = await context.PriceLists.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == plDto.Id);
        pl.Should().NotBeNull();
        pl!.Name.Should().Be("Bảng giá Thiết bị vệ sinh Q2/2026");
        pl.Items.Should().HaveCount(2);
        pl.Items.Should().Contain(i => i.NewCode == "TL2138" && i.UnitPrice == 1150000 && i.VatRate == 8);
        pl.Items.Should().Contain(i => i.NewCode == "LV5010" && i.UnitPrice == 650000 && i.VatRate == 8);
    }

    // =========================================================================
    // 2. Existing product -> "Đã có sản phẩm" (Matched)
    // =========================================================================
    [Fact]
    public async Task Scenario02_ExistingProduct_MatchesAsDaCoSanPham()
    {
        using var context = CreateInMemoryDbContext();
        var existingProd = new Product
        {
            Code = "SP000001",
            NewCode = "TL2138",
            LegacyCode = "BC-01",
            Name = "Bồn cầu 1 khối cao cấp LACASA",
            TaxTreatment = TaxTreatment.Standard10,
            IsTaxReductionEligible = true,
            IsActive = true
        };
        context.Products.Add(existingProd);
        await context.SaveChangesAsync();

        var excelService = CreateExcelPricingService(context);

        var dto = new ExcelImportCommitRequest
        {
            Name = "Bảng giá Q3/2026",
            Year = 2026,
            Quarter = 3,
            Items = new List<ExcelParsedItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    NewCode = "TL2138",
                    LegacyCode = "BC-01",
                    ProductInfo = "Bồn cầu 1 khối cao cấp LACASA",
                    UnitPrice = 1190000,
                    MatchedProductId = existingProd.Id,
                    MatchStatus = PriceMatchStatus.Matched,
                    HasChanges = false
                }
            }
        };

        var plDto = await excelService.CommitImportAsync(dto, "admin");
        var savedItem = await context.PriceListItems.FirstOrDefaultAsync(i => i.PriceListId == plDto.Id);

        savedItem.Should().NotBeNull();
        savedItem!.MatchStatus.Should().Be(PriceMatchStatus.Matched);
        savedItem.ProductId.Should().Be(existingProd.Id);
        savedItem.UnitPrice.Should().Be(1190000);
    }

    // =========================================================================
    // 3. New product -> "Thêm sản phẩm mới" (NewProduct)
    // =========================================================================
    [Fact]
    public async Task Scenario03_NewProduct_MatchesAsThemSanPhamMoi()
    {
        using var context = CreateInMemoryDbContext();
        var excelService = CreateExcelPricingService(context);

        var dto = new ExcelImportCommitRequest
        {
            Name = "Bảng giá Sản phẩm Mới 2026",
            Year = 2026,
            Quarter = 3,
            AutoCreateProducts = true,
            Items = new List<ExcelParsedItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    Group = "Sen vòi",
                    NewCode = "SV9999",
                    ProductInfo = "Sen cây tắm nhiệt độ cao cấp",
                    UnitPrice = 2400000,
                    MatchStatus = PriceMatchStatus.NewProduct,
                    MatchedProductId = null
                }
            }
        };

        var plDto = await excelService.CommitImportAsync(dto, "admin");

        // Verify product was created in database
        var createdProduct = await context.Products.FirstOrDefaultAsync(p => p.NewCode == "SV9999");
        createdProduct.Should().NotBeNull();
        createdProduct!.Name.Should().Be("Sen cây tắm nhiệt độ cao cấp");

        // Verify PriceListItem is linked to the newly created product
        var item = await context.PriceListItems.FirstOrDefaultAsync(i => i.PriceListId == plDto.Id);
        item.Should().NotBeNull();
        item!.ProductId.Should().Be(createdProduct.Id);
        item.MatchStatus.Should().Be(PriceMatchStatus.NewProduct);
    }

    // =========================================================================
    // 4. Changed product -> "Thay đổi sản phẩm" (ReviewRequired & Diff)
    // =========================================================================
    [Fact]
    public async Task Scenario04_ChangedProduct_DetectsChanges_PreservesMasterDataUnlessApproved()
    {
        using var context = CreateInMemoryDbContext();
        var prod = new Product
        {
            Code = "SP000001",
            NewCode = "TL2138",
            LegacyCode = "OLD-01",
            Name = "Bồn cầu 1 khối nguyên bản",
            Description = "Mô tả cũ",
            IsActive = true
        };
        context.Products.Add(prod);
        await context.SaveChangesAsync();

        var excelService = CreateExcelPricingService(context);

        // Case A: Quản trị viên chọn [Giữ nguyên] (ApplyMasterDataUpdate = false)
        var dtoKeep = new ExcelImportCommitRequest
        {
            Name = "Bảng giá Q2/2026",
            Year = 2026,
            Quarter = 2,
            Items = new List<ExcelParsedItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    NewCode = "TL2138",
                    LegacyCode = "NEW-CODE-99",
                    ProductInfo = "Bồn cầu 1 khối nắp êm phiên bản mới",
                    UnitPrice = 1200000,
                    MatchedProductId = prod.Id,
                    MatchStatus = PriceMatchStatus.ReviewRequired,
                    HasChanges = true,
                    ApplyMasterDataUpdate = false // Giữ nguyên danh mục gốc!
                }
            }
        };

        await excelService.CommitImportAsync(dtoKeep, "admin");

        // Master Data must NOT be overwritten!
        var prodAfterKeep = await context.Products.FindAsync(prod.Id);
        prodAfterKeep!.Name.Should().Be("Bồn cầu 1 khối nguyên bản");
        prodAfterKeep.LegacyCode.Should().Be("OLD-01");

        // Case B: Quản trị viên phê duyệt [Cập nhật sản phẩm] (ApplyMasterDataUpdate = true)
        var dtoUpdate = new ExcelImportCommitRequest
        {
            Name = "Bảng giá Q3/2026",
            Year = 2026,
            Quarter = 3,
            Items = new List<ExcelParsedItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    NewCode = "TL2138",
                    LegacyCode = "NEW-CODE-99",
                    ProductInfo = "Bồn cầu 1 khối nắp êm phiên bản mới",
                    UnitPrice = 1250000,
                    MatchedProductId = prod.Id,
                    MatchStatus = PriceMatchStatus.ReviewRequired,
                    HasChanges = true,
                    ApplyMasterDataUpdate = true // Quản trị viên đồng ý cập nhật
                }
            }
        };

        await excelService.CommitImportAsync(dtoUpdate, "admin");

        // Master Data is now updated because user explicitly approved it
        var prodAfterUpdate = await context.Products.FindAsync(prod.Id);
        prodAfterUpdate!.Name.Should().Be("Bồn cầu 1 khối nắp êm phiên bản mới");
        prodAfterUpdate.LegacyCode.Should().Be("NEW-CODE-99");
    }

    // =========================================================================
    // 5. Verify Price History across multiple imports
    // =========================================================================
    [Fact]
    public async Task Scenario05_VerifyPriceHistory_PreservedAcrossMultipleImports()
    {
        using var context = CreateInMemoryDbContext();
        var plService = CreatePriceListService(context);

        var prod = new Product { Code = "SP000001", NewCode = "TL2138", Name = "Bồn cầu 1 khối", IsActive = true };
        context.Products.Add(prod);

        var plQ1 = new PriceList { Name = "Bảng giá Q1/2026", Year = 2026, Quarter = 1, Status = PriceListStatus.Expired, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var plQ2 = new PriceList { Name = "Bảng giá Q2/2026", Year = 2026, Quarter = 2, Status = PriceListStatus.Expired, EffectiveFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) };
        var plQ3 = new PriceList { Name = "Bảng giá Q3/2026", Year = 2026, Quarter = 3, Status = PriceListStatus.Active, EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) };
        context.PriceLists.AddRange(plQ1, plQ2, plQ3);
        await context.SaveChangesAsync();

        context.PriceListItems.AddRange(
            new PriceListItem { PriceListId = plQ1.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1100000, VatRate = 8, CreatedAt = plQ1.EffectiveFrom.Value },
            new PriceListItem { PriceListId = plQ2.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1150000, VatRate = 8, CreatedAt = plQ2.EffectiveFrom.Value },
            new PriceListItem { PriceListId = plQ3.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1200000, VatRate = 8, CreatedAt = plQ3.EffectiveFrom.Value }
        );
        await context.SaveChangesAsync();

        var history = await plService.GetProductPriceHistoryAsync("TL2138");

        history.Should().HaveCount(3);
        history.Select(h => h.UnitPrice).Should().ContainInOrder(1200000, 1150000, 1100000);
    }

    // =========================================================================
    // 6. Verify "Giá hiện tại" resolved from active PriceList
    // =========================================================================
    [Fact]
    public async Task Scenario06_VerifyCurrentPrice_ResolvedFromActiveEffectivePriceList()
    {
        using var context = CreateInMemoryDbContext();
        var plService = CreatePriceListService(context);

        var prod = new Product { Code = "SP000001", NewCode = "TL2138", Name = "Bồn cầu 1 khối", IsActive = true };
        context.Products.Add(prod);

        // Bảng giá cũ (đã hết hạn)
        var plOld = new PriceList
        {
            Name = "Bảng giá Q1/2026 (Hết hạn)",
            Status = PriceListStatus.Expired,
            EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc)
        };
        // Bảng giá đang hiệu lực (Active)
        var plActive = new PriceList
        {
            Name = "Bảng giá Q2/2026 (Đang áp dụng)",
            Status = PriceListStatus.Active,
            EffectiveFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc)
        };
        // Bảng giá tương lai (Draft)
        var plDraft = new PriceList
        {
            Name = "Bảng giá Q3/2026 (Dự thảo)",
            Status = PriceListStatus.Draft,
            EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        context.PriceLists.AddRange(plOld, plActive, plDraft);
        await context.SaveChangesAsync();

        context.PriceListItems.AddRange(
            new PriceListItem { PriceListId = plOld.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1000000 },
            new PriceListItem { PriceListId = plActive.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1250000 },
            new PriceListItem { PriceListId = plDraft.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1500000 }
        );
        await context.SaveChangesAsync();

        // Xét giá tại ngày 2026-05-15
        var asOfDate = new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc);
        var currentPrice = await plService.GetProductCurrentPriceAsync(prod.Id, asOfDate);

        // Giá hiện tại phải là từ bảng giá Active (1.250.000), không phải Draft (1.500.000) hay Expired (1.000.000)
        currentPrice.Should().Be(1250000);
    }

    // =========================================================================
    // 7. Verify dynamic multi-period comparison from database
    // =========================================================================
    [Fact]
    public async Task Scenario07_VerifyDynamicMultiPeriodComparison_FromDatabase()
    {
        using var context = CreateInMemoryDbContext();
        var plService = CreatePriceListService(context);

        var p1 = new Product { Code = "SP01", NewCode = "TL2138", Name = "Bồn cầu 1 khối", IsActive = true };
        var p2 = new Product { Code = "SP02", NewCode = "LV5010", Name = "Lavabo oval", IsActive = true };
        context.Products.AddRange(p1, p2);

        var plQ1 = new PriceList { Name = "Bảng giá Q1/2026", Year = 2026, Quarter = 1, Status = PriceListStatus.Active, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var plQ2 = new PriceList { Name = "Bảng giá Q2/2026", Year = 2026, Quarter = 2, Status = PriceListStatus.Active, EffectiveFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) };
        var plQ3 = new PriceList { Name = "Bảng giá Q3/2026", Year = 2026, Quarter = 3, Status = PriceListStatus.Active, EffectiveFrom = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) };
        context.PriceLists.AddRange(plQ1, plQ2, plQ3);
        await context.SaveChangesAsync();

        context.PriceListItems.AddRange(
            new PriceListItem { PriceListId = plQ1.Id, ProductId = p1.Id, NewCode = "TL2138", UnitPrice = 1100000 },
            new PriceListItem { PriceListId = plQ2.Id, ProductId = p1.Id, NewCode = "TL2138", UnitPrice = 1150000 },
            new PriceListItem { PriceListId = plQ3.Id, ProductId = p1.Id, NewCode = "TL2138", UnitPrice = 1200000 },
            new PriceListItem { PriceListId = plQ1.Id, ProductId = p2.Id, NewCode = "LV5010", UnitPrice = 600000 },
            new PriceListItem { PriceListId = plQ2.Id, ProductId = p2.Id, NewCode = "LV5010", UnitPrice = 620000 }
        );
        await context.SaveChangesAsync();

        var multiComp = await plService.CompareMultiplePriceListsAsync();

        multiComp.Should().NotBeNull();
        multiComp.Periods.Should().HaveCount(3);
        multiComp.Items.Should().HaveCount(2);

        var rowTL = multiComp.Items.First(x => x.NewCode == "TL2138");
        rowTL.PeriodPrices[plQ1.Id].Should().Be(1100000);
        rowTL.PeriodPrices[plQ2.Id].Should().Be(1150000);
        rowTL.PeriodPrices[plQ3.Id].Should().Be(1200000);
    }

    // =========================================================================
    // 8. Verify all comparison prices use the same pre-VAT basis
    // =========================================================================
    [Fact]
    public async Task Scenario08_VerifyAllComparisonPrices_UsePreVatBasis()
    {
        using var context = CreateInMemoryDbContext();
        var plService = CreatePriceListService(context);

        var prod = new Product { Code = "SP01", NewCode = "TL2138", Name = "Bồn cầu 1 khối", IsActive = true };
        context.Products.Add(prod);

        var plBase = new PriceList { Name = "Bảng giá Cũ", Year = 2026, Status = PriceListStatus.Active, EffectiveFrom = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) };
        var plTarget = new PriceList { Name = "Bảng giá Mới", Year = 2026, Status = PriceListStatus.Active, EffectiveFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc) };
        context.PriceLists.AddRange(plBase, plTarget);
        await context.SaveChangesAsync();

        context.PriceListItems.AddRange(
            new PriceListItem { PriceListId = plBase.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1000000, VatRate = 8 },
            new PriceListItem { PriceListId = plTarget.Id, ProductId = prod.Id, NewCode = "TL2138", UnitPrice = 1150000, VatRate = 8 }
        );
        await context.SaveChangesAsync();

        var compResult = await plService.ComparePriceListsAsync(plBase.Id, plTarget.Id);

        compResult.Should().NotBeNull();
        var item = compResult.Items.First();
        item.BasePrice.Should().Be(1000000);
        item.TargetPrice.Should().Be(1150000);
        item.PriceDifference.Should().Be(150000);

        var multiComp = await plService.CompareMultiplePriceListsAsync(new List<int> { plBase.Id, plTarget.Id });
        multiComp.PriceBasis.Should().Be("Chưa VAT");
    }

    // =========================================================================
    // 9. Create GTGT invoice
    // =========================================================================
    [Fact]
    public async Task Scenario09_CreateGtgtInvoice_ValidStructureAndIdentifiers()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("admin");
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        var dto = new InvoiceDto
        {
            Type = InvoiceType.VatInvoice,
            InvoiceDate = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc),
            CustomerName = "Công ty Cổ phần Thương mại Alpha",
            CustomerTaxCode = "0101234567",
            CustomerAddress = "123 Đường Láng, Đống Đa, Hà Nội",
            SubTotal = 5000000,
            TotalTax = 400000,
            GrandTotal = 5400000,
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    ProductCode = "TL2138",
                    ProductName = "Bồn cầu 1 khối",
                    UnitName = "Bộ",
                    Quantity = 2,
                    UnitPrice = 2500000,
                    TaxRate = 8,
                    TaxAmount = 400000,
                    LineTotal = 5000000
                }
            }
        };

        var created = await invoiceService.CreateManualInvoiceAsync(dto);

        created.Should().NotBeNull();
        created.Type.Should().Be(InvoiceType.VatInvoice);
        created.FormNumber.Should().Be("1"); // 01GTKT
        created.InvoiceSeries.Should().StartWith("1C");
        created.InvoiceNo.Should().MatchRegex(@"^\d{8}$"); // 8 digits e.g. 00000001
        created.GrandTotal.Should().Be(5400000);
    }

    // =========================================================================
    // 10. Verify VAT calculation formula
    // =========================================================================
    [Theory]
    [InlineData(10000000, 1000000, 8, 720000, 9720000)]
    [InlineData(5000000, 0, 10, 500000, 5500000)]
    [InlineData(2000000, 200000, 5, 90000, 1890000)]
    [InlineData(3000000, 0, 0, 0, 3000000)]
    public void Scenario10_VerifyVatCalculationFormula(
        decimal subTotal, decimal discount, decimal taxRate, decimal expectedTax, decimal expectedGrandTotal)
    {
        var taxableAmount = Math.Max(0, subTotal - discount);
        var calculatedTax = Math.Round(taxableAmount * (taxRate / 100m), 0);
        var grandTotal = taxableAmount + calculatedTax;

        calculatedTax.Should().Be(expectedTax);
        grandTotal.Should().Be(expectedGrandTotal);
    }

    // =========================================================================
    // 11. Test 8% eligible case in 2026
    // =========================================================================
    [Fact]
    public void Scenario11_Test8PercentEligibleCase_In2026()
    {
        var engine = new VatRuleEngine();
        var product = new Product
        {
            Code = "SP01",
            Name = "Bồn cầu 1 khối",
            TaxTreatment = TaxTreatment.Standard10,
            IsTaxReductionEligible = true
        };

        var invoiceDate2026 = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var result = engine.DetermineVatRate(product, invoiceDate2026);

        result.Rate.Should().Be(8m);
        result.IsReduced.Should().BeTrue();
        result.DisplayText.Should().Be("8%");
    }

    // =========================================================================
    // 12. Test 10% case (non-eligible or post-2026)
    // =========================================================================
    [Fact]
    public void Scenario12_Test10PercentStandardCase_NonEligibleOrPost2026()
    {
        var engine = new VatRuleEngine();

        // Case A: Trong năm 2026 nhưng sản phẩm không đủ điều kiện giảm thuế
        var productNonEligible = new Product
        {
            Code = "SP02",
            Name = "Mặt hàng viễn thông / tài chính không được giảm thuế",
            TaxTreatment = TaxTreatment.Standard10,
            IsTaxReductionEligible = false
        };
        var dateIn2026 = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var resNonEligible = engine.DetermineVatRate(productNonEligible, dateIn2026);

        resNonEligible.Rate.Should().Be(10m);
        resNonEligible.IsReduced.Should().BeFalse();

        // Case B: Sản phẩm đủ điều kiện nhưng ngày hóa đơn sau 31/12/2026 (sang năm 2027)
        var productEligible = new Product
        {
            Code = "SP01",
            Name = "Bồn cầu 1 khối",
            TaxTreatment = TaxTreatment.Standard10,
            IsTaxReductionEligible = true
        };
        var dateIn2027 = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var resPost2026 = engine.DetermineVatRate(productEligible, dateIn2027);

        // Phải tự động quay về 10% mà không cần sửa code!
        resPost2026.Rate.Should().Be(10m);
        resPost2026.IsReduced.Should().BeFalse();
    }

    // =========================================================================
    // 13. Test 5% case
    // =========================================================================
    [Fact]
    public void Scenario13_Test5PercentCase()
    {
        var engine = new VatRuleEngine();
        var product = new Product
        {
            Code = "SP03",
            Name = "Nước sạch y tế / thuốc",
            TaxTreatment = TaxTreatment.Rate5
        };

        var result = engine.DetermineVatRate(product, DateTime.UtcNow);

        result.Rate.Should().Be(5m);
        result.DisplayText.Should().Be("5%");
        result.IsNonTaxable.Should().BeFalse();
    }

    // =========================================================================
    // 14. Test 0% case
    // =========================================================================
    [Fact]
    public void Scenario14_Test0PercentCase()
    {
        var engine = new VatRuleEngine();
        var product = new Product
        {
            Code = "SP04",
            Name = "Hàng xuất khẩu",
            TaxTreatment = TaxTreatment.ZeroRate
        };

        var result = engine.DetermineVatRate(product, DateTime.UtcNow);

        result.Rate.Should().Be(0m);
        result.DisplayText.Should().Be("0%");
        result.IsNonTaxable.Should().BeFalse();
    }

    // =========================================================================
    // 15. Test non-taxable case
    // =========================================================================
    [Fact]
    public void Scenario15_TestNonTaxableCase()
    {
        var engine = new VatRuleEngine();
        var product = new Product
        {
            Code = "SP05",
            Name = "Mặt hàng giống cây trồng / nông sản chưa chế biến",
            TaxTreatment = TaxTreatment.NonTaxable
        };

        var result = engine.DetermineVatRate(product, DateTime.UtcNow);

        result.Rate.Should().Be(0m);
        result.IsNonTaxable.Should().BeTrue();
        result.DisplayText.Should().Be("Không chịu thuế");
    }

    // =========================================================================
    // 16. Test invoice containing different tax rates (multi-rate grouping)
    // =========================================================================
    [Fact]
    public async Task Scenario16_TestInvoiceWithMultipleTaxRates_Breakdown()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("admin");
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        var dto = new InvoiceDto
        {
            Type = InvoiceType.VatInvoice,
            InvoiceDate = new DateTime(2026, 9, 17, 0, 0, 0, DateTimeKind.Utc),
            CustomerName = "Công ty TNHH Đa Ngành",
            CustomerTaxCode = "0109876543",
            CustomerAddress = "456 Lê Duẩn, Hà Nội",
            SubTotal = 3000000,
            TotalTax = 280000, // 80.000 + 200.000
            GrandTotal = 3280000,
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    ProductCode = "SP8",
                    ProductName = "Hàng hóa thuế 8%",
                    UnitName = "Cái",
                    Quantity = 1,
                    UnitPrice = 1000000,
                    TaxRate = 8,
                    TaxAmount = 80000,
                    LineTotal = 1000000
                },
                new()
                {
                    SortOrder = 2,
                    ProductCode = "SP10",
                    ProductName = "Hàng hóa thuế 10%",
                    UnitName = "Cái",
                    Quantity = 1,
                    UnitPrice = 2000000,
                    TaxRate = 10,
                    TaxAmount = 200000,
                    LineTotal = 2000000
                }
            }
        };

        var created = await invoiceService.CreateManualInvoiceAsync(dto);
        var xmlString = await invoiceService.GenerateXmlAsync(created.Id);

        var doc = XDocument.Parse(xmlString);
        var thttltsuatElements = doc.Descendants().Where(x => x.Name.LocalName == "LTSuat").ToList();

        // Must contain groups for both tax rates
        thttltsuatElements.Should().NotBeEmpty();
        thttltsuatElements.Select(x => x.Element(x.Name.Namespace + "TSuat")?.Value).Should().Contain("8%");
        thttltsuatElements.Select(x => x.Element(x.Name.Namespace + "TSuat")?.Value).Should().Contain("10%");
    }

    // =========================================================================
    // 17. Verify finalized invoice keeps its historical tax rate
    // =========================================================================
    [Fact]
    public async Task Scenario17_VerifyFinalizedInvoice_KeepsHistoricalTaxRate()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("admin");
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        // Tạo hóa đơn năm 2026 với thuế 8%
        var dto = new InvoiceDto
        {
            Type = InvoiceType.VatInvoice,
            InvoiceDate = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc),
            CustomerName = "Công ty Khách Hàng Lịch Sử",
            CustomerTaxCode = "0105556667",
            SubTotal = 10000000,
            TotalTax = 800000,
            GrandTotal = 10800000,
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    ProductCode = "TL2138",
                    ProductName = "Bồn cầu 1 khối LACASA",
                    UnitName = "Bộ",
                    Quantity = 4,
                    UnitPrice = 2500000,
                    TaxRate = 8,
                    TaxAmount = 800000,
                    LineTotal = 10000000
                }
            }
        };

        var created = await invoiceService.CreateManualInvoiceAsync(dto);
        var success = await invoiceService.IssueInvoiceAsync(created.Id);
        success.Should().BeTrue();

        // Giả sử sau này thời gian chuyển sang năm 2027 (thuế quay lại 10%)
        var savedInvoice = await context.Invoices.Include(i => i.Items).FirstOrDefaultAsync(i => i.Id == created.Id);

        savedInvoice.Should().NotBeNull();
        savedInvoice!.Items.First().TaxRate.Should().Be(8m);
        savedInvoice.Items.First().TaxAmount.Should().Be(800000m);
        savedInvoice.TotalTax.Should().Be(800000m);
        savedInvoice.GrandTotal.Should().Be(10800000m);
    }
}
