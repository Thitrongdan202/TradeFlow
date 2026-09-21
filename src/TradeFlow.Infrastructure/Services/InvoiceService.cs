using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TradeFlow.Application.Common.Helpers;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

internal class Utf8StringWriter : System.IO.StringWriter
{
    public Utf8StringWriter(StringBuilder sb) : base(sb) { }
    public override Encoding Encoding => Encoding.UTF8;
}

public class InvoiceService : IInvoiceService
{
    private readonly TradeFlowDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditService _auditService;
    private readonly IDigitalSignatureService _digitalSignatureService;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment? _webHostEnvironment;

    public InvoiceService(
        TradeFlowDbContext context,
        ICurrentUserService currentUserService,
        IAuditService auditService,
        IDigitalSignatureService digitalSignatureService,
        Microsoft.AspNetCore.Hosting.IWebHostEnvironment? webHostEnvironment = null)
    {
        _context = context;
        _currentUserService = currentUserService;
        _auditService = auditService;
        _digitalSignatureService = digitalSignatureService;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default)
    {
        var invoices = await _context.Invoices
            .Include(x => x.Items)
            .AsNoTracking()
            .OrderByDescending(x => x.InvoiceDate)
            .ToListAsync(cancellationToken);

        return invoices.Select(MapToDto).ToList();
    }

    public async Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices
            .Include(x => x.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (inv == null) return null;
        return MapToDto(inv);
    }

    public async Task<InvoiceDto> CreateManualInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        // 1. Internal reference sequence (e.g. INV-2026-0001)
        var internalSeq = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "Invoice", cancellationToken);
        if (internalSeq == null)
        {
            internalSeq = new Domain.Entities.Settings.SystemSequence { SequenceKey = "Invoice", Prefix = "INV-", CurrentNumber = 1 };
            _context.SystemSequences.Add(internalSeq);
        }
        string invNumber = $"{internalSeq.Prefix}{DateTime.Now.Year}-{internalSeq.CurrentNumber:D4}";
        internalSeq.CurrentNumber++;

        // 2. Legal invoice number sequence (8 digits: 00000001) - Decoupled from internal tracking
        var legalSeq = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "LegalInvoiceNo", cancellationToken);
        if (legalSeq == null)
        {
            legalSeq = new Domain.Entities.Settings.SystemSequence { SequenceKey = "LegalInvoiceNo", Prefix = "", CurrentNumber = 1 };
            _context.SystemSequences.Add(legalSeq);
        }
        string legalInvNo = legalSeq.CurrentNumber.ToString("D8");
        legalSeq.CurrentNumber++;

        string yearSuffix = DateTime.Now.ToString("yy");
        bool isVat = dto.Type == InvoiceType.VatInvoice;
        string formNo = isVat ? "1" : "2";
        string series = isVat ? $"1C{yearSuffix}TFL" : $"2C{yearSuffix}TFL";

        decimal invoiceTotalTax = isVat ? dto.TotalTax : 0m;
        decimal invoiceGrandTotal = isVat ? dto.GrandTotal : (dto.SubTotal - dto.TotalDiscount);

        var invoice = new Invoice
        {
            InvoiceNumber = invNumber,
            FormNumber = formNo,
            InvoiceSeries = series,
            InvoiceNo = legalInvNo,
            InvoiceDate = dto.InvoiceDate == default ? DateTime.UtcNow : dto.InvoiceDate,
            CustomerId = dto.CustomerId,

            CompanyName = !string.IsNullOrWhiteSpace(company?.CompanyName) ? company.CompanyName : "LACASA",
            CompanyTaxCode = company?.TaxCode,
            CompanyAddress = company?.Address,
            CompanyPhone = company?.Phone,
            CompanyEmail = company?.Email,
            CompanyLogoUrl = company?.LogoPath,
            CompanyBankAccount = company?.BankAccount,
            CompanyBankName = company?.BankName,

            CustomerName = dto.CustomerName,
            CustomerCompanyName = dto.CustomerCompanyName,
            CustomerTaxCode = dto.CustomerTaxCode,
            CustomerAddress = dto.CustomerAddress,
            CustomerEmail = dto.CustomerEmail,
            CustomerBankAccount = dto.CustomerBankAccount,
            CustomerBankName = dto.CustomerBankName,
            PaymentMethod = dto.PaymentMethod ?? "TM/CK",

            Notes = dto.Notes,
            Status = InvoiceStatus.Draft,
            Type = dto.Type,
            SignatureStatus = DigitalSignatureStatus.Unsigned,

            SubTotal = dto.SubTotal,
            TotalDiscount = dto.TotalDiscount,
            TotalTax = invoiceTotalTax,
            GrandTotal = invoiceGrandTotal
        };

        int sortOrder = 1;
        foreach (var item in dto.Items)
        {
            decimal itemTaxRate = isVat ? item.TaxRate : 0m;
            decimal itemPreTax = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            decimal itemTaxAmount = isVat ? item.TaxAmount : 0m;
            decimal itemLineTotal = isVat ? item.LineTotal : itemPreTax;

            invoice.Items.Add(new InvoiceItem
            {
                SortOrder = sortOrder++,
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxRate = itemTaxRate,
                TaxAmount = itemTaxAmount,
                LineTotal = itemLineTotal
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceCreated, "Invoice", invoice.Id.ToString(), "Manual invoice created", _currentUserService.UserId);

        return MapToDto(invoice);
    }

    public async Task<InvoiceDto> CreateInvoiceFromOrderAsync(int salesOrderId, InvoiceType type = InvoiceType.VatInvoice, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

        if (order == null || (order.Status != SalesOrderStatus.Confirmed && order.Status != SalesOrderStatus.Invoiced))
        {
            throw new Exception("Sales Order must be Confirmed to create an Invoice.");
        }

        var customer = await _context.Customers.FindAsync(new object[] { order.CustomerId }, cancellationToken);
        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        // 1. Internal tracking reference
        var internalSeq = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "Invoice", cancellationToken);
        if (internalSeq == null)
        {
            internalSeq = new Domain.Entities.Settings.SystemSequence { SequenceKey = "Invoice", Prefix = "INV-", CurrentNumber = 1 };
            _context.SystemSequences.Add(internalSeq);
        }
        string invNumber = $"{internalSeq.Prefix}{DateTime.Now.Year}-{internalSeq.CurrentNumber:D4}";
        internalSeq.CurrentNumber++;

        // 2. Legal invoice number sequence (8 digits)
        var legalSeq = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "LegalInvoiceNo", cancellationToken);
        if (legalSeq == null)
        {
            legalSeq = new Domain.Entities.Settings.SystemSequence { SequenceKey = "LegalInvoiceNo", Prefix = "", CurrentNumber = 1 };
            _context.SystemSequences.Add(legalSeq);
        }
        string legalInvNo = legalSeq.CurrentNumber.ToString("D8");
        legalSeq.CurrentNumber++;

        string yearSuffix = DateTime.Now.ToString("yy");
        bool isVat = type == InvoiceType.VatInvoice;
        string formNo = isVat ? "1" : "2";
        string series = isVat ? $"1C{yearSuffix}TFL" : $"2C{yearSuffix}TFL";

        decimal invoiceTotalTax = isVat ? order.TotalTax : 0m;
        decimal invoiceGrandTotal = isVat ? order.GrandTotal : (order.SubTotal - order.TotalDiscount);

        var invoice = new Invoice
        {
            InvoiceNumber = invNumber,
            FormNumber = formNo,
            InvoiceSeries = series,
            InvoiceNo = legalInvNo,
            InvoiceDate = order.OrderDate,
            SalesOrderId = order.Id,
            CustomerId = order.CustomerId,

            CompanyName = !string.IsNullOrWhiteSpace(company?.CompanyName) ? company.CompanyName : "LACASA",
            CompanyTaxCode = company?.TaxCode,
            CompanyAddress = company?.Address,
            CompanyPhone = company?.Phone,
            CompanyEmail = company?.Email,
            CompanyLogoUrl = company?.LogoPath,
            CompanyBankAccount = company?.BankAccount,
            CompanyBankName = company?.BankName,

            CustomerName = customer?.Name ?? order.CustomerName,
            CustomerCompanyName = customer?.CompanyName,
            CustomerTaxCode = customer?.TaxCode ?? order.CustomerTaxCode,
            CustomerAddress = customer?.Address ?? order.CustomerAddress,
            CustomerEmail = customer?.Email,
            PaymentMethod = "TM/CK",

            Notes = order.Notes,
            Status = InvoiceStatus.Draft,
            Type = type,
            SignatureStatus = DigitalSignatureStatus.Unsigned,

            SubTotal = order.SubTotal,
            TotalDiscount = order.TotalDiscount,
            TotalTax = invoiceTotalTax,
            GrandTotal = invoiceGrandTotal
        };

        int sortOrder = 1;
        foreach (var oi in order.Items)
        {
            decimal itemTaxRate = isVat ? oi.TaxRate : 0m;
            decimal itemPreTax = (oi.Quantity * oi.UnitPrice) - oi.DiscountAmount;
            decimal itemTaxAmount = isVat ? oi.TaxAmount : 0m;
            decimal itemLineTotal = isVat ? oi.LineTotal : itemPreTax;

            invoice.Items.Add(new InvoiceItem
            {
                SortOrder = sortOrder++,
                ProductId = oi.ProductId,
                ProductCode = oi.ProductCode,
                ProductName = oi.ProductName,
                UnitName = oi.UnitName,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                DiscountAmount = oi.DiscountAmount,
                TaxRate = itemTaxRate,
                TaxAmount = itemTaxAmount,
                LineTotal = itemLineTotal
            });
        }

        order.Status = SalesOrderStatus.Invoiced;

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(AuditEventType.InvoiceCreated, "Invoice", invoice.Id.ToString(), $"Invoice created from SO {order.Code}", _currentUserService.UserId);

        return MapToDto(invoice);
    }

    public Task<InvoiceDto> CreateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        return CreateManualInvoiceAsync(dto, cancellationToken);
    }

    public async Task<bool> UpdateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        inv.CustomerName = dto.CustomerName;
        inv.CustomerCompanyName = dto.CustomerCompanyName;
        inv.CustomerTaxCode = dto.CustomerTaxCode;
        inv.CustomerAddress = dto.CustomerAddress;
        inv.CustomerEmail = dto.CustomerEmail;
        inv.CustomerBankAccount = dto.CustomerBankAccount;
        inv.CustomerBankName = dto.CustomerBankName;
        inv.PaymentMethod = dto.PaymentMethod;
        inv.Notes = dto.Notes;
        inv.Type = dto.Type;
        bool isVat = dto.Type == InvoiceType.VatInvoice;
        inv.FormNumber = isVat ? "1" : "2";
        inv.InvoiceSeries = isVat ? $"1C{inv.InvoiceDate:yy}TFL" : $"2C{inv.InvoiceDate:yy}TFL";

        _context.InvoiceItems.RemoveRange(inv.Items);
        inv.Items.Clear();

        int sortOrder = 1;
        foreach (var item in dto.Items)
        {
            decimal taxRate = isVat ? item.TaxRate : 0m;
            decimal taxAmount = isVat ? item.TaxAmount : 0m;
            decimal linePreTax = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            decimal lineTotal = isVat ? item.LineTotal : linePreTax;

            inv.Items.Add(new InvoiceItem
            {
                SortOrder = sortOrder++,
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                LineTotal = lineTotal
            });
        }

        inv.SubTotal = dto.SubTotal;
        inv.TotalDiscount = dto.TotalDiscount;
        inv.TotalTax = isVat ? dto.TotalTax : 0m;
        inv.GrandTotal = isVat ? dto.GrandTotal : (dto.SubTotal - dto.TotalDiscount);

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceUpdated, "Invoice", inv.Id.ToString(), "Draft invoice updated", _currentUserService.UserId);

        return true;
    }

    public async Task<bool> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.FindAsync(new object[] { id }, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        inv.Status = InvoiceStatus.Issued;

        // Auto-sign on issuance if currently unsigned
        if (inv.SignatureStatus == DigitalSignatureStatus.Unsigned)
        {
            await _digitalSignatureService.SignInvoiceAsync(inv, null, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceIssued, "Invoice", inv.Id.ToString(), "Invoice issued and signed", _currentUserService.UserId);
        return true;
    }

    public async Task<bool> SignInvoiceAsync(int invoiceId, string? signedBy = null, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.FindAsync(new object[] { invoiceId }, cancellationToken);
        if (inv == null) return false;

        var result = await _digitalSignatureService.SignInvoiceAsync(inv, signedBy, cancellationToken);
        if (result.Succeeded)
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(AuditEventType.InvoiceUpdated, "Invoice", inv.Id.ToString(), $"Invoice signed by {inv.SignedBy}", _currentUserService.UserId);
            return true;
        }
        return false;
    }

    public async Task<bool> DeleteInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        _context.Invoices.Remove(inv);

        if (inv.SalesOrderId.HasValue)
        {
            var so = await _context.SalesOrders.FindAsync(new object[] { inv.SalesOrderId.Value }, cancellationToken);
            if (so != null && so.Status == SalesOrderStatus.Invoiced)
            {
                so.Status = SalesOrderStatus.Confirmed;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceDeleted, "Invoice", inv.Id.ToString(), "Draft invoice deleted", _currentUserService.UserId);

        return true;
    }

    public async Task<string> GenerateXmlAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (inv == null) throw new Exception("Invoice not found");

        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        bool isVat = inv.Type == InvoiceType.VatInvoice;
        string invoiceName = isVat ? "Hóa đơn giá trị gia tăng" : "Hóa đơn bán hàng";

        // Group tax rates for THTTLTSuat
        var taxGroups = inv.Items
            .GroupBy(x => x.TaxRate)
            .OrderBy(x => x.Key)
            .Select(g => new
            {
                TaxRate = g.Key,
                TaxRateStr = $"{g.Key:0.##}%",
                ThTien = g.Sum(x => x.Quantity * x.UnitPrice - x.DiscountAmount),
                TThue = g.Sum(x => x.TaxAmount),
                TToan = g.Sum(x => (x.Quantity * x.UnitPrice - x.DiscountAmount) + x.TaxAmount)
            })
            .ToList();

        string amountInWords = NumberToTextHelper.ConvertToWords((long)inv.GrandTotal) + " đồng chẵn.";

        // Build XML according to ND123 / TT78 standard structure:
        // HDon -> DLHDon (TTChung, NDHDon [NBan, NMua, DSHHDVu, TToan], TTKhac), MCCQT, DLQRCode, DSCKS
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("HDon",
                new XElement("DLHDon",
                    new XAttribute("Id", $"Invoice_{inv.InvoiceNo}"),
                    new XElement("TTChung",
                        new XElement("PBan", "2.0.0"),
                        new XElement("THDon", invoiceName),
                        new XElement("KHMSHDon", inv.FormNumber),
                        new XElement("KHHDon", inv.InvoiceSeries),
                        new XElement("SHDon", inv.InvoiceNo),
                        new XElement("NLap", inv.InvoiceDate.ToString("yyyy-MM-dd")),
                        new XElement("DVTTe", "VND"),
                        new XElement("TGia", "1"),
                        new XElement("HTTToan", inv.PaymentMethod ?? "TM/CK"),
                        new XElement("MSTTCGP", "0101243150")
                    ),
                    new XElement("NDHDon",
                        new XElement("NBan",
                            new XElement("Ten", inv.CompanyName),
                            new XElement("MST", inv.CompanyTaxCode ?? ""),
                            new XElement("DChi", inv.CompanyAddress ?? ""),
                            new XElement("SDThoai", inv.CompanyPhone ?? ""),
                            new XElement("DCTDTu", inv.CompanyEmail ?? ""),
                            new XElement("STKNHang", inv.CompanyBankAccount ?? ""),
                            new XElement("TNHang", inv.CompanyBankName ?? ""),
                            new XElement("Website", company?.Website ?? "")
                        ),
                        new XElement("NMua",
                            new XElement("Ten", inv.CustomerName ?? ""),
                            new XElement("TenDV", inv.CustomerCompanyName ?? ""),
                            new XElement("MST", inv.CustomerTaxCode ?? ""),
                            new XElement("DChi", inv.CustomerAddress ?? ""),
                            new XElement("SDThoai", inv.CustomerEmail ?? ""),
                            new XElement("DCTDTu", inv.CustomerEmail ?? ""),
                            new XElement("STKNHang", inv.CustomerBankAccount ?? ""),
                            new XElement("TNHang", inv.CustomerBankName ?? ""),
                            new XElement("HTTToan", inv.PaymentMethod ?? "TM/CK")
                        ),
                        new XElement("DSHHDVu",
                            inv.Items.OrderBy(x => x.SortOrder).Select(item =>
                                new XElement("HHDVu",
                                    new XElement("TChat", "1"),
                                    new XElement("STT", item.SortOrder.ToString()),
                                    new XElement("MHHDVu", item.ProductCode ?? ""),
                                    new XElement("THHDVu", item.ProductName ?? ""),
                                    new XElement("DVTinh", item.UnitName ?? ""),
                                    new XElement("SLuong", item.Quantity.ToString("G29")),
                                    new XElement("DGia", item.UnitPrice.ToString("0")),
                                    new XElement("TLCKhau", "0"),
                                    new XElement("STCKhau", item.DiscountAmount.ToString("0")),
                                    new XElement("ThTien", (item.Quantity * item.UnitPrice - item.DiscountAmount).ToString("0")),
                                    new XElement("TSuat", isVat ? $"{item.TaxRate:0.##}%" : "KCT"),
                                    new XElement("TThue", item.TaxAmount.ToString("0"))
                                )
                            )
                        ),
                        new XElement("TToan",
                            new XElement("THTTLTSuat",
                                taxGroups.Select(tg =>
                                    new XElement("LTSuat",
                                        new XElement("TSuat", tg.TaxRateStr),
                                        new XElement("ThTien", tg.ThTien.ToString("0")),
                                        new XElement("TThue", tg.TThue.ToString("0")),
                                        new XElement("TToan", tg.TToan.ToString("0"))
                                    )
                                )
                            ),
                            new XElement("TgTCThue", inv.SubTotal.ToString("0")),
                            new XElement("TgTThue", inv.TotalTax.ToString("0")),
                            new XElement("TgTTTBSo", inv.GrandTotal.ToString("0")),
                            new XElement("TgTTTBChu", amountInWords)
                        )
                    ),
                    new XElement("TTKhac")
                ),
                new XElement("MCCQT", inv.TaxAuthorityCode ?? ""),
                new XElement("DLQRCode", inv.QrCodeData ?? ""),
                new XElement("DSCKS",
                    new XElement("NBan",
                        inv.SignatureStatus == DigitalSignatureStatus.Signed
                            ? new XElement(XName.Get("Signature", "http://www.w3.org/2000/09/xmldsig#"),
                                new XElement(XName.Get("SignedInfo", "http://www.w3.org/2000/09/xmldsig#"),
                                    new XElement(XName.Get("CanonicalizationMethod", "http://www.w3.org/2000/09/xmldsig#"), new XAttribute("Algorithm", "http://www.w3.org/TR/2001/REC-xml-c14n-20010315")),
                                    new XElement(XName.Get("SignatureMethod", "http://www.w3.org/2000/09/xmldsig#"), new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256")),
                                    new XElement(XName.Get("Reference", "http://www.w3.org/2000/09/xmldsig#"), new XAttribute("URI", $"#Invoice_{inv.InvoiceNo}"),
                                        new XElement(XName.Get("DigestMethod", "http://www.w3.org/2000/09/xmldsig#"), new XAttribute("Algorithm", "http://www.w3.org/2001/04/xmlenc#sha256")),
                                        new XElement(XName.Get("DigestValue", "http://www.w3.org/2000/09/xmldsig#"), inv.SignatureValue ?? "")
                                    )
                                ),
                                new XElement(XName.Get("SignatureValue", "http://www.w3.org/2000/09/xmldsig#"), inv.SignatureValue ?? ""),
                                new XElement(XName.Get("KeyInfo", "http://www.w3.org/2000/09/xmldsig#"),
                                    new XElement(XName.Get("X509Data", "http://www.w3.org/2000/09/xmldsig#"),
                                        new XElement(XName.Get("X509SubjectName", "http://www.w3.org/2000/09/xmldsig#"), inv.CertificateSubject ?? $"CN={inv.CompanyName}"),
                                        new XElement(XName.Get("X509Certificate", "http://www.w3.org/2000/09/xmldsig#"), "")
                                    )
                                ),
                                new XElement(XName.Get("Object", "http://www.w3.org/2000/09/xmldsig#"),
                                    new XElement(XName.Get("SigningTime", "http://www.w3.org/2000/09/xmldsig#"), inv.SignedAt?.ToString("o") ?? inv.InvoiceDate.ToString("o"))
                                )
                            )
                            : null
                    )
                )
            )
        );

        var sb = new StringBuilder();
        using (var writer = new Utf8StringWriter(sb))
        {
            doc.Save(writer, SaveOptions.None);
        }
        return sb.ToString();
    }

    public async Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);
        if (inv == null) throw new Exception("Invoice not found");

        await _auditService.LogAsync(AuditEventType.InvoiceExportedPdf, "Invoice", inv.Id.ToString(), "PDF exported", _currentUserService.UserId);

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        bool isVat = inv.Type == InvoiceType.VatInvoice;
        var invoiceDate = inv.InvoiceDate;

        // Visual Palette strictly matching empty invoice template (media_1789552936429.pdf)
        string borderBlue = "#0b5394";  // Royal blue lines & titles
        string tableHeadBg = "#d9edf7"; // Light sky blue header
        string rowAltBg = "#f2f7fb";
        string white = "#ffffff";
        string black = "#000000";
        string textBlue = "#0b5394";
        var viCulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");

        var itemsList = inv.Items.OrderBy(x => x.SortOrder).ToList();
        string amountWords = NumberToTextHelper.ConvertToWords((long)inv.GrandTotal) + " đồng chẵn.";

        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginTop(8, Unit.Millimetre);
                page.MarginBottom(8, Unit.Millimetre);
                page.MarginLeft(10, Unit.Millimetre);
                page.MarginRight(10, Unit.Millimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(8.5f).FontColor(black));

                // Outer decorative border frame (matching empty template)
                page.Content().Border(1.5f).BorderColor(borderBlue).Padding(2).Border(0.75f).BorderColor(borderBlue).Column(main =>
                {
                    // ── 1. HEADER AREA ─────────────────────────────────────
                    main.Item().PaddingHorizontal(10).PaddingTop(8).PaddingBottom(6).Column(hdr =>
                    {
                        hdr.Item().Row(r =>
                        {
                            // Left spacer to keep center title perfectly centered
                            r.ConstantItem(120);

                            // Center: Title + Subtitle
                            r.RelativeItem().AlignCenter().Column(tc =>
                            {
                                string title = "HÓA ĐƠN GIÁ TRỊ GIA TĂNG";
                                string subtitle = "(VAT INVOICE)";

                                tc.Item().AlignCenter().Text(title)
                                    .FontSize(15).Bold().FontColor(borderBlue);
                                tc.Item().AlignCenter().Text(subtitle)
                                    .FontSize(10).SemiBold().FontColor(borderBlue);
                            });

                            // Right: Mẫu số, Ký hiệu, Số
                            r.ConstantItem(145).Column(rc =>
                            {
                                rc.Item().Text(t =>
                                {
                                    t.Span("Mẫu số ").FontSize(8).FontColor(textBlue);
                                    t.Span("(Form No.)").FontSize(7.5f).Italic().FontColor(textBlue);
                                    t.Span($": {inv.FormNumber}").FontSize(8).FontColor(black);
                                });
                                rc.Item().Text(t =>
                                {
                                    t.Span("Ký hiệu ").FontSize(8).FontColor(textBlue);
                                    t.Span("(Serial No.)").FontSize(7.5f).Italic().FontColor(textBlue);
                                    t.Span($": {inv.InvoiceSeries}").FontSize(8).FontColor(black);
                                });
                                rc.Item().Text(t =>
                                {
                                    t.Span("Số ").FontSize(8).FontColor(textBlue);
                                    t.Span("(Invoice No.)").FontSize(7.5f).Italic().FontColor(textBlue);
                                    t.Span($": {inv.InvoiceNo}").FontSize(8.5f).Bold().FontColor(black);
                                });
                            });
                        });

                        // Date line: Ngày (day) ... tháng (month) ... năm (year) ...
                        hdr.Item().PaddingTop(2).AlignCenter().Text(t =>
                        {
                            t.Span("Ngày ").FontSize(8.5f).FontColor(textBlue);
                            t.Span("(day) ").FontSize(7.5f).Italic().FontColor(textBlue);
                            t.Span(invoiceDate.ToString("dd")).FontSize(8.5f).Bold().FontColor(black);
                            t.Span("   tháng ").FontSize(8.5f).FontColor(textBlue);
                            t.Span("(month) ").FontSize(7.5f).Italic().FontColor(textBlue);
                            t.Span(invoiceDate.ToString("MM")).FontSize(8.5f).Bold().FontColor(black);
                            t.Span("   năm ").FontSize(8.5f).FontColor(textBlue);
                            t.Span("(year) ").FontSize(7.5f).Italic().FontColor(textBlue);
                            t.Span(invoiceDate.ToString("yyyy")).FontSize(8.5f).Bold().FontColor(black);
                        });
                    });

                    // ── 2. SELLER INFORMATION BLOCK ────────────────────────
                    main.Item().BorderTop(0.75f).BorderColor(borderBlue).PaddingHorizontal(8).PaddingVertical(4).Column(sel =>
                    {
                        // Đơn vị bán hàng (Seller)
                        sel.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("Đơn vị bán hàng ").FontSize(8.5f).Bold().FontColor(textBlue);
                                t.Span("(Seller): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.CompanyName).FontSize(8.5f).Bold().FontColor(black);
                        });

                        // MST (Tax Code)
                        sel.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("MST ").FontSize(8.5f).Bold().FontColor(textBlue);
                                t.Span("(Tax Code): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.CompanyTaxCode ?? "").FontSize(8.5f).FontColor(black);
                        });

                        // Địa chỉ (Address)
                        sel.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("Địa chỉ ").FontSize(8.5f).FontColor(textBlue);
                                t.Span("(Address): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.CompanyAddress ?? "").FontSize(8.5f).FontColor(black);
                        });

                        // Điện thoại / Email
                        sel.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.RelativeItem().Row(rLeft =>
                            {
                                rLeft.AutoItem().Text(t =>
                                {
                                    t.Span("Điện thoại ").FontSize(8.5f).FontColor(textBlue);
                                    t.Span("(Tel.): ").FontSize(7.5f).Italic().FontColor(textBlue);
                                });
                                rLeft.RelativeItem().PaddingLeft(4).Text(inv.CompanyPhone ?? "").FontSize(8.5f).FontColor(black);
                            });

                            r.RelativeItem().Row(rRight =>
                            {
                                rRight.AutoItem().Text(t =>
                                {
                                    t.Span("Email: ").FontSize(8.5f).FontColor(textBlue);
                                });
                                rRight.RelativeItem().PaddingLeft(4).Text(inv.CompanyEmail ?? "").FontSize(8.5f).FontColor(black);
                            });
                        });

                        // STK / Ngân hàng
                        sel.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.RelativeItem().Row(rLeft =>
                            {
                                rLeft.AutoItem().Text(t =>
                                {
                                    t.Span("STK ").FontSize(8.5f).FontColor(textBlue);
                                    t.Span("(Account No.): ").FontSize(7.5f).Italic().FontColor(textBlue);
                                });
                                rLeft.RelativeItem().PaddingLeft(4).Text(inv.CompanyBankAccount ?? "").FontSize(8.5f).FontColor(black);
                            });

                            r.RelativeItem().Row(rRight =>
                            {
                                rRight.AutoItem().Text(t =>
                                {
                                    t.Span("Ngân hàng ").FontSize(8.5f).FontColor(textBlue);
                                    t.Span("(Bank): ").FontSize(7.5f).Italic().FontColor(textBlue);
                                });
                                rRight.RelativeItem().PaddingLeft(4).Text(inv.CompanyBankName ?? "").FontSize(8.5f).FontColor(black);
                            });
                        });
                    });

                    // ── 3. BUYER INFORMATION BLOCK ─────────────────────────
                    main.Item().BorderTop(0.75f).BorderColor(borderBlue).PaddingHorizontal(8).PaddingVertical(4).Column(buy =>
                    {
                        // Người mua hàng (Buyer)
                        buy.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("Người mua hàng ").FontSize(8.5f).FontColor(textBlue);
                                t.Span("(Buyer): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.CustomerName).FontSize(8.5f).Bold().FontColor(black);
                        });

                        // Đơn vị (Co. name)
                        buy.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("Đơn vị ").FontSize(8.5f).FontColor(textBlue);
                                t.Span("(Co. name): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.CustomerCompanyName ?? "").FontSize(8.5f).FontColor(black);
                        });

                        // MST (Tax Code)
                        buy.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("MST ").FontSize(8.5f).FontColor(textBlue);
                                t.Span("(Tax Code): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.CustomerTaxCode ?? "").FontSize(8.5f).FontColor(black);
                        });

                        // Địa chỉ (Address)
                        buy.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("Địa chỉ ").FontSize(8.5f).FontColor(textBlue);
                                t.Span("(Address): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.CustomerAddress ?? "").FontSize(8.5f).FontColor(black);
                        });

                        // HTTT (Pay. method)
                        buy.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.AutoItem().Text(t =>
                            {
                                t.Span("HTTT ").FontSize(8.5f).FontColor(textBlue);
                                t.Span("(Pay. method): ").FontSize(7.5f).Italic().FontColor(textBlue);
                            });
                            r.RelativeItem().PaddingLeft(4).Text(inv.PaymentMethod ?? "TM/CK").FontSize(8.5f).FontColor(black);
                        });

                        // STK / Ngân hàng
                        buy.Item().PaddingVertical(1.5f).Row(r =>
                        {
                            r.RelativeItem().Row(rLeft =>
                            {
                                rLeft.AutoItem().Text(t =>
                                {
                                    t.Span("STK ").FontSize(8.5f).FontColor(textBlue);
                                    t.Span("(Account No.): ").FontSize(7.5f).Italic().FontColor(textBlue);
                                });
                                rLeft.RelativeItem().PaddingLeft(4).Text(inv.CustomerBankAccount ?? "").FontSize(8.5f).FontColor(black);
                            });

                            r.RelativeItem().Row(rRight =>
                            {
                                rRight.AutoItem().Text(t =>
                                {
                                    t.Span("Ngân hàng ").FontSize(8.5f).FontColor(textBlue);
                                    t.Span("(Bank): ").FontSize(7.5f).Italic().FontColor(textBlue);
                                });
                                rRight.RelativeItem().PaddingLeft(4).Text(inv.CustomerBankName ?? "").FontSize(8.5f).FontColor(black);
                            });
                        });
                    });

                    // ── 4. PRODUCT TABLE (8 columns for VAT) ────────────────
                    main.Item().BorderTop(1f).BorderColor(borderBlue).Table(table =>
                    {
                        // Column definitions matching template proportions
                        table.ColumnsDefinition(cols =>
                        {
                            cols.ConstantColumn(28);   // STT
                            cols.RelativeColumn(3.4f); // Tên hàng hoá, dịch vụ
                            cols.ConstantColumn(36);   // ĐVT
                            cols.ConstantColumn(40);   // SL
                            cols.ConstantColumn(62);   // Đơn giá
                            cols.ConstantColumn(68);   // Thành tiền
                            if (isVat)
                            {
                                cols.ConstantColumn(42); // Thuế suất
                                cols.ConstantColumn(64); // Tiền thuế
                            }
                        });

                        // Header Row 1: Labels
                        table.Header(h =>
                        {
                            void Th(string vi, string en, bool center = true)
                            {
                                var cell = h.Cell().Background(tableHeadBg).Border(0.5f).BorderColor(borderBlue).Padding(3);
                                if (center)
                                {
                                    cell.AlignCenter().Column(c =>
                                    {
                                        c.Item().AlignCenter().Text(vi).FontSize(8).Bold().FontColor(textBlue);
                                        c.Item().AlignCenter().Text(en).FontSize(7).Italic().FontColor(textBlue);
                                    });
                                }
                                else
                                {
                                    cell.Column(c =>
                                    {
                                        c.Item().Text(vi).FontSize(8).Bold().FontColor(textBlue);
                                        c.Item().Text(en).FontSize(7).Italic().FontColor(textBlue);
                                    });
                                }
                            }

                            Th("STT", "(No.)");
                            Th("Tên hàng hoá, dịch vụ", "(Description)", false);
                            Th("ĐVT", "(Unit)");
                            Th("SL", "(Quantity)");
                            Th("Đơn giá", "(Unit Price)");
                            Th("Thành tiền", "(Amount)");
                            if (isVat)
                            {
                                Th("Thuế suất", "(Tax Rate)");
                                Th("Tiền thuế", "(Tax Amount)");
                            }
                        });

                        // Header Row 2: Column numbers (1 | 2 | 3 | 4 | 5 | 6 = 4 x 5 | 7 | 8 = 6 x 7)
                        void ColIdx(string text) =>
                            table.Cell().Background(rowAltBg).Border(0.5f).BorderColor(borderBlue).Padding(2).AlignCenter().Text(text).FontSize(7.5f).Italic().FontColor(textBlue);

                        ColIdx("1");
                        ColIdx("2");
                        ColIdx("3");
                        ColIdx("4");
                        ColIdx("5");
                        ColIdx("6 = 4 x 5");
                        if (isVat)
                        {
                            ColIdx("7");
                            ColIdx("8 = 6 x 7");
                        }

                        // Data rows
                        int stt = 1;
                        foreach (var item in itemsList)
                        {
                            var lineAmt = item.Quantity * item.UnitPrice - item.DiscountAmount;

                            table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).AlignCenter().Text(stt.ToString()).FontSize(8).FontColor(black);
                            table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).Text(item.ProductName).FontSize(8).FontColor(black);
                            table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).AlignCenter().Text(item.UnitName).FontSize(8).FontColor(black);
                            table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).AlignRight().Text(item.Quantity.ToString("G29")).FontSize(8).FontColor(black);
                            table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).AlignRight().Text(item.UnitPrice.ToString("N0", viCulture)).FontSize(8).FontColor(black);
                            table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).AlignRight().Text(lineAmt.ToString("N0", viCulture)).FontSize(8).FontColor(black);

                            if (isVat)
                            {
                                table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).AlignCenter().Text($"{item.TaxRate}%").FontSize(8).FontColor(black);
                                table.Cell().Border(0.5f).BorderColor(borderBlue).Padding(3).AlignRight().Text(item.TaxAmount.ToString("N0", viCulture)).FontSize(8).FontColor(black);
                            }

                            stt++;
                        }

                        // Pad table to 8 rows minimum to match the visual height of the reference template
                        int emptyCount = Math.Max(0, 8 - itemsList.Count);
                        int colCount = isVat ? 8 : 6;
                        for (int r = 0; r < emptyCount; r++)
                        {
                            for (int c = 0; c < colCount; c++)
                            {
                                table.Cell().Border(0.5f).BorderColor(borderBlue).MinHeight(18).Text("");
                            }
                        }
                    });

                    // ── 5. TOTALS SECTION ──────────────────────────────────
                    main.Item().BorderTop(0.5f).BorderColor(borderBlue).Column(tot =>
                    {
                        void TotRow(string vi, string en, decimal val, bool boldVal = false)
                        {
                            tot.Item().BorderBottom(0.5f).BorderColor(borderBlue).Row(r =>
                            {
                                r.RelativeItem().AlignRight().PaddingRight(8).PaddingVertical(3).Text(t =>
                                {
                                    t.Span(vi).FontSize(8.5f).FontColor(textBlue);
                                    t.Span(" ").FontSize(8.5f);
                                    t.Span(en).FontSize(7.5f).Italic().FontColor(textBlue);
                                    t.Span(":").FontSize(8.5f).FontColor(textBlue);
                                });

                                var valText = r.ConstantItem(85).BorderLeft(0.5f).BorderColor(borderBlue).PaddingRight(6).PaddingVertical(3).AlignRight().Text(val.ToString("N0", viCulture));
                                if (boldVal)
                                    valText.FontSize(9).Bold().FontColor(black);
                                else
                                    valText.FontSize(8.5f).FontColor(black);
                            });
                        }

                        if (isVat)
                        {
                            TotRow("Cộng tiền hàng", "(Sub total)", inv.SubTotal);
                            TotRow("Cộng tiền thuế GTGT", "(VAT amount)", inv.TotalTax);
                        }
                        TotRow("Tổng cộng tiền thanh toán", "(Total payment)", inv.GrandTotal, true);
                    });

                    // ── 6. AMOUNT IN WORDS ─────────────────────────────────
                    main.Item().BorderTop(0.5f).BorderColor(borderBlue).PaddingHorizontal(8).PaddingVertical(4).Row(r =>
                    {
                        r.AutoItem().Text(t =>
                        {
                            t.Span("Số tiền viết bằng chữ ").FontSize(8.5f).FontColor(textBlue);
                            t.Span("(Amount in words)").FontSize(7.5f).Italic().FontColor(textBlue);
                            t.Span(": ").FontSize(8.5f).FontColor(textBlue);
                        });
                        r.RelativeItem().PaddingLeft(4).Text(amountWords).FontSize(8.5f).Italic().FontColor(black);
                    });

                    // ── 7. SIGNATURE SECTION ───────────────────────────────
                    main.Item().BorderTop(0.75f).BorderColor(borderBlue).Row(sigRow =>
                    {
                        // Buyer signature (left)
                        sigRow.RelativeItem().PaddingVertical(8).AlignCenter().Column(bCol =>
                        {
                            bCol.Item().AlignCenter().Text(t =>
                            {
                                t.Span("Người mua hàng ").FontSize(9).Bold().FontColor(textBlue);
                                t.Span("(Buyer)").FontSize(8).Italic().FontColor(textBlue);
                            });
                            bCol.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").FontSize(8).Italic().FontColor("#4b5563");
                            bCol.Item().AlignCenter().Text("(Sign, full name)").FontSize(7.5f).Italic().FontColor("#4b5563");
                            bCol.Item().MinHeight(60);
                        });

                        // Seller signature (right)
                        sigRow.RelativeItem().PaddingVertical(8).AlignCenter().Column(sCol =>
                        {
                            sCol.Item().AlignCenter().Text(t =>
                            {
                                t.Span("Người bán hàng ").FontSize(9).Bold().FontColor(textBlue);
                                t.Span("(Seller)").FontSize(8).Italic().FontColor(textBlue);
                            });
                            sCol.Item().AlignCenter().Text("(Ký, ghi rõ họ tên, đóng dấu nếu có)").FontSize(8).Italic().FontColor("#4b5563");
                            sCol.Item().AlignCenter().Text("(Sign, full name, stamp if any)").FontSize(7.5f).Italic().FontColor("#4b5563");

                            // Actual system signature state representation
                            if (inv.SignatureStatus == DigitalSignatureStatus.Signed)
                            {
                                // Electronic confirmation box (matching visual reference with LACASA branding)
                                sCol.Item().PaddingTop(6).PaddingHorizontal(12).Border(1).BorderColor("#4caf50").Background("#f1f8e9").Padding(8).Column(c =>
                                {
                                    c.Item().AlignCenter().Text("Đã được ký điện tử bởi").FontSize(8.5f).SemiBold().FontColor("#2e7d32");
                                    c.Item().AlignCenter().Text("(Signed digitally by)").FontSize(7.5f).Italic().FontColor("#2e7d32");
                                    c.Item().PaddingTop(2).AlignCenter().Text("✓").FontSize(22).Bold().FontColor("#4caf50");
                                    c.Item().AlignCenter().Text(inv.CompanyName.ToUpper()).FontSize(8.5f).Bold().FontColor("#1b5e20");
                                    c.Item().PaddingTop(2).AlignCenter().Text($"Ký ngày: {(inv.SignedAt ?? inv.InvoiceDate):dd/MM/yyyy}").FontSize(8).FontColor("#2e7d32");
                                });
                            }
                            else if (inv.SignatureStatus == DigitalSignatureStatus.Invalid)
                            {
                                sCol.Item().PaddingTop(10).PaddingHorizontal(12).Border(1).BorderColor(Colors.Red.Medium).Background(Colors.Red.Lighten5).Padding(6).Column(c =>
                                {
                                    c.Item().AlignCenter().Text("Chữ ký không hợp lệ").FontSize(8.5f).Bold().FontColor(Colors.Red.Darken2);
                                    c.Item().AlignCenter().Text("(Invalid signature)").FontSize(7.5f).Italic().FontColor(Colors.Red.Darken2);
                                });
                            }
                            else
                            {
                                // Unsigned: clean empty space for signing/stamp (no fabricated signature)
                                sCol.Item().MinHeight(60);
                            }
                        });
                    });

                    // ── 8. FOOTER NOTE ─────────────────────────────────────
                    main.Item().BorderTop(0.5f).BorderColor(borderBlue).PaddingHorizontal(8).PaddingVertical(3).Row(foot =>
                    {
                        foot.RelativeItem().Text("(Khởi tạo theo quy định về hóa đơn chứng từ bán hàng & cung ứng dịch vụ)")
                            .FontSize(7.5f).Italic().FontColor("#4b5563");

                        foot.AutoItem().Text("Trang 1/1")
                            .FontSize(7.5f).FontColor("#4b5563");
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private InvoiceDto MapToDto(Invoice invoice)
    {
        var dto = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            FormNumber = invoice.FormNumber,
            InvoiceSeries = invoice.InvoiceSeries,
            InvoiceNo = invoice.InvoiceNo,
            InvoiceDate = invoice.InvoiceDate,
            SalesOrderId = invoice.SalesOrderId,
            CustomerId = invoice.CustomerId,
            CompanyName = invoice.CompanyName,
            CompanyTaxCode = invoice.CompanyTaxCode,
            CompanyAddress = invoice.CompanyAddress,
            CompanyPhone = invoice.CompanyPhone,
            CompanyEmail = invoice.CompanyEmail,
            CompanyBankAccount = invoice.CompanyBankAccount,
            CompanyBankName = invoice.CompanyBankName,
            CompanyLogoUrl = invoice.CompanyLogoUrl,
            CustomerName = invoice.CustomerName,
            CustomerCompanyName = invoice.CustomerCompanyName,
            CustomerTaxCode = invoice.CustomerTaxCode,
            CustomerAddress = invoice.CustomerAddress,
            CustomerEmail = invoice.CustomerEmail,
            CustomerBankAccount = invoice.CustomerBankAccount,
            CustomerBankName = invoice.CustomerBankName,
            PaymentMethod = invoice.PaymentMethod,
            Notes = invoice.Notes,
            Status = invoice.Status,
            Type = invoice.Type,
            SubTotal = invoice.SubTotal,
            TotalDiscount = invoice.TotalDiscount,
            TotalTax = invoice.TotalTax,
            GrandTotal = invoice.GrandTotal,
            TaxAuthorityCode = invoice.TaxAuthorityCode,
            QrCodeData = invoice.QrCodeData,
            SignatureStatus = invoice.SignatureStatus,
            SignedBy = invoice.SignedBy,
            SignedAt = invoice.SignedAt
        };

        foreach (var item in invoice.Items.OrderBy(x => x.SortOrder))
        {
            decimal discountRate = (item.Quantity * item.UnitPrice) > 0
                ? Math.Round((item.DiscountAmount / (item.Quantity * item.UnitPrice)) * 100m, 2)
                : 0;

            dto.Items.Add(new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
                SortOrder = item.SortOrder,
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountRate = discountRate,
                DiscountAmount = item.DiscountAmount,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                LineTotal = item.LineTotal
            });
        }

        // Calculate Tax Breakdowns (THTTLTSuat)
        dto.TaxBreakdowns = invoice.Items
            .GroupBy(x => x.TaxRate)
            .OrderBy(x => x.Key)
            .Select(g => new TaxBreakdownDto
            {
                TaxRate = g.Key,
                AmountBeforeTax = g.Sum(x => x.Quantity * x.UnitPrice - x.DiscountAmount),
                TaxAmount = g.Sum(x => x.TaxAmount)
            })
            .ToList();

        return dto;
    }

    public async Task<OrderDocumentDto?> GetOrderDocumentByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices
            .Include(x => x.Items)
            .Include(x => x.SalesOrder)
                .ThenInclude(so => so!.Items)
                    .ThenInclude(soi => soi.Product)
                        .ThenInclude(p => p.Category)
            .Include(x => x.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

        if (inv == null) return null;

        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        var doc = new OrderDocumentDto
        {
            OrderId = inv.SalesOrderId ?? inv.Id,
            OrderCode = inv.SalesOrder?.Code ?? inv.InvoiceNumber,
            OrderDate = inv.InvoiceDate,
            CustomerName = inv.CustomerName,
            CustomerPhone = inv.Customer?.Phone ?? inv.SalesOrder?.CustomerPhone ?? "",
            CustomerAddress = inv.CustomerAddress ?? inv.Customer?.Address ?? inv.SalesOrder?.CustomerAddress,
            CompanyName = !string.IsNullOrWhiteSpace(company?.CompanyName) ? company.CompanyName : "TỔNG KHO THIẾT BỊ VỆ SINH LACASA",
            Email = !string.IsNullOrWhiteSpace(company?.Email) ? company.Email : "tongkhothietbivesinh@gmail.com",
            Hotline = !string.IsNullOrWhiteSpace(company?.OrderHotline) ? company.OrderHotline : (company?.Phone ?? "0369.074.789 - Hotline"),
            CustomerNotes = !string.IsNullOrWhiteSpace(inv.Notes) ? inv.Notes : company?.DefaultOrderNote,
            VatNote = !string.IsNullOrWhiteSpace(company?.DefaultVatNote) ? company.DefaultVatNote : "Đơn giá trên chưa bao gồm thuế GTGT (8%).",
            BankAccountHolder = company?.BankAccountHolder ?? "TRẦN VĂN TUẤN",
            BankAccount = company?.BankAccount ?? "4987.9177",
            BankName = company?.BankName ?? "NGÂN HÀNG Á CHÂU (ACB)",
            QrCodePath = company?.OrderQrCodePath ?? "/images/lacasa_qr.png",
            FooterNote1 = company?.OrderFooterNote1 ?? "Quý khách kiểm tra hàng hóa đúng số lượng trên hóa đơn và kiểm hàng trước khi rời khỏi kho Lacasa, kẻ vỡ Lacasa không chịu trách nhiệm.",
            FooterNote2 = company?.OrderFooterNote2 ?? "Hàng hóa mua không nhận trả hàng ngoại trừ hàng bị lỗi do nhà sản xuất, đổi trả trong vòng 10 ngày kể từ ngày xuất kho."
        };

        int stt = 1;
        foreach (var itm in inv.Items.OrderBy(x => x.SortOrder))
        {
            var soItem = inv.SalesOrder?.Items.FirstOrDefault(x => x.ProductId == itm.ProductId || x.ProductCode == itm.ProductCode);
            string categoryName = soItem?.Product?.Category?.Name ?? "";
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                categoryName = itm.ProductName;
            }

            string description = soItem?.Product?.Specifications ?? soItem?.Product?.Description ?? itm.ProductName;
            decimal discountedPrice = itm.Quantity > 0 && itm.DiscountAmount > 0 
                ? (itm.UnitPrice - (itm.DiscountAmount / itm.Quantity)) 
                : itm.UnitPrice;
            decimal lineTotal = itm.Quantity * discountedPrice;

            doc.Items.Add(new OrderDocumentItemDto
            {
                No = stt++,
                CategoryName = categoryName.ToUpper(),
                ProductCode = itm.ProductCode,
                Description = description,
                Quantity = itm.Quantity,
                UnitPrice = itm.UnitPrice,
                DiscountedPrice = discountedPrice,
                LineTotal = lineTotal
            });
        }

        doc.TotalQuantity = doc.Items.Sum(x => x.Quantity);
        doc.TotalAmount = doc.Items.Sum(x => x.LineTotal);
        doc.GrandTotal = doc.TotalAmount;

        return doc;
    }

    public async Task<OrderDocumentDto?> GetOrderDocumentByOrderIdAsync(int salesOrderId, CancellationToken cancellationToken = default)
    {
        var order = await _context.SalesOrders
            .Include(x => x.Items)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.Category)
            .Include(x => x.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken);

        if (order == null) return null;

        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);

        var doc = new OrderDocumentDto
        {
            OrderId = order.Id,
            OrderCode = order.Code,
            OrderDate = order.OrderDate,
            CustomerName = order.CustomerName,
            CustomerPhone = order.CustomerPhone ?? order.Customer?.Phone,
            CustomerAddress = order.CustomerAddress ?? order.Customer?.Address,
            CompanyName = !string.IsNullOrWhiteSpace(company?.CompanyName) ? company.CompanyName : "TỔNG KHO THIẾT BỊ VỆ SINH LACASA",
            Email = !string.IsNullOrWhiteSpace(company?.Email) ? company.Email : "tongkhothietbivesinh@gmail.com",
            Hotline = !string.IsNullOrWhiteSpace(company?.OrderHotline) ? company.OrderHotline : (company?.Phone ?? "0369.074.789 - Hotline"),
            CustomerNotes = !string.IsNullOrWhiteSpace(order.Notes) ? order.Notes : company?.DefaultOrderNote,
            VatNote = !string.IsNullOrWhiteSpace(company?.DefaultVatNote) ? company.DefaultVatNote : "Đơn giá trên chưa bao gồm thuế GTGT (8%).",
            BankAccountHolder = company?.BankAccountHolder ?? "TRẦN VĂN TUẤN",
            BankAccount = company?.BankAccount ?? "4987.9177",
            BankName = company?.BankName ?? "NGÂN HÀNG Á CHÂU (ACB)",
            QrCodePath = company?.OrderQrCodePath ?? "/images/lacasa_qr.png",
            FooterNote1 = company?.OrderFooterNote1 ?? "Quý khách kiểm tra hàng hóa đúng số lượng trên hóa đơn và kiểm hàng trước khi rời khỏi kho Lacasa, kẻ vỡ Lacasa không chịu trách nhiệm.",
            FooterNote2 = company?.OrderFooterNote2 ?? "Hàng hóa mua không nhận trả hàng ngoại trừ hàng bị lỗi do nhà sản xuất, đổi trả trong vòng 10 ngày kể từ ngày xuất kho."
        };

        int stt = 1;
        foreach (var oi in order.Items)
        {
            string categoryName = oi.Product?.Category?.Name ?? "";
            if (string.IsNullOrWhiteSpace(categoryName))
            {
                categoryName = oi.ProductName;
            }

            string description = oi.Product?.Specifications ?? oi.Product?.Description ?? oi.ProductName;
            decimal discountedPrice = oi.Quantity > 0 && oi.DiscountAmount > 0 
                ? (oi.UnitPrice - (oi.DiscountAmount / oi.Quantity)) 
                : oi.UnitPrice;
            decimal lineTotal = oi.Quantity * discountedPrice;

            doc.Items.Add(new OrderDocumentItemDto
            {
                No = stt++,
                CategoryName = categoryName.ToUpper(),
                ProductCode = oi.ProductCode,
                Description = description,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                DiscountedPrice = discountedPrice,
                LineTotal = lineTotal
            });
        }

        doc.TotalQuantity = doc.Items.Sum(x => x.Quantity);
        doc.TotalAmount = doc.Items.Sum(x => x.LineTotal);
        doc.GrandTotal = doc.TotalAmount;

        return doc;
    }

    public Task<byte[]> GenerateOrderDocumentPdfAsync(OrderDocumentDto model, CancellationToken cancellationToken = default)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        var pdfDoc = new Pdf.OrderDocumentPdf(model, _webHostEnvironment?.WebRootPath);
        byte[] bytes = pdfDoc.GeneratePdf();
        return Task.FromResult(bytes);
    }
}
