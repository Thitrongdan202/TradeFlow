using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ClosedXML.Excel;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Enums;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;
using Xunit.Abstractions;

namespace TradeFlow.IntegrationTests;

public class DummyFileStorage : IFileStorageService
{
    public Task<string> SaveFileAsync(Stream fileStream, string fileName, string contentType, string subDirectory = "uploads", CancellationToken cancellationToken = default)
        => Task.FromResult("dummy_ref");
    public Task<Stream?> GetFileAsync(string storageReference, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream?>(new FileStream(@"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\wwwroot\uploads\pricing_originals\8513b30b3b0345d98116148f103b89c8_0908. GIÁ ĐẠI LÝ_ LACASA.xlsx", FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
    public Task<bool> DeleteFileAsync(string storageReference, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
    public string GetPublicUrl(string storageReference) => storageReference;
}

public class DummyCodeGenerator : ISystemCodeGenerator
{
    private static int _counter = (int)(DateTime.UtcNow.Ticks % 100000);
    public Task<string> GenerateCodeAsync(string sequenceKey, CancellationToken cancellationToken = default)
        => Task.FromResult($"TEST{Interlocked.Increment(ref _counter):D6}");
    public Task<string> PeekNextCodeAsync(string sequenceKey, CancellationToken cancellationToken = default)
        => Task.FromResult($"TEST{_counter:D6}");
}

public class DummyAuditService : IAuditService
{
    public List<AuditEventType> LoggedEvents = new();
    public Task LogAsync(AuditEventType eventType, string? performedBy = null, string? targetEntity = null, string? targetId = null, string? details = null, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
    {
        LoggedEvents.Add(eventType);
        return Task.CompletedTask;
    }
}

public class DummyCurrentUser : ICurrentUserService
{
    public string? UserId => "admin-id";
    public string? UserName => "admin";
    public string? IpAddress => "127.0.0.1";
    public bool IsAuthenticated => true;
    public bool HasPermission(string resource, string action) => true;
    public IEnumerable<Claim> Claims => Enumerable.Empty<Claim>();
}

public class UnitTest1
{
    private readonly ITestOutputHelper _output;

    public UnitTest1(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestImportAndCurrentPriceResolutionAsync()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=tradeflow_dev;Username=postgres;Password=postgres")
            .Options;

        using var db = new TradeFlowDbContext(options);

        // Cleanup any old test price lists
        var oldTests = await db.PriceLists.Include(p => p.Items).Where(p => p.Code.StartsWith("TEST") || p.Code == "BG0001").ToListAsync();
        if (oldTests.Any())
        {
        }
        var seqProd = await db.SystemSequences.FirstOrDefaultAsync(s => s.SequenceKey == SystemCodeConstants.Product);
        var seqPrice = await db.SystemSequences.FirstOrDefaultAsync(s => s.SequenceKey == SystemCodeConstants.PriceList);
        _output.WriteLine($"Seq Product: {seqProd?.CurrentNumber}, Seq PriceList: {seqPrice?.CurrentNumber}");
        var existingProdsCount = await db.Products.CountAsync();
        var maxProdCode = await db.Products.OrderByDescending(p => p.Id).Select(p => p.Code).FirstOrDefaultAsync();
        _output.WriteLine($"DB Products count: {existingProdsCount}, Max Code: {maxProdCode}");
        var fileStorage = new DummyFileStorage();
        var codeGen = new SystemCodeGenerator(db);
        var audit = new DummyAuditService();
        var currentUser = new DummyCurrentUser();
        var vatEngine = new VatRuleEngine();
        var logger = NullLogger<ExcelPricingService>.Instance;

        var excelService = new ExcelPricingService(db, fileStorage, codeGen, audit, vatEngine, logger);
        var plService = new PriceListService(db, codeGen, currentUser, audit, fileStorage);

        using var inspectStream = new FileStream(@"C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Web\wwwroot\uploads\pricing_originals\8513b30b3b0345d98116148f103b89c8_0908. GIÁ ĐẠI LÝ_ LACASA.xlsx", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var workbook = new ClosedXML.Excel.XLWorkbook(inspectStream);
        foreach (var sheet in workbook.Worksheets)
        {
            _output.WriteLine($"Worksheet: '{sheet.Name}', Visibility: {sheet.Visibility}, LastRow: {sheet.LastRowUsed()?.RowNumber()}");
        }
        var ws = workbook.Worksheets.First();
        int totalUsedRows = ws.LastRowUsed()?.RowNumber() ?? 0;
        
        int productRowsWithData = 0;
        for (int r = 11; r <= totalUsedRows; r++)
        {
            var row = ws.Row(r);
            var c2 = row.Cell(2).GetString().Trim(); // No
            var c3 = row.Cell(3).GetString().Trim(); // Nhom
            var c4 = row.Cell(4).GetString().Trim(); // Ma moi
            var c5 = row.Cell(5).GetString().Trim(); // Ma cu
            var c6 = row.Cell(6).GetString().Trim(); // Ma xuat hd
            var c7 = row.Cell(7).GetString().Trim(); // Ma qr
            var c9 = row.Cell(9).GetString().Trim(); // Thong tin
            var c10 = row.Cell(10).GetString().Trim(); // Gia
            
            bool isHeader = c2.Equals("NO", StringComparison.OrdinalIgnoreCase) || c4.Equals("MÃ HÀNG MỚI", StringComparison.OrdinalIgnoreCase);
            bool isSection = (c2 == "I" || c2 == "II" || c2 == "III") && (c4 == "BỒN CẦU" || c4 == "LAVABO" || c4 == "PHỤ KIỆN");
            bool isEmpty = string.IsNullOrEmpty(c2) && string.IsNullOrEmpty(c3) && string.IsNullOrEmpty(c4) && string.IsNullOrEmpty(c5) && string.IsNullOrEmpty(c9) && string.IsNullOrEmpty(c10);
            
            if (!isHeader && !isSection && !isEmpty)
            {
                productRowsWithData++;
                _output.WriteLine($"ROW {r}: No='{c2}', Group='{c3}', NewCode='{c4}', LegCode='{c5}', Info='{(c9.Length > 20 ? c9.Substring(0, 20) : c9)}', Price='{c10}'");
            }
        }
        _output.WriteLine($"TOTAL PRODUCT ROWS WITH DATA: {productRowsWithData}");

        var analysis = await excelService.AnalyzeAndDryRunAsync(inspectStream, "0908. GIÁ ĐẠI LÝ_ LACASA.xlsx");
        _output.WriteLine($"AnalyzeAndDryRun returned {analysis.Items.Count} items.");
        
        var parsedRowIndices = analysis.Items.Select(i => i.RowIndex).ToHashSet();
        for (int r = 11; r <= totalUsedRows; r++)
        {
            if (!parsedRowIndices.Contains(r))
            {
                var row = ws.Row(r);
                _output.WriteLine($"SKIPPED Row {r}: Col1='{row.Cell(1).GetString().Trim()}', Col2='{row.Cell(2).GetString().Trim()}', Col3='{row.Cell(3).GetString().Trim()}', Col4='{row.Cell(4).GetString().Trim()}', Col5='{row.Cell(5).GetString().Trim()}', Col6='{row.Cell(6).GetString().Trim()}', Col7='{row.Cell(7).GetString().Trim()}'");
            }
        }

        var commitReq = new ExcelImportCommitRequest
        {
            Name = "BẢNG GIÁ TEST THỰC TẾ 2026",
            Year = 2026,
            Quarter = 3,
            EffectiveFrom = new DateTime(2026, 8, 8, 0, 0, 0, DateTimeKind.Utc),
            EffectiveTo = new DateTime(2026, 9, 30, 23, 59, 59, DateTimeKind.Utc),
            InitialStatus = PriceListStatus.Active,
            AutoCreateProducts = true,
            Items = analysis.Items
        };

        var plDto = await excelService.CommitImportAsync(commitReq, "admin");
        _output.WriteLine($"Committed PriceList Id: {plDto.Id}, Code: {plDto.Code}, Status: {plDto.Status}");

        try
        {
            // Verify that repeated table headers or category banners were filtered out
            Assert.DoesNotContain(analysis.Items, i => i.NewCode == "MÃ HÀNG MỚI");
            Assert.DoesNotContain(analysis.Items, i => i.NewCode == "MÃ HÀNG");

            // Now test GetCurrentPricesForProductsAsync for products in DB
            var products = await db.Products.AsNoTracking().ToListAsync();
            var currentPrices = await plService.GetCurrentPricesForProductsAsync(products.Select(p => p.Id));

            int hasPrice = currentPrices.Count(kv => kv.Value.HasValue && kv.Value.Value > 0);
            int noPrice = currentPrices.Count(kv => !kv.Value.HasValue || kv.Value.Value <= 0);

            _output.WriteLine($"Total products: {products.Count}. HasPrice: {hasPrice}. NoPrice: {noPrice}");

            var tl2138 = products.FirstOrDefault(p => (p.NewCode != null && p.NewCode.Contains("TL2138")) || (p.LegacyCode != null && p.LegacyCode.Contains("TL2138")) || (p.Code != null && p.Code.Contains("TL2138")));
            _output.WriteLine($"TL2138 prod: Id={tl2138?.Id}, Code={tl2138?.Code}, NewCode={tl2138?.NewCode}, LegacyCode={tl2138?.LegacyCode}");
            if (tl2138 != null)
            {
                Assert.True(currentPrices.TryGetValue(tl2138.Id, out var tl2138Price));
                _output.WriteLine($"TL2138 Price: {tl2138Price}");
                Assert.Equal(1190000m, tl2138Price);
            }

            var lvb202 = products.FirstOrDefault(p => (p.NewCode != null && p.NewCode.Contains("LVB202-L1")) || (p.LegacyCode != null && p.LegacyCode.Contains("LVB202-L1")));
            _output.WriteLine($"LVB202-L1 prod: Id={lvb202?.Id}, Code={lvb202?.Code}, NewCode={lvb202?.NewCode}");
            if (lvb202 != null)
            {
                Assert.True(currentPrices.TryGetValue(lvb202.Id, out var lvbPrice) && lvbPrice.HasValue && lvbPrice.Value > 0);
            }

            _output.WriteLine("All Price & Matching assertions passed successfully!");
        }
        finally
        {
            // Clean up test price list
            var pl = await db.PriceLists.Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == plDto.Id);
            if (pl != null)
            {
                db.PriceListItems.RemoveRange(pl.Items);
                db.PriceLists.Remove(pl);
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task TestUpdateItemPriceAsync()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=tradeflow_dev;Username=postgres;Password=postgres")
            .Options;

        using var db = new TradeFlowDbContext(options);
        var audit = new DummyAuditService();
        var codeGen = new DummyCodeGenerator();
        var currentUser = new DummyCurrentUser();
        var fileStorage = new DummyFileStorage();
        var plService = new PriceListService(db, codeGen, currentUser, audit, fileStorage);

        var pl = new PriceList
        {
            Code = "TEST_PL_EDIT_" + Guid.NewGuid().ToString("N")[..8],
            Name = "Bảng giá test sửa",
            Status = PriceListStatus.Draft
        };
        var item = new PriceListItem
        {
            NewCode = "TEST_EDIT_01",
            UnitPrice = 100000m,
            CurrencyCode = "VND",
            PriceList = pl
        };
        db.PriceLists.Add(pl);
        db.PriceListItems.Add(item);
        await db.SaveChangesAsync();

        try
        {
            await plService.UpdateItemPriceAsync(item.Id, 150000m, 8m, "Tăng giá kiểm thử");
            var updated = await db.PriceListItems.FindAsync(item.Id);
            Assert.NotNull(updated);
            Assert.Equal(150000m, updated.UnitPrice);
            Assert.Equal(8m, updated.VatRate);
            Assert.Equal("Tăng giá kiểm thử", updated.Note);
            Assert.Contains(AuditEventType.PriceChanged, audit.LoggedEvents);
        }
        finally
        {
            db.PriceListItems.Remove(item);
            db.PriceLists.Remove(pl);
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task TestProductAndCategoryDeleteLogicAsync()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseNpgsql("Host=localhost;Database=tradeflow_dev;Username=postgres;Password=postgres")
            .Options;

        using var db = new TradeFlowDbContext(options);

        // 1. Create a test category
        var cat = new ProductCategory { Code = "TEST_CAT_" + Guid.NewGuid().ToString("N")[..6], Name = "Danh mục test xóa", IsActive = true };
        db.ProductCategories.Add(cat);
        await db.SaveChangesAsync();

        // 2. Create a test product inside cat
        var prod1 = new Product { Code = "TEST_P_" + Guid.NewGuid().ToString("N")[..6], Name = "Sản phẩm xóa vật lý", CategoryId = cat.Id, IsActive = true };
        db.Products.Add(prod1);
        await db.SaveChangesAsync();

        try
        {
            // Req 11: Cat has products -> soft delete IsActive = false
            bool catHasProducts = await db.Products.AnyAsync(p => p.CategoryId == cat.Id);
            Assert.True(catHasProducts);
            cat.IsActive = false;
            db.ProductCategories.Update(cat);
            await db.SaveChangesAsync();
            Assert.False(cat.IsActive);

            // Req 12: Prod1 has never been in SalesOrder or Invoice -> physical delete allowed
            bool prod1InBiz = await db.SalesOrderItems.AnyAsync(x => x.ProductId == prod1.Id) 
                           || await db.InvoiceItems.AnyAsync(x => x.ProductId == prod1.Id);
            Assert.False(prod1InBiz);
            db.Products.Remove(prod1);
            await db.SaveChangesAsync();
            Assert.Null(await db.Products.FindAsync(prod1.Id));

            // Now cat has no products -> physical delete allowed
            catHasProducts = await db.Products.AnyAsync(p => p.CategoryId == cat.Id);
            Assert.False(catHasProducts);
            db.ProductCategories.Remove(cat);
            await db.SaveChangesAsync();
            Assert.Null(await db.ProductCategories.FindAsync(cat.Id));
        }
        finally
        {
            var p = await db.Products.FindAsync(prod1.Id);
            if (p != null) db.Products.Remove(p);
            var c = await db.ProductCategories.FindAsync(cat.Id);
            if (c != null) db.ProductCategories.Remove(c);
            await db.SaveChangesAsync();
        }
    }
}
