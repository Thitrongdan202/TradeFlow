using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using TradeFlow.Infrastructure.Services.Security;
using Xunit;

namespace TradeFlow.UnitTests.Security;

public class DigitalSignatureTamperTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly RsaSoftwareSigningProvider _signingProvider;
    private readonly DigitalSignatureService _sut;
    private SignerIdentity _activeSigner = null!;
    private const string SignerPin = "DirectorSecretPin123!";

    public DigitalSignatureTamperTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);

        _signingProvider = new RsaSoftwareSigningProvider();
        var auditMock = new Mock<IAuditService>();

        _sut = new DigitalSignatureService(
            _context,
            _signingProvider,
            auditMock.Object,
            NullLogger<DigitalSignatureService>.Instance);

        InitializeSignerAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeSignerAsync()
    {
        var keyPair = await _signingProvider.GenerateKeyPairAsync(
            SignerPin,
            "CN=GIAM DOC, O=TRADEFLOW, C=VN");

        _activeSigner = new SignerIdentity
        {
            UserId = "user-director",
            UserName = "director",
            FullName = "Trần Văn Giám Đốc",
            Position = "Tổng Giám đốc",
            SignerRole = SignerRole.Director,
            Status = SignerStatus.Active,
            ProviderType = SigningProviderType.SoftwareRsa,
            CertificateSerialNumber = keyPair.CertificateSerialNumber,
            CertificateSubject = "CN=GIAM DOC, O=TRADEFLOW, C=VN",
            CertificateIssuer = "TradeFlow CA",
            CertificateThumbprint = keyPair.CertificateThumbprint,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddYears(1),
            EncryptedPrivateKey = keyPair.EncryptedPrivateKey,
            KeySalt = keyPair.KeySalt,
            PinVerificationHash = keyPair.PinVerificationHash,
            PublicKeyPem = keyPair.PublicKeyPem,
            PublicKeyXml = keyPair.PublicKeyXml,
            EnrollmentCodeHash = "TESTHASH",
            EnrolledBy = "Admin",
            EnrolledAt = DateTime.UtcNow
        };

        _context.SignerIdentities.Add(_activeSigner);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private Invoice CreateTestInvoice()
    {
        var inv = new Invoice
        {
            InvoiceSeries = "1C26TFL",
            InvoiceNo = "00000042",
            FormNumber = "1/001",
            InvoiceDate = new DateTime(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc),
            Status = InvoiceStatus.Issued,
            Type = InvoiceType.VatInvoice,
            CustomerId = 1,
            CustomerName = "Công ty TNHH Khách Hàng Thử Nghiệm",
            CustomerTaxCode = "0109999888",
            CustomerAddress = "Hà Nội",
            CompanyTaxCode = "0101234567",
            SubTotal = 100_000_000,
            TotalDiscount = 5_000_000,
            TotalTax = 9_500_000,
            GrandTotal = 104_500_000,
            SignatureStatus = DigitalSignatureStatus.Unsigned,
            Items = new List<InvoiceItem>
            {
                new InvoiceItem
                {
                    SortOrder = 1,
                    ProductId = 101,
                    ProductCode = "SP01",
                    ProductName = "Máy tính trạm Dell Precision",
                    Quantity = 2,
                    UnitPrice = 50_000_000,
                    DiscountAmount = 5_000_000,
                    TaxRate = 10,
                    TaxAmount = 9_500_000,
                    LineTotal = 104_500_000
                }
            }
        };

        _context.Invoices.Add(inv);
        _context.SaveChanges();
        return inv;
    }

    [Fact]
    public async Task SignInvoiceAsync_WithValidPin_ProducesLegallyCompliantSignature()
    {
        // Arrange
        var invoice = CreateTestInvoice();

        // Act
        var result = await _sut.SignInvoiceAsync(invoice, _activeSigner.Id, SignerPin);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.SignatureValue.Should().NotBeNullOrWhiteSpace();
        result.CertificateSerialNumber.Should().Be(_activeSigner.CertificateSerialNumber);
        result.DocumentHash.Should().NotBeNullOrWhiteSpace();

        // Invoice state must be updated
        invoice.SignatureStatus.Should().Be(DigitalSignatureStatus.Signed);
        invoice.SignatureValue.Should().NotBeNullOrWhiteSpace();
        invoice.SignedBy.Should().Be(_activeSigner.FullName);
        invoice.SignerIdentityId.Should().Be(_activeSigner.Id);
        invoice.DocumentHash.Should().Be(result.DocumentHash);

        // Verification immediately after signing must be VALID
        var verifyResult = await _sut.VerifyInvoiceSignatureAsync(invoice);
        verifyResult.IsValid.Should().BeTrue();
        verifyResult.Status.Should().Be(DigitalSignatureStatus.Valid);
        verifyResult.IsTampered.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyInvoiceSignatureAsync_WhenInvoiceTotalTamperedPostSigning_ReportsTampered()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        var signResult = await _sut.SignInvoiceAsync(invoice, _activeSigner.Id, SignerPin);
        signResult.Succeeded.Should().BeTrue();

        // ACT OF TAMPERING: Malicious database edit to reduce payment amount
        invoice.GrandTotal = 50_000_000;

        // Act - Run verification
        var verifyResult = await _sut.VerifyInvoiceSignatureAsync(invoice);

        // Assert
        verifyResult.IsValid.Should().BeFalse("hóa đơn bị can thiệp số tiền không được coi là hợp lệ");
        verifyResult.Status.Should().Be(DigitalSignatureStatus.Tampered);
        verifyResult.IsTampered.Should().BeTrue();
        verifyResult.Message.Should().ContainEquivalentOf("thay đổi sau khi ký");
        invoice.SignatureStatus.Should().Be(DigitalSignatureStatus.Tampered);
    }

    [Fact]
    public async Task VerifyInvoiceSignatureAsync_WhenLineItemTamperedPostSigning_ReportsTampered()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        var signResult = await _sut.SignInvoiceAsync(invoice, _activeSigner.Id, SignerPin);
        signResult.Succeeded.Should().BeTrue();

        // ACT OF TAMPERING: Modify line item quantity
        invoice.Items.First().Quantity = 999;

        // Act
        var verifyResult = await _sut.VerifyInvoiceSignatureAsync(invoice);

        // Assert
        verifyResult.IsValid.Should().BeFalse();
        verifyResult.Status.Should().Be(DigitalSignatureStatus.Tampered);
        verifyResult.IsTampered.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyInvoiceSignatureAsync_WhenSignerRevokedPostSigning_ReportsRevoked()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        await _sut.SignInvoiceAsync(invoice, _activeSigner.Id, SignerPin);

        // Signer certificate gets revoked
        _activeSigner.Status = SignerStatus.Revoked;
        _activeSigner.RevocationReason = "Khai trừ chức vụ";
        await _context.SaveChangesAsync();

        // Act
        var verifyResult = await _sut.VerifyInvoiceSignatureAsync(invoice);

        // Assert
        verifyResult.IsValid.Should().BeFalse();
        verifyResult.Status.Should().Be(DigitalSignatureStatus.Revoked);
        verifyResult.Message.Should().ContainEquivalentOf("thu hồi");
    }

    [Fact]
    public async Task VerifyInvoiceSignatureAsync_WhenSignerCertificateExpired_ReportsExpired()
    {
        // Arrange
        var invoice = CreateTestInvoice();
        await _sut.SignInvoiceAsync(invoice, _activeSigner.Id, SignerPin);

        // Certificate expiration in past
        _activeSigner.ValidTo = DateTime.UtcNow.AddDays(-10);
        await _context.SaveChangesAsync();

        // Act
        var verifyResult = await _sut.VerifyInvoiceSignatureAsync(invoice);

        // Assert
        verifyResult.IsValid.Should().BeFalse();
        verifyResult.Status.Should().Be(DigitalSignatureStatus.CertificateExpired);
        verifyResult.Message.Should().ContainEquivalentOf("hết hạn");
    }
}
