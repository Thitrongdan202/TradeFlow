using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Models.Settings;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services.DataBackup;

public class BackupValidator
{
    private readonly TradeFlowDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BackupValidator(
        TradeFlowDbContext context,
        IWebHostEnvironment env,
        ILogger logger)
    {
        _context = context;
        _env = env;
        _logger = logger;
    }

    public async Task<BackupValidationResult> ValidateAndExtractArchiveAsync(
        Stream archiveStream,
        string fileName,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        string stagingId = Guid.NewGuid().ToString("N");
        string tempRoot = Path.Combine(Path.GetTempPath(), "TradeFlow_Restore_Staging");
        string stagingDir = Path.Combine(tempRoot, stagingId);
        Directory.CreateDirectory(stagingDir);

        try
        {
            // ZipSlip & decompression bomb protection
            const long maxTotalUncompressedBytes = 10L * 1024L * 1024L * 1024L; // 10 GB
            const int maxEntriesCount = 50000;
            long totalUncompressedBytes = 0;
            int entriesCount = 0;

            string fullStagingDir = Path.GetFullPath(stagingDir) + Path.DirectorySeparatorChar;

            using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                foreach (var entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    entriesCount++;
                    if (entriesCount > maxEntriesCount)
                    {
                        throw new InvalidDataException("Tệp lưu trữ vượt quá giới hạn an toàn về số lượng tệp con (Zip bomb defense).");
                    }

                    string destPath = Path.GetFullPath(Path.Combine(stagingDir, entry.FullName));
                    if (!destPath.StartsWith(fullStagingDir, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new SecurityException("Phát hiện đường dẫn tệp không hợp lệ bên trong bản sao lưu (ZipSlip exploit protection).");
                    }

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        Directory.CreateDirectory(destPath);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);

                    totalUncompressedBytes += entry.Length;
                    if (totalUncompressedBytes > maxTotalUncompressedBytes)
                    {
                        throw new InvalidDataException("Dung lượng giải nén vượt quá hạn mức bảo vệ an toàn hệ thống (10GB).");
                    }

                    using var entryStream = entry.Open();
                    using var outputStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await entryStream.CopyToAsync(outputStream, cancellationToken);
                }
            }

            return await ValidateStagedFolderInternalAsync(stagingDir, stagingId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi kiểm tra tính hợp lệ của tệp sao lưu: {Message}", ex.Message);
            try
            {
                if (Directory.Exists(stagingDir)) Directory.Delete(stagingDir, true);
            }
            catch { /* ignore cleanup error */ }

            return new BackupValidationResult
            {
                IsValid = false,
                StagingId = stagingId,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<BackupValidationResult> ValidateExtractedFolderAsync(
        string stagingFolderPath,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        string stagingId = Path.GetFileName(stagingFolderPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (string.IsNullOrEmpty(stagingId)) stagingId = Guid.NewGuid().ToString("N");

        return await ValidateStagedFolderInternalAsync(stagingFolderPath, stagingId, cancellationToken);
    }

    private async Task<BackupValidationResult> ValidateStagedFolderInternalAsync(
        string stagingDir,
        string stagingId,
        CancellationToken cancellationToken)
    {
        var result = new BackupValidationResult
        {
            StagingId = stagingId,
            StagingPath = stagingDir,
            IsValid = true
        };

        // 1. Check manifest.json
        string manifestPath = Path.Combine(stagingDir, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            result.IsValid = false;
            result.Errors.Add("Không tìm thấy tệp đặc tả hệ thống 'manifest.json' trong gói sao lưu.");
            return result;
        }

        TradeFlowBackupManifest? manifest = null;
        try
        {
            string manifestText = await File.ReadAllTextAsync(manifestPath, cancellationToken);
            manifest = JsonSerializer.Deserialize<TradeFlowBackupManifest>(manifestText, JsonOptions);
            result.Manifest = manifest;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.Errors.Add($"Tệp manifest.json không hợp lệ hoặc bị lỗi cú pháp: {ex.Message}");
            return result;
        }

        if (manifest == null || string.IsNullOrEmpty(manifest.FormatVersion))
        {
            result.IsValid = false;
            result.Errors.Add("Cấu trúc manifest.json không nhận diện được phiên bản sao lưu.");
            return result;
        }

        if (!manifest.FormatVersion.StartsWith("tradeflow-backup", StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add($"Phiên bản đặc tả '{manifest.FormatVersion}' có thể khác phiên bản hiện tại của TradeFlow.");
        }

        // 2. Check checksums.json
        string checksumsPath = Path.Combine(stagingDir, "checksums.json");
        if (File.Exists(checksumsPath))
        {
            try
            {
                string csText = await File.ReadAllTextAsync(checksumsPath, cancellationToken);
                var checksumMap = JsonSerializer.Deserialize<Dictionary<string, string>>(csText, JsonOptions);
                if (checksumMap != null)
                {
                    int verifiedCount = 0;
                    int tamperedCount = 0;
                    foreach (var (relPath, expectedHash) in checksumMap)
                    {
                        bool isDataPayload = relPath.StartsWith("data/", StringComparison.OrdinalIgnoreCase) || 
                                             relPath.Equals("manifest.json", StringComparison.OrdinalIgnoreCase);
                        if (!isDataPayload && verifiedCount > 50) continue;

                        string fullPath = Path.Combine(stagingDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                        if (File.Exists(fullPath))
                        {
                            byte[] bytes = await File.ReadAllBytesAsync(fullPath, cancellationToken);
                            string actualHash = ComputeSha256(bytes);
                            verifiedCount++;
                            if (!string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                            {
                                tamperedCount++;
                                result.Warnings.Add($"Tệp '{relPath}' có mã SHA-256 không khớp với bản gốc ({actualHash} != {expectedHash}).");
                            }
                        }
                    }
                    if (tamperedCount > 0)
                    {
                        result.Warnings.Add($"Phát hiện {tamperedCount} tệp có dấu hiệu bị chỉnh sửa sau khi xuất bản sao lưu.");
                    }
                }
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Không thể hoàn tất đối soát SHA-256 toàn vẹn: {ex.Message}");
            }
        }

        // 3. Preflight duplicate and conflict detection against current database
        await DetectConflictsAsync(stagingDir, result, cancellationToken);

        // 4. File duplicates & conflicts
        string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        string uploadsDir = Path.Combine(webRoot, "uploads");
        string stagingFilesDir = Path.Combine(stagingDir, "files");

        if (Directory.Exists(stagingFilesDir))
        {
            var files = Directory.GetFiles(stagingFilesDir, "*.*", SearchOption.AllDirectories);
            result.TotalFilesInBackup = files.Length;
            result.TotalFileSizeBytes = files.Sum(f => new FileInfo(f).Length);

            foreach (var f in files)
            {
                string rel = Path.GetRelativePath(stagingFilesDir, f);
                string existingFile = Path.Combine(uploadsDir, rel);
                if (File.Exists(existingFile))
                {
                    var incomingInfo = new FileInfo(f);
                    var existingInfo = new FileInfo(existingFile);

                    if (FilesAreEqual(incomingInfo, existingInfo))
                    {
                        result.FileDuplicatesCount++;
                    }
                    else
                    {
                        result.FileConflictsCount++;
                    }
                }
            }
        }

        return result;
    }

    private async Task DetectConflictsAsync(
        string stagingDir,
        BackupValidationResult result,
        CancellationToken cancellationToken)
    {
        string dataDir = Path.Combine(stagingDir, "data");
        if (!Directory.Exists(dataDir))
        {
            result.Warnings.Add("Không tìm thấy thư mục 'data' chứa dữ liệu bảng trong bản sao lưu.");
            return;
        }

        async Task<List<T>> ReadTableAsync<T>(string tableName)
        {
            string file = Path.Combine(dataDir, $"{tableName}.json");
            if (!File.Exists(file)) return new List<T>();
            try
            {
                string json = await File.ReadAllTextAsync(file, cancellationToken);
                return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Lỗi đọc dữ liệu bảng '{tableName}': {ex.Message}");
                return new List<T>();
            }
        }

        // Initialize 7 Category preview summaries
        var catSettings = new BackupCategoryPreviewDto { CategoryName = "Cài đặt & Cấu hình hệ thống" };
        var catIdentity = new BackupCategoryPreviewDto { CategoryName = "Tài khoản & Phân quyền" };
        var catLookups = new BackupCategoryPreviewDto { CategoryName = "Danh mục & Tham chiếu" };
        var catMaster = new BackupCategoryPreviewDto { CategoryName = "Dữ liệu nguồn (Master Data)" };
        var catPricing = new BackupCategoryPreviewDto { CategoryName = "Bảng giá & Hợp đồng mẫu" };
        var catTrans = new BackupCategoryPreviewDto { CategoryName = "Nghiệp vụ & Chứng từ giao dịch" };
        var catAudit = new BackupCategoryPreviewDto { CategoryName = "Nhật ký kiểm toán" };

        result.Categories = new List<BackupCategoryPreviewDto>
        {
            catSettings,
            catIdentity,
            catLookups,
            catMaster,
            catPricing,
            catTrans,
            catAudit
        };

        // -------------------------------------------------------------
        // CATEGORY 1: Settings
        // -------------------------------------------------------------
        catSettings.EntityTypes.AddRange(new[] { "CompanySettings", "CompanySecuritySettings", "ContractPartyASettings", "DocumentCategories", "SystemSequences" });

        var csList = await ReadTableAsync<CompanySettingsBackupDto>("CompanySettings");
        catSettings.TotalInBackup += csList.Count;
        var existingCs = await _context.CompanySettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (csList.Count > 0)
        {
            var incomingCs = csList[0];
            if (existingCs == null)
            {
                catSettings.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existingCs.CompanyName, incomingCs.CompanyName, StringComparison.Ordinal) ||
                            !string.Equals(existingCs.TaxCode, incomingCs.TaxCode, StringComparison.Ordinal) ||
                            !string.Equals(existingCs.Address, incomingCs.Address, StringComparison.Ordinal);
                if (diff)
                {
                    catSettings.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "CompanySettings",
                        EntityDisplayName = "Thông tin doanh nghiệp",
                        BusinessKey = "ID 1",
                        DisplayName = existingCs.CompanyName,
                        CurrentSummary = $"Tên: {existingCs.CompanyName}, MST: {existingCs.TaxCode}, ĐC: {existingCs.Address}",
                        BackupSummary = $"Tên: {incomingCs.CompanyName}, MST: {incomingCs.TaxCode}, ĐC: {incomingCs.Address}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catSettings.MatchedCount++;
                }
            }
        }

        var cpaList = await ReadTableAsync<ContractPartyASettingsBackupDto>("ContractPartyASettings");
        catSettings.TotalInBackup += cpaList.Count;
        var existingCpa = await _context.ContractPartyASettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (cpaList.Count > 0)
        {
            var incomingCpa = cpaList[0];
            if (existingCpa == null)
            {
                catSettings.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existingCpa.CompanyName, incomingCpa.CompanyName, StringComparison.Ordinal) ||
                            !string.Equals(existingCpa.RepresentativeName, incomingCpa.RepresentativeName, StringComparison.Ordinal);
                if (diff)
                {
                    catSettings.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "ContractPartyASettings",
                        EntityDisplayName = "Cấu hình Bên A hợp đồng",
                        BusinessKey = "ID 1",
                        DisplayName = existingCpa.CompanyName,
                        CurrentSummary = $"Bên A: {existingCpa.CompanyName}, Đại diện: {existingCpa.RepresentativeName}",
                        BackupSummary = $"Bên A: {incomingCpa.CompanyName}, Đại diện: {incomingCpa.RepresentativeName}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catSettings.MatchedCount++;
                }
            }
        }

        var docCats = await ReadTableAsync<DocumentCategoryBackupDto>("DocumentCategories");
        catSettings.TotalInBackup += docCats.Count;
        var existingDocCats = await _context.DocumentCategories.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var dc in docCats)
        {
            if (!existingDocCats.TryGetValue(dc.Code, out var existing))
            {
                catSettings.NewCount++;
            }
            else
            {
                if (!string.Equals(existing.Name, dc.Name, StringComparison.Ordinal))
                {
                    catSettings.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "DocumentCategory",
                        EntityDisplayName = "Loại chứng từ đính kèm",
                        BusinessKey = dc.Code,
                        DisplayName = dc.Name,
                        CurrentSummary = $"Mã: {existing.Code}, Tên: {existing.Name}",
                        BackupSummary = $"Mã: {dc.Code}, Tên: {dc.Name}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catSettings.MatchedCount++;
                }
            }
        }

        var seqs = await ReadTableAsync<SystemSequenceBackupDto>("SystemSequences");
        catSettings.TotalInBackup += seqs.Count;
        var existingSeqs = await _context.SystemSequences.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var seq in seqs)
        {
            var existing = existingSeqs.FirstOrDefault(x => x.SequenceKey == seq.SequenceKey && x.Prefix == seq.Prefix);
            if (existing == null) catSettings.NewCount++;
            else if (existing.CurrentNumber != seq.CurrentNumber) catSettings.ConflictCount++;
            else catSettings.MatchedCount++;
        }

        // -------------------------------------------------------------
        // CATEGORY 2: Identity & Permissions
        // -------------------------------------------------------------
        catIdentity.EntityTypes.AddRange(new[] { "Users", "Roles", "RolePermissions", "UserRoles" });
        var users = await ReadTableAsync<UserBackupDto>("Users");
        catIdentity.TotalInBackup += users.Count;
        var existingUsers = await _context.Users.AsNoTracking().ToDictionaryAsync(x => x.UserName?.ToUpperInvariant() ?? string.Empty, cancellationToken);
        foreach (var u in users)
        {
            string key = (u.UserName ?? string.Empty).ToUpperInvariant();
            if (!existingUsers.TryGetValue(key, out var existing))
            {
                catIdentity.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existing.FullName, u.FullName, StringComparison.Ordinal) ||
                            !string.Equals(existing.Email, u.Email, StringComparison.OrdinalIgnoreCase);
                if (diff)
                {
                    catIdentity.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "User",
                        EntityDisplayName = "Tài khoản người dùng",
                        BusinessKey = u.UserName,
                        DisplayName = u.FullName ?? u.UserName,
                        CurrentSummary = $"Tên: {existing.FullName}, Email: {existing.Email}",
                        BackupSummary = $"Tên: {u.FullName}, Email: {u.Email}",
                        CurrentModifiedAt = existing.LastLoginAt,
                        BackupModifiedAt = u.LastLoginAt,
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catIdentity.MatchedCount++;
                }
            }
        }

        var roles = await ReadTableAsync<RoleBackupDto>("Roles");
        catIdentity.TotalInBackup += roles.Count;
        var existingRoles = await _context.Roles.AsNoTracking().ToDictionaryAsync(x => x.Name?.ToUpperInvariant() ?? string.Empty, cancellationToken);
        foreach (var r in roles)
        {
            if (existingRoles.ContainsKey((r.Name ?? string.Empty).ToUpperInvariant())) catIdentity.MatchedCount++;
            else catIdentity.NewCount++;
        }

        var rPerms = await ReadTableAsync<RolePermissionBackupDto>("RolePermissions");
        catIdentity.TotalInBackup += rPerms.Count;
        catIdentity.MatchedCount += rPerms.Count;

        var uRoles = await ReadTableAsync<UserRoleBackupDto>("UserRoles");
        catIdentity.TotalInBackup += uRoles.Count;
        catIdentity.MatchedCount += uRoles.Count;

        // -------------------------------------------------------------
        // CATEGORY 3: Reference & Lookups
        // -------------------------------------------------------------
        catLookups.EntityTypes.AddRange(new[] { "ProductCategories", "UnitOfMeasures", "Currencies", "Departments", "Positions" });

        var prodCats = await ReadTableAsync<ProductCategoryBackupDto>("ProductCategories");
        catLookups.TotalInBackup += prodCats.Count;
        var existingProdCats = await _context.ProductCategories.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var pc in prodCats)
        {
            if (!existingProdCats.TryGetValue(pc.Code, out var existing))
            {
                catLookups.NewCount++;
            }
            else
            {
                if (!string.Equals(existing.Name, pc.Name, StringComparison.Ordinal))
                {
                    catLookups.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "ProductCategory",
                        EntityDisplayName = "Danh mục sản phẩm",
                        BusinessKey = pc.Code,
                        DisplayName = pc.Name,
                        CurrentSummary = $"Tên: {existing.Name}",
                        BackupSummary = $"Tên: {pc.Name}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catLookups.MatchedCount++;
                }
            }
        }

        var uoms = await ReadTableAsync<UnitOfMeasureBackupDto>("UnitOfMeasures");
        catLookups.TotalInBackup += uoms.Count;
        var existingUoms = await _context.UnitOfMeasures.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var u in uoms)
        {
            if (!existingUoms.TryGetValue(u.Code, out var existing))
            {
                catLookups.NewCount++;
            }
            else
            {
                if (!string.Equals(existing.Name, u.Name, StringComparison.Ordinal))
                {
                    catLookups.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "UnitOfMeasure",
                        EntityDisplayName = "Đơn vị tính",
                        BusinessKey = u.Code,
                        DisplayName = u.Name,
                        CurrentSummary = $"Tên: {existing.Name}, Ký hiệu: {existing.Symbol}",
                        BackupSummary = $"Tên: {u.Name}, Ký hiệu: {u.Symbol}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catLookups.MatchedCount++;
                }
            }
        }

        var currs = await ReadTableAsync<CurrencyBackupDto>("Currencies");
        catLookups.TotalInBackup += currs.Count;
        var existingCurrs = await _context.Currencies.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var c in currs)
        {
            if (!existingCurrs.TryGetValue(c.Code, out var existing)) catLookups.NewCount++;
            else if (existing.ExchangeRate != c.ExchangeRate) catLookups.ConflictCount++;
            else catLookups.MatchedCount++;
        }

        var depts = await ReadTableAsync<DepartmentBackupDto>("Departments");
        catLookups.TotalInBackup += depts.Count;
        var existingDepts = await _context.Departments.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var d in depts)
        {
            if (!existingDepts.TryGetValue(d.Code, out var existing)) catLookups.NewCount++;
            else if (!string.Equals(existing.Name, d.Name, StringComparison.Ordinal)) catLookups.ConflictCount++;
            else catLookups.MatchedCount++;
        }

        var poses = await ReadTableAsync<PositionBackupDto>("Positions");
        catLookups.TotalInBackup += poses.Count;
        var existingPoses = await _context.Positions.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var p in poses)
        {
            if (!existingPoses.TryGetValue(p.Code, out var existing)) catLookups.NewCount++;
            else if (!string.Equals(existing.Name, p.Name, StringComparison.Ordinal)) catLookups.ConflictCount++;
            else catLookups.MatchedCount++;
        }

        // -------------------------------------------------------------
        // CATEGORY 4: Master Data
        // -------------------------------------------------------------
        catMaster.EntityTypes.AddRange(new[] { "Warehouses", "Customers", "Suppliers", "Products", "ProductImages", "Employees", "EmployeeRequests", "SignerIdentities", "SignerEnrollmentCodes" });

        var whs = await ReadTableAsync<WarehouseBackupDto>("Warehouses");
        catMaster.TotalInBackup += whs.Count;
        var existingWhs = await _context.Warehouses.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var w in whs)
        {
            if (!existingWhs.TryGetValue(w.Code, out var existing))
            {
                catMaster.NewCount++;
            }
            else
            {
                if (!string.Equals(existing.Name, w.Name, StringComparison.Ordinal))
                {
                    catMaster.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Warehouse",
                        EntityDisplayName = "Kho hàng",
                        BusinessKey = w.Code,
                        DisplayName = w.Name,
                        CurrentSummary = $"Tên: {existing.Name}, ĐC: {existing.Address}",
                        BackupSummary = $"Tên: {w.Name}, ĐC: {w.Address}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catMaster.MatchedCount++;
                }
            }
        }

        var custs = await ReadTableAsync<CustomerBackupDto>("Customers");
        catMaster.TotalInBackup += custs.Count;
        var existingCusts = await _context.Customers.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var c in custs)
        {
            if (!existingCusts.TryGetValue(c.Code, out var existing))
            {
                catMaster.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existing.Name, c.Name, StringComparison.Ordinal) ||
                            !string.Equals(existing.Phone, c.Phone, StringComparison.Ordinal) ||
                            !string.Equals(existing.TaxCode, c.TaxCode, StringComparison.Ordinal);
                if (diff)
                {
                    catMaster.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Customer",
                        EntityDisplayName = "Khách hàng",
                        BusinessKey = c.Code,
                        DisplayName = c.Name,
                        CurrentSummary = $"Tên: {existing.Name}, SĐT: {existing.Phone}, MST: {existing.TaxCode}",
                        BackupSummary = $"Tên: {c.Name}, SĐT: {c.Phone}, MST: {c.TaxCode}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catMaster.MatchedCount++;
                }
            }
        }

        var supps = await ReadTableAsync<SupplierBackupDto>("Suppliers");
        catMaster.TotalInBackup += supps.Count;
        var existingSupps = await _context.Suppliers.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var s in supps)
        {
            if (!existingSupps.TryGetValue(s.Code, out var existing))
            {
                catMaster.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existing.Name, s.Name, StringComparison.Ordinal) ||
                            !string.Equals(existing.Phone, s.Phone, StringComparison.Ordinal);
                if (diff)
                {
                    catMaster.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Supplier",
                        EntityDisplayName = "Nhà cung cấp",
                        BusinessKey = s.Code,
                        DisplayName = s.Name,
                        CurrentSummary = $"Tên: {existing.Name}, SĐT: {existing.Phone}",
                        BackupSummary = $"Tên: {s.Name}, SĐT: {s.Phone}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catMaster.MatchedCount++;
                }
            }
        }

        var prods = await ReadTableAsync<ProductBackupDto>("Products");
        catMaster.TotalInBackup += prods.Count;
        var existingProds = await _context.Products.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var p in prods)
        {
            if (!existingProds.TryGetValue(p.Code, out var existing))
            {
                catMaster.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existing.Name, p.Name, StringComparison.Ordinal) ||
                            !string.Equals(existing.Specifications, p.Specifications, StringComparison.Ordinal);
                if (diff)
                {
                    catMaster.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Product",
                        EntityDisplayName = "Sản phẩm",
                        BusinessKey = p.Code,
                        DisplayName = p.Name,
                        CurrentSummary = $"Tên: {existing.Name}, Tên ngắn: {existing.ShortName}",
                        BackupSummary = $"Tên: {p.Name}, Tên ngắn: {p.ShortName}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catMaster.MatchedCount++;
                }
            }
        }

        var emps = await ReadTableAsync<EmployeeBackupDto>("Employees");
        catMaster.TotalInBackup += emps.Count;
        var existingEmps = await _context.Employees.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var e in emps)
        {
            if (!existingEmps.TryGetValue(e.Code, out var existing))
            {
                catMaster.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existing.FullName, e.FullName, StringComparison.Ordinal) ||
                            !string.Equals(existing.NationalId, e.NationalId, StringComparison.Ordinal);
                if (diff)
                {
                    catMaster.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Employee",
                        EntityDisplayName = "Nhân viên",
                        BusinessKey = e.Code,
                        DisplayName = e.FullName,
                        CurrentSummary = $"Họ tên: {existing.FullName}, CCCD: {existing.NationalId}",
                        BackupSummary = $"Họ tên: {e.FullName}, CCCD: {e.NationalId}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catMaster.MatchedCount++;
                }
            }
        }

        // -------------------------------------------------------------
        // CATEGORY 5: Pricing & Templates
        // -------------------------------------------------------------
        catPricing.EntityTypes.AddRange(new[] { "PriceLists", "PriceListItems", "ContractTemplates" });

        var pls = await ReadTableAsync<PriceListBackupDto>("PriceLists");
        catPricing.TotalInBackup += pls.Count;
        var existingPls = await _context.PriceLists.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var pl in pls)
        {
            if (!existingPls.TryGetValue(pl.Code, out var existing))
            {
                catPricing.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existing.Name, pl.Name, StringComparison.Ordinal);
                if (diff)
                {
                    catPricing.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "PriceList",
                        EntityDisplayName = "Bảng giá",
                        BusinessKey = pl.Code,
                        DisplayName = pl.Name,
                        CurrentSummary = $"Tên: {existing.Name}",
                        BackupSummary = $"Tên: {pl.Name}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catPricing.MatchedCount++;
                }
            }
        }

        var cts = await ReadTableAsync<ContractTemplateBackupDto>("ContractTemplates");
        catPricing.TotalInBackup += cts.Count;
        var existingCts = await _context.ContractTemplates.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var ct in cts)
        {
            if (!existingCts.TryGetValue(ct.Code, out var existing))
            {
                catPricing.NewCount++;
            }
            else
            {
                bool diff = !string.Equals(existing.Title, ct.Title, StringComparison.Ordinal);
                if (diff)
                {
                    catPricing.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "ContractTemplate",
                        EntityDisplayName = "Mẫu hợp đồng",
                        BusinessKey = ct.Code,
                        DisplayName = ct.Title,
                        CurrentSummary = $"Tiêu đề: {existing.Title}",
                        BackupSummary = $"Tiêu đề: {ct.Title}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catPricing.MatchedCount++;
                }
            }
        }

        // -------------------------------------------------------------
        // CATEGORY 6: Transactions & Documents
        // -------------------------------------------------------------
        catTrans.EntityTypes.AddRange(new[] { "Quotations", "QuotationItems", "SalesOrders", "SalesOrderItems", "Invoices", "InvoiceItems", "PartnerPayments", "Contracts", "ContractItems", "DocumentAttachments", "DocumentSignatureAudits" });

        var quots = await ReadTableAsync<QuotationBackupDto>("Quotations");
        catTrans.TotalInBackup += quots.Count;
        var existingQuots = await _context.Quotations.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var q in quots)
        {
            if (!existingQuots.TryGetValue(q.Code, out var existing))
            {
                catTrans.NewCount++;
            }
            else
            {
                bool diff = existing.GrandTotal != q.GrandTotal || existing.Status != q.Status;
                if (diff)
                {
                    catTrans.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Quotation",
                        EntityDisplayName = "Báo giá",
                        BusinessKey = q.Code,
                        DisplayName = $"Báo giá #{q.Code}",
                        CurrentSummary = $"Tổng tiền: {existing.GrandTotal:N0}, TT: {existing.Status}",
                        BackupSummary = $"Tổng tiền: {q.GrandTotal:N0}, TT: {q.Status}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catTrans.MatchedCount++;
                }
            }
        }

        var sos = await ReadTableAsync<SalesOrderBackupDto>("SalesOrders");
        catTrans.TotalInBackup += sos.Count;
        var existingSos = await _context.SalesOrders.AsNoTracking().ToDictionaryAsync(x => x.Code, cancellationToken);
        foreach (var so in sos)
        {
            if (!existingSos.TryGetValue(so.Code, out var existing))
            {
                catTrans.NewCount++;
            }
            else
            {
                bool diff = existing.GrandTotal != so.GrandTotal || existing.Status != so.Status;
                if (diff)
                {
                    catTrans.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "SalesOrder",
                        EntityDisplayName = "Đơn bán hàng",
                        BusinessKey = so.Code,
                        DisplayName = $"Đơn hàng #{so.Code}",
                        CurrentSummary = $"Tổng tiền: {existing.GrandTotal:N0}, TT: {existing.Status}",
                        BackupSummary = $"Tổng tiền: {so.GrandTotal:N0}, TT: {so.Status}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catTrans.MatchedCount++;
                }
            }
        }

        var invs = await ReadTableAsync<InvoiceBackupDto>("Invoices");
        catTrans.TotalInBackup += invs.Count;
        var existingInvs = await _context.Invoices.AsNoTracking().ToDictionaryAsync(x => x.InvoiceNumber, cancellationToken);
        foreach (var inv in invs)
        {
            if (!existingInvs.TryGetValue(inv.InvoiceNumber, out var existing))
            {
                catTrans.NewCount++;
            }
            else
            {
                bool diff = existing.GrandTotal != inv.GrandTotal || existing.Status != inv.Status;
                if (diff)
                {
                    catTrans.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Invoice",
                        EntityDisplayName = "Hóa đơn",
                        BusinessKey = inv.InvoiceNumber,
                        DisplayName = $"Hóa đơn #{inv.InvoiceNumber}",
                        CurrentSummary = $"Tổng tiền: {existing.GrandTotal:N0}, TT: {existing.Status}",
                        BackupSummary = $"Tổng tiền: {inv.GrandTotal:N0}, TT: {inv.Status}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catTrans.MatchedCount++;
                }
            }
        }

        var pps = await ReadTableAsync<PartnerPaymentBackupDto>("PartnerPayments");
        catTrans.TotalInBackup += pps.Count;
        var existingPps = await _context.PartnerPayments.AsNoTracking().ToDictionaryAsync(x => x.PaymentCode, cancellationToken);
        foreach (var pp in pps)
        {
            if (!existingPps.TryGetValue(pp.PaymentCode, out var existing))
            {
                catTrans.NewCount++;
            }
            else
            {
                bool diff = existing.Amount != pp.Amount;
                if (diff)
                {
                    catTrans.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "PartnerPayment",
                        EntityDisplayName = "Phiếu thu / chi",
                        BusinessKey = pp.PaymentCode,
                        DisplayName = $"Chứng từ #{pp.PaymentCode}",
                        CurrentSummary = $"Số tiền: {existing.Amount:N0}, Loại: {existing.TransactionType}",
                        BackupSummary = $"Số tiền: {pp.Amount:N0}, Loại: {pp.TransactionType}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catTrans.MatchedCount++;
                }
            }
        }

        var cnts = await ReadTableAsync<ContractBackupDto>("Contracts");
        catTrans.TotalInBackup += cnts.Count;
        var existingCnts = await _context.Contracts.AsNoTracking().ToDictionaryAsync(x => x.ContractNumber, cancellationToken);
        foreach (var cnt in cnts)
        {
            if (!existingCnts.TryGetValue(cnt.ContractNumber, out var existing))
            {
                catTrans.NewCount++;
            }
            else
            {
                bool diff = existing.TotalAmount != cnt.TotalAmount || existing.Status != cnt.Status;
                if (diff)
                {
                    catTrans.ConflictCount++;
                    result.DetectedConflicts.Add(new BackupRecordConflictDto
                    {
                        EntityType = "Contract",
                        EntityDisplayName = "Hợp đồng kinh tế",
                        BusinessKey = cnt.ContractNumber,
                        DisplayName = cnt.Title,
                        CurrentSummary = $"Tiêu đề: {existing.Title}, Giá trị: {existing.TotalAmount:N0}",
                        BackupSummary = $"Tiêu đề: {cnt.Title}, Giá trị: {cnt.TotalAmount:N0}",
                        IsExactMatch = false,
                        SelectedAction = ConflictResolutionAction.KeepExisting
                    });
                }
                else
                {
                    catTrans.MatchedCount++;
                }
            }
        }

        // -------------------------------------------------------------
        // CATEGORY 7: Audit Logs
        // -------------------------------------------------------------
        catAudit.EntityTypes.Add("AuditLogs");
        var audits = await ReadTableAsync<AuditLogBackupDto>("AuditLogs");
        catAudit.TotalInBackup += audits.Count;
        catAudit.NewCount += audits.Count;

        result.TotalRecordsInBackup = result.Categories.Sum(c => c.TotalInBackup);
    }

    private static string ComputeSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static bool FilesAreEqual(FileInfo f1, FileInfo f2)
    {
        if (f1.Length != f2.Length) return false;
        if (f1.Length == 0) return true;

        const int bufferSize = 64 * 1024;
        byte[] buffer1 = new byte[bufferSize];
        byte[] buffer2 = new byte[bufferSize];

        using var fs1 = f1.OpenRead();
        using var fs2 = f2.OpenRead();

        long remaining = f1.Length;
        while (remaining > 0)
        {
            int toRead = (int)Math.Min(bufferSize, remaining);
            fs1.ReadExactly(buffer1, 0, toRead);
            fs2.ReadExactly(buffer2, 0, toRead);

            if (!buffer1.AsSpan(0, toRead).SequenceEqual(buffer2.AsSpan(0, toRead)))
                return false;

            remaining -= toRead;
        }
        return true;
    }
}
