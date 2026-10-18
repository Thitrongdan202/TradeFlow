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

public class DebtReportingRedesignTests
{
    private TradeFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradeFlowDbContext(options);

        var company = new CompanySettings("CÔNG TY CỔ PHẦN THƯƠNG MẠI DỊCH VỤ TRADEFLOW")
        {
            TaxCode = "0319205643",
            Address = "Tòa nhà TradeFlow, 100 Nguyễn Thị Minh Khai, Q.3, TP.HCM",
            Phone = "02839998888"
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
            new("Permission", "Permission:Customers:View"),
            new("Permission", "Permission:Suppliers:View"),
            new("Permission", "Permission:Customers:Edit"),
            new("Permission", "Permission:Suppliers:Edit")
        };
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    private PartnerDebtService CreateDebtService(TradeFlowDbContext context)
    {
        var mockAuth = new Mock<IAuthorizationService>();
        mockAuth.Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var mockAudit = new Mock<IAuditService>();
        var exportService = new PartnerDebtExportService(
            context,
            mockAuth.Object,
            mockAudit.Object,
            new Mock<ILogger<PartnerDebtExportService>>().Object);

        var mockCurrentUser = new Mock<ICurrentUserService>();
        mockCurrentUser.Setup(u => u.UserId).Returns("admin-id");
        mockCurrentUser.Setup(u => u.UserName).Returns("admin");

        return new PartnerDebtService(
            context,
            exportService,
            mockAuth.Object,
            mockAudit.Object,
            mockCurrentUser.Object,
            new Mock<ILogger<PartnerDebtService>>().Object);
    }

    private static IXLRow GetFirstDataRow(IXLWorksheet ws)
    {
        var headerRow = ws.RowsUsed().First(r => r.Cell(1).GetString().Trim() == "STT");
        return ws.Row(headerRow.RowNumber() + 1);
    }

    private static IXLRow? FindDataRow(IXLWorksheet ws, int colIndex, string text)
    {
        return ws.RowsUsed().FirstOrDefault(r => r.Cell(colIndex).GetString().Contains(text));
    }

    // =========================================================================
    // SCENARIO 1: One customer with one outstanding invoice
    // =========================================================================
    [Fact]
    public async Task Scenario01_OneCustomer_OneOutstandingInvoice_ExportsAll4Sheets()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC01", Name = "Khách Hàng Đơn Lẻ", Phone = "0901112233", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var inv = new Invoice
        {
            InvoiceNumber = "HD-SC01-01",
            InvoiceNo = "00000101",
            CustomerId = cust.Id,
            CustomerName = cust.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            SubTotal = 10000000m,
            GrandTotal = 10000000m,
            PaymentMethod = "Công nợ 30 ngày",
            Status = InvoiceStatus.Pending
        };
        context.Invoices.Add(inv);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        Assert.NotNull(wb.Worksheet("Tổng hợp công nợ"));
        Assert.NotNull(wb.Worksheet("Chi tiết theo chứng từ"));
        Assert.NotNull(wb.Worksheet("Chi tiết từng mặt hàng"));
        Assert.NotNull(wb.Worksheet("Lịch sử thu, chi, điều chỉnh"));

        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var dataRow = GetFirstDataRow(wsB);
        Assert.Contains(inv.InvoiceNo, dataRow.Cell(6).GetString());
        Assert.Equal(10000000d, dataRow.Cell(10).GetDouble()); // Giá trị
        Assert.Equal(10000000d, dataRow.Cell(13).GetDouble()); // Còn nợ
    }

    // =========================================================================
    // SCENARIO 2: One customer with multiple outstanding invoices
    // =========================================================================
    [Fact]
    public async Task Scenario02_OneCustomer_MultipleInvoices_ListsEachInvoiceSeparatelyInSheetB()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC02", Name = "Công ty TNHH Đa Hóa Đơn", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var inv1 = new Invoice { InvoiceNumber = "HD-SC02-A", InvoiceNo = "00000201", CustomerId = cust.Id, CustomerName = cust.Name, InvoiceDate = DateTime.UtcNow.AddDays(-20), GrandTotal = 6000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending };
        var inv2 = new Invoice { InvoiceNumber = "HD-SC02-B", InvoiceNo = "00000202", CustomerId = cust.Id, CustomerName = cust.Name, InvoiceDate = DateTime.UtcNow.AddDays(-10), GrandTotal = 8000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending };
        context.Invoices.AddRange(inv1, inv2);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var r1 = FindDataRow(wsB, 6, "00000202");
        var r2 = FindDataRow(wsB, 6, "00000201");
        Assert.NotNull(r1);
        Assert.NotNull(r2);

        var totalRow = wsB.RowsUsed().Last();
        Assert.Equal(14000000d, totalRow.Cell(13).GetDouble());
    }

    // =========================================================================
    // SCENARIO 3: An invoice containing several product lines (Sheet C)
    // =========================================================================
    [Fact]
    public async Task Scenario03_InvoiceWithMultipleLines_SheetC_ItemizesLinesWithoutFabricatingAllocations()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC03", Name = "Xây Dựng Nam Hà", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var inv = new Invoice
        {
            InvoiceNumber = "HD-SC03-01",
            InvoiceNo = "00000301",
            CustomerId = cust.Id,
            CustomerName = cust.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-5),
            GrandTotal = 9900000m,
            PaymentMethod = "Công nợ",
            Status = InvoiceStatus.Pending
        };
        inv.Items.Add(new InvoiceItem { ProductCode = "SP-001", ProductName = "Sắt phi 10", UnitName = "Cây", Quantity = 50, UnitPrice = 100000m, LineTotal = 5000000m, SortOrder = 1 });
        inv.Items.Add(new InvoiceItem { ProductCode = "SP-002", ProductName = "Xi măng Hà Tiên", UnitName = "Bao", Quantity = 40, UnitPrice = 100000m, LineTotal = 4000000m, SortOrder = 2 });
        inv.Items.Add(new InvoiceItem { ProductCode = "SP-003", ProductName = "Gạch ống 4 lỗ", UnitName = "Viên", Quantity = 1000, UnitPrice = 900m, LineTotal = 900000m, SortOrder = 3 });
        context.Invoices.Add(inv);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsC = wb.Worksheet("Chi tiết từng mặt hàng");
        Assert.NotNull(wsC);

        // Verify allocation note banner is present in Sheet C
        var bannerRow = wsC.RowsUsed().FirstOrDefault(r => r.Cell(1).GetString().Contains("LƯU Ý KẾ TOÁN QUAN TRỌNG"));
        Assert.NotNull(bannerRow);

        // Verify all 3 product lines exist with exact data
        var sp1 = FindDataRow(wsC, 7, "SP-001");
        Assert.NotNull(sp1);
        Assert.Equal(50d, sp1.Cell(11).GetDouble()); // Qty
        Assert.Equal(5000000d, sp1.Cell(15).GetDouble()); // Line Total

        var sp2 = FindDataRow(wsC, 7, "SP-002");
        var sp3 = FindDataRow(wsC, 7, "SP-003");
        Assert.NotNull(sp2);
        Assert.NotNull(sp3);

        // Verify document-level settlement note is stated, not arbitrary line allocation
        Assert.Contains("Document-level settlement", sp1.Cell(18).GetString());
    }

    // =========================================================================
    // SCENARIO 4: An invoice with a partial payment
    // =========================================================================
    [Fact]
    public async Task Scenario04_InvoiceWithPartialPayment_CorrectlyReconcilesOriginalPaidAndRemaining()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC04", Name = "Công ty TNHH Hoàng Quân", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var inv = new Invoice
        {
            InvoiceNumber = "HD-SC04-01",
            InvoiceNo = "00000401",
            CustomerId = cust.Id,
            CustomerName = cust.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-15),
            GrandTotal = 20000000m,
            PaymentMethod = "Công nợ",
            Status = InvoiceStatus.Pending
        };
        context.Invoices.Add(inv);
        await context.SaveChangesAsync();

        // Partial payment of 8,000,000 VND
        var payment = new PartnerPayment
        {
            PaymentCode = "PT000041",
            PartnerType = (int)PartnerType.Customer,
            PartnerId = cust.Id,
            CustomerId = cust.Id,
            TransactionType = DebtTransactionType.Payment,
            Amount = 8000000m,
            PaymentDate = DateTime.UtcNow.AddDays(-2),
            InvoiceId = inv.Id,
            PaymentMethod = "Chuyển khoản BIDV"
        };
        context.PartnerPayments.Add(payment);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var docRow = GetFirstDataRow(wsB);
        Assert.Equal(20000000d, docRow.Cell(10).GetDouble()); // Original
        Assert.Equal(8000000d, docRow.Cell(11).GetDouble());  // Paid
        Assert.Equal(12000000d, docRow.Cell(13).GetDouble()); // Remaining
        Assert.Equal("Đã thanh toán một phần", docRow.Cell(14).GetString());

        var wsD = wb.Worksheet("Lịch sử thu, chi, điều chỉnh");
        var txRow = GetFirstDataRow(wsD);
        Assert.Contains("PT000041", txRow.Cell(3).GetString());
        Assert.Equal(8000000d, txRow.Cell(9).GetDouble());
    }

    // =========================================================================
    // SCENARIO 5: An invoice that is fully settled
    // =========================================================================
    [Fact]
    public async Task Scenario05_FullySettledInvoice_ReportsZeroRemainingAndSettledStatus()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC05", Name = "Cửa hàng Kim Khí", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var inv = new Invoice
        {
            InvoiceNumber = "HD-SC05-01",
            InvoiceNo = "00000501",
            CustomerId = cust.Id,
            CustomerName = cust.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            GrandTotal = 5000000m,
            PaymentMethod = "Công nợ",
            Status = InvoiceStatus.Issued
        };
        context.Invoices.Add(inv);

        var payment = new PartnerPayment
        {
            PaymentCode = "PT000051",
            PartnerType = (int)PartnerType.Customer,
            PartnerId = cust.Id,
            CustomerId = cust.Id,
            TransactionType = DebtTransactionType.Payment,
            Amount = 5000000m,
            PaymentDate = DateTime.UtcNow.AddDays(-1),
            InvoiceId = inv.Id
        };
        context.PartnerPayments.Add(payment);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var docRow = GetFirstDataRow(wsB);
        Assert.Equal(5000000d, docRow.Cell(10).GetDouble());
        Assert.Equal(5000000d, docRow.Cell(11).GetDouble());
        Assert.Equal(0d, docRow.Cell(13).GetDouble());
        Assert.Equal("Đã thanh toán đủ", docRow.Cell(14).GetString());
    }

    // =========================================================================
    // SCENARIO 6: A customer with an applicable discount or debt adjustment
    // =========================================================================
    [Fact]
    public async Task Scenario06_CustomerDiscountOrAdjustment_DistinguishedInSheetDAndSheetA()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC06", Name = "Tổng Thầu Mekong", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var inv = new Invoice
        {
            InvoiceNumber = "HD-SC06-01",
            InvoiceNo = "00000601",
            CustomerId = cust.Id,
            CustomerName = cust.Name,
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            GrandTotal = 30000000m,
            PaymentMethod = "Công nợ",
            Status = InvoiceStatus.Pending
        };
        context.Invoices.Add(inv);

        // Payment 20,000,000 + Discount 1,000,000
        var pay = new PartnerPayment
        {
            PaymentCode = "PT000061",
            PartnerType = (int)PartnerType.Customer,
            PartnerId = cust.Id,
            CustomerId = cust.Id,
            TransactionType = DebtTransactionType.Payment,
            Amount = 20000000m,
            PaymentDate = DateTime.UtcNow.AddDays(-3),
            InvoiceId = inv.Id
        };
        var disc = new PartnerPayment
        {
            PaymentCode = "CK000062",
            PartnerType = (int)PartnerType.Customer,
            PartnerId = cust.Id,
            CustomerId = cust.Id,
            TransactionType = DebtTransactionType.PaymentDiscount,
            Amount = 1000000m,
            PaymentDate = DateTime.UtcNow.AddDays(-3),
            InvoiceId = inv.Id,
            Notes = "Chiết khấu thanh toán trước hạn"
        };
        context.PartnerPayments.AddRange(pay, disc);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsD = wb.Worksheet("Lịch sử thu, chi, điều chỉnh");
        Assert.NotNull(wsD);

        // Distinct transactions in Sheet D
        var ptRow = FindDataRow(wsD, 3, "PT000061");
        var ckRow = FindDataRow(wsD, 3, "CK000062");
        Assert.NotNull(ptRow);
        Assert.NotNull(ckRow);
        Assert.Contains("Phiếu thu tiền (Thu nợ KH)", ptRow.Cell(2).GetString());
        Assert.Contains("Chiết khấu thanh toán KH", ckRow.Cell(2).GetString());
        Assert.Equal(1000000d, ckRow.Cell(9).GetDouble());

        // Net Closing Balance should be 30M - 20M - 1M = 9,000,000 VND
        var wsA = wb.Worksheet("Tổng hợp công nợ");
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var docRow = GetFirstDataRow(wsB);
        Assert.Equal(9000000d, docRow.Cell(13).GetDouble());
    }

    // =========================================================================
    // SCENARIO 7: A supplier with multiple outstanding purchase documents / adjustments
    // =========================================================================
    [Fact]
    public async Task Scenario07_SupplierWithMultipleObligations_DisplaysInSheetBAsPayables()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var sup = new Supplier { Code = "NCC-SC07", Name = "Thép Hòa Phát Cần Thơ", IsActive = true };
        context.Suppliers.Add(sup);
        await context.SaveChangesAsync();

        var adj1 = new PartnerPayment { PaymentCode = "DC000701", PartnerType = (int)PartnerType.Supplier, PartnerId = sup.Id, SupplierId = sup.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 40000000m, PaymentDate = DateTime.UtcNow.AddDays(-20) };
        var adj2 = new PartnerPayment { PaymentCode = "DC000702", PartnerType = (int)PartnerType.Supplier, PartnerId = sup.Id, SupplierId = sup.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 60000000m, PaymentDate = DateTime.UtcNow.AddDays(-10) };
        context.PartnerPayments.AddRange(adj1, adj2);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Supplier,
            PartnerIds = new List<int> { sup.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        Assert.NotNull(wsB);
        var docRow = GetFirstDataRow(wsB);
        Assert.Equal("Nợ NCC (Phải trả)", docRow.Cell(2).GetString());
        var totalRow = wsB.RowsUsed().Last();
        Assert.Equal(100000000d, totalRow.Cell(13).GetDouble()); // Total Remaining
    }

    // =========================================================================
    // SCENARIO 8: A supplier with a partial payment
    // =========================================================================
    [Fact]
    public async Task Scenario08_SupplierPartialPayment_ReducesPayableBalanceAccurately()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var sup = new Supplier { Code = "NCC-SC08", Name = "Nhựa Tiền Phong Miền Nam", IsActive = true };
        context.Suppliers.Add(sup);

        var debt = new PartnerPayment { PaymentCode = "DC000801", PartnerType = (int)PartnerType.Supplier, PartnerId = sup.Id, SupplierId = sup.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 50000000m, PaymentDate = DateTime.UtcNow.AddDays(-15) };
        var payment = new PartnerPayment { PaymentCode = "PC000802", PartnerType = (int)PartnerType.Supplier, PartnerId = sup.Id, SupplierId = sup.Id, TransactionType = DebtTransactionType.Payment, Amount = 20000000m, PaymentDate = DateTime.UtcNow.AddDays(-5), PaymentMethod = "Chuyển khoản Vietcombank" };
        context.PartnerPayments.AddRange(debt, payment);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Supplier,
            PartnerIds = new List<int> { sup.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var docRow = GetFirstDataRow(wsB);
        Assert.Equal(50000000d, docRow.Cell(10).GetDouble()); // Original
        Assert.Equal(20000000d, docRow.Cell(11).GetDouble()); // Paid
        Assert.Equal(30000000d, docRow.Cell(13).GetDouble()); // Remaining
        Assert.Equal("Đã thanh toán một phần", docRow.Cell(14).GetString());
    }

    // =========================================================================
    // SCENARIO 9: A supplier with a fully settled payable
    // =========================================================================
    [Fact]
    public async Task Scenario09_SupplierFullySettled_ReconcilesToZeroPayable()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var sup = new Supplier { Code = "NCC-SC09", Name = "Công ty TNHH Sơn Dulux", IsActive = true };
        context.Suppliers.Add(sup);

        var debt = new PartnerPayment { PaymentCode = "DC000901", PartnerType = (int)PartnerType.Supplier, PartnerId = sup.Id, SupplierId = sup.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 15000000m, PaymentDate = DateTime.UtcNow.AddDays(-20) };
        var pay = new PartnerPayment { PaymentCode = "PC000902", PartnerType = (int)PartnerType.Supplier, PartnerId = sup.Id, SupplierId = sup.Id, TransactionType = DebtTransactionType.Payment, Amount = 15000000m, PaymentDate = DateTime.UtcNow.AddDays(-2) };
        context.PartnerPayments.AddRange(debt, pay);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Supplier,
            PartnerIds = new List<int> { sup.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var docRow = GetFirstDataRow(wsB);
        Assert.Equal(0d, docRow.Cell(13).GetDouble());
        Assert.Equal("Đã thanh toán đủ", docRow.Cell(14).GetString());
    }

    // =========================================================================
    // SCENARIO 10: Relevant cancelled, reversed, or corrected records excluded
    // =========================================================================
    [Fact]
    public async Task Scenario10_CancelledInvoices_AreStrictlyExcludedFromDebtCalculationsAndExports()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC10", Name = "Khách Hàng Hủy Hóa Đơn", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        // Active invoice: 10M
        var activeInv = new Invoice { InvoiceNumber = "HD-SC10-ACTIVE", InvoiceNo = "00001001", CustomerId = cust.Id, CustomerName = cust.Name, InvoiceDate = DateTime.UtcNow, GrandTotal = 10000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending };
        // Cancelled invoice: 50M (MUST NOT BE INCLUDED)
        var cancelledInv = new Invoice { InvoiceNumber = "HD-SC10-CANCEL", InvoiceNo = "00001002", CustomerId = cust.Id, CustomerName = cust.Name, InvoiceDate = DateTime.UtcNow, GrandTotal = 50000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Cancelled };
        context.Invoices.AddRange(activeInv, cancelledInv);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");

        // Only 1 document row should exist (activeInv), total should be 10,000,000 NOT 60,000,000
        var activeRow = FindDataRow(wsB, 6, "00001001");
        var cancelledRow = FindDataRow(wsB, 6, "00001002");
        Assert.NotNull(activeRow);
        Assert.Null(cancelledRow);
        var totalRow = wsB.RowsUsed().Last();
        Assert.Equal(10000000d, totalRow.Cell(13).GetDouble()); // Total Row
    }

    // =========================================================================
    // SCENARIO 11: Multiple customers and suppliers with overlapping names/tax codes
    // =========================================================================
    [Fact]
    public async Task Scenario11_OverlappingTaxCodesAndNames_PreservesDualRoleSeparation()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        // Same tax code and similar name as Customer AND Supplier (Dual-role entity)
        var cust = new Customer { Code = "KH-SC11", Name = "Tập Đoàn Tân Á Đại Thành", TaxCode = "0100998877", Phone = "0988776655", IsActive = true };
        var sup = new Supplier { Code = "NCC-SC11", Name = "Tập Đoàn Tân Á Đại Thành", TaxCode = "0100998877", Phone = "0988776655", IsActive = true };
        context.Customers.Add(cust);
        context.Suppliers.Add(sup);
        await context.SaveChangesAsync();

        // Customer receivable: 7,000,000
        var inv = new Invoice { InvoiceNumber = "HD-SC11-01", InvoiceNo = "00001101", CustomerId = cust.Id, CustomerName = cust.Name, InvoiceDate = DateTime.UtcNow, GrandTotal = 7000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending };
        context.Invoices.Add(inv);

        // Supplier payable: 12,000,000
        var supDebt = new PartnerPayment { PaymentCode = "DC001101", PartnerType = (int)PartnerType.Supplier, PartnerId = sup.Id, SupplierId = sup.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 12000000m, PaymentDate = DateTime.UtcNow };
        context.PartnerPayments.Add(supDebt);
        await context.SaveChangesAsync();

        var bytes = await service.ExportDebtManagementWorkbookAsync(new DebtManagementExportRequest
        {
            Scope = DebtExportScope.AllFiltered
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsA = wb.Worksheet("Tổng hợp công nợ");
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");

        // Must show both distinctly with their own codes and directions
        var custRow = FindDataRow(wsA, 3, "KH-SC11");
        var supRow = FindDataRow(wsA, 3, "NCC-SC11");
        Assert.NotNull(custRow);
        Assert.NotNull(supRow);
        Assert.Equal("Khách hàng nợ (Phải thu)", custRow.Cell(2).GetString());
        Assert.Equal("Nợ NCC (Phải trả)", supRow.Cell(2).GetString());
    }

    // =========================================================================
    // SCENARIO 12: Multiple companies / branches branding
    // =========================================================================
    [Fact]
    public async Task Scenario12_CompanyBrandingAndMetadata_AccuratelyRenderedInAllSheets()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var cust = new Customer { Code = "KH-SC12", Name = "Công ty TNHH Hải Nam", IsActive = true };
        context.Customers.Add(cust);
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { cust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        foreach (var ws in wb.Worksheets)
        {
            Assert.Contains("CÔNG TY CỔ PHẦN THƯƠNG MẠI DỊCH VỤ TRADEFLOW", ws.Cell(1, 1).GetString());
            Assert.Contains("0319205643", ws.Cell(2, 1).GetString());
        }
    }

    // =========================================================================
    // SCENARIO 13: A filtered report and a paginated screen (All records exported)
    // =========================================================================
    [Fact]
    public async Task Scenario13_PaginatedData_ExportsAllMatchingRecordsNotJustFirstPage()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        // Seed 35 customers with debt (screen page size = 20)
        for (int i = 1; i <= 35; i++)
        {
            var c = new Customer { Code = $"KH-PAG-{i:D3}", Name = $"Khách Hàng Phân Trang {i}", IsActive = true };
            context.Customers.Add(c);
        }
        await context.SaveChangesAsync();

        var allCusts = await context.Customers.ToListAsync();
        foreach (var c in allCusts)
        {
            context.Invoices.Add(new Invoice
            {
                InvoiceNumber = $"HD-{c.Code}",
                InvoiceNo = $"INV-{c.Id}",
                CustomerId = c.Id,
                CustomerName = c.Name,
                InvoiceDate = DateTime.UtcNow,
                GrandTotal = 1000000m,
                PaymentMethod = "Công nợ",
                Status = InvoiceStatus.Pending
            });
        }
        await context.SaveChangesAsync();

        // Export All Filtered
        var bytes = await service.ExportDebtManagementWorkbookAsync(new DebtManagementExportRequest
        {
            Scope = DebtExportScope.AllFiltered
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");

        // Total documents in Sheet B must be all 35, NOT merely 20 from page 1!
        var docRows = wsB.RowsUsed().Where(r => r.Cell(1).GetString() != "STT" && int.TryParse(r.Cell(1).GetString(), out _)).ToList();
        Assert.Equal(35, docRows.Count);
    }

    // =========================================================================
    // SCENARIO 14: Export of one customer excludes unrelated customers
    // =========================================================================
    [Fact]
    public async Task Scenario14_SingleCustomerExport_StrictlyExcludesUnrelatedCustomers()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var targetCust = new Customer { Code = "KH-TARGET", Name = "Khách Hàng Mục Tiêu", IsActive = true };
        var otherCust = new Customer { Code = "KH-OTHER", Name = "Khách Hàng Khác", IsActive = true };
        context.Customers.AddRange(targetCust, otherCust);
        await context.SaveChangesAsync();

        context.Invoices.Add(new Invoice { InvoiceNumber = "HD-TARGET", InvoiceNo = "00001401", CustomerId = targetCust.Id, CustomerName = targetCust.Name, InvoiceDate = DateTime.UtcNow, GrandTotal = 1000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending });
        context.Invoices.Add(new Invoice { InvoiceNumber = "HD-OTHER", InvoiceNo = "00001402", CustomerId = otherCust.Id, CustomerName = otherCust.Name, InvoiceDate = DateTime.UtcNow, GrandTotal = 9000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending });
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Customer,
            PartnerIds = new List<int> { targetCust.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        Assert.NotNull(FindDataRow(wsB, 3, "KH-TARGET"));
        Assert.Null(FindDataRow(wsB, 3, "KH-OTHER"));
    }

    // =========================================================================
    // SCENARIO 15: Export of one supplier excludes unrelated suppliers
    // =========================================================================
    [Fact]
    public async Task Scenario15_SingleSupplierExport_StrictlyExcludesUnrelatedSuppliers()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var targetSup = new Supplier { Code = "NCC-TARGET", Name = "Nhà Cung Cấp Mục Tiêu", IsActive = true };
        var otherSup = new Supplier { Code = "NCC-OTHER", Name = "Nhà Cung Cấp Khác", IsActive = true };
        context.Suppliers.AddRange(targetSup, otherSup);
        await context.SaveChangesAsync();

        context.PartnerPayments.Add(new PartnerPayment { PaymentCode = "DC-TARGET", PartnerType = (int)PartnerType.Supplier, PartnerId = targetSup.Id, SupplierId = targetSup.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 2000000m, PaymentDate = DateTime.UtcNow });
        context.PartnerPayments.Add(new PartnerPayment { PaymentCode = "DC-OTHER", PartnerType = (int)PartnerType.Supplier, PartnerId = otherSup.Id, SupplierId = otherSup.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 8000000m, PaymentDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var bytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
        {
            Scope = DebtExportScope.CurrentPartner,
            PartnerType = PartnerType.Supplier,
            PartnerIds = new List<int> { targetSup.Id }
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        Assert.NotNull(FindDataRow(wsB, 3, "NCC-TARGET"));
        Assert.Null(FindDataRow(wsB, 3, "NCC-OTHER"));
    }

    // =========================================================================
    // SCENARIO 16: Consolidated export includes all records matching filters
    // =========================================================================
    [Fact]
    public async Task Scenario16_ConsolidatedExport_IncludesAllFilteredRecordsMatchingCriteria()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        var c1 = new Customer { Code = "KH-C1", Name = "Khách Hàng Một", IsActive = true };
        var c2 = new Customer { Code = "KH-C2", Name = "Khách Hàng Hai", IsActive = true };
        var s1 = new Supplier { Code = "NCC-S1", Name = "Nhà Cung Cấp Một", IsActive = true };
        context.Customers.AddRange(c1, c2);
        context.Suppliers.Add(s1);
        await context.SaveChangesAsync();

        context.Invoices.Add(new Invoice { InvoiceNumber = "HD-C1", InvoiceNo = "00001601", CustomerId = c1.Id, CustomerName = c1.Name, InvoiceDate = DateTime.UtcNow, GrandTotal = 3000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending });
        context.Invoices.Add(new Invoice { InvoiceNumber = "HD-C2", InvoiceNo = "00001602", CustomerId = c2.Id, CustomerName = c2.Name, InvoiceDate = DateTime.UtcNow, GrandTotal = 4000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending });
        context.PartnerPayments.Add(new PartnerPayment { PaymentCode = "DC-S1", PartnerType = (int)PartnerType.Supplier, PartnerId = s1.Id, SupplierId = s1.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 5000000m, PaymentDate = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var bytes = await service.ExportDebtManagementWorkbookAsync(new DebtManagementExportRequest
        {
            Scope = DebtExportScope.AllFiltered
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsA = wb.Worksheet("Tổng hợp công nợ");
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");

        var docRows = wsB.RowsUsed().Where(r => r.Cell(1).GetString() != "STT" && int.TryParse(r.Cell(1).GetString(), out _)).ToList();
        Assert.Equal(3, docRows.Count); // 3 documents
    }

    // =========================================================================
    // SCENARIO 17: Reconciliation between exported totals and TradeFlow UI balances
    // =========================================================================
    [Fact]
    public async Task Scenario17_FullReconciliation_ExportedTotalsMatchUISummaryAndRecords100Percent()
    {
        using var context = CreateInMemoryDbContext();
        var service = CreateDebtService(context);

        // Setup complex scenario with customers, suppliers, partial payments, discounts
        var cust1 = new Customer { Code = "KH-REC-01", Name = "Doanh nghiệp Hoàng Gia", IsActive = true };
        var cust2 = new Customer { Code = "KH-REC-02", Name = "Công ty CP Đại Dương", IsActive = true };
        var sup1 = new Supplier { Code = "NCC-REC-01", Name = "Tập Đoàn Thép Sông Gianh", IsActive = true };
        context.Customers.AddRange(cust1, cust2);
        context.Suppliers.Add(sup1);
        await context.SaveChangesAsync();

        // Cust 1: 15M invoice, 5M paid, 1M discount => Remaining 9M
        var inv1 = new Invoice { InvoiceNumber = "HD-REC-01", InvoiceNo = "00001701", CustomerId = cust1.Id, CustomerName = cust1.Name, InvoiceDate = DateTime.UtcNow.AddDays(-5), GrandTotal = 15000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending };
        context.Invoices.Add(inv1);
        context.PartnerPayments.Add(new PartnerPayment { PaymentCode = "PT-REC-01", PartnerType = (int)PartnerType.Customer, PartnerId = cust1.Id, CustomerId = cust1.Id, TransactionType = DebtTransactionType.Payment, Amount = 5000000m, PaymentDate = DateTime.UtcNow.AddDays(-3), InvoiceId = inv1.Id });
        context.PartnerPayments.Add(new PartnerPayment { PaymentCode = "CK-REC-01", PartnerType = (int)PartnerType.Customer, PartnerId = cust1.Id, CustomerId = cust1.Id, TransactionType = DebtTransactionType.PaymentDiscount, Amount = 1000000m, PaymentDate = DateTime.UtcNow.AddDays(-3), InvoiceId = inv1.Id });

        // Cust 2: 8M invoice, 0 paid => Remaining 8M
        var inv2 = new Invoice { InvoiceNumber = "HD-REC-02", InvoiceNo = "00001702", CustomerId = cust2.Id, CustomerName = cust2.Name, InvoiceDate = DateTime.UtcNow.AddDays(-2), GrandTotal = 8000000m, PaymentMethod = "Công nợ", Status = InvoiceStatus.Pending };
        context.Invoices.Add(inv2);

        // Sup 1: 20M adjustment obligation, 6M payment => Remaining 14M
        context.PartnerPayments.Add(new PartnerPayment { PaymentCode = "DC-REC-01", PartnerType = (int)PartnerType.Supplier, PartnerId = sup1.Id, SupplierId = sup1.Id, TransactionType = DebtTransactionType.Adjustment, Amount = 20000000m, PaymentDate = DateTime.UtcNow.AddDays(-10) });
        context.PartnerPayments.Add(new PartnerPayment { PaymentCode = "PC-REC-01", PartnerType = (int)PartnerType.Supplier, PartnerId = sup1.Id, SupplierId = sup1.Id, TransactionType = DebtTransactionType.Payment, Amount = 6000000m, PaymentDate = DateTime.UtcNow.AddDays(-1) });
        await context.SaveChangesAsync();

        // 1. Get Live UI Debt Summary
        var uiSummary = await service.GetDebtSummaryAsync();
        Assert.Equal(17000000m, uiSummary.TotalCustomerReceivables); // 9M + 8M = 17M
        Assert.Equal(14000000m, uiSummary.TotalSupplierPayables);   // 20M - 6M = 14M
        Assert.Equal(3000000m, uiSummary.NetDifference);            // 17M - 14M = 3M
        Assert.Equal(2, uiSummary.CustomersWithDebtCount);
        Assert.Equal(1, uiSummary.SuppliersWithDebtCount);

        // 2. Export Management Workbook and Verify Exact Match
        var bytes = await service.ExportDebtManagementWorkbookAsync(new DebtManagementExportRequest
        {
            Scope = DebtExportScope.AllFiltered
        }, CreateAdminUser());

        using var wb = new XLWorkbook(new MemoryStream(bytes));
        var wsA = wb.Worksheet("Tổng hợp công nợ");

        // Check Executive KPI cells in Sheet A
        var kpi1 = FindDataRow(wsA, 1, "Tổng công nợ phải thu");
        var kpi2 = FindDataRow(wsA, 1, "Tổng công nợ phải trả");
        var kpi3 = FindDataRow(wsA, 1, "Chênh lệch công nợ");
        Assert.NotNull(kpi1);
        Assert.NotNull(kpi2);
        Assert.NotNull(kpi3);
        Assert.Equal(17000000d, kpi1.Cell(3).GetDouble()); // Tổng phải thu KH
        Assert.Equal(14000000d, kpi2.Cell(3).GetDouble()); // Tổng phải trả NCC
        Assert.Equal(3000000d, kpi3.Cell(3).GetDouble());  // Chênh lệch đối soát

        // Check Sheet B total remaining balances
        var wsB = wb.Worksheet("Chi tiết theo chứng từ");
        var wsBReceivablesRemaining = wsB.RowsUsed().Skip(3).Where(r => r.Cell(2).GetString().Contains("Phải thu")).Sum(r => r.Cell(13).GetDouble());
        var wsBPayablesRemaining = wsB.RowsUsed().Skip(3).Where(r => r.Cell(2).GetString().Contains("Phải trả")).Sum(r => r.Cell(13).GetDouble());

        Assert.Equal(17000000d, wsBReceivablesRemaining);
        Assert.Equal(14000000d, wsBPayablesRemaining);

        // 100% Reconciliation Confirmed!

        // Save sample workbooks to artifact directory for user audit
        string artifactDir = @"C:\Users\thitr\.gemini\antigravity\brain\fb94d497-815d-460b-bd53-9de32ef4d901";
        if (Directory.Exists(artifactDir))
        {
            File.WriteAllBytes(Path.Combine(artifactDir, "debt_management_report_sample.xlsx"), bytes);

            var statementBytes = await service.ExportPartnerDebtStatementAsync(new PartnerDebtStatementExportRequest
            {
                Scope = DebtExportScope.CurrentPartner,
                PartnerType = PartnerType.Customer,
                PartnerIds = new List<int> { cust1.Id }
            }, CreateAdminUser());
            File.WriteAllBytes(Path.Combine(artifactDir, "partner_debt_statement_sample.xlsx"), statementBytes);
        }
    }
}
