using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.MasterData;

public class ProductServiceTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly ProductService _sut;

    private class TestDbContextFactory : IDbContextFactory<TradeFlowDbContext>
    {
        private readonly DbContextOptions<TradeFlowDbContext> _options;
        public TestDbContextFactory(DbContextOptions<TradeFlowDbContext> options) => _options = options;
        public TradeFlowDbContext CreateDbContext() => new TradeFlowDbContext(_options);
        public Task<TradeFlowDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) 
            => Task.FromResult(new TradeFlowDbContext(_options));
    }

    public ProductServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);
        _mockAudit = new Mock<IAuditService>();

        _sut = new ProductService(new TestDbContextFactory(dbOptions), _mockAudit.Object, NullLogger<ProductService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private ClaimsPrincipal CreateAdminUser()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Name, "administrator"),
            new Claim(ClaimTypes.Role, "Administrator")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private ClaimsPrincipal CreateStaffUser()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "staff-1"),
            new Claim(ClaimTypes.Name, "staff_user"),
            new Claim(ClaimTypes.Role, "SalesStaff")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    [Fact]
    public async Task DeleteProduct_UnauthorizedRole_RejectsWithPermissionError()
    {
        // Arrange
        var product = new Product
        {
            Code = "SP001",
            Name = "Sản phẩm thử nghiệm",
            IsActive = false
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var staff = CreateStaffUser();

        // Act
        var result = await _sut.DeleteProductAsync(product.Id, staff);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Chỉ Quản trị viên (Administrator)");

        // Verify product is not deleted
        var inDb = await _context.Products.FindAsync(product.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteProduct_ActiveProduct_RejectsRequiresDeactivationFirst()
    {
        // Arrange: Product is still active
        var product = new Product
        {
            Code = "SP002",
            Name = "Sản phẩm đang hoạt động",
            IsActive = true
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var admin = CreateAdminUser();

        // Act
        var result = await _sut.DeleteProductAsync(product.Id, admin);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("đang ở trạng thái Hoạt động");

        // Verify product remains intact
        var inDb = await _context.Products.FindAsync(product.Id);
        inDb.Should().NotBeNull();
        inDb!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProduct_ReferencedInSalesOrder_BlocksPhysicalDeletion()
    {
        // Arrange: Inactive product but linked to a SalesOrderItem
        var product = new Product
        {
            Code = "SP003",
            Name = "Sản phẩm đã bán",
            IsActive = false
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var order = new SalesOrder
        {
            Code = "SO-2026-9999",
            CustomerId = 1,
            CustomerName = "Khách A",
            GrandTotal = 500000m,
            OrderDate = DateTime.UtcNow
        };
        _context.SalesOrders.Add(order);
        await _context.SaveChangesAsync();

        var orderItem = new SalesOrderItem
        {
            SalesOrderId = order.Id,
            ProductId = product.Id,
            ProductCode = product.Code,
            ProductName = product.Name,
            Quantity = 2,
            UnitPrice = 250000m,
            LineTotal = 500000m
        };
        _context.SalesOrderItems.Add(orderItem);
        await _context.SaveChangesAsync();

        var admin = CreateAdminUser();

        // Act
        var result = await _sut.DeleteProductAsync(product.Id, admin);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.IsReferenced.Should().BeTrue();
        result.ReferenceCount.Should().Be(1);
        result.ErrorMessage.Should().Contain("chứng từ giao dịch lịch sử");

        // Verify entity in DB was NOT physically deleted
        var inDb = await _context.Products.FindAsync(product.Id);
        inDb.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteProduct_InactiveAndUnreferenced_AllowsPhysicalDeletion()
    {
        // Arrange: Inactive product with zero historical sales/invoices/quotations
        var product = new Product
        {
            Code = "SP004",
            Name = "Sản phẩm ngừng dùng chưa từng bán",
            IsActive = false
        };
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // Add a price list item to verify secondary lookup cleanup
        var priceList = new PriceList
        {
            Code = "PL-TEST",
            Name = "Bảng giá test",
            Status = Domain.Enums.PriceListStatus.Active
        };
        _context.PriceLists.Add(priceList);
        await _context.SaveChangesAsync();

        var pli = new PriceListItem
        {
            PriceListId = priceList.Id,
            ProductId = product.Id,
            UnitPrice = 100000m
        };
        _context.PriceListItems.Add(pli);
        await _context.SaveChangesAsync();

        var admin = CreateAdminUser();

        // Act
        var result = await _sut.DeleteProductAsync(product.Id, admin);

        // Assert
        result.Succeeded.Should().BeTrue();

        // Verify physical removal
        _context.ChangeTracker.Clear();
        var inDb = await _context.Products.FindAsync(product.Id);
        inDb.Should().BeNull();

        // Verify PriceListItem cleaned up
        var pliInDb = await _context.PriceListItems.FirstOrDefaultAsync(x => x.ProductId == product.Id);
        pliInDb.Should().BeNull();
    }

    [Fact]
    public async Task BulkDeleteProducts_NonAdmin_RejectsBulkDeletion()
    {
        var staff = CreateStaffUser();
        var result = await _sut.BulkDeleteProductsAsync(new List<int> { 1, 2, 3 }, staff);

        result.DeletedCount.Should().Be(0);
        result.Messages.Should().Contain(m => m.Contains("Chỉ Quản trị viên (Administrator)"));
    }
}
