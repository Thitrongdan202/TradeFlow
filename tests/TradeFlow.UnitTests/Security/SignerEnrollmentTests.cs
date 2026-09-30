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
using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services.Security;
using Xunit;

namespace TradeFlow.UnitTests.Security;

public class SignerEnrollmentTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly RsaSoftwareSigningProvider _signingProvider;
    private readonly SecurityService _sut;
    private readonly ApplicationUser _testUser;

    public SignerEnrollmentTests()
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

        _testUser = new ApplicationUser
        {
            Id = "user-123",
            UserName = "nguyenvana",
            FullName = "Nguyễn Văn A",
            Email = "vana@tradeflow.vn",
            Status = UserStatus.Active
        };

        _userManagerMock.Setup(m => m.FindByIdAsync("user-123"))
            .ReturnsAsync(_testUser);

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
    public async Task IssueEnrollmentCodeAsync_GeneratesSecureOneTimeCode_StoresOnlyHashInDb()
    {
        // Act
        var result = await _sut.IssueEnrollmentCodeAsync(
            targetUserId: _testUser.Id,
            targetRole: SignerRole.Director,
            position: "Giám đốc điều hành",
            expirationMinutes: 60,
            issuedBy: "admin");

        // Assert
        result.Succeeded.Should().BeTrue();
        result.RawCode.Should().NotBeNullOrWhiteSpace();
        result.RawCode.Should().StartWith("TF-SIGN-");

        // Verify database stores only SHA-256 hash, NEVER the raw code
        var codeInDb = await _context.SignerEnrollmentCodes.FirstOrDefaultAsync();
        codeInDb.Should().NotBeNull();
        codeInDb!.TargetUserId.Should().Be(_testUser.Id);
        codeInDb.TargetSignerRole.Should().Be(SignerRole.Director);
        codeInDb.TargetPosition.Should().Be("Giám đốc điều hành");
        codeInDb.IsUsed.Should().BeFalse();
        codeInDb.IsRevoked.Should().BeFalse();

        // The hash in DB must match SHA-256 of raw code
        string expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(result.RawCode!)));
        codeInDb.CodeHash.Should().Be(expectedHash);
        codeInDb.CodeHash.Should().NotBe(result.RawCode);
    }

    [Fact]
    public async Task EnrollSignerAsync_WithValidCodeAndPin_EnrollsSignerAndMarksCodeUsed()
    {
        // Arrange
        var issueResult = await _sut.IssueEnrollmentCodeAsync(
            targetUserId: _testUser.Id,
            targetRole: SignerRole.Director,
            position: "Giám đốc",
            expirationMinutes: 30,
            issuedBy: "board");

        string pin = "SignerPin123!";

        // Act
        var enrollResult = await _sut.EnrollSignerAsync(issueResult.RawCode!, pin, performedBy: _testUser.UserName);

        // Assert
        enrollResult.Succeeded.Should().BeTrue();
        enrollResult.CertificateSerialNumber.Should().NotBeNullOrWhiteSpace();
        enrollResult.FullName.Should().Be("Nguyễn Văn A");

        // Code in DB should now be marked as used
        var codeInDb = await _context.SignerEnrollmentCodes.FirstOrDefaultAsync();
        codeInDb!.IsUsed.Should().BeTrue();
        codeInDb.UsedAt.Should().NotBeNull();

        // Signer identity record must exist
        var signer = await _context.SignerIdentities.FirstOrDefaultAsync();
        signer.Should().NotBeNull();
        signer!.UserId.Should().Be(_testUser.Id);
        signer.SignerRole.Should().Be(SignerRole.Director);
        signer.Position.Should().Be("Giám đốc");
        signer.Status.Should().Be(SignerStatus.Active);
        signer.CertificateSerialNumber.Should().Be(enrollResult.CertificateSerialNumber);
    }

    [Fact]
    public async Task EnrollSignerAsync_ReusingSameCodeTwice_Fails()
    {
        // Arrange
        var issueResult = await _sut.IssueEnrollmentCodeAsync(
            targetUserId: _testUser.Id,
            targetRole: SignerRole.AuthorizedSigner,
            position: "Kế toán viên",
            expirationMinutes: 30,
            issuedBy: "admin");

        // First use
        var firstResult = await _sut.EnrollSignerAsync(issueResult.RawCode!, "Pin123456");
        firstResult.Succeeded.Should().BeTrue();

        // Act - Second use with same code
        var secondResult = await _sut.EnrollSignerAsync(issueResult.RawCode!, "Pin123456");

        // Assert
        secondResult.Succeeded.Should().BeFalse();
        secondResult.ErrorMessage.Should().Contain("đã được sử dụng");
    }

    [Fact]
    public async Task EnrollSignerAsync_WithExpiredCode_Fails()
    {
        // Arrange - manually insert an expired code
        string rawCode = "TF-SIGN-EXPIRED-CODE-0001";
        string codeHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawCode)));

        _context.SignerEnrollmentCodes.Add(new SignerEnrollmentCode
        {
            CodeHash = codeHash,
            TargetUserId = _testUser.Id,
            TargetUserName = _testUser.UserName ?? string.Empty,
            TargetFullName = _testUser.FullName,
            TargetPosition = "Phó Giám đốc",
            TargetSignerRole = SignerRole.DeputyDirector,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10), // expired 10 mins ago
            IsUsed = false,
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            CreatedBy = "admin"
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.EnrollSignerAsync(rawCode, "Pin123456");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("hết hạn");
    }

    [Fact]
    public async Task EnrollSignerAsync_WithRevokedCode_Fails()
    {
        // Arrange
        var issueResult = await _sut.IssueEnrollmentCodeAsync(
            _testUser.Id, SignerRole.AuthorizedSigner, "Kế toán", 30, "admin");

        var codeInDb = await _context.SignerEnrollmentCodes.FirstAsync();
        await _sut.RevokeEnrollmentCodeAsync(codeInDb.Id, "Nghi ngờ lộ mã", "admin");

        // Act
        var result = await _sut.EnrollSignerAsync(issueResult.RawCode!, "Pin123456");

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("thu hồi");
    }

    [Fact]
    public async Task RevokeSignerAsync_SetsSignerStatusToRevoked_WithReason()
    {
        // Arrange
        var issueResult = await _sut.IssueEnrollmentCodeAsync(
            _testUser.Id, SignerRole.Director, "Giám đốc", 30, "admin");
        var enrollResult = await _sut.EnrollSignerAsync(issueResult.RawCode!, "Pin123456");

        // Act
        bool revoked = await _sut.RevokeSignerAsync(enrollResult.SignerIdentityId!.Value, "Hết nhiệm kỳ đại diện pháp luật", "HĐQT");

        // Assert
        revoked.Should().BeTrue();
        var signer = await _context.SignerIdentities.FindAsync(enrollResult.SignerIdentityId.Value);
        signer!.Status.Should().Be(SignerStatus.Revoked);
        signer.RevocationReason.Should().Be("Hết nhiệm kỳ đại diện pháp luật");
        signer.RevokedBy.Should().Be("HĐQT");
        signer.RevokedAt.Should().NotBeNull();
    }
}
