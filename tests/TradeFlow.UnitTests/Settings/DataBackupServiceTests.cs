using System;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Settings;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Settings;

public class DataBackupServiceTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly IConfiguration _configuration;
    private readonly string _tempWebRoot;
    private readonly DataBackupService _sut;

    public DataBackupServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);

        _auditServiceMock = new Mock<IAuditService>();

        _tempWebRoot = Path.Combine(Path.GetTempPath(), "TradeFlowBackupTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);
        Directory.CreateDirectory(Path.Combine(_tempWebRoot, "uploads"));

        // Create a dummy uploaded file to test packaging
        File.WriteAllText(Path.Combine(_tempWebRoot, "uploads", "test_doc.pdf"), "sample file content");

        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        var authServiceMock = new Mock<Microsoft.AspNetCore.Authorization.IAuthorizationService>();
        authServiceMock
            .Setup(a => a.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<string>()))
            .ReturnsAsync((ClaimsPrincipal u, object? r, string p) =>
            {
                if (u.IsInRole("Administrator") || u.HasClaim("Permission", "Permission:CompanySettings:View"))
                    return Microsoft.AspNetCore.Authorization.AuthorizationResult.Success();
                return Microsoft.AspNetCore.Authorization.AuthorizationResult.Failed();
            });

        // Build configuration pointing to the test database or mock connection string
        var configDict = new System.Collections.Generic.Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=tradeflow_dev;Username=postgres;Password=postgres"
        };
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

        _sut = new DataBackupService(
            _context,
            _configuration,
            _envMock.Object,
            _auditServiceMock.Object,
            authServiceMock.Object,
            NullLogger<DataBackupService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_tempWebRoot))
        {
            try { Directory.Delete(_tempWebRoot, true); } catch { }
        }
    }

    [Fact]
    public async Task CreateBackupAsync_WhenUnauthenticated_ReturnsUnauthorizedError()
    {
        // Arrange
        var options = new BackupOptions();
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await _sut.CreateBackupAsync(options, unauthenticatedUser);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quyền");
    }

    [Fact]
    public async Task CreateBackupAsync_WhenUserHasNoAdminOrPermission_ReturnsForbiddenError()
    {
        // Arrange
        var options = new BackupOptions();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "normal_user")
        }, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.CreateBackupAsync(options, user);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.ErrorMessage.Should().Contain("quyền");
    }

    [Fact]
    public async Task CreateBackupAsync_WhenUserIsAdmin_CreatesValidZipArchive()
    {
        // Arrange
        var options = new BackupOptions { IncludeUploads = true };
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Name, "administrator"),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "TestAuth");
        var adminUser = new ClaimsPrincipal(identity);

        // Act
        var result = await _sut.CreateBackupAsync(options, adminUser);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.BackupId.Should().NotBeNullOrEmpty();
        result.FilePath.Should().NotBeNullOrEmpty();
        File.Exists(result.FilePath).Should().BeTrue();

        // Inspect zip archive entries
        using (var zip = ZipFile.OpenRead(result.FilePath))
        {
            zip.GetEntry("database.dump").Should().NotBeNull();
            zip.GetEntry("manifest.json").Should().NotBeNull();
            zip.GetEntry("uploads/test_doc.pdf").Should().NotBeNull();

            // Verify manifest JSON contents
            var manifestEntry = zip.GetEntry("manifest.json")!;
            using var reader = new StreamReader(manifestEntry.Open());
            var manifestJson = await reader.ReadToEndAsync();
            var manifest = JsonSerializer.Deserialize<BackupManifest>(manifestJson);
            manifest.Should().NotBeNull();
            manifest!.AppName.Should().Contain("TradeFlow");
            manifest.DatabaseName.Should().Be("tradeflow_dev");
        }

        // Clean up created backup
        if (File.Exists(result.FilePath))
        {
            try { File.Delete(result.FilePath); } catch { }
        }
    }

    [Fact]
    public async Task GetPreparedBackupStreamAsync_WhenValidBackupId_ReturnsReadableStream()
    {
        // Arrange
        var options = new BackupOptions();
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Name, "administrator"),
            new Claim(ClaimTypes.Role, "Administrator")
        }, "TestAuth");
        var adminUser = new ClaimsPrincipal(identity);

        var backupResult = await _sut.CreateBackupAsync(options, adminUser);
        backupResult.Succeeded.Should().BeTrue();

        // Act
        var stream = await _sut.GetPreparedBackupStreamAsync(backupResult.BackupId!, adminUser);

        // Assert
        stream.Should().NotBeNull();
        stream!.CanRead.Should().BeTrue();
        stream.Dispose();

        // Clean up
        if (File.Exists(backupResult.FilePath))
        {
            try { File.Delete(backupResult.FilePath); } catch { }
        }
    }

    [Fact]
    public async Task GetLatestBackupInfoAsync_WhenBackupAuditLogExists_ReturnsCorrectMetadata()
    {
        // Arrange
        var backupDetails = JsonSerializer.Serialize(new
        {
            FileName = "tradeflow_backup_20261014_100000.zip",
            FileSizeBytes = 1048576L,
            Scope = "Toàn bộ cơ sở dữ liệu PostgreSQL & tệp đính kèm",
            UploadsCount = 5
        });

        var log = new AuditLog(
            AuditEventType.DatabaseBackupCreated,
            performedBy: "admin_tester",
            targetEntity: "DatabaseBackup",
            targetId: "backup-test-1",
            details: backupDetails,
            ipAddress: "127.0.0.1",
            userAgent: "TestAgent");

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();

        // Act
        var metadata = await _sut.GetLatestBackupInfoAsync();

        // Assert
        metadata.Should().NotBeNull();
        metadata!.FileName.Should().Be("tradeflow_backup_20261014_100000.zip");
        metadata.FileSizeBytes.Should().Be(1048576L);
        metadata.CreatedBy.Should().Be("admin_tester");
    }
}
