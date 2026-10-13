using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Partners;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Partners;

public class PartnerAndDebtManagementTests
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
            Phone = "0901234567"
        };
        context.CompanySettings.Add(company);
        context.SaveChanges();

        return context;
    }

    private ClaimsPrincipal CreateAdminUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new(ClaimTypes.Role, "Administrator"),
            new("Permission", "Permission:Customers:Delete"),
            new("Permission", "Permission:Suppliers:Delete")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private ClaimsPrincipal CreateSalesStaffUser()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "sales01"),
            new(ClaimTypes.Role, "Staff"),
            new("Permission", "Permission:Customers:View")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private Mock<IAuthorizationService> CreateMockAuthService(bool canDelete)
    {
        var mock = new Mock<IAuthorizationService>();
        mock.Setup(a => a.AuthorizeAsync(
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<object?>(),
                It.Is<string>(p => p.Contains("Delete"))))
            .ReturnsAsync(canDelete ? AuthorizationResult.Success() : AuthorizationResult.Failed());
        return mock;
    }

    [Fact]
    public async Task CustomerQuery_FilterByClassificationAndStatus_ReturnsMatchingResults()
    {
        using var context = CreateInMemoryDbContext();
        var mockAuth = CreateMockAuthService(true);
        var mockAudit = new Mock<IAuditService>();
        var mockUser = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<PartnerService>>();

        var partnerService = new PartnerService(context, mockAudit.Object, mockAuth.Object, mockUser.Object, mockLogger.Object);

        // Seed 3 customers: 1 retail active, 1 company active, 1 retail inactive
        var c1 = new Customer
        {
            Code = "KH0001",
            Name = "Anh Hùng Thợ Điện",
            Phone = "0912345678",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var c2 = new Customer
        {
            Code = "KH0002",
            Name = "Công ty CP Xây Dựng Miền Tây",
            CompanyName = "Công ty CP Xây Dựng Miền Tây",
            TaxCode = "1801234567",
            Phone = "02923888999",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var c3 = new Customer
        {
            Code = "KH0003",
            Name = "Khách vãng lai cũ",
            Phone = "0933333333",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        context.Customers.AddRange(c1, c2, c3);
        await context.SaveChangesAsync();

        // 1. Filter: Corporate only
        var corporateResult = await partnerService.GetCustomersAsync(new CustomerFilterDto
        {
            CustomerType = CustomerClassification.Company
        });
        Assert.Single(corporateResult.Items);
        Assert.Equal("KH0002", corporateResult.Items[0].Code);
        Assert.True(corporateResult.Items[0].IsCompany);

        // 2. Filter: Retail only
        var retailResult = await partnerService.GetCustomersAsync(new CustomerFilterDto
        {
            CustomerType = CustomerClassification.Individual
        });
        Assert.Equal(2, retailResult.Items.Count);
        Assert.All(retailResult.Items, x => Assert.False(x.IsCompany));

        // 3. Filter: Active only
        var activeResult = await partnerService.GetCustomersAsync(new CustomerFilterDto
        {
            IsActive = true
        });
        Assert.Equal(2, activeResult.Items.Count);
    }

    [Fact]
    public async Task SupplierQuery_CrossRoleOverlap_DetectsMatchingCustomer()
    {
        using var context = CreateInMemoryDbContext();
        var mockAuth = CreateMockAuthService(true);
        var mockAudit = new Mock<IAuditService>();
        var mockUser = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<PartnerService>>();

        var partnerService = new PartnerService(context, mockAudit.Object, mockAuth.Object, mockUser.Object, mockLogger.Object);

        // A business that is both a customer and supplier (same MST "0311223344")
        var customer = new Customer
        {
            Code = "KH0010",
            Name = "Đại lý Thiết Bị Hòa Phát",
            CompanyName = "Công ty TNHH SX & TM Hòa Phát",
            TaxCode = "0311223344",
            Phone = "0909000111",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        var supplier = new Supplier
        {
            Code = "NCC0010",
            Name = "Công ty TNHH SX & TM Hòa Phát",
            CompanyName = "Công ty TNHH SX & TM Hòa Phát",
            TaxCode = "0311223344",
            Phone = "0909000111",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Customers.Add(customer);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        // Query supplier and verify cross-role overlap link
        var suppliers = await partnerService.GetSuppliersAsync(new SupplierFilterDto());
        Assert.Single(suppliers.Items);
        var item = suppliers.Items[0];
        Assert.Equal("NCC0010", item.Code);
        Assert.Equal(customer.Id, item.MatchingCustomerId);
        Assert.Equal("KH0010", item.MatchingCustomerCode);

        // Also query customer and verify matching supplier link
        var customers = await partnerService.GetCustomersAsync(new CustomerFilterDto());
        Assert.Single(customers.Items);
        Assert.Equal(supplier.Id, customers.Items[0].MatchingSupplierId);
        Assert.Equal("NCC0010", customers.Items[0].MatchingSupplierCode);
    }

    [Fact]
    public async Task DeleteCustomer_ReferencedInSalesOrderOrInvoice_EnforcesSafetySoftDeactivation()
    {
        using var context = CreateInMemoryDbContext();
        var mockAuth = CreateMockAuthService(canDelete: true); // user has permission
        var mockAudit = new Mock<IAuditService>();
        var mockUser = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<PartnerService>>();

        var partnerService = new PartnerService(context, mockAudit.Object, mockAuth.Object, mockUser.Object, mockLogger.Object);

        var customer = new Customer
        {
            Code = "KH0020",
            Name = "Công ty Xây Lắp An Giang",
            TaxCode = "1600123456",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Add a historical sales order for this customer
        var order = new SalesOrder
        {
            Code = "SO-2026-0001",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            OrderDate = DateTime.UtcNow,
            GrandTotal = 15000000m,
            Status = SalesOrderStatus.Confirmed
        };
        context.SalesOrders.Add(order);
        await context.SaveChangesAsync();

        var adminUser = CreateAdminUser();

        // Attempt deletion
        var deleteResult = await partnerService.DeleteCustomerAsync(customer.Id, adminUser);

        // Verify: Physical deletion was blocked!
        Assert.False(deleteResult.Succeeded);
        Assert.True(deleteResult.IsReferenced);
        Assert.True(deleteResult.SoftDeleted);
        Assert.Contains("bảo toàn tính toàn vẹn chứng từ", deleteResult.ErrorMessage);

        // Verify entity in DB was soft-deactivated, NOT removed
        var refreshedCustomer = await context.Customers.FindAsync(customer.Id);
        Assert.NotNull(refreshedCustomer);
        Assert.False(refreshedCustomer.IsActive); // Soft deactivated
    }

    [Fact]
    public async Task DeleteCustomer_Unreferenced_AllowsPhysicalDeletion()
    {
        using var context = CreateInMemoryDbContext();
        var mockAuth = CreateMockAuthService(canDelete: true);
        var mockAudit = new Mock<IAuditService>();
        var mockUser = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<PartnerService>>();

        var partnerService = new PartnerService(context, mockAudit.Object, mockAuth.Object, mockUser.Object, mockLogger.Object);

        var customer = new Customer
        {
            Code = "KH0030",
            Name = "Khách Hàng Chưa Từng Mua",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var adminUser = CreateAdminUser();
        var deleteResult = await partnerService.DeleteCustomerAsync(customer.Id, adminUser);

        Assert.True(deleteResult.Succeeded);

        // Verify physical removal
        var refreshedCustomer = await context.Customers.FindAsync(customer.Id);
        Assert.Null(refreshedCustomer);
    }

    [Fact]
    public async Task DeleteCustomer_UnauthorizedRole_RejectsWithPermissionError()
    {
        using var context = CreateInMemoryDbContext();
        var mockAuth = CreateMockAuthService(canDelete: false); // Staff without delete permission
        var mockAudit = new Mock<IAuditService>();
        var mockUser = new Mock<ICurrentUserService>();
        var mockLogger = new Mock<ILogger<PartnerService>>();

        var partnerService = new PartnerService(context, mockAudit.Object, mockAuth.Object, mockUser.Object, mockLogger.Object);

        var customer = new Customer
        {
            Code = "KH0040",
            Name = "Khách Thường",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var staffUser = CreateSalesStaffUser();
        var deleteResult = await partnerService.DeleteCustomerAsync(customer.Id, staffUser);

        Assert.False(deleteResult.Succeeded);
        Assert.Contains("Bạn không có quyền", deleteResult.ErrorMessage);

        // Record must remain intact
        var preserved = await context.Customers.FindAsync(customer.Id);
        Assert.NotNull(preserved);
        Assert.True(preserved.IsActive);
    }

    [Fact]
    public async Task SingleObligationDebt_SettledPosTransaction_HasZeroOutstandingDebt()
    {
        using var context = CreateInMemoryDbContext();
        var mockDebtLogger = new Mock<ILogger<PartnerDebtService>>();
        var debtService = new PartnerDebtService(context, mockDebtLogger.Object);

        var customer = new Customer
        {
            Code = "KH0050",
            Name = "Khách Mua Tiền Mặt",
            Phone = "0977889900",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // Retail POS invoice paid immediately in cash
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-2026-0001",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = DateTime.UtcNow,
            SubTotal = 500000m,
            GrandTotal = 500000m,
            PaymentMethod = "Tiền mặt",
            Status = InvoiceStatus.Issued
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var result = await debtService.GetDebtRecordsAsync(new DebtFilterDto
        {
            SearchTerm = "KH0050"
        });

        Assert.Single(result.Items);
        var rec = result.Items[0];
        Assert.Equal(500000m, rec.OriginalAmount);
        Assert.Equal(500000m, rec.PaidAmount);
        Assert.Equal(0m, rec.OutstandingAmount);
        Assert.Equal(DebtStatus.Settled, rec.DebtStatus);
    }

    [Fact]
    public async Task SingleObligationDebt_CreditInvoice_GeneratesOutstandingReceivableWithoutDoubleCounting()
    {
        using var context = CreateInMemoryDbContext();
        var mockDebtLogger = new Mock<ILogger<PartnerDebtService>>();
        var debtService = new PartnerDebtService(context, mockDebtLogger.Object);

        var customer = new Customer
        {
            Code = "KH0060",
            Name = "Nhà Thầu Hoàng Long",
            Phone = "0944556677",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        // 1. Order created for 20,000,000 VND
        var order = new SalesOrder
        {
            Code = "SO-2026-0002",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            OrderDate = DateTime.UtcNow.AddDays(-2),
            GrandTotal = 20000000m,
            Status = SalesOrderStatus.Confirmed
        };
        context.SalesOrders.Add(order);
        await context.SaveChangesAsync();

        // 2. Order is converted to an Invoice with credit term (Công nợ)
        var invoice = new Invoice
        {
            InvoiceNumber = "INV-2026-0002",
            SalesOrderId = order.Id,
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-1),
            SubTotal = 20000000m,
            GrandTotal = 20000000m,
            PaymentMethod = "Công nợ 30 ngày",
            Status = InvoiceStatus.Pending
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Query debt records for this customer
        var records = await debtService.GetDebtRecordsAsync(new DebtFilterDto
        {
            SearchTerm = "KH0060"
        });

        // SINGLE OBLIGATION RULE VERIFICATION:
        // Must only show 1 debt record for the Invoice. The order must NOT be double counted!
        Assert.Single(records.Items);
        var debtRec = records.Items[0];
        Assert.Equal(invoice.InvoiceNo, debtRec.DocumentCode);
        Assert.Equal(20000000m, debtRec.OutstandingAmount);
        Assert.Equal(DebtStatus.Outstanding, debtRec.DebtStatus);
        Assert.Equal(DebtDirection.Receivable, debtRec.Direction);

        // Summary KPI Verification
        var summary = await debtService.GetDebtSummaryAsync();
        Assert.Equal(20000000m, summary.TotalCustomerReceivables);
        Assert.Equal(0m, summary.TotalSupplierPayables);
        Assert.Equal(20000000m, summary.NetDifference);
        Assert.Equal(1, summary.CustomersWithDebtCount);
        Assert.Equal(0, summary.SuppliersWithDebtCount);
    }
}
