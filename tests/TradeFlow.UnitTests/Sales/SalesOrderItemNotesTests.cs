using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Sales;

public class SalesOrderItemNotesTests
{
    private TradeFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradeFlowDbContext(options);

        var company = new CompanySettings("CÔNG TY TNHH XNK THƯƠNG MẠI DỊCH VỤ LACASA")
        {
            TaxCode = "0319205643",
            Address = "13 Nguyễn Văn Mai, P. Xuân Hòa, TP. HCM",
            Phone = "0901234567"
        };
        context.CompanySettings.Add(company);

        var customer = new Customer
        {
            Code = "KH-0001",
            Name = "Công ty TNHH Khách Hàng Thử Nghiệm",
            Address = "123 Phố Huế, Hai Bà Trưng, Hà Nội",
            Phone = "0987654321",
            IsActive = true
        };
        context.Customers.Add(customer);

        var productA = new Product
        {
            Code = "SP-001",
            Name = "Bồn cầu thông minh",
            IsActive = true
        };
        var productB = new Product
        {
            Code = "SP-002",
            Name = "Vòi sen tắm đứng Inox 304",
            IsActive = true
        };
        context.Products.AddRange(productA, productB);

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task CreateOrderAsync_WithLineItemNotes_PersistsNotesIndependentlyForEachLine()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns("test-user-id");
        var mockAuditService = new Mock<IAuditService>();
        var salesService = new SalesService(context, mockCurrentUserService.Object, mockAuditService.Object);

        var customer = await context.Customers.FirstAsync();
        var productA = await context.Products.FirstAsync(p => p.Code == "SP-001");
        var productB = await context.Products.FirstAsync(p => p.Code == "SP-002");

        var orderDto = new SalesOrderDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            OrderDate = DateTime.Now,
            Notes = "Ghi chú chung toàn đơn hàng",
            Items = new List<SalesOrderItemDto>
            {
                new()
                {
                    ProductId = productA.Id,
                    ProductCode = productA.Code,
                    ProductName = productA.Name,
                    Quantity = 2,
                    UnitPrice = 5000000,
                    LineTotal = 10000000,
                    Notes = "Yêu cầu kiểm tra linh kiện nắp điện tử trước khi giao"
                },
                new()
                {
                    ProductId = productB.Id,
                    ProductCode = productB.Code,
                    ProductName = productB.Name,
                    Quantity = 1,
                    UnitPrice = 2500000,
                    LineTotal = 2500000,
                    Notes = "Tặng kèm dây sen dài 1.5m"
                },
                new()
                {
                    ProductId = productA.Id,
                    ProductCode = productA.Code,
                    ProductName = productA.Name,
                    Quantity = 1,
                    UnitPrice = 5000000,
                    LineTotal = 5000000,
                    Notes = "Lắp đặt tại lầu 2, đổi màu viền nắp sang bạc"
                }
            }
        };

        // Act
        var createdOrderDto = await salesService.CreateOrderAsync(orderDto);

        // Assert - DTO returned
        Assert.NotNull(createdOrderDto);
        Assert.Equal(3, createdOrderDto.Items.Count);
        Assert.Equal("Yêu cầu kiểm tra linh kiện nắp điện tử trước khi giao", createdOrderDto.Items[0].Notes);
        Assert.Equal("Tặng kèm dây sen dài 1.5m", createdOrderDto.Items[1].Notes);
        Assert.Equal("Lắp đặt tại lầu 2, đổi màu viền nắp sang bạc", createdOrderDto.Items[2].Notes);

        // Assert - In database
        var dbOrder = await context.SalesOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == createdOrderDto.Id);

        Assert.NotNull(dbOrder);
        Assert.Equal("Ghi chú chung toàn đơn hàng", dbOrder.Notes);
        Assert.Equal(3, dbOrder.Items.Count);

        var itemsList = dbOrder.Items.ToList();
        Assert.Equal("Yêu cầu kiểm tra linh kiện nắp điện tử trước khi giao", itemsList[0].Notes);
        Assert.Equal("Tặng kèm dây sen dài 1.5m", itemsList[1].Notes);
        Assert.Equal("Lắp đặt tại lầu 2, đổi màu viền nắp sang bạc", itemsList[2].Notes);
    }

    [Fact]
    public async Task UpdateOrderAsync_WithUpdatedLineItemNotes_PersistsCorrectly()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var mockCurrentUserService = new Mock<ICurrentUserService>();
        mockCurrentUserService.Setup(s => s.UserId).Returns("test-user-id");
        var mockAuditService = new Mock<IAuditService>();
        var salesService = new SalesService(context, mockCurrentUserService.Object, mockAuditService.Object);

        var customer = await context.Customers.FirstAsync();
        var productA = await context.Products.FirstAsync(p => p.Code == "SP-001");

        var createDto = new SalesOrderDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            OrderDate = DateTime.Now,
            Items = new List<SalesOrderItemDto>
            {
                new()
                {
                    ProductId = productA.Id,
                    ProductCode = productA.Code,
                    ProductName = productA.Name,
                    Quantity = 1,
                    UnitPrice = 5000000,
                    LineTotal = 5000000,
                    Notes = "Ghi chú ban đầu"
                }
            }
        };

        var created = await salesService.CreateOrderAsync(createDto);

        // Act - Update order with modified note
        created.Items[0].Notes = "Ghi chú đã cập nhật sau khi thỏa thuận khách hàng";
        var success = await salesService.UpdateOrderAsync(created);

        // Assert
        Assert.True(success);

        var dbOrder = await context.SalesOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == created.Id);

        Assert.NotNull(dbOrder);
        Assert.Equal("Ghi chú đã cập nhật sau khi thỏa thuận khách hàng", dbOrder.Items.First().Notes);

        var fetchedDto = await salesService.GetOrderByIdAsync(created.Id);
        Assert.NotNull(fetchedDto);
        Assert.Equal("Ghi chú đã cập nhật sau khi thỏa thuận khách hàng", fetchedDto.Items.First().Notes);
    }
}
