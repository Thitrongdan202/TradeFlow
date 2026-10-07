using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Settings;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Triển khai dịch vụ quản lý thương hiệu và màn hình tải của công ty.
/// </summary>
public class CompanyBrandingService : ICompanyBrandingService
{
    private const string CacheKey = "CompanyBrandingConfig";
    private static readonly SemaphoreSlim _lock = new(1, 1);
    private readonly TradeFlowDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _environment;
    private readonly IAuditService _auditService;
    private readonly ILogger<CompanyBrandingService> _logger;
    private readonly IServiceScopeFactory? _scopeFactory;

    public CompanyBrandingService(
        TradeFlowDbContext context,
        IMemoryCache cache,
        IWebHostEnvironment environment,
        IAuditService auditService,
        ILogger<CompanyBrandingService> logger,
        IServiceScopeFactory? scopeFactory = null)
    {
        _context = context;
        _cache = cache;
        _environment = environment;
        _auditService = auditService;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    public async Task<CompanyBrandingDto> GetBrandingAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out CompanyBrandingDto? cached) && cached != null)
        {
            return cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(CacheKey, out cached) && cached != null)
            {
                return cached;
            }

            CompanySettings? settings = null;
            if (_scopeFactory != null)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();
                settings = await db.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                settings = await _context.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            }
        if (settings == null)
        {
            settings = new CompanySettings("TỔNG KHO THIẾT BỊ VỆ SINH LACASA")
            {
                LogoPath = "/uploads/branding/lacasa_logo.png",
                ShowLoadingScreen = true,
                ShowCompanyNameOnLoading = true,
                SpinnerColor = "#10b981",
                SpinnerOpacity = 0.8,
                SpinnerSpeed = "normal"
            };
        }

        var dto = new CompanyBrandingDto
        {
            CompanyName = string.IsNullOrWhiteSpace(settings.CompanyName) ? "TradeFlow" : settings.CompanyName,
            LogoPath = settings.LogoPath,
            ShowLoadingScreen = settings.ShowLoadingScreen,
            ShowCompanyNameOnLoading = settings.ShowCompanyNameOnLoading,
            SpinnerColor = string.IsNullOrWhiteSpace(settings.SpinnerColor) ? "#10b981" : settings.SpinnerColor,
            SpinnerOpacity = settings.SpinnerOpacity <= 0 ? 0.8 : settings.SpinnerOpacity,
            SpinnerSpeed = string.IsNullOrWhiteSpace(settings.SpinnerSpeed) ? "normal" : settings.SpinnerSpeed
        };

        _cache.Set(CacheKey, dto, TimeSpan.FromMinutes(10));
        return dto;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CompanyBrandingDto> UpdateBrandingAsync(CompanyBrandingDto dto, string updatedBy, CancellationToken cancellationToken = default)
    {
        var settings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = new CompanySettings(dto.CompanyName);
            _context.CompanySettings.Add(settings);
        }

        settings.CompanyName = dto.CompanyName.Trim();
        settings.ShowLoadingScreen = dto.ShowLoadingScreen;
        settings.ShowCompanyNameOnLoading = dto.ShowCompanyNameOnLoading;
        settings.SpinnerColor = string.IsNullOrWhiteSpace(dto.SpinnerColor) ? "#10b981" : dto.SpinnerColor.Trim();
        settings.SpinnerOpacity = Math.Clamp(dto.SpinnerOpacity, 0.1, 1.0);
        settings.SpinnerSpeed = string.IsNullOrWhiteSpace(dto.SpinnerSpeed) ? "normal" : dto.SpinnerSpeed.Trim().ToLowerInvariant();
        settings.UpdatedBy = updatedBy;
        settings.UpdatedAt = DateTime.UtcNow;

        if (dto.LogoPath != null)
        {
            settings.LogoPath = dto.LogoPath.Trim();
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.CompanySettingsChanged,
            updatedBy,
            nameof(CompanySettings),
            settings.Id.ToString(),
            $"Cập nhật cấu hình thương hiệu và màn hình tải: {settings.CompanyName}",
            null,
            null,
            cancellationToken);

        InvalidateCache();
        return await GetBrandingAsync(cancellationToken);
    }

    public async Task<string> UploadLogoAsync(Stream fileStream, string originalFileName, string contentType, string updatedBy, CancellationToken cancellationToken = default)
    {
        if (fileStream == null || fileStream.Length == 0)
        {
            throw new ArgumentException("Tệp tải lên không có dữ liệu.", nameof(fileStream));
        }

        if (fileStream.Length > 5 * 1024 * 1024)
        {
            throw new ArgumentException("Kích thước tệp logo không được vượt quá 5MB.");
        }

        var ext = Path.GetExtension(originalFileName)?.ToLowerInvariant();
        var allowedExtensions = new[] { ".png", ".svg", ".webp", ".jpg", ".jpeg" };
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            throw new ArgumentException("Định dạng tệp không được hỗ trợ. Vui lòng chọn tệp PNG, SVG, WebP hoặc JPG.");
        }

        // Validate MIME type & file magic bytes
        byte[] buffer = new byte[Math.Min(fileStream.Length, 512)];
        int readBytes = await fileStream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        fileStream.Position = 0; // Reset stream

        bool isValid = ValidateImageHeader(buffer, readBytes, ext);
        if (!isValid)
        {
            throw new ArgumentException("Tệp ảnh không hợp lệ hoặc đã bị chỉnh sửa bất thường.");
        }

        // Ensure safe storage directory
        var brandingDir = Path.Combine(_environment.WebRootPath, "uploads", "branding");
        if (!Directory.Exists(brandingDir))
        {
            Directory.CreateDirectory(brandingDir);
        }

        var uniqueFileName = $"logo_{Guid.NewGuid():N}{ext}";
        var destinationPath = Path.Combine(brandingDir, uniqueFileName);

        await using (var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await fileStream.CopyToAsync(output, cancellationToken);
        }

        var relativePath = $"/uploads/branding/{uniqueFileName}";

        // Persist to database
        var settings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings == null)
        {
            settings = new CompanySettings("TradeFlow");
            _context.CompanySettings.Add(settings);
        }

        settings.LogoPath = relativePath;
        settings.UpdatedBy = updatedBy;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.CompanySettingsChanged,
            updatedBy,
            nameof(CompanySettings),
            settings.Id.ToString(),
            $"Tải lên logo thương hiệu mới: {uniqueFileName}",
            null,
            null,
            cancellationToken);

        InvalidateCache();
        _logger.LogInformation("Company logo uploaded successfully: {Path}", relativePath);
        return relativePath;
    }

    public async Task RemoveLogoAsync(string updatedBy, CancellationToken cancellationToken = default)
    {
        var settings = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        if (settings != null)
        {
            settings.LogoPath = null;
            settings.UpdatedBy = updatedBy;
            settings.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.LogAsync(
                AuditEventType.CompanySettingsChanged,
                updatedBy,
                nameof(CompanySettings),
                settings.Id.ToString(),
                "Xóa logo thương hiệu công ty, trở về biểu tượng mặc định",
                null,
                null,
                cancellationToken);
        }

        InvalidateCache();
    }

    public void InvalidateCache()
    {
        _cache.Remove(CacheKey);
    }

    private static bool ValidateImageHeader(byte[] header, int length, string ext)
    {
        if (length < 4) return false;

        // PNG: 89 50 4E 47 (.PNG)
        if (ext == ".png")
        {
            return header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        }

        // JPEG: FF D8 FF
        if (ext == ".jpg" || ext == ".jpeg")
        {
            return header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        }

        // WebP: RIFF ... WEBP
        if (ext == ".webp")
        {
            return length >= 12 &&
                   header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                   header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;
        }

        // SVG: contains XML / <svg
        if (ext == ".svg")
        {
            var text = System.Text.Encoding.UTF8.GetString(header, 0, length).TrimStart();
            return text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase) ||
                   text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
