using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Partners;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Partners;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Partners;

public class PartnerAndDebtManagementTests
{
    private PartnerDebtService CreatePartnerDebtService(
        TradeFlowDbContext context,
        IPartnerDebtExportService? exportService = null,
        IAuthorizationService? authService = null)
    {
        var mockLogger = new Mock<ILogger<PartnerDebtService>>();
        var mockAuth = new Mock<IAuthorizationService>();
        mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());
        var auth = authService ?? mockAuth.Object;

        var mockAudit = new Mock<IAuditService>();
        var export = exportService ?? new PartnerDebtExportService(context, auth, mockAudit.Object, new Mock<ILogger<PartnerDebtExportService>>().Object);

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(u => u.UserId).Returns("admin-id");
        mockCurrentUser.Setup(u => u.UserName).Returns("admin");

        return new PartnerDebtService(context, export, auth, mockAudit.Object, mockCurrentUser.Object, mockLogger.Object);
    }
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
        var debtService = CreatePartnerDebtService(context);

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
        var debtService = CreatePartnerDebtService(context);

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

    [Fact]
    public async Task SinglePartnerDebtStatement_ExcelExport_GeneratesProfessionalWorkbookWithRealNumbers()
    {
        using var context = CreateInMemoryDbContext();
        var debtService = CreatePartnerDebtService(context);

        var customer = new Customer
        {
            Code = "KH-EXPORT-01",
            Name = "Công ty TNHH Vật Liệu XD An Phát",
            Phone = "0912345678",
            TaxCode = "0311223344",
            Address = "Số 10 Đường 3/2, Q.10, TP.HCM",
            IsActive = true
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = "HD-2026-0088",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            SubTotal = 15000000m,
            GrandTotal = 15000000m,
            PaymentMethod = "Chuyển khoản",
            Status = InvoiceStatus.Issued
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var adminUser = CreateAdminUser();
        var exportRequest = new PartnerDebtStatementExportRequest
        {
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { customer.Id },
            IncludeLineDetails = true,
            DatePresetTitle = "Tháng này"
        };

        var bytes = await debtService.ExportPartnerDebtStatementAsync(exportRequest, adminUser);
        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 0);

        // Verify with ClosedXML
        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        // Single partner statement must contain 2 worksheets: "Tổng hợp công nợ" and "Chi tiết giao dịch"
        Assert.True(workbook.Worksheets.Count >= 2);
        var summarySheet = workbook.Worksheet("Tổng hợp công nợ");
        Assert.NotNull(summarySheet);
        Assert.Contains(customer.Code, summarySheet.Cell(8, 2).GetString());

        var detailSheet = workbook.Worksheet("Chi tiết giao dịch");
        Assert.NotNull(detailSheet);

        // Verify financial cells contain real numeric values (not plain strings)
        var closingCell = summarySheet.Cell("C16");
        Assert.Equal(XLDataType.Number, closingCell.DataType);
        Assert.Equal(15000000d, closingCell.GetDouble());
    }

    [Fact]
    public async Task MultiPartnerDebtStatement_ExcelExport_CombinesIntoSingleWorkbookWithSafeSheetNames()
    {
        using var context = CreateInMemoryDbContext();
        var debtService = CreatePartnerDebtService(context);

        var customer1 = new Customer { Code = "KH-SAFE-01", Name = "Khách Hàng Một", IsActive = true };
        var customer2 = new Customer { Code = "KH/SPECIAL*CHARS:TEST?[02]", Name = "Khách Hàng Hai", IsActive = true };
        context.Customers.AddRange(customer1, customer2);
        await context.SaveChangesAsync();

        var adminUser = CreateAdminUser();
        var exportRequest = new PartnerDebtStatementExportRequest
        {
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { customer1.Id, customer2.Id },
            DatePresetTitle = "Toàn thời gian"
        };

        var bytes = await debtService.ExportPartnerDebtStatementAsync(exportRequest, adminUser);
        Assert.NotNull(bytes);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        // Multi-partner export must produce exactly 1 summary sheet + 1 detail sheet per partner
        Assert.Equal(3, workbook.Worksheets.Count);
        Assert.NotNull(workbook.Worksheet("Tổng hợp công nợ"));

        // Verify safe sheet names: prohibited characters \ / ? * [ ] : must be cleaned
        foreach (var ws in workbook.Worksheets)
        {
            Assert.True(ws.Name.Length <= 31, $"Worksheet name '{ws.Name}' exceeds 31 characters");
            Assert.DoesNotContain("/", ws.Name);
            Assert.DoesNotContain("\\", ws.Name);
            Assert.DoesNotContain("?", ws.Name);
            Assert.DoesNotContain("*", ws.Name);
            Assert.DoesNotContain("[", ws.Name);
            Assert.DoesNotContain("]", ws.Name);
            Assert.DoesNotContain(":", ws.Name);
        }
    }

    [Fact]
    public async Task ConsolidatedDebtManagementWorkbook_ExcelExport_Contains4SheetsWithValidTotals()
    {
        using var context = CreateInMemoryDbContext();
        var debtService = CreatePartnerDebtService(context);

        var customer = new Customer { Code = "KH-MGT-01", Name = "Công ty Quản Lý", IsActive = true };
        context.Customers.Add(customer);

        var supplier = new Supplier { Code = "NCC-MGT-01", Name = "Nhà Cung Cấp Tổng", IsActive = true };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = "HD-MGT-001",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = DateTime.UtcNow,
            SubTotal = 5000000m,
            GrandTotal = 5000000m,
            PaymentMethod = "Công nợ",
            Status = InvoiceStatus.Issued
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        var adminUser = CreateAdminUser();
        var req = new DebtManagementExportRequest
        {
            Scope = DebtExportScope.AllFiltered,
            DatePresetTitle = "Tháng này"
        };

        var bytes = await debtService.ExportDebtManagementWorkbookAsync(req, adminUser);
        Assert.NotNull(bytes);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        // Consolidated management workbook must contain 4 core sheets:
        Assert.NotNull(workbook.Worksheet("Tổng quan công nợ"));
        Assert.NotNull(workbook.Worksheet("Tổng hợp phải thu KH"));
        Assert.NotNull(workbook.Worksheet("Tổng hợp phải trả NCC"));
        Assert.NotNull(workbook.Worksheet("Chi tiết giao dịch"));

        var overviewSheet = workbook.Worksheet("Tổng quan công nợ");
        var customerReceivableCell = overviewSheet.Cell(6, 3);
        Assert.Equal(XLDataType.Number, customerReceivableCell.DataType);
        Assert.Equal(5000000d, customerReceivableCell.GetDouble());
    }

    [Fact]
    public async Task RecordDebtPayment_CustomerPartialPayment_ReducesOutstandingBalanceAndCreatesPTSequence()
    {
        using var context = CreateInMemoryDbContext();
        var debtService = CreatePartnerDebtService(context);

        var customer = new Customer
        {
            Code = "KH-PAY-01",
            Name = "Công ty TNHH Cơ Khí Đại Việt",
            IsActive = true
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = "HD-PAY-001",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-5),
            SubTotal = 10000000m,
            GrandTotal = 10000000m,
            PaymentMethod = "Công nợ",
            Status = InvoiceStatus.Issued
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Record a partial payment of 4,000,000 VND
        var adminUser = CreateAdminUser();
        var paymentDto = new RecordDebtPaymentDto
        {
            PartnerType = PartnerType.Customer,
            PartnerId = customer.Id,
            TransactionType = DebtTransactionType.Payment,
            Amount = 4000000m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Chuyển khoản Vietcombank",
            ReferenceCode = "UNC-10029",
            InvoiceId = invoice.Id,
            Notes = "Thanh toán đợt 1 hóa đơn HD-PAY-001"
        };

        var result = await debtService.RecordPaymentAsync(paymentDto, adminUser);
        Assert.True(result.Succeeded);
        Assert.StartsWith("PT", result.PaymentCode);
        Assert.Equal(6000000m, result.NewRemainingBalance);

        // Check PartnerPayment entity was recorded
        var paymentRecord = await context.PartnerPayments.FindAsync(result.PaymentId);
        Assert.NotNull(paymentRecord);
        Assert.Equal(4000000m, paymentRecord.Amount);
        Assert.Equal(DebtTransactionType.Payment, paymentRecord.TransactionType);

        // Check debt summary recalculation
        var summary = await debtService.GetDebtSummaryAsync();
        Assert.Equal(6000000m, summary.TotalCustomerReceivables);

        // Check invoice debt status changed to PartiallyPaid
        var debtRecords = await debtService.GetDebtRecordsAsync(new DebtFilterDto { SearchTerm = customer.Code });
        Assert.Single(debtRecords.Items);
        Assert.Equal(6000000m, debtRecords.Items[0].OutstandingAmount);
        Assert.Equal(4000000m, debtRecords.Items[0].PaidAmount);
        Assert.Equal(DebtStatus.PartiallyPaid, debtRecords.Items[0].DebtStatus);
    }

    [Fact]
    public async Task RecordDebtPayment_SupplierPayment_ReducesPayableAndCreatesPCSequence()
    {
        using var context = CreateInMemoryDbContext();
        var debtService = CreatePartnerDebtService(context);

        var supplier = new Supplier
        {
            Code = "NCC-PAY-01",
            Name = "Công ty TNHH Thép Việt Nhật",
            IsActive = true
        };
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync();

        // Record an initial payable increase via adjustment or legacy balance
        var adminUser = CreateAdminUser();
        var initialDebt = new PartnerPayment
        {
            PaymentCode = "DC-0001",
            PartnerType = (int)PartnerType.Supplier,
            PartnerId = supplier.Id,
            SupplierId = supplier.Id,
            TransactionType = DebtTransactionType.Adjustment,
            Amount = 25000000m,
            PaymentDate = DateTime.UtcNow.AddDays(-10),
            PaymentMethod = "Chứng từ kế toán",
            Notes = "Số dư nợ đầu kỳ nhà cung cấp"
        };
        context.PartnerPayments.Add(initialDebt);
        await context.SaveChangesAsync();

        // Record supplier payment of 10,000,000 VND
        var payDto = new RecordDebtPaymentDto
        {
            PartnerType = PartnerType.Supplier,
            PartnerId = supplier.Id,
            TransactionType = DebtTransactionType.Payment,
            Amount = 10000000m,
            PaymentDate = DateTime.UtcNow,
            PaymentMethod = "Chuyển khoản",
            Notes = "Thanh toán tiền mua thép đợt 1"
        };

        var payResult = await debtService.RecordPaymentAsync(payDto, adminUser);
        Assert.True(payResult.Succeeded);
        Assert.StartsWith("PC", payResult.PaymentCode);

        // Summary verify
        var summary = await debtService.GetDebtSummaryAsync();
        Assert.Equal(15000000m, summary.TotalSupplierPayables);
    }

    [Fact]
    public async Task RecordDebtPayment_CustomerDiscount_CreatesCKSequenceAndReducesReceivable()
    {
        using var context = CreateInMemoryDbContext();
        var debtService = CreatePartnerDebtService(context);

        var customer = new Customer
        {
            Code = "KH-DISC-01",
            Name = "Đại Lý Cấp 1 Sao Mai",
            IsActive = true
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var invoice = new Invoice
        {
            InvoiceNumber = "HD-DISC-001",
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-3),
            SubTotal = 50000000m,
            GrandTotal = 50000000m,
            PaymentMethod = "Công nợ",
            Status = InvoiceStatus.Issued
        };
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();

        // Record 1% payment discount = 500,000 VND
        var adminUser = CreateAdminUser();
        var discountDto = new RecordDebtPaymentDto
        {
            PartnerType = PartnerType.Customer,
            PartnerId = customer.Id,
            TransactionType = DebtTransactionType.PaymentDiscount,
            Amount = 500000m,
            PaymentDate = DateTime.UtcNow,
            Notes = "Chiết khấu thanh toán 1% do thanh toán trước hạn"
        };

        var result = await debtService.RecordPaymentAsync(discountDto, adminUser);
        Assert.True(result.Succeeded);
        Assert.StartsWith("CK", result.PaymentCode);

        var summary = await debtService.GetDebtSummaryAsync();
        Assert.Equal(49500000m, summary.TotalCustomerReceivables);
    }

    [Fact]
    public async Task RecordDebtPayment_Validation_RejectsNegativeOrZeroAmount()
    {
        using var context = CreateInMemoryDbContext();
        var debtService = CreatePartnerDebtService(context);
        var adminUser = CreateAdminUser();

        var zeroDto = new RecordDebtPaymentDto
        {
            PartnerType = PartnerType.Customer,
            PartnerId = 999,
            TransactionType = DebtTransactionType.Payment,
            Amount = 0m
        };

        var zeroResult = await debtService.RecordPaymentAsync(zeroDto, adminUser);
        Assert.False(zeroResult.Succeeded);
        Assert.Contains("lớn hơn 0", zeroResult.ErrorMessage);

        var negDto = new RecordDebtPaymentDto
        {
            PartnerType = PartnerType.Customer,
            PartnerId = 999,
            TransactionType = DebtTransactionType.Payment,
            Amount = -500000m
        };

        var negResult = await debtService.RecordPaymentAsync(negDto, adminUser);
        Assert.False(negResult.Succeeded);
        Assert.Contains("lớn hơn 0", negResult.ErrorMessage);
    }
}
