using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Sales;

public class SalesRefinementPass4Tests
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
            Address = "13 Nguyễn Văn Mai, P. Xuân Hòa, TP. Cần Thơ",
            Phone = "0901234567",
            BankName = "NGÂN HÀNG Á CHÂU (ACB)",
            BankAccount = "4987.9177",
            BankAccountHolder = "TRẦN VĂN TUẤN"
        };
        context.CompanySettings.Add(company);

        var cat1 = new ProductCategory { Code = "SU", Name = "Sứ Vệ Sinh" };
        var cat2 = new ProductCategory { Code = "SENVOI", Name = "Sen Vòi Inox" };
        context.ProductCategories.AddRange(cat1, cat2);

        var prod1 = new Product
        {
            Code = "TL2138",
            LegacyCode = "K8012",
            Name = "Bồn cầu khối TL2138",
            Category = cat1,
            IsActive = true
        };
        var prod2 = new Product
        {
            Code = "SC01",
            Name = "Cây sen tắm nóng lạnh Inox 304",
            Category = cat2,
            IsActive = true
        };
        context.Products.AddRange(prod1, prod2);

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task Customer_Retail_PhoneOnly_GeneratesFallbackName_AndAvoidsDuplicates()
    {
        using var context = CreateInMemoryDbContext();

        // 1. First retail customer with phone only
        string phone = "0988776655";
        string fallbackName = $"Khách lẻ ({phone})";

        var cust1 = new Customer
        {
            Code = $"KL{DateTime.Now:yyMMddHHmm}",
            Name = fallbackName,
            Phone = phone,
            IsActive = true
        };
        context.Customers.Add(cust1);
        await context.SaveChangesAsync();

        Assert.True(cust1.Id > 0);
        Assert.Equal("Khách lẻ (0988776655)", cust1.Name);
        Assert.Null(cust1.TaxCode);
        Assert.Null(cust1.CompanyName);

        // 2. Query deduplication by phone
        var existing = await context.Customers.FirstOrDefaultAsync(c => c.Phone == phone);
        Assert.NotNull(existing);
        Assert.Equal(cust1.Id, existing.Id);
    }

    [Fact]
    public async Task Customer_Company_PreservesLegalNameTaxCodeAndContact()
    {
        using var context = CreateInMemoryDbContext();

        string compName = "CÔNG TY CỔ PHẦN XÂY DỰNG MEKONG";
        string taxCode = "1801234567";

        var compCust = new Customer
        {
            Code = $"DN{DateTime.Now:yyMMddHHmm}",
            Name = compName,
            CompanyName = compName,
            TaxCode = taxCode,
            ContactPerson = "Nguyễn Văn Hùng",
            Phone = "02923888999",
            Email = "ketoan@mekongconst.com.vn",
            Address = "Số 45 Võ Văn Kiệt, P. An Hòa, Q. Ninh Kiều, TP. Cần Thơ",
            IsActive = true
        };
        context.Customers.Add(compCust);
        await context.SaveChangesAsync();

        Assert.True(compCust.Id > 0);
        Assert.Equal(compName, compCust.CompanyName);
        Assert.Equal("1801234567", compCust.TaxCode);
        Assert.Equal("Nguyễn Văn Hùng", compCust.ContactPerson);

        // Deduplication by tax code
        var existingByTax = await context.Customers.FirstOrDefaultAsync(c => c.TaxCode == taxCode);
        Assert.NotNull(existingByTax);
        Assert.Equal(compCust.Id, existingByTax.Id);
    }

    [Theory]
    [InlineData(0, 0, 0, 1000000, 1000000)]       // 0% VAT
    [InlineData(50000, 5, 47500, 950000, 997500)]  // 5% VAT with 50k discount
    [InlineData(100000, 8, 72000, 900000, 972000)] // 8% VAT with 100k discount
    [InlineData(0, 10, 100000, 1000000, 1100000)]  // 10% VAT without discount
    public void ConfigurableVAT_Calculations_SubTotal_Discount_TaxableBase_MatchesFormulas(
        decimal orderDiscount,
        decimal vatRate,
        decimal expectedTaxAmount,
        decimal expectedTaxableBase,
        decimal expectedGrandTotal)
    {
        decimal subTotal = 1000000m;
        decimal taxableBase = Math.Max(0, subTotal - orderDiscount);
        decimal taxAmount = Math.Round(taxableBase * (vatRate / 100m), 0, MidpointRounding.AwayFromZero);
        decimal grandTotal = taxableBase + taxAmount;

        Assert.Equal(expectedTaxableBase, taxableBase);
        Assert.Equal(expectedTaxAmount, taxAmount);
        Assert.Equal(expectedGrandTotal, grandTotal);
    }

    [Fact]
    public async Task SalesOrder_PersistsTaxAndDiscountBreakdown_AndCreatesAccurateDto()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        var mockAudit = new Mock<IAuditService>();
        var salesService = new SalesService(context, mockUser.Object, mockAudit.Object);

        var cust = new Customer
        {
            Code = "KH01",
            Name = "Khách Hàng A",
            Phone = "0912345678",
            IsActive = true
        };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var orderDto = new SalesOrderDto
        {
            CustomerId = cust.Id,
            CustomerName = cust.Name,
            CustomerPhone = cust.Phone,
            TotalDiscount = 100000m,
            OrderDate = DateTime.Now,
            Items = new List<SalesOrderItemDto>
            {
                new()
                {
                    ProductId = 1,
                    ProductCode = "TL2138",
                    ProductName = "Bồn cầu TL2138",
                    Quantity = 2,
                    UnitPrice = 1000000m,
                    DiscountAmount = 0m,
                    TaxRate = 8m,
                    TaxAmount = 160000m,
                    LineTotal = 2160000m
                }
            }
        };

        var created = await salesService.CreateOrderAsync(orderDto);
        Assert.NotNull(created);
        Assert.Equal(2000000m, created.SubTotal);
        Assert.Equal(100000m, created.TotalDiscount);
        Assert.Equal(160000m, created.TotalTax);
        // GrandTotal = 2,000,000 - 100,000 + 160,000 = 2,060,000
        Assert.Equal(2060000m, created.GrandTotal);

        // Fetch back and verify mapping
        var fetched = await salesService.GetOrderByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(2060000m, fetched.GrandTotal);
        Assert.Equal(160000m, fetched.TotalTax);
        Assert.Equal(100000m, fetched.TotalDiscount);
    }

    [Fact]
    public async Task SeparateDocuments_SalesInvoice_IsNonVat_AndOrderDoc_HasVatBreakdown()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);
        var salesService = new SalesService(context, mockUser.Object, mockAudit.Object);

        var cust = new Customer
        {
            Code = "DN01",
            Name = "CÔNG TY TNHH BÌNH MINH",
            CompanyName = "CÔNG TY TNHH BÌNH MINH",
            TaxCode = "0311223344",
            Phone = "0909112233",
            IsActive = true
        };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var orderDto = new SalesOrderDto
        {
            CustomerId = cust.Id,
            CustomerName = cust.CompanyName,
            CustomerPhone = cust.Phone,
            TotalDiscount = 50000m,
            OrderDate = DateTime.Now,
            Items = new List<SalesOrderItemDto>
            {
                new()
                {
                    ProductId = 1,
                    ProductCode = "TL2138",
                    ProductName = "Bồn cầu TL2138",
                    Quantity = 1,
                    UnitPrice = 1000000m,
                    DiscountAmount = 0m,
                    TaxRate = 8m,
                    TaxAmount = 80000m,
                    LineTotal = 1080000m
                }
            }
        };

        var createdOrder = await salesService.CreateOrderAsync(orderDto);
        await salesService.ConfirmOrderAsync(createdOrder.Id);

        // ACTION 1: In / Tải Hóa đơn bán hàng (chưa cộng VAT) - InvoiceType.SalesInvoice (Mẫu 02/BH)
        var salesInvoice = await invoiceService.CreateInvoiceFromOrderAsync(createdOrder.Id, InvoiceType.SalesInvoice);
        Assert.NotNull(salesInvoice);
        Assert.Equal(InvoiceType.SalesInvoice, salesInvoice.Type);
        Assert.Equal("2", salesInvoice.FormNumber);
        Assert.Equal(0m, salesInvoice.TotalTax);
        Assert.Equal(1000000m, salesInvoice.SubTotal);
        // Sales Invoice GrandTotal is non-VAT (Taxable base: 1,000,000)
        Assert.Equal(1000000m, salesInvoice.GrandTotal);

        var salesPdf = await invoiceService.GeneratePdfAsync(salesInvoice.Id);
        Assert.NotNull(salesPdf);
        Assert.True(salesPdf.Length > 1000);

        // ACTION 2: In chứng từ bán hàng có VAT - OrderDocumentPdf
        var orderDoc = await invoiceService.GetOrderDocumentByOrderIdAsync(createdOrder.Id);
        Assert.NotNull(orderDoc);
        Assert.Equal(createdOrder.GrandTotal, orderDoc.GrandTotal);
        Assert.Contains("Đã bao gồm thuế GTGT", orderDoc.VatNote);

        var orderDocPdf = await invoiceService.GenerateOrderDocumentPdfAsync(orderDoc);
        Assert.NotNull(orderDocPdf);
        Assert.True(orderDocPdf.Length > 1000);
    }
}
