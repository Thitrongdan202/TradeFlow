using System;
using System.Collections.Generic;
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
using TradeFlow.Infrastructure.Services.Security;
using Xunit;

namespace TradeFlow.UnitTests.Security;

public class EmployeeOffboardingTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly RsaSoftwareSigningProvider _signingProvider;
    private readonly SecurityService _sut;

    private readonly ApplicationUser _departingEmployee;
    private readonly ApplicationUser _adminUser;

    public EmployeeOffboardingTests()
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

        _departingEmployee = new ApplicationUser
        {
            Id = "user-dep-456",
            UserName = "hoangminh",
            FullName = "Hoàng Minh",
            Email = "minh@tradeflow.vn",
            Status = UserStatus.Active,
            SecurityStamp = "initial-stamp-123"
        };

        _adminUser = new ApplicationUser
        {
            Id = "user-admin-001",
            UserName = "admin",
            FullName = "Hệ Thống Admin",
            Email = "admin@tradeflow.vn",
            Status = UserStatus.Active
        };

        _userManagerMock.Setup(m => m.FindByIdAsync("user-dep-456"))
            .ReturnsAsync(_departingEmployee);

        _userManagerMock.Setup(m => m.FindByIdAsync("user-admin-001"))
            .ReturnsAsync(_adminUser);

        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.UpdateSecurityStampAsync(It.IsAny<ApplicationUser>()))
            .Callback<ApplicationUser>(u => u.SecurityStamp = Guid.NewGuid().ToString())
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync("valid-reset-token");

        _userManagerMock.Setup(m => m.ResetPasswordAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _sut = new SecurityService(
            _context,
            _userManagerMock.Object,
            _signingProvider,
            _auditServiceMock.Object,
            NullLogger<SecurityService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task OffboardEmployeeAsync_DisablesAccount_InvalidatesSessions_AndRevokesCertificates()
    {
        // Arrange
        // 1. Create active signer identity for departing employee
        var signer = new SignerIdentity
        {
            UserId = _departingEmployee.Id,
            UserName = _departingEmployee.UserName ?? string.Empty,
            FullName = _departingEmployee.FullName,
            Position = "Kế toán trưởng",
            SignerRole = SignerRole.AuthorizedSigner,
            Status = SignerStatus.Active,
            CertificateSerialNumber = "CERT-SN-9999",
            CertificateSubject = "CN=HOANG MINH",
            ValidFrom = DateTime.UtcNow.AddMonths(-1),
            ValidTo = DateTime.UtcNow.AddMonths(11),
            EncryptedPrivateKey = "ENC_KEY",
            KeySalt = "SALT",
            PinVerificationHash = "HASH",
            PublicKeyPem = "PEM",
            PublicKeyXml = "XML",
            EnrollmentCodeHash = "CODE_HASH",
            EnrolledBy = "admin",
            EnrolledAt = DateTime.UtcNow.AddMonths(-1)
        };
        _context.SignerIdentities.Add(signer);

        // 2. Create pending enrollment code
        var pendingCode = new SignerEnrollmentCode
        {
            CodeHash = "PENDING_HASH",
            TargetUserId = _departingEmployee.Id,
            TargetUserName = _departingEmployee.UserName ?? string.Empty,
            TargetFullName = _departingEmployee.FullName,
            TargetPosition = "Phó Giám đốc",
            TargetSignerRole = SignerRole.DeputyDirector,
            ExpiresAt = DateTime.UtcNow.AddDays(1),
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "admin"
        };
        _context.SignerEnrollmentCodes.Add(pendingCode);

        // 3. Create a historic invoice signed by this employee
        var historicInvoice = new Invoice
        {
            InvoiceSeries = "1C26TFL",
            InvoiceNo = "00000010",
            InvoiceDate = DateTime.UtcNow.AddDays(-10),
            Status = InvoiceStatus.Issued,
            Type = InvoiceType.SalesInvoice,
            CustomerId = 1,
            CustomerName = "Công ty Đối Tác",
            CompanyTaxCode = "0101234567",
            GrandTotal = 20_000_000,
            SignatureStatus = DigitalSignatureStatus.Signed,
            SignerIdentityId = signer.Id,
            SignedBy = signer.FullName,
            SignatureValue = "HISTORIC_SIGNATURE_DATA"
        };
        _context.Invoices.Add(historicInvoice);
        await _context.SaveChangesAsync();

        string initialStamp = _departingEmployee.SecurityStamp ?? string.Empty;

        // Act - Execute Offboarding
        var result = await _sut.OffboardEmployeeAsync(
            userId: _departingEmployee.Id,
            reason: "Nghỉ việc theo nguyện vọng cá nhân",
            performedBy: "admin");

        // Assert
        result.Succeeded.Should().BeTrue();
        result.RevokedSignerIdentitiesCount.Should().Be(1);

        // Account is disabled
        _departingEmployee.Status.Should().Be(UserStatus.Disabled);

        // Active sessions invalidated
        _departingEmployee.SecurityStamp.Should().NotBe(initialStamp);

        // Signer identity revoked
        var updatedSigner = await _context.SignerIdentities.FindAsync(signer.Id);
        updatedSigner!.Status.Should().Be(SignerStatus.Revoked);
        updatedSigner.RevocationReason.Should().Contain("Nghỉ việc");
        updatedSigner.RevokedBy.Should().Be("admin");

        // Pending codes revoked
        var updatedCode = await _context.SignerEnrollmentCodes.FindAsync(pendingCode.Id);
        updatedCode!.IsRevoked.Should().BeTrue();
        updatedCode.RevocationReason.Should().Contain("Nghỉ việc");

        // Historic signed invoices MUST remain intact and signed
        var preservedInvoice = await _context.Invoices.FindAsync(historicInvoice.Id);
        preservedInvoice.Should().NotBeNull();
        preservedInvoice!.SignatureStatus.Should().Be(DigitalSignatureStatus.Signed);
        preservedInvoice.SignedBy.Should().Be(signer.FullName);
        preservedInvoice.SignatureValue.Should().Be("HISTORIC_SIGNATURE_DATA");

        // Audit log verified
        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.UserOffboarded,
            "admin",
            "ApplicationUser",
            _departingEmployee.UserName,
            It.Is<string>(desc => desc.Contains("Nghỉ việc"))), Times.Once);
    }

    [Fact]
    public async Task OffboardEmployeeAsync_CannotOffboardSelf()
    {
        // Act - admin attempts to offboard themselves
        var result = await _sut.OffboardEmployeeAsync(
            userId: _adminUser.Id,
            reason: "Tự hủy tài khoản",
            performedBy: _adminUser.UserName!);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("không thể tự thực hiện quy trình thôi việc cho chính mình");
    }

    [Fact]
    public async Task AdminResetPasswordAsync_GeneratesSecureTempPassword_AndInvalidatesActiveSessions()
    {
        // Arrange
        string initialStamp = _departingEmployee.SecurityStamp ?? string.Empty;

        // Act
        var result = await _sut.AdminResetPasswordAsync(_departingEmployee.Id, "admin");

        // Assert
        result.Succeeded.Should().BeTrue();
        result.TempPassword.Should().NotBeNullOrWhiteSpace();
        result.TempPassword!.Length.Should().BeGreaterThanOrEqualTo(10);

        // Security stamp must change to revoke old active sessions
        _departingEmployee.SecurityStamp.Should().NotBe(initialStamp);

        // Audit log must NOT contain the plaintext password!
        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.PasswordReset,
            "admin",
            "ApplicationUser",
            _departingEmployee.UserName,
            It.Is<string>(desc => !desc.Contains(result.TempPassword!))), Times.Once);
    }
}
