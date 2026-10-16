using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services.DataBackup;

namespace TradeFlow.Infrastructure.Services;

public class DataBackupService : IDataBackupService
{
    private static readonly SemaphoreSlim _backupLock = new(1, 1);
    private static readonly ConcurrentDictionary<string, PreparedBackupItem> _preparedBackups = new();

    private readonly TradeFlowDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly IAuditService _auditService;
    private readonly IAuthorizationService _authService;
    private readonly ILogger<DataBackupService> _logger;

    private readonly BackupExporter _exporter;
    private readonly BackupValidator _validator;
    private readonly BackupRestorer _restorer;

    private class PreparedBackupItem
    {
        public string BackupId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/zip";
        public string UserId { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    public DataBackupService(
        TradeFlowDbContext context,
        IConfiguration configuration,
        IWebHostEnvironment env,
        IAuditService auditService,
        IAuthorizationService authService,
        ILogger<DataBackupService> logger)
    {
        _context = context;
        _configuration = configuration;
        _env = env;
        _auditService = auditService;
        _authService = authService;
        _logger = logger;

        _exporter = new BackupExporter(context, configuration, env, logger);
        _validator = new BackupValidator(context, env, logger);
        _restorer = new BackupRestorer(context, env, auditService, logger);
    }

    public async Task<BackupResult> CreateBackupAsync(BackupOptions options, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        // 1. Authorization check
        var authResult = await _authService.AuthorizeAsync(user, "Permission:CompanySettings:View");
        bool isAdmin = user.IsInRole("Administrator");
        if (!authResult.Succeeded && !isAdmin)
        {
            _logger.LogWarning("Unauthorized backup attempt by user: {User}", user.Identity?.Name);
            return new BackupResult
            {
                Succeeded = false,
                ErrorMessage = "Bạn không có quyền quản trị để tạo và tải bản sao lưu dữ liệu hệ thống."
            };
        }

        // 2. Prevent concurrent backup executions
        if (!await _backupLock.WaitAsync(0, cancellationToken))
        {
            return new BackupResult
            {
                Succeeded = false,
                ErrorMessage = "Một tác vụ sao lưu dữ liệu khác đang được thực hiện. Vui lòng chờ hoàn tất trong giây lát."
            };
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "TradeFlow_Backups");
        Directory.CreateDirectory(tempDir);

        string backupId = Guid.NewGuid().ToString("N");
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string zipFileName = $"tradeflow_backup_{timestamp}.zip";
        string zipFilePath = Path.Combine(tempDir, $"{backupId}_{zipFileName}");

        try
        {
            CleanupOldPreparedBackups(tempDir);

            // Export using modern JSON + uploads archive engine
            var result = await _exporter.ExportAsync(options, user, zipFilePath, cancellationToken);
            if (!result.Succeeded) return result;

            result.BackupId = backupId;
            result.FileName = zipFileName;

            // Cache prepared backup item (valid for 30 minutes)
            string currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? "Admin";
            var preparedItem = new PreparedBackupItem
            {
                BackupId = backupId,
                FilePath = zipFilePath,
                FileName = zipFileName,
                ContentType = "application/zip",
                UserId = currentUserId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30)
            };
            _preparedBackups[backupId] = preparedItem;

            // Audit log
            await _auditService.LogAsync(
                AuditEventType.DatabaseBackupCreated,
                performedBy: user.Identity?.Name ?? "Administrator",
                targetEntity: "DatabaseBackup",
                targetId: backupId,
                details: JsonSerializer.Serialize(new
                {
                    FileName = zipFileName,
                    FileSizeBytes = result.FileSizeBytes,
                    Scope = result.Manifest?.IncludedComponents,
                    TablesCount = result.Manifest?.EntityCounts.Count ?? 0,
                    AssetsCount = result.Manifest?.FileCount ?? 0,
                    Database = result.Manifest?.DatabaseName,
                    Timestamp = timestamp
                }),
                cancellationToken: cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during backup generation");
            return new BackupResult
            {
                Succeeded = false,
                ErrorMessage = "Đã xảy ra lỗi hệ thống khi chuẩn bị tệp sao lưu: " + ex.Message
            };
        }
        finally
        {
            _backupLock.Release();
        }
    }

    public Task<Stream?> GetPreparedBackupStreamAsync(string backupId, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupId))
        {
            return Task.FromResult<Stream?>(null);
        }

        if (!_preparedBackups.TryGetValue(backupId, out var item))
        {
            return Task.FromResult<Stream?>(null);
        }

        if (DateTime.UtcNow > item.ExpiresAt || !File.Exists(item.FilePath))
        {
            _preparedBackups.TryRemove(backupId, out _);
            return Task.FromResult<Stream?>(null);
        }

        // Verify authorized user
        string currentUserId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity?.Name ?? string.Empty;
        bool isAdmin = user.IsInRole("Administrator");
        if (!isAdmin && item.UserId != currentUserId)
        {
            _logger.LogWarning("Access denied: User {User} attempted to access backup created by {Owner}", currentUserId, item.UserId);
            return Task.FromResult<Stream?>(null);
        }

        Stream fs = new FileStream(item.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(fs);
    }

    public async Task<BackupMetadataDto?> GetLatestBackupInfoAsync(CancellationToken cancellationToken = default)
    {
        var latestLog = await _context.AuditLogs
            .Where(x => x.EventType == AuditEventType.DatabaseBackupCreated)
            .OrderByDescending(x => x.OccurredAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestLog == null)
        {
            return null;
        }

        string? fileName = null;
        long fileSize = 0;
        string? scope = "Toàn bộ 38 bảng dữ liệu & tệp tài sản kinh doanh đính kèm";

        if (!string.IsNullOrEmpty(latestLog.Details))
        {
            try
            {
                using var doc = JsonDocument.Parse(latestLog.Details);
                if (doc.RootElement.TryGetProperty("FileName", out var fnProp))
                {
                    fileName = fnProp.GetString();
                }
                if (doc.RootElement.TryGetProperty("FileSizeBytes", out var fsProp) && fsProp.TryGetInt64(out var sizeVal))
                {
                    fileSize = sizeVal;
                }
            }
            catch
            {
                // fallback
            }
        }

        return new BackupMetadataDto
        {
            CreatedAt = latestLog.OccurredAt,
            FileName = fileName ?? $"tradeflow_backup_{latestLog.OccurredAt:yyyyMMdd_HHmmss}.zip",
            FileSizeBytes = fileSize,
            CreatedBy = latestLog.PerformedBy ?? "Hệ thống",
            ScopeDescription = scope
        };
    }

    public async Task<BackupValidationResult> ValidateAndPreviewArchiveAsync(
        Stream archiveStream,
        string fileName,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:CompanySettings:View");
        if (!authResult.Succeeded && !user.IsInRole("Administrator"))
        {
            return new BackupValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Bạn không có quyền quản trị để kiểm tra và nạp bản sao lưu." }
            };
        }

        return await _validator.ValidateAndExtractArchiveAsync(archiveStream, fileName, user, cancellationToken);
    }

    public async Task<BackupValidationResult> ValidateAndPreviewExtractedFolderAsync(
        string stagingFolderPath,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:CompanySettings:View");
        if (!authResult.Succeeded && !user.IsInRole("Administrator"))
        {
            return new BackupValidationResult
            {
                IsValid = false,
                Errors = new List<string> { "Bạn không có quyền quản trị để kiểm tra và nạp bản sao lưu." }
            };
        }

        return await _validator.ValidateExtractedFolderAsync(stagingFolderPath, user, cancellationToken);
    }

    public async Task<RestoreResult> ExecuteRestoreAsync(
        ImportExecutionPlan plan,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:CompanySettings:View");
        if (!authResult.Succeeded && !user.IsInRole("Administrator"))
        {
            return new RestoreResult
            {
                Succeeded = false,
                ErrorMessage = "Bạn không có quyền quản trị để thực hiện phục hồi dữ liệu hệ thống."
            };
        }

        return await _restorer.RestoreAsync(plan, user, cancellationToken);
    }

    public Task CleanupStagingAsync(string stagingId)
    {
        if (string.IsNullOrWhiteSpace(stagingId)) return Task.CompletedTask;
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "TradeFlow_Restore_Staging", stagingId);
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể xóa thư mục tạm staging {StagingId}: {Message}", stagingId, ex.Message);
        }
        return Task.CompletedTask;
    }

    private static void CleanupOldPreparedBackups(string tempDir)
    {
        try
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _preparedBackups)
            {
                if (now > kvp.Value.ExpiresAt)
                {
                    if (_preparedBackups.TryRemove(kvp.Key, out var item))
                    {
                        if (File.Exists(item.FilePath))
                        {
                            try { File.Delete(item.FilePath); } catch { /* ignore */ }
                        }
                    }
                }
            }

            if (Directory.Exists(tempDir))
            {
                var files = Directory.GetFiles(tempDir, "*tradeflow_backup_*.zip");
                foreach (var f in files)
                {
                    if (File.GetCreationTimeUtc(f) < now.AddHours(-2))
                    {
                        try { File.Delete(f); } catch { /* ignore */ }
                    }
                }
            }
        }
        catch
        {
            // Ignore background cleanup errors
        }
    }
}
