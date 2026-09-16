using System.Xml.Linq;
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

public class InvoiceTemplateTests
{
    private TradeFlowDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new TradeFlowDbContext(options);

        // Seed LACASA CompanySettings
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

    [Fact]
    public async Task CreateSyntheticInvoice_DecouplesInternalAndLegalIdentifiers()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("test-user");
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        var dto = new InvoiceDto
        {
            Type = InvoiceType.VatInvoice,
            InvoiceDate = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc),
            CustomerName = "Nguyễn Văn A",
            CustomerCompanyName = "Công ty TNHH Thương Mại Toàn Cầu",
            CustomerTaxCode = "0109876543",
            CustomerAddress = "456 Lê Duẩn, Hà Nội",
            CustomerEmail = "khachhang@toancau.vn",
            CustomerBankAccount = "987654321",
            CustomerBankName = "Techcombank",
            PaymentMethod = "TM/CK",
            SubTotal = 1000000,
            TotalTax = 80000,
            GrandTotal = 1080000,
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    ProductCode = "SP01",
                    ProductName = "Bàn ăn gỗ sồi LACASA",
                    UnitName = "Bộ",
                    Quantity = 1,
                    UnitPrice = 1000000,
                    TaxRate = 8,
                    TaxAmount = 80000,
                    LineTotal = 1000000
                }
            }
        };

        var created = await invoiceService.CreateManualInvoiceAsync(dto);

        // 1. Verify legal identifiers are decoupled from internal tracking
        Assert.NotNull(created);
        Assert.StartsWith("INV-", created.InvoiceNumber); // Internal tracking
        Assert.Equal("1", created.FormNumber);            // Mẫu số 1 (01/GTGT)
        Assert.Equal("1C26TFL", created.InvoiceSeries);   // Ký hiệu
        Assert.Equal("00000001", created.InvoiceNo);      // Số hóa đơn 8 chữ số
        Assert.Equal(DigitalSignatureStatus.Unsigned, created.SignatureStatus);
        Assert.Equal("LACASA", created.CompanyName);
        Assert.Equal("Vietcombank - CN Tân Bình", created.CompanyBankName);
        Assert.Equal("Techcombank", created.CustomerBankName);
    }

    [Fact]
    public async Task GenerateXml_MatchesElectronicInvoiceStructure()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("test-user");
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        var dto = new InvoiceDto
        {
            Type = InvoiceType.VatInvoice,
            InvoiceDate = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc),
            CustomerName = "Trần Thị B",
            CustomerCompanyName = "Công ty TNHH MTV Ánh Dương",
            CustomerTaxCode = "0314093206",
            CustomerAddress = "117 đường số 6, An Lạc, Bình Tân",
            CustomerEmail = "contact@anhduong.vn",
            CustomerBankAccount = "1020304050",
            CustomerBankName = "MBBank",
            PaymentMethod = "TM/CK",
            SubTotal = 2000000,
            TotalTax = 160000,
            GrandTotal = 2160000,
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    ProductCode = "CS01",
                    ProductName = "Ghế ăn cao cấp LACASA",
                    UnitName = "Cái",
                    Quantity = 2,
                    UnitPrice = 1000000,
                    TaxRate = 8,
                    TaxAmount = 160000,
                    LineTotal = 2000000
                }
            }
        };

        var created = await invoiceService.CreateManualInvoiceAsync(dto);

        // Sign invoice
        var signSuccess = await invoiceService.SignInvoiceAsync(created.Id);
        Assert.True(signSuccess);

        // Generate XML
        string xml = await invoiceService.GenerateXmlAsync(created.Id);
        Assert.False(string.IsNullOrWhiteSpace(xml));

        var xDoc = XDocument.Parse(xml);
        var root = xDoc.Root;
        Assert.NotNull(root);
        Assert.Equal("HDon", root.Name.LocalName);

        // Verify ND123 conceptual components
        var dlHDon = root.Element("DLHDon");
        Assert.NotNull(dlHDon);

        var ttChung = dlHDon.Element("TTChung");
        Assert.NotNull(ttChung);
        Assert.Equal("Hóa đơn giá trị gia tăng", ttChung.Element("THDon")?.Value);
        Assert.Equal("1", ttChung.Element("KHMSHDon")?.Value);
        Assert.Equal("1C26TFL", ttChung.Element("KHHDon")?.Value);
        Assert.Equal("00000001", ttChung.Element("SHDon")?.Value);

        var ndHDon = dlHDon.Element("NDHDon");
        Assert.NotNull(ndHDon);

        var nBan = ndHDon.Element("NBan");
        Assert.NotNull(nBan);
        Assert.Equal("LACASA", nBan.Element("Ten")?.Value);
        Assert.Equal("0319205643", nBan.Element("MST")?.Value);
        Assert.Equal("Vietcombank - CN Tân Bình", nBan.Element("TNHang")?.Value);

        var nMua = ndHDon.Element("NMua");
        Assert.NotNull(nMua);
        Assert.Equal("Trần Thị B", nMua.Element("Ten")?.Value);
        Assert.Equal("0314093206", nMua.Element("MST")?.Value);
        Assert.Equal("MBBank", nMua.Element("TNHang")?.Value);

        var dshhdvu = ndHDon.Element("DSHHDVu");
        Assert.NotNull(dshhdvu);
        var items = dshhdvu.Elements("HHDVu").ToList();
        Assert.Single(items);
        Assert.Equal("Ghế ăn cao cấp LACASA", items[0].Element("THHDVu")?.Value);
        Assert.Equal("8%", items[0].Element("TSuat")?.Value);

        var tToan = ndHDon.Element("TToan");
        Assert.NotNull(tToan);
        Assert.Equal("2000000", tToan.Element("TgTCThue")?.Value);
        Assert.Equal("160000", tToan.Element("TgTThue")?.Value);
        Assert.Equal("2160000", tToan.Element("TgTTTBSo")?.Value);
        Assert.Contains("đồng chẵn", tToan.Element("TgTTTBChu")?.Value);

        // Verify MCCQT, DLQRCode, DSCKS existence
        Assert.NotNull(root.Element("MCCQT"));
        Assert.NotNull(root.Element("DLQRCode"));
        var dscks = root.Element("DSCKS");
        Assert.NotNull(dscks);
        var sig = dscks.Element("NBan")?.Element(XName.Get("Signature", "http://www.w3.org/2000/09/xmldsig#"));
        Assert.NotNull(sig);
    }

    [Fact]
    public async Task GeneratePdf_SucceedsWithValidBytes()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("test-user");
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        var dto = new InvoiceDto
        {
            Type = InvoiceType.VatInvoice,
            InvoiceDate = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc),
            CustomerName = "Lê Văn C",
            CustomerCompanyName = "Công ty TNHH Kiến Trúc & Xây Dựng",
            CustomerTaxCode = "0301234567",
            CustomerAddress = "789 Điện Biên Phủ, Phường 25, Bình Thạnh",
            CustomerEmail = "kientruc@xaydung.com",
            PaymentMethod = "TM/CK",
            SubTotal = 5000000,
            TotalTax = 400000,
            GrandTotal = 5400000,
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    ProductCode = "SF01",
                    ProductName = "Sofa da thật LACASA Royal",
                    UnitName = "Bộ",
                    Quantity = 1,
                    UnitPrice = 5000000,
                    TaxRate = 8,
                    TaxAmount = 400000,
                    LineTotal = 5000000
                }
            }
        };

        var created = await invoiceService.CreateManualInvoiceAsync(dto);
        var pdfBytes = await invoiceService.GeneratePdfAsync(created.Id);

        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        // PDF header magic bytes: %PDF
        Assert.True(pdfBytes.Length > 100);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    [Fact]
    public async Task GenerateSyntheticInvoice_ExportsFiles_MatchesRequirements()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("test-user");
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        var dto = new InvoiceDto
        {
            Type = InvoiceType.VatInvoice,
            InvoiceDate = new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc),
            CustomerName = "Đặng Hoàng Long",
            CustomerCompanyName = "Công ty CP Đầu Tư & Phát Triển Nam Á",
            CustomerTaxCode = "0316789123",
            CustomerAddress = "120 Nguyễn Thị Minh Khai, Phường 6, Quận 3, TP Hồ Chí Minh",
            CustomerEmail = "long.dang@nama.com.vn",
            CustomerBankAccount = "0071001234567",
            CustomerBankName = "Vietcombank - CN Kỳ Đồng",
            PaymentMethod = "TM/CK",
            SubTotal = 15500000,
            TotalTax = 1240000,
            GrandTotal = 16740000,
            Items = new List<InvoiceItemDto>
            {
                new()
                {
                    SortOrder = 1,
                    ProductCode = "LC-BA01",
                    ProductName = "Bàn ăn tròn mặt đá cẩm thạch LACASA",
                    UnitName = "Cái",
                    Quantity = 1,
                    UnitPrice = 9500000,
                    TaxRate = 8,
                    TaxAmount = 760000,
                    LineTotal = 9500000
                },
                new()
                {
                    SortOrder = 2,
                    ProductCode = "LC-GA04",
                    ProductName = "Ghế ăn bọc da Microfiber cao cấp",
                    UnitName = "Chiếc",
                    Quantity = 4,
                    UnitPrice = 1500000,
                    TaxRate = 8,
                    TaxAmount = 480000,
                    LineTotal = 6000000
                }
            }
        };

        var invoice = await invoiceService.CreateManualInvoiceAsync(dto);
        Assert.NotNull(invoice);

        // Verify legal identifiers
        Assert.Equal("1", invoice.FormNumber);
        Assert.Equal("1C26TFL", invoice.InvoiceSeries);
        Assert.Equal("00000001", invoice.InvoiceNo);
        Assert.Equal(DigitalSignatureStatus.Unsigned, invoice.SignatureStatus);

        // Sign invoice
        var signed = await invoiceService.SignInvoiceAsync(invoice.Id, "LACASA");
        Assert.True(signed);

        // Export PDF
        byte[] pdfBytes = await invoiceService.GeneratePdfAsync(invoice.Id);
        Assert.NotEmpty(pdfBytes);

        // Export XML
        string xmlContent = await invoiceService.GenerateXmlAsync(invoice.Id);
        Assert.NotEmpty(xmlContent);

        // Save artifacts for verification inspection
        string outDir = Path.Combine(AppContext.BaseDirectory, "synthetic_test_output");
        Directory.CreateDirectory(outDir);
        File.WriteAllBytes(Path.Combine(outDir, "synthetic_invoice.pdf"), pdfBytes);
        File.WriteAllText(Path.Combine(outDir, "synthetic_invoice.xml"), xmlContent, System.Text.Encoding.UTF8);

        Assert.True(File.Exists(Path.Combine(outDir, "synthetic_invoice.pdf")));
        Assert.True(File.Exists(Path.Combine(outDir, "synthetic_invoice.xml")));
    }
}
