using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Sales;

public class QuotationTests
{
    private TradeFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradeFlowDbContext(options);

        // Seed CompanySettings
        var company = new CompanySettings("CÔNG TY TNHH XNK THƯƠNG MẠI DỊCH VỤ LACASA")
        {
            TaxCode = "0319205643",
            Address = "13 Nguyễn Văn Mai, P. Xuân Hòa, TP. HCM",
            Phone = "0901234567",
            Email = "contact@lacasa.vn",
            BankAccount = "123456789",
            BankName = "Vietcombank",
            BankAccountHolder = "TRẦN VĂN TUẤN",
            TradingName = "LACASA",
            Website = "https://lacasa.vn"
        };
        context.CompanySettings.Add(company);

        // Seed Customer
        var customer = new Customer
        {
            Code = "KH-0001",
            Name = "Công ty TNHH Khách Hàng Thử Nghiệm",
            TaxCode = "0102030405",
            Address = "123 Phố Huế, Hai Bà Trưng, Hà Nội",
            Phone = "0987654321",
            Email = "khachhang@test.vn",
            ContactPerson = "Nguyễn Văn A",
            IsActive = true
        };
        context.Customers.Add(customer);

        // Seed Products
        var unit = new UnitOfMeasure { Code = "CAI", Name = "Cái", IsActive = true };
        context.UnitOfMeasures.Add(unit);

        var p1 = new Product
        {
            Code = "SP-001",
            Name = "Sản phẩm A tiêu chuẩn",
            Unit = unit,
            IsActive = true
        };
        var p2 = new Product
        {
            Code = "SP-002",
            Name = "Sản phẩm B cao cấp",
            Unit = unit,
            IsActive = true
        };
        context.Products.AddRange(p1, p2);

        // Seed Sequence
        var seq = new SystemSequence("Quotation", "BG-", "{Prefix}{Year}-{Number:D4}", "Mã báo giá")
        {
            CurrentNumber = 1,
            Step = 1,
            CreatedAt = DateTime.UtcNow
        };
        context.SystemSequences.Add(seq);

        context.SaveChanges();
        return context;
    }

    private (QuotationService Service, TradeFlowDbContext Context, Mock<ISalesService> MockSales) CreateService(TradeFlowDbContext context)
    {
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("user-1");
        mockUser.Setup(u => u.UserName).Returns("admin");

        var mockAudit = new Mock<IAuditService>();

        var mockSales = new Mock<ISalesService>();
        mockSales.Setup(s => s.CreateOrderAsync(It.IsAny<SalesOrderDto>(), default))
            .ReturnsAsync((SalesOrderDto input, System.Threading.CancellationToken ct) =>
            {
                var so = new SalesOrder
                {
                    Code = "SO-2026-0099",
                    CustomerId = input.CustomerId,
                    CustomerName = input.CustomerName,
                    CustomerTaxCode = input.CustomerTaxCode,
                    CustomerAddress = input.CustomerAddress,
                    CustomerPhone = input.CustomerPhone,
                    OrderDate = DateTime.UtcNow,
                    SubTotal = input.Items.Sum(x => x.Quantity * (x.UnitPrice ?? 0)),
                    GrandTotal = input.Items.Sum(x => x.Quantity * (x.UnitPrice ?? 0) - x.DiscountAmount),
                    Status = SalesOrderStatus.Draft
                };
                context.SalesOrders.Add(so);
                context.SaveChanges();
                input.Id = so.Id;
                input.Code = so.Code;
                return input;
            });

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

        var service = new QuotationService(context, mockUser.Object, mockAudit.Object, mockSales.Object, mockEnv.Object);
        return (service, context, mockSales);
    }

    [Fact]
    public async Task CreateQuotation_CalculatesTotalsAndDiscountsCorrectly_WithoutVAT()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        var dto = new QuotationDto
        {
            CustomerId = 1,
            QuotationDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            SalespersonName = "Nguyễn Văn Bán Hàng",
            Items = new List<QuotationItemDto>
            {
                new()
                {
                    ProductId = 1,
                    ProductCode = "SP-001",
                    ProductName = "Sản phẩm A",
                    UnitName = "Cái",
                    Quantity = 2,
                    UnitPrice = 1000000m, // 2 * 1,000,000 = 2,000,000
                    DiscountRate = 5m,     // 5% of 2,000,000 = 100,000
                    PriceSource = "Bảng giá tiêu chuẩn"
                },
                new()
                {
                    ProductId = 2,
                    ProductCode = "SP-002",
                    ProductName = "Sản phẩm B",
                    UnitName = "Cái",
                    Quantity = 1,
                    UnitPrice = 2500000m, // 1 * 2,500,000 = 2,500,000
                    DiscountAmount = 200000m, // fixed 200,000 discount
                    PriceSource = "Bảng giá VIP"
                }
            }
        };

        var created = await service.CreateQuotationAsync(dto);

        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.StartsWith("BG-", created.Code);
        Assert.Equal(QuotationStatus.Draft, created.Status);

        // SubTotal: 2,000,000 + 2,500,000 = 4,500,000
        Assert.Equal(4500000m, created.SubTotal);
        // TotalDiscount: 100,000 + 200,000 = 300,000
        Assert.Equal(300000m, created.TotalDiscount);
        // GrandTotal: 4,500,000 - 300,000 = 4,200,000 (Commercial quotation, NO VAT)
        Assert.Equal(4200000m, created.GrandTotal);

        Assert.Equal(2, created.Items.Count);
        Assert.Equal(1900000m, created.Items[0].LineTotal);
        Assert.Equal(2300000m, created.Items[1].LineTotal);
    }

    [Fact]
    public async Task ConvertToSalesOrder_PreservesPricingAndDiscount_AndEstablishesTwoWayLink()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, mockSales) = CreateService(context);

        var dto = new QuotationDto
        {
            CustomerId = 1,
            QuotationDate = DateTime.UtcNow,
            Items = new List<QuotationItemDto>
            {
                new()
                {
                    ProductId = 1,
                    ProductCode = "SP-001",
                    ProductName = "Sản phẩm A",
                    UnitName = "Cái",
                    Quantity = 3,
                    UnitPrice = 1190000m,
                    DiscountRate = 10m,
                    DiscountAmount = 357000m,
                    PriceSource = "Bảng giá 2026"
                }
            }
        };

        var quotation = await service.CreateQuotationAsync(dto);
        Assert.Equal(QuotationStatus.Draft, quotation.Status);

        // Convert to Sales Order
        var salesOrder = await service.ConvertToSalesOrderAsync(quotation.Id);

        Assert.NotNull(salesOrder);
        Assert.True(salesOrder.Id > 0);

        // Check SalesOrder creation payload preserved exact fields
        mockSales.Verify(s => s.CreateOrderAsync(It.Is<SalesOrderDto>(so =>
            so.CustomerId == 1 &&
            so.Items.Count == 1 &&
            so.Items[0].UnitPrice == 1190000m &&
            so.Items[0].Quantity == 3 &&
            so.Items[0].DiscountRate == 10m &&
            so.Items[0].DiscountAmount == 357000m &&
            so.Items[0].LineTotal == (3 * 1190000m) - 357000m
        ), default), Times.Once);

        // Verify 2-way relationship in DbContext
        var updatedQuotation = await context.Quotations.FindAsync(quotation.Id);
        Assert.NotNull(updatedQuotation);
        Assert.Equal(QuotationStatus.Converted, updatedQuotation.Status);
        Assert.Equal(salesOrder.Id, updatedQuotation.SalesOrderId);

        var updatedOrder = await context.SalesOrders.FindAsync(salesOrder.Id);
        Assert.NotNull(updatedOrder);
        Assert.Equal(quotation.Id, updatedOrder.QuotationId);
    }

    [Fact]
    public async Task ConvertToSalesOrder_PreventsDuplicateConversion()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        var dto = new QuotationDto
        {
            CustomerId = 1,
            QuotationDate = DateTime.UtcNow,
            Items = new List<QuotationItemDto>
            {
                new()
                {
                    ProductId = 1,
                    ProductCode = "SP-001",
                    ProductName = "Sản phẩm A",
                    UnitName = "Cái",
                    Quantity = 1,
                    UnitPrice = 500000m
                }
            }
        };

        var quotation = await service.CreateQuotationAsync(dto);
        await service.ConvertToSalesOrderAsync(quotation.Id);

        // Second conversion attempt MUST throw InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ConvertToSalesOrderAsync(quotation.Id));
    }

    [Fact]
    public async Task GeneratePdf_RendersValidPdfByteArray()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        var dto = new QuotationDto
        {
            CustomerId = 1,
            QuotationDate = DateTime.UtcNow,
            Items = new List<QuotationItemDto>
            {
                new()
                {
                    ProductId = 1,
                    ProductCode = "SP-001",
                    ProductName = "Sản phẩm A",
                    UnitName = "Cái",
                    Quantity = 2,
                    UnitPrice = 1200000m,
                    DiscountAmount = 50000m
                }
            }
        };

        var quotation = await service.CreateQuotationAsync(dto);

        var pdfBytes = await service.GeneratePdfAsync(quotation.Id);

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        try
        {
            var artifactPath = @"C:\Users\thitr\.gemini\antigravity\brain\fb94d497-815d-460b-bd53-9de32ef4d901\synthetic_quotation_document.pdf";
            File.WriteAllBytes(artifactPath, pdfBytes);
        }
        catch { }

        // Validate PDF signature header %PDF-
        var header = Encoding.ASCII.GetString(pdfBytes.Take(5).ToArray());
        Assert.Equal("%PDF-", header);
    }

    [Fact]
    public async Task UpdateStatus_TransitionsSuccessfully()
    {
        using var context = CreateInMemoryDbContext();
        var (service, _, _) = CreateService(context);

        var dto = new QuotationDto
        {
            CustomerId = 1,
            QuotationDate = DateTime.UtcNow,
            Items = new List<QuotationItemDto>
            {
                new() { ProductId = 1, ProductCode = "SP-001", ProductName = "SP A", Quantity = 1, UnitPrice = 100000m }
            }
        };

        var quotation = await service.CreateQuotationAsync(dto);
        Assert.Equal(QuotationStatus.Draft, quotation.Status);

        var sentOk = await service.UpdateStatusAsync(quotation.Id, QuotationStatus.Sent);
        Assert.True(sentOk);
        var qSent = await service.GetQuotationByIdAsync(quotation.Id);
        Assert.Equal(QuotationStatus.Sent, qSent!.Status);

        var acceptOk = await service.UpdateStatusAsync(quotation.Id, QuotationStatus.Accepted);
        Assert.True(acceptOk);
        var qAccepted = await service.GetQuotationByIdAsync(quotation.Id);
        Assert.Equal(QuotationStatus.Accepted, qAccepted!.Status);
    }
}
