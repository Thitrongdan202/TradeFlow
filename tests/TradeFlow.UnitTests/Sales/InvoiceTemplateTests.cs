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

    [Fact]
    public async Task GenerateOrderDocument_FromSalesOrder_MatchesLacasaTemplate()
    {
        using var context = CreateInMemoryDbContext();
        var company = await context.CompanySettings.FirstAsync();
        company.CompanyName = "TỔNG KHO THIẾT BỊ VỆ SINH LACASA";
        company.Email = "tongkhothietbivesinh@gmail.com";
        company.OrderHotline = "0369.074.789 - Hotline";
        company.BankAccountHolder = "TRẦN VĂN TUẤN";
        company.BankAccount = "4987.9177";
        company.BankName = "NGÂN HÀNG Á CHÂU (ACB)";
        company.DefaultVatNote = "Đơn giá trên chưa bao gồm thuế GTGT (8%).";
        await context.SaveChangesAsync();

        var category1 = new ProductCategory { Name = "LAVABO ĐỂ BÀN" };
        var category2 = new ProductCategory { Name = "BỒN CẦU 1 KHỐI" };
        context.ProductCategories.AddRange(category1, category2);
        await context.SaveChangesAsync();

        var prod1 = new Product
        {
            Code = "LVB205-LS",
            Name = "Lavabo Cơm Hươu",
            Specifications = "Lavabo Cơm Hươu- Viền Đen, Vuông, Dài Bàn, 490x370x130",
            CategoryId = category1.Id,
            Category = category1
        };
        var prod2 = new Product
        {
            Code = "TL2138 (K8012)",
            Name = "Bồn Cầu Trắng",
            Specifications = "Bồn Cầu Trắng, Xả Mưa, 680x370x700",
            CategoryId = category2.Id,
            Category = category2
        };
        context.Products.AddRange(prod1, prod2);
        await context.SaveChangesAsync();

        var customer = new Customer
        {
            Name = "C11 Thiên Định-Chị Ngân-Cần Thơ",
            Phone = "0901482169",
            Address = "136E2/14 Lê Phước Thọ ( Vòng Xoay Võ Văn Kiệt) - Phường Long Hòa - Quận Bình Thủy, Cần Thơ"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();

        var order = new SalesOrder
        {
            Code = "DH000578",
            OrderDate = new DateTime(2026, 9, 16),
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            CustomerAddress = customer.Address,
            Notes = "Giao hàng giờ hành chính",
            Items = new List<SalesOrderItem>
            {
                new SalesOrderItem
                {
                    ProductId = prod1.Id,
                    Product = prod1,
                    ProductCode = prod1.Code,
                    ProductName = prod1.Name,
                    Quantity = 2,
                    UnitPrice = 425000,
                    DiscountAmount = 0,
                    LineTotal = 850000
                },
                new SalesOrderItem
                {
                    ProductId = prod2.Id,
                    Product = prod2,
                    ProductCode = prod2.Code,
                    ProductName = prod2.Name,
                    Quantity = 2,
                    UnitPrice = 1190000,
                    DiscountAmount = 0,
                    LineTotal = 2380000
                }
            }
        };
        context.SalesOrders.Add(order);
        await context.SaveChangesAsync();

        var mockUser = new Mock<ICurrentUserService>();
        var mockAudit = new Mock<IAuditService>();
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        var doc = await invoiceService.GetOrderDocumentByOrderIdAsync(order.Id);
        Assert.NotNull(doc);
        Assert.Equal("DH000578", doc.OrderCode);
        Assert.Equal("TỔNG KHO THIẾT BỊ VỆ SINH LACASA", doc.CompanyName);
        Assert.Equal("tongkhothietbivesinh@gmail.com", doc.Email);
        Assert.Equal("0369.074.789 - Hotline", doc.Hotline);
        Assert.Equal("C11 Thiên Định-Chị Ngân-Cần Thơ", doc.CustomerName);
        Assert.Equal("0901482169", doc.CustomerPhone);
        Assert.Equal("TRẦN VĂN TUẤN", doc.BankAccountHolder);
        Assert.Equal("4987.9177", doc.BankAccount);
        Assert.Equal("NGÂN HÀNG Á CHÂU (ACB)", doc.BankName);

        // Verify items
        Assert.Equal(2, doc.Items.Count);
        Assert.Equal(1, doc.Items[0].No);
        Assert.Equal("LAVABO ĐỂ BÀN", doc.Items[0].CategoryName);
        Assert.Equal("LVB205-LS", doc.Items[0].ProductCode);
        Assert.Equal(2, doc.Items[0].Quantity);
        Assert.Equal(425000, doc.Items[0].UnitPrice);
        Assert.Equal(850000, doc.Items[0].LineTotal);

        Assert.Equal(2, doc.Items[1].No);
        Assert.Equal("BỒN CẦU 1 KHỐI", doc.Items[1].CategoryName);
        Assert.Equal("TL2138 (K8012)", doc.Items[1].ProductCode);
        Assert.Equal(2, doc.Items[1].Quantity);
        Assert.Equal(1190000, doc.Items[1].UnitPrice);
        Assert.Equal(2380000, doc.Items[1].LineTotal);

        // Verify totals
        Assert.Equal(4, doc.TotalQuantity);
        Assert.Equal(3230000, doc.TotalAmount);
        Assert.Equal(3230000, doc.GrandTotal);

        // Generate PDF
        byte[] pdfBytes = await invoiceService.GenerateOrderDocumentPdfAsync(doc);
        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 1000);

        string targetPdf = @"C:\Users\thitr\.gemini\antigravity\brain\fb94d497-815d-460b-bd53-9de32ef4d901\synthetic_order_document.pdf";
        await File.WriteAllBytesAsync(targetPdf, pdfBytes);
    }

    [Fact]
    public async Task Phase5_SalesOrderToInvoice_PriceAndDiscountCalculations_MatchScenario()
    {
        using var context = CreateInMemoryDbContext();
        var mockUser = new Mock<ICurrentUserService>();
        mockUser.Setup(u => u.UserId).Returns("test-user");
        var mockAudit = new Mock<IAuditService>();
        var salesService = new SalesService(context, mockUser.Object, mockAudit.Object);
        var sigService = new DigitalSignatureService(context);
        var invoiceService = new InvoiceService(context, mockUser.Object, mockAudit.Object, sigService);

        // 1. Setup Customer & Product
        var customer = new Customer
        {
            Code = "KH-TEST",
            Name = "Khách Hàng Thử Nghiệm",
            TaxCode = "0109876543",
            Address = "123 Phố Huế, Hà Nội",
            Phone = "0987654321"
        };
        context.Customers.Add(customer);

        var product = new Product
        {
            Code = "TL2138",
            Name = "Bồn Cầu Trắng 1 Khối",
            IsActive = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        // 2. Setup PriceList (Standard selling price before VAT = 1.190.000)
        var priceList = new Domain.Entities.Pricing.PriceList
        {
            Code = "PL-2026",
            Status = PriceListStatus.Active,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            Items = new List<Domain.Entities.Pricing.PriceListItem>
            {
                new()
                {
                    ProductId = product.Id,
                    UnitPrice = 1190000m,
                    VatRate = 8m
                }
            }
        };
        context.PriceLists.Add(priceList);
        await context.SaveChangesAsync();

        // 3. Create Sales Order with Qty = 2, UnitPrice = 1.190.000, DiscountRate = 5%
        var orderDto = new SalesOrderDto
        {
            CustomerId = customer.Id,
            CustomerName = customer.Name,
            CustomerPhone = customer.Phone,
            CustomerAddress = customer.Address,
            OrderDate = DateTime.UtcNow,
            Notes = "Đơn hàng test chiết khấu 5%",
            Items = new List<SalesOrderItemDto>
            {
                new()
                {
                    ProductId = product.Id,
                    ProductCode = product.Code,
                    ProductName = product.Name,
                    UnitName = "Bộ",
                    Quantity = 2,
                    UnitPrice = 1190000m,
                    DiscountRate = 5m,
                    TaxRate = 8m
                }
            }
        };

        var createdOrder = await salesService.CreateOrderAsync(orderDto);
        Assert.NotNull(createdOrder);

        // Verify Order Calculations:
        // Giá trước VAT: 1.190.000
        // Chiết khấu: 5% -> 119.000
        // Giá sau CK (DiscountedUnitPrice): 1.130.500
        // Thành tiền trước VAT: 2.261.000
        // Tiền thuế VAT 8%: 180.880
        // Thành tiền sau VAT (LineTotal): 2.441.880
        var orderItem = createdOrder.Items.Single();
        Assert.Equal(2, orderItem.Quantity);
        Assert.Equal(1190000m, orderItem.UnitPrice);
        Assert.Equal(5m, orderItem.DiscountRate);
        Assert.Equal(119000m, orderItem.DiscountAmount);
        Assert.Equal(1130500m, orderItem.DiscountedUnitPrice);
        Assert.Equal(2261000m, (orderItem.Quantity * (orderItem.UnitPrice ?? 0)) - orderItem.DiscountAmount);
        Assert.Equal(180880m, orderItem.TaxAmount);
        Assert.Equal(2441880m, orderItem.LineTotal);

        Assert.Equal(2380000m, createdOrder.SubTotal);
        Assert.Equal(119000m, createdOrder.TotalDiscount);
        Assert.Equal(180880m, createdOrder.TotalTax);
        Assert.Equal(2441880m, createdOrder.GrandTotal);

        // 4. Confirm Order
        var confirmed = await salesService.ConfirmOrderAsync(createdOrder.Id);
        Assert.True(confirmed);

        // 5. Create Hóa đơn GTGT (VatInvoice)
        var vatInvoice = await invoiceService.CreateInvoiceFromOrderAsync(createdOrder.Id, InvoiceType.VatInvoice);
        Assert.NotNull(vatInvoice);
        Assert.Equal(InvoiceType.VatInvoice, vatInvoice.Type);
        Assert.Equal("1", vatInvoice.FormNumber);
        Assert.Equal(2380000m, vatInvoice.SubTotal);
        Assert.Equal(119000m, vatInvoice.TotalDiscount);
        Assert.Equal(180880m, vatInvoice.TotalTax);
        Assert.Equal(2441880m, vatInvoice.GrandTotal);

        var vatItem = vatInvoice.Items.Single();
        Assert.Equal(2, vatItem.Quantity);
        Assert.Equal(1190000m, vatItem.UnitPrice);
        Assert.Equal(119000m, vatItem.DiscountAmount);
        Assert.Equal(5m, vatItem.DiscountRate);
        Assert.Equal(1130500m, vatItem.DiscountedUnitPrice);
        Assert.Equal(2261000m, vatItem.PreTaxAmount);
        Assert.Equal(8m, vatItem.TaxRate);
        Assert.Equal(180880m, vatItem.TaxAmount);
        Assert.Equal(2441880m, vatItem.LineTotal);

        // 6. Create Hóa đơn bán hàng (SalesInvoice)
        var salesInvoice = await invoiceService.CreateInvoiceFromOrderAsync(createdOrder.Id, InvoiceType.SalesInvoice);
        Assert.NotNull(salesInvoice);
        Assert.Equal(InvoiceType.SalesInvoice, salesInvoice.Type);
        Assert.Equal("2", salesInvoice.FormNumber);
        Assert.Equal(2380000m, salesInvoice.SubTotal);
        Assert.Equal(119000m, salesInvoice.TotalDiscount);
        Assert.Equal(0m, salesInvoice.TotalTax);
        Assert.Equal(2261000m, salesInvoice.GrandTotal);

        var salesItem = salesInvoice.Items.Single();
        Assert.Equal(2, salesItem.Quantity);
        Assert.Equal(1190000m, salesItem.UnitPrice);
        Assert.Equal(119000m, salesItem.DiscountAmount);
        Assert.Equal(5m, salesItem.DiscountRate);
        Assert.Equal(1130500m, salesItem.DiscountedUnitPrice);
        Assert.Equal(2261000m, salesItem.PreTaxAmount);
        Assert.Equal(0m, salesItem.TaxRate);
        Assert.Equal(0m, salesItem.TaxAmount);
        Assert.Equal(2261000m, salesItem.LineTotal);
    }
}
