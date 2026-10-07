using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Settings;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;
using Xunit;

namespace TradeFlow.UnitTests.Settings;

public class CompanyBrandingServiceTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly string _tempWebRoot;
    private readonly CompanyBrandingService _sut;

    public CompanyBrandingServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);

        _cache = new MemoryCache(new MemoryCacheOptions());

        _tempWebRoot = Path.Combine(Path.GetTempPath(), "TradeFlowTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempWebRoot);

        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.WebRootPath).Returns(_tempWebRoot);

        _auditServiceMock = new Mock<IAuditService>();

        _sut = new CompanyBrandingService(
            _context,
            _cache,
            _envMock.Object,
            _auditServiceMock.Object,
            NullLogger<CompanyBrandingService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();

        try
        {
            if (Directory.Exists(_tempWebRoot))
            {
                Directory.Delete(_tempWebRoot, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup failure in temp directory
        }
    }

    [Fact]
    public async Task GetBrandingAsync_WhenNoSettingsInDb_ReturnsDefaultBranding()
    {
        // Act
        var result = await _sut.GetBrandingAsync();

        // Assert
        result.Should().NotBeNull();
        result.CompanyName.Should().Be("TỔNG KHO THIẾT BỊ VỆ SINH LACASA");
        result.LogoPath.Should().Be("/uploads/branding/lacasa_logo.png");
        result.ShowLoadingScreen.Should().BeTrue();
        result.ShowCompanyNameOnLoading.Should().BeTrue();
        result.SpinnerColor.Should().Be("#10b981");
        result.SpinnerOpacity.Should().Be(0.8);
        result.SpinnerSpeed.Should().Be("normal");
    }

    [Fact]
    public async Task GetBrandingAsync_WhenSettingsExistInDb_ReturnsStoredBranding()
    {
        // Arrange
        var settings = new CompanySettings("CONG TY TNHH ABC")
        {
            LogoPath = "/uploads/branding/custom_logo.png",
            ShowLoadingScreen = true,
            ShowCompanyNameOnLoading = false,
            SpinnerColor = "#2563eb",
            SpinnerOpacity = 0.9,
            SpinnerSpeed = "fast"
        };
        _context.CompanySettings.Add(settings);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetBrandingAsync();

        // Assert
        result.Should().NotBeNull();
        result.CompanyName.Should().Be("CONG TY TNHH ABC");
        result.LogoPath.Should().Be("/uploads/branding/custom_logo.png");
        result.ShowLoadingScreen.Should().BeTrue();
        result.ShowCompanyNameOnLoading.Should().BeFalse();
        result.SpinnerColor.Should().Be("#2563eb");
        result.SpinnerOpacity.Should().Be(0.9);
        result.SpinnerSpeed.Should().Be("fast");
    }

    [Fact]
    public async Task UpdateBrandingAsync_ValidDto_UpdatesDatabaseAndInvalidatesCache()
    {
        // Arrange
        var initial = new CompanySettings("OLD NAME");
        _context.CompanySettings.Add(initial);
        await _context.SaveChangesAsync();

        // Prime cache
        await _sut.GetBrandingAsync();

        var updateDto = new CompanyBrandingDto
        {
            CompanyName = "NEW COMPANY NAME",
            ShowLoadingScreen = true,
            ShowCompanyNameOnLoading = true,
            SpinnerColor = "#0f766e",
            SpinnerOpacity = 0.85,
            SpinnerSpeed = "SLOW",
            LogoPath = "/uploads/branding/new_logo.png"
        };

        // Act
        var result = await _sut.UpdateBrandingAsync(updateDto, "admin_user");

        // Assert
        result.CompanyName.Should().Be("NEW COMPANY NAME");
        result.SpinnerColor.Should().Be("#0f766e");
        result.SpinnerOpacity.Should().Be(0.85);
        result.SpinnerSpeed.Should().Be("slow"); // normalized to lower case
        result.LogoPath.Should().Be("/uploads/branding/new_logo.png");

        var dbRecord = await _context.CompanySettings.FirstAsync();
        dbRecord.CompanyName.Should().Be("NEW COMPANY NAME");
        dbRecord.UpdatedBy.Should().Be("admin_user");

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.CompanySettingsChanged,
            "admin_user",
            nameof(CompanySettings),
            dbRecord.Id.ToString(),
            It.Is<string>(msg => msg.Contains("NEW COMPANY NAME")),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBrandingAsync_ClampsOpacityWithinRange()
    {
        // Arrange
        var initial = new CompanySettings("TEST");
        _context.CompanySettings.Add(initial);
        await _context.SaveChangesAsync();

        var updateDtoTooLow = new CompanyBrandingDto
        {
            CompanyName = "TEST",
            SpinnerOpacity = -0.5
        };

        // Act
        var resultLow = await _sut.UpdateBrandingAsync(updateDtoTooLow, "admin");
        resultLow.SpinnerOpacity.Should().Be(0.1);

        var updateDtoTooHigh = new CompanyBrandingDto
        {
            CompanyName = "TEST",
            SpinnerOpacity = 2.5
        };

        var resultHigh = await _sut.UpdateBrandingAsync(updateDtoTooHigh, "admin");
        resultHigh.SpinnerOpacity.Should().Be(1.0);
    }

    [Fact]
    public async Task UploadLogoAsync_WhenEmptyStream_ThrowsArgumentException()
    {
        using var emptyStream = new MemoryStream();

        Func<Task> act = async () => await _sut.UploadLogoAsync(emptyStream, "test.png", "image/png", "admin");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*không có dữ liệu*");
    }

    [Fact]
    public async Task UploadLogoAsync_WhenInvalidExtension_ThrowsArgumentException()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(data);

        Func<Task> act = async () => await _sut.UploadLogoAsync(stream, "script.exe", "application/octet-stream", "admin");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Định dạng tệp không được hỗ trợ*");
    }

    [Fact]
    public async Task UploadLogoAsync_WhenMagicBytesDoNotMatchPng_ThrowsArgumentException()
    {
        var fakePngData = Encoding.UTF8.GetBytes("This is not a real PNG file.");
        using var stream = new MemoryStream(fakePngData);

        Func<Task> act = async () => await _sut.UploadLogoAsync(stream, "fake.png", "image/png", "admin");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Tệp ảnh không hợp lệ*");
    }

    [Fact]
    public async Task UploadLogoAsync_ValidPngFile_SavesFileUpdatesSettingsAndLogsAudit()
    {
        // Valid PNG header: 89 50 4E 47 0D 0A 1A 0A
        var validPngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D };
        using var stream = new MemoryStream(validPngBytes);

        // Act
        var relativePath = await _sut.UploadLogoAsync(stream, "company_brand.png", "image/png", "manager");

        // Assert
        relativePath.Should().StartWith("/uploads/branding/logo_");
        relativePath.Should().EndWith(".png");

        var diskPath = Path.Combine(_tempWebRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        File.Exists(diskPath).Should().BeTrue();

        var dbRecord = await _context.CompanySettings.FirstOrDefaultAsync();
        dbRecord.Should().NotBeNull();
        dbRecord!.LogoPath.Should().Be(relativePath);
        dbRecord.UpdatedBy.Should().Be("manager");

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.CompanySettingsChanged,
            "manager",
            nameof(CompanySettings),
            dbRecord.Id.ToString(),
            It.Is<string>(msg => msg.Contains("Tải lên logo thương hiệu mới")),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadLogoAsync_ValidSvgFile_SavesFileAndUpdatesSettings()
    {
        var svgContent = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"100\" height=\"100\"><circle cx=\"50\" cy=\"50\" r=\"40\"/></svg>";
        var svgBytes = Encoding.UTF8.GetBytes(svgContent);
        using var stream = new MemoryStream(svgBytes);

        // Act
        var relativePath = await _sut.UploadLogoAsync(stream, "vector_logo.svg", "image/svg+xml", "admin");

        // Assert
        relativePath.Should().StartWith("/uploads/branding/logo_");
        relativePath.Should().EndWith(".svg");

        var diskPath = Path.Combine(_tempWebRoot, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        File.Exists(diskPath).Should().BeTrue();

        var dbRecord = await _context.CompanySettings.FirstOrDefaultAsync();
        dbRecord.Should().NotBeNull();
        dbRecord!.LogoPath.Should().Be(relativePath);
    }

    [Fact]
    public async Task RemoveLogoAsync_ClearsLogoPathAndLogsAudit()
    {
        // Arrange
        var settings = new CompanySettings("TEST")
        {
            LogoPath = "/uploads/branding/current_logo.png"
        };
        _context.CompanySettings.Add(settings);
        await _context.SaveChangesAsync();

        // Act
        await _sut.RemoveLogoAsync("admin_remover");

        // Assert
        var updatedRecord = await _context.CompanySettings.FirstAsync();
        updatedRecord.LogoPath.Should().BeNull();
        updatedRecord.UpdatedBy.Should().Be("admin_remover");

        _auditServiceMock.Verify(a => a.LogAsync(
            AuditEventType.CompanySettingsChanged,
            "admin_remover",
            nameof(CompanySettings),
            updatedRecord.Id.ToString(),
            It.Is<string>(msg => msg.Contains("Xóa logo thương hiệu")),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
