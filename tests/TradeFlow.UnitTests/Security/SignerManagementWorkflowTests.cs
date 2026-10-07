using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
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

public class SignerManagementWorkflowTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly RsaSoftwareSigningProvider _signingProvider;
    private readonly SecurityService _securityService;
    private readonly DigitalSignatureService _signatureService;
    private readonly ApplicationUser _directorUser;

    public SignerManagementWorkflowTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _auditServiceMock = new Mock<IAuditService>();
        _signingProvider = new RsaSoftwareSigningProvider();

        _directorUser = new ApplicationUser
        {
            Id = "user-giamdoc",
            UserName = "giamdoc",
            FullName = "Nguyễn Văn Giám Đốc",
            Email = "giamdoc@tradeflow.vn",
            Status = UserStatus.Active
        };

        _userManagerMock.Setup(m => m.FindByIdAsync("user-giamdoc"))
            .ReturnsAsync(_directorUser);

        _securityService = new SecurityService(
            _context,
            _userManagerMock.Object,
            _signingProvider,
            _auditServiceMock.Object,
            NullLogger<SecurityService>.Instance);

        _signatureService = new DigitalSignatureService(
            _context,
            _signingProvider,
            _auditServiceMock.Object,
            NullLogger<DigitalSignatureService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task Provider_ChangePinAsync_WithCorrectOldPin_SucceedsAndCanSignWithNewPin()
    {
        // Arrange
        string oldPin = "OldSecretPin123!";
        string newPin = "NewSecretPin456!";
        var keyPair = await _signingProvider.GenerateKeyPairAsync(oldPin, "CN=TEST PIN CHANGE");

        // Act
        var changeResult = await _signingProvider.ChangePinAsync(
            keyPair.EncryptedPrivateKey,
            keyPair.KeySalt,
            oldPin,
            newPin);

        // Assert
        changeResult.Should().NotBeNull();
        changeResult.EncryptedPrivateKey.Should().NotBeNullOrWhiteSpace();
        changeResult.KeySalt.Should().NotBe(keyPair.KeySalt, "phải tạo salt mới khi đổi PIN");

        // Sign with new PIN using newly encrypted private key
        byte[] testData = Encoding.UTF8.GetBytes("DATA_TO_SIGN");
        byte[] signature = await _signingProvider.SignDataAsync(
            testData,
            changeResult.EncryptedPrivateKey,
            changeResult.KeySalt,
            newPin);

        signature.Should().NotBeNullOrEmpty();
        bool isValid = await _signingProvider.VerifySignatureAsync(testData, signature, keyPair.PublicKeyPem);
        isValid.Should().BeTrue("chữ ký với mã PIN mới phải hợp lệ với khóa công khai ban đầu");

        // Attempting to sign with old PIN should fail
        var actOldPin = async () => await _signingProvider.SignDataAsync(
            testData,
            changeResult.EncryptedPrivateKey,
            changeResult.KeySalt,
            oldPin);

        await actOldPin.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task Provider_ChangePinAsync_WithWrongOldPin_ThrowsCryptographicException()
    {
        // Arrange
        var keyPair = await _signingProvider.GenerateKeyPairAsync("RightPin123", "CN=TEST WRONG PIN");

        // Act & Assert
        var act = async () => await _signingProvider.ChangePinAsync(
            keyPair.EncryptedPrivateKey,
            keyPair.KeySalt,
            "WrongPin999",
            "NewPin888");

        await act.Should().ThrowAsync<CryptographicException>();
    }

    [Fact]
    public async Task PreProvisionedSigner_SetupPinAndCredential_EnablesSigning()
    {
        // Arrange - Pre-provisioned signer profile in DB (without credential)
        var preProvisionedSigner = new SignerIdentity
        {
            UserId = _directorUser.Id,
            UserName = _directorUser.UserName ?? "giamdoc",
            FullName = "Nguyễn Văn Giám Đốc",
            Position = "Giám đốc",
            SignerRole = SignerRole.Director,
            Status = SignerStatus.Active,
            ProviderType = SigningProviderType.SoftwareRsa,
            HandwrittenSignatureImage = "data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=",
            EnrolledBy = "SYSTEM",
            EnrolledAt = DateTime.UtcNow
        };
        _context.SignerIdentities.Add(preProvisionedSigner);
        await _context.SaveChangesAsync();

        preProvisionedSigner.HasCredential.Should().BeFalse();

        // Act - Setup PIN and generate RSA credentials
        string initialPin = "GiamDocPin2026!";
        var result = await _securityService.SetupSignerPinAndCredentialAsync(
            preProvisionedSigner.Id,
            initialPin,
            performedBy: "admin");

        // Assert
        result.Succeeded.Should().BeTrue();
        var reloaded = await _context.SignerIdentities.FindAsync(preProvisionedSigner.Id);
        reloaded.Should().NotBeNull();
        reloaded!.HasCredential.Should().BeTrue();
        reloaded.CertificateSerialNumber.Should().NotBeNullOrWhiteSpace();
        reloaded.CertificateSubject.Should().Contain("GIÁM ĐỐC");
        reloaded.EncryptedPrivateKey.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task IssueEnrollmentCode_WithExpirationDisabled_CreatesCodeWithoutExpiration()
    {
        // Act
        var result = await _securityService.IssueEnrollmentCodeAsync(
            targetUserId: _directorUser.Id,
            targetRole: SignerRole.Director,
            position: "Tổng Giám đốc",
            hasExpiration: false,
            expirationMinutes: null,
            issuedBy: "admin");

        // Assert
        result.Succeeded.Should().BeTrue();
        result.HasExpiration.Should().BeFalse();
        result.ExpiresAt.Should().BeNull();

        var codeInDb = await _context.SignerEnrollmentCodes.FirstOrDefaultAsync();
        codeInDb.Should().NotBeNull();
        codeInDb!.HasExpiration.Should().BeFalse();
        codeInDb.ExpiresAt.Should().BeNull();
        codeInDb.IsUsed.Should().BeFalse();
        codeInDb.IsRevoked.Should().BeFalse();

        var codesDto = await _securityService.GetEnrollmentCodesAsync();
        var codeDto = codesDto.FirstOrDefault(c => c.Id == codeInDb.Id);
        codeDto.Should().NotBeNull();
        codeDto!.HasExpiration.Should().BeFalse();
        codeDto.IsExpired.Should().BeFalse();
        codeDto.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task SigningWithWrongPin_IncrementsFailedAttempts_AndLocksOutAfterMaxAttempts()
    {
        // Arrange
        string pin = "CorrectPin123!";
        var keyPair = await _signingProvider.GenerateKeyPairAsync(pin, "CN=TEST LOCKOUT");

        var signer = new SignerIdentity
        {
            UserId = _directorUser.Id,
            UserName = _directorUser.UserName ?? "giamdoc",
            FullName = "Nguyễn Văn Giám Đốc",
            Position = "Giám đốc",
            SignerRole = SignerRole.Director,
            Status = SignerStatus.Active,
            ProviderType = SigningProviderType.SoftwareRsa,
            CertificateSerialNumber = keyPair.CertificateSerialNumber,
            CertificateSubject = "CN=TEST LOCKOUT",
            CertificateIssuer = "TradeFlow CA",
            CertificateThumbprint = keyPair.CertificateThumbprint,
            ValidFrom = DateTime.UtcNow.AddDays(-1),
            ValidTo = DateTime.UtcNow.AddYears(1),
            EncryptedPrivateKey = keyPair.EncryptedPrivateKey,
            KeySalt = keyPair.KeySalt,
            PinVerificationHash = keyPair.PinVerificationHash,
            PublicKeyPem = keyPair.PublicKeyPem,
            HandwrittenSignatureImage = "data:image/svg+xml;base64,TEST_SIGNATURE_IMG",
            EnrolledBy = "admin",
            EnrolledAt = DateTime.UtcNow
        };
        _context.SignerIdentities.Add(signer);

        var invoice = new Invoice
        {
            InvoiceNumber = "INV-2026-0001",
            InvoiceNo = "00000001",
            InvoiceSeries = "1C26TAA",
            Type = InvoiceType.VatInvoice,
            InvoiceDate = DateTime.UtcNow,
            CustomerId = 1,
            CustomerName = "Khách hàng Test",
            PaymentMethod = "Chuyển khoản",
            Status = InvoiceStatus.Issued,
            SubTotal = 1000000,
            TotalTax = 100000,
            GrandTotal = 1100000
        };
        _context.Invoices.Add(invoice);
        _context.CompanySecuritySettings.Add(new CompanySecuritySettings { MaxFailedSignAttempts = 3 });
        await _context.SaveChangesAsync();

        // Act 1: 1st wrong attempt
        var r1 = await _signatureService.SignInvoiceAsync(invoice, signer.Id, "WrongPin1");
        r1.Succeeded.Should().BeFalse();
        r1.ErrorMessage.Should().Contain("Mã PIN không chính xác");

        var signerAfterR1 = await _context.SignerIdentities.FindAsync(signer.Id);
        signerAfterR1!.FailedPinAttempts.Should().Be(1);
        signerAfterR1.IsLockedOut.Should().BeFalse();

        // Act 2: 2nd wrong attempt
        var r2 = await _signatureService.SignInvoiceAsync(invoice, signer.Id, "WrongPin2");
        r2.Succeeded.Should().BeFalse();

        var signerAfterR2 = await _context.SignerIdentities.FindAsync(signer.Id);
        signerAfterR2!.FailedPinAttempts.Should().Be(2);
        signerAfterR2.IsLockedOut.Should().BeFalse();

        // Act 3: 3rd wrong attempt -> locks out for 15 minutes!
        var r3 = await _signatureService.SignInvoiceAsync(invoice, signer.Id, "WrongPin3");
        r3.Succeeded.Should().BeFalse();
        r3.ErrorMessage.Should().Contain("đã bị tạm khóa");

        var signerAfterR3 = await _context.SignerIdentities.FindAsync(signer.Id);
        signerAfterR3!.FailedPinAttempts.Should().Be(3);
        signerAfterR3.IsLockedOut.Should().BeTrue();
        signerAfterR3.LockoutEnd.Should().NotBeNull();
        signerAfterR3.LockoutEnd!.Value.Should().BeAfter(DateTime.UtcNow.AddMinutes(14));

        // Act 4: Attempt with correct PIN while locked out -> still blocked
        var r4 = await _signatureService.SignInvoiceAsync(invoice, signer.Id, pin);
        r4.Succeeded.Should().BeFalse();
        r4.ErrorMessage.Should().Contain("đang bị tạm khóa").And.Contain("do nhập sai mã PIN");

        // Act 5: Admin unlocks signer
        bool unlockResult = await _securityService.UnlockSignerLockoutAsync(signer.Id, "admin");
        unlockResult.Should().BeTrue();

        var signerAfterUnlock = await _context.SignerIdentities.FindAsync(signer.Id);
        signerAfterUnlock!.IsLockedOut.Should().BeFalse();
        signerAfterUnlock.FailedPinAttempts.Should().Be(0);

        // Act 6: Signing with correct PIN now succeeds and copies handwritten signature
        var rSuccess = await _signatureService.SignInvoiceAsync(invoice, signer.Id, pin);
        rSuccess.Succeeded.Should().BeTrue();

        var signedInvoice = await _context.Invoices.FindAsync(invoice.Id);
        signedInvoice!.SignatureStatus.Should().Be(DigitalSignatureStatus.Signed);
        signedInvoice.SignatureValue.Should().NotBeNullOrWhiteSpace();
        signedInvoice.SignerHandwrittenSignatureImage.Should().Be("data:image/svg+xml;base64,TEST_SIGNATURE_IMG");
    }

    [Fact]
    public async Task UpdateSignerProfileAsync_UpdatesPositionAndHandwrittenSignatureImage()
    {
        // Arrange
        var signer = new SignerIdentity
        {
            UserId = _directorUser.Id,
            UserName = _directorUser.UserName ?? "giamdoc",
            FullName = "Nguyễn Văn A",
            Position = "Phó Giám đốc",
            SignerRole = SignerRole.DeputyDirector,
            Status = SignerStatus.Active,
            HandwrittenSignatureImage = null,
            EnrolledBy = "admin",
            EnrolledAt = DateTime.UtcNow
        };
        _context.SignerIdentities.Add(signer);
        await _context.SaveChangesAsync();

        string newSvg = "data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciPjwvc3ZnPg==";

        // Act
        bool result = await _securityService.UpdateSignerProfileAsync(
            signer.Id,
            fullName: "Nguyễn Văn B",
            position: "Quyền Giám đốc",
            handwrittenSignatureImage: newSvg,
            performedBy: "admin");

        // Assert
        result.Should().BeTrue();
        var reloaded = await _context.SignerIdentities.FindAsync(signer.Id);
        reloaded!.FullName.Should().Be("Nguyễn Văn B");
        reloaded.Position.Should().Be("Quyền Giám đốc");
        reloaded.HandwrittenSignatureImage.Should().Be(newSvg);
    }
}
