using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
using Npgsql;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Settings;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

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
        string dumpFilePath = Path.Combine(tempDir, $"{backupId}_database.dump");

        try
        {
            CleanupOldPreparedBackups(tempDir);

            // 3. Parse Connection String securely
            var connStr = _configuration.GetConnectionString("DefaultConnection") 
                          ?? throw new InvalidOperationException("DefaultConnection string not found");
            var connBuilder = new NpgsqlConnectionStringBuilder(connStr);

            string host = string.IsNullOrWhiteSpace(connBuilder.Host) ? "localhost" : connBuilder.Host;
            int port = connBuilder.Port > 0 ? connBuilder.Port : 5432;
            string database = connBuilder.Database ?? "tradeflow_dev";
            string username = connBuilder.Username ?? "postgres";
            string password = connBuilder.Password ?? string.Empty;

            // 4. Locate pg_dump
            string pgDumpPath = FindPgDumpExecutable();
            if (string.IsNullOrEmpty(pgDumpPath) || !File.Exists(pgDumpPath))
            {
                _logger.LogError("pg_dump binary was not found in deployment environment");
                return new BackupResult
                {
                    Succeeded = false,
                    ErrorMessage = "Không tìm thấy tiện ích sao lưu cơ sở dữ liệu (pg_dump) trên máy chủ."
                };
            }

            _logger.LogInformation("Starting database backup using {PgDumpPath} for database {Database}...", pgDumpPath, database);

            // 5. Execute pg_dump with custom compressed format (-Fc)
            var startInfo = new ProcessStartInfo
            {
                FileName = pgDumpPath,
                Arguments = $"-h {host} -p {port} -U {username} -d {database} -Fc -f \"{dumpFilePath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = false,
                CreateNoWindow = true
            };

            // Pass password securely through PGPASSWORD environment variable - NEVER in command arguments
            if (!string.IsNullOrEmpty(password))
            {
                startInfo.EnvironmentVariables["PGPASSWORD"] = password;
            }

            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0 || !File.Exists(dumpFilePath) || new FileInfo(dumpFilePath).Length == 0)
                {
                    _logger.LogError("pg_dump exited with code {ExitCode}. Error: {Error}", process.ExitCode, stderr);
                    return new BackupResult
                    {
                        Succeeded = false,
                        ErrorMessage = "Quá trình trích xuất cơ sở dữ liệu thất bại. Vui lòng kiểm tra dịch vụ PostgreSQL."
                    };
                }
            }

            // 6. Build Manifest Metadata
            var manifest = new BackupManifest
            {
                AppName = "TradeFlow ERP",
                AppVersion = _configuration["TradeFlow:Version"] ?? "1.0.0",
                BackupDate = DateTime.UtcNow,
                DatabaseName = database,
                DatabaseFormat = "PostgreSQL Custom Archive (pg_dump -Fc)",
                CreatedBy = user.Identity?.Name ?? "Administrator",
                IncludedComponents = new List<string> { "PostgreSQL Database Schema & Data" }
            };

            // 7. Create ZIP archive with streaming
            int uploadsCount = 0;
            using (var zipStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false))
            {
                // 7a. Add database dump
                var dbEntry = archive.CreateEntry("database.dump", CompressionLevel.Fastest);
                using (var entryStream = dbEntry.Open())
                using (var dumpStream = new FileStream(dumpFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    await dumpStream.CopyToAsync(entryStream, cancellationToken);
                }

                // 7b. Add persistent uploads (product images, original Excel pricing sheets, etc.)
                if (options.IncludeUploads)
                {
                    string uploadsDir = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads");
                    if (Directory.Exists(uploadsDir))
                    {
                        manifest.IncludedComponents.Add("Uploaded Business Assets (Product Images, Document Attachments)");
                        var uploadFiles = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories);
                        uploadsCount = uploadFiles.Length;

                        foreach (var file in uploadFiles)
                        {
                            string relativePath = Path.GetRelativePath(uploadsDir, file).Replace('\\', '/');
                            string entryName = $"uploads/{relativePath}";
                            var fileEntry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                            using var entryStream = fileEntry.Open();
                            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                            await fs.CopyToAsync(entryStream, cancellationToken);
                        }
                    }
                }

                manifest.UploadedFilesCount = uploadsCount;

                // 7c. Add manifest.json
                var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Fastest);
                using (var entryStream = manifestEntry.Open())
                {
                    var manifestJson = JsonSerializer.SerializeToUtf8Bytes(manifest, new JsonSerializerOptions { WriteIndented = true });
                    await entryStream.WriteAsync(manifestJson, cancellationToken);
                }
            }

            // 8. Delete intermediate dump file
            if (File.Exists(dumpFilePath))
            {
                try { File.Delete(dumpFilePath); } catch { /* ignore */ }
            }

            long totalSizeBytes = new FileInfo(zipFilePath).Length;
            manifest.TotalSizeBytes = totalSizeBytes;

            // 9. Store in prepared items cache (valid for 30 minutes)
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

            // 10. Record Audit Log
            await _auditService.LogAsync(
                AuditEventType.DatabaseBackupCreated,
                performedBy: user.Identity?.Name,
                targetEntity: "DatabaseBackup",
                targetId: backupId,
                details: JsonSerializer.Serialize(new
                {
                    FileName = zipFileName,
                    FileSizeBytes = totalSizeBytes,
                    Scope = manifest.IncludedComponents,
                    UploadsCount = uploadsCount,
                    Database = database,
                    Timestamp = timestamp
                }),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Backup created successfully: {FileName} ({SizeBytes} bytes, {UploadsCount} assets)", 
                zipFileName, totalSizeBytes, uploadsCount);

            return new BackupResult
            {
                Succeeded = true,
                BackupId = backupId,
                FilePath = zipFilePath,
                FileName = zipFileName,
                FileSizeBytes = totalSizeBytes,
                ContentType = "application/zip",
                CreatedAt = DateTime.UtcNow,
                Manifest = manifest
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during backup generation");
            return new BackupResult
            {
                Succeeded = false,
                ErrorMessage = "Đã xảy ra lỗi hệ thống khi chuẩn bị tệp sao lưu. Vui lòng liên hệ quản trị viên."
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
        string? scope = "Toàn bộ cơ sở dữ liệu PostgreSQL & tệp đính kèm";

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

    private static string FindPgDumpExecutable()
    {
        // 1. Direct standard Windows installations
        string[] candidates =
        {
            @"C:\Program Files\PostgreSQL\16\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\15\bin\pg_dump.exe",
            @"C:\Program Files\PostgreSQL\14\bin\pg_dump.exe",
            @"C:\Program Files (x86)\PostgreSQL\16\bin\pg_dump.exe",
            @"/usr/bin/pg_dump",
            @"/usr/local/bin/pg_dump"
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        // 2. Search in PATH
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where.exe" : "which",
                    Arguments = "pg_dump",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            if (proc.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0 && File.Exists(lines[0]))
                {
                    return lines[0];
                }
            }
        }
        catch
        {
            // Ignore
        }

        return string.Empty;
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

            // Also check directory files older than 2 hours
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
