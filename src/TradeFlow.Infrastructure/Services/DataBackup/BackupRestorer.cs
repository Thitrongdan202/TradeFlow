using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Settings;
using TradeFlow.Domain.Entities.Contracts;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Entities.Employees;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Partners;
using TradeFlow.Domain.Entities.Pricing;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services.DataBackup;

public class BackupRestorer
{
    private readonly TradeFlowDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IAuditService _auditService;
    private readonly ILogger _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BackupRestorer(
        TradeFlowDbContext context,
        IWebHostEnvironment env,
        IAuditService auditService,
        ILogger logger)
    {
        _context = context;
        _env = env;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<RestoreResult> RestoreAsync(
        ImportExecutionPlan plan,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var result = new RestoreResult { Succeeded = true };

        string stagingDir = !string.IsNullOrEmpty(plan.StagingDirectoryPath) && Directory.Exists(plan.StagingDirectoryPath)
            ? plan.StagingDirectoryPath
            : (!string.IsNullOrEmpty(plan.StagingId) ? Path.Combine(Path.GetTempPath(), "TradeFlow_Restore_Staging", plan.StagingId) : string.Empty);

        if (string.IsNullOrEmpty(stagingDir) || !Directory.Exists(stagingDir))
        {
            return new RestoreResult
            {
                Succeeded = false,
                ErrorMessage = "Phiên làm việc nhập dữ liệu đã hết hạn hoặc không tìm thấy thư mục tạm. Vui lòng tải lại tệp sao lưu."
            };
        }

        string manifestPath = Path.Combine(stagingDir, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            return new RestoreResult
            {
                Succeeded = false,
                ErrorMessage = "Tệp manifest.json không tồn tại trong thư mục nhập dữ liệu."
            };
        }

        string dataDir = Path.Combine(stagingDir, "data");
        var remapping = new IdRemappingContext();

        // Helper to resolve policy
        ConflictResolutionAction ResolveAction(string category, string entityType, string businessKey)
        {
            if (plan.RecordSpecificDecisions.TryGetValue($"{entityType}_{businessKey}", out var specific))
                return specific;
            if (plan.RecordSpecificDecisions.TryGetValue(businessKey, out var specKey))
                return specKey;
            if (plan.CategoryPolicies.TryGetValue(category, out var catAction))
                return catAction;
            return plan.GlobalConflictPolicy;
        }

        CategoryRestoreSummary GetSummary(string categoryName)
        {
            if (!result.SummaryByCategory.TryGetValue(categoryName, out var summary))
            {
                summary = new CategoryRestoreSummary { CategoryName = categoryName };
                result.SummaryByCategory[categoryName] = summary;
            }
            return summary;
        }

        async Task<List<T>> ReadTableAsync<T>(string tableName)
        {
            string file = Path.Combine(dataDir, $"{tableName}.json");
            if (!File.Exists(file)) return new List<T>();
            string json = await File.ReadAllTextAsync(file, cancellationToken);
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // =========================================================================
                // TIER 1: CORE SETTINGS & IDENTITY
                // =========================================================================
                var sumSettings = GetSummary("Cài đặt & Cấu hình hệ thống");
                var sumIdentity = GetSummary("Tài khoản & Phân quyền");

                // 1. CompanySettings
                var csList = await ReadTableAsync<CompanySettingsBackupDto>("CompanySettings");
                if (csList.Count > 0)
                {
                    sumSettings.Processed++;
                    var incoming = csList[0];
                    var existing = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
                    if (existing == null)
                    {
                        var newCs = new CompanySettings(incoming.CompanyName);
                        MapCompanySettings(newCs, incoming);
                        _context.CompanySettings.Add(newCs);
                        await _context.SaveChangesAsync(cancellationToken);
                        sumSettings.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        var action = ResolveAction(sumSettings.CategoryName, "CompanySettings", "ID 1");
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            MapCompanySettings(existing, incoming);
                            await _context.SaveChangesAsync(cancellationToken);
                            sumSettings.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumSettings.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 2. CompanySecuritySettings
                var cssList = await ReadTableAsync<CompanySecuritySettingsBackupDto>("CompanySecuritySettings");
                if (cssList.Count > 0)
                {
                    sumSettings.Processed++;
                    var incoming = cssList[0];
                    var existing = await _context.CompanySecuritySettings.FirstOrDefaultAsync(cancellationToken);
                    if (existing == null)
                    {
                        var newCss = new CompanySecuritySettings();
                        MapSecuritySettings(newCss, incoming);
                        _context.CompanySecuritySettings.Add(newCss);
                        await _context.SaveChangesAsync(cancellationToken);
                        sumSettings.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        var action = ResolveAction(sumSettings.CategoryName, "CompanySecuritySettings", "ID 1");
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            MapSecuritySettings(existing, incoming);
                            await _context.SaveChangesAsync(cancellationToken);
                            sumSettings.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumSettings.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 3. ContractPartyASettings
                var cpaList = await ReadTableAsync<ContractPartyASettingsBackupDto>("ContractPartyASettings");
                if (cpaList.Count > 0)
                {
                    sumSettings.Processed++;
                    var incoming = cpaList[0];
                    var existing = await _context.ContractPartyASettings.FirstOrDefaultAsync(cancellationToken);
                    if (existing == null)
                    {
                        var newCpa = new ContractPartyASettings();
                        MapPartyASettings(newCpa, incoming);
                        _context.ContractPartyASettings.Add(newCpa);
                        await _context.SaveChangesAsync(cancellationToken);
                        sumSettings.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        var action = ResolveAction(sumSettings.CategoryName, "ContractPartyASettings", "ID 1");
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            MapPartyASettings(existing, incoming);
                            await _context.SaveChangesAsync(cancellationToken);
                            sumSettings.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumSettings.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 4. DocumentCategories
                var docCats = await ReadTableAsync<DocumentCategoryBackupDto>("DocumentCategories");
                var existingDocCats = await _context.DocumentCategories.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var dc in docCats)
                {
                    sumSettings.Processed++;
                    if (!existingDocCats.TryGetValue(dc.Code, out var existing))
                    {
                        var newDc = new DocumentCategory
                        {
                            Code = dc.Code,
                            Name = dc.Name,
                            Description = dc.Description,
                            DisplayOrder = dc.DisplayOrder,
                            IsActive = dc.IsActive
                        };
                        _context.DocumentCategories.Add(newDc);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("DocumentCategory", dc.Id, newDc.Id);
                        sumSettings.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("DocumentCategory", dc.Id, existing.Id);
                        var action = ResolveAction(sumSettings.CategoryName, "DocumentCategory", dc.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = dc.Name;
                            existing.Description = dc.Description;
                            existing.DisplayOrder = dc.DisplayOrder;
                            existing.IsActive = dc.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumSettings.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumSettings.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 5. SystemSequences
                var seqs = await ReadTableAsync<SystemSequenceBackupDto>("SystemSequences");
                var existingSeqs = await _context.SystemSequences.ToListAsync(cancellationToken);
                foreach (var seq in seqs)
                {
                    sumSettings.Processed++;
                    var existing = existingSeqs.FirstOrDefault(x => x.SequenceKey == seq.SequenceKey && x.Prefix == seq.Prefix);
                    if (existing == null)
                    {
                        var newSeq = new SystemSequence
                        {
                            SequenceKey = seq.SequenceKey,
                            Prefix = seq.Prefix,
                            CurrentNumber = seq.CurrentNumber,
                            Step = seq.Step,
                            FormatPattern = seq.FormatPattern,
                            Description = seq.Description
                        };
                        _context.SystemSequences.Add(newSeq);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("SystemSequence", seq.Id, newSeq.Id);
                        sumSettings.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("SystemSequence", seq.Id, existing.Id);
                        if (seq.CurrentNumber > existing.CurrentNumber)
                        {
                            existing.CurrentNumber = seq.CurrentNumber;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumSettings.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumSettings.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 6. Roles
                var roles = await ReadTableAsync<RoleBackupDto>("Roles");
                var existingRoles = await _context.Roles.ToDictionaryAsync(x => x.Name?.ToUpperInvariant() ?? string.Empty, cancellationToken);
                foreach (var r in roles)
                {
                    sumIdentity.Processed++;
                    string key = (r.Name ?? string.Empty).ToUpperInvariant();
                    if (!existingRoles.TryGetValue(key, out var existingRole))
                    {
                        var newRole = new ApplicationRole
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = r.Name,
                            NormalizedName = r.Name.ToUpperInvariant(),
                            Description = r.Description,
                            IsSystem = r.IsSystem,
                            IsActive = r.IsActive,
                            CreatedAt = r.CreatedAt,
                            CreatedBy = r.CreatedBy
                        };
                        _context.Roles.Add(newRole);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Role", r.Id, newRole.Id);
                        sumIdentity.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Role", r.Id, existingRole.Id);
                        sumIdentity.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 7. RolePermissions
                var rPerms = await ReadTableAsync<RolePermissionBackupDto>("RolePermissions");
                var existingPerms = await _context.RolePermissions.ToListAsync(cancellationToken);
                foreach (var rp in rPerms)
                {
                    sumIdentity.Processed++;
                    string targetRoleId = remapping.Get("Role", rp.RoleId) ?? rp.RoleId;
                    bool exists = existingPerms.Any(x => x.RoleId == targetRoleId && x.Resource == rp.Resource && x.Action == rp.Action);
                    if (!exists)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = targetRoleId,
                            Resource = rp.Resource,
                            Action = rp.Action,
                            IsGranted = rp.IsGranted,
                            GrantedAt = rp.GrantedAt,
                            GrantedBy = rp.GrantedBy
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumIdentity.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumIdentity.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 8. Users
                var users = await ReadTableAsync<UserBackupDto>("Users");
                var existingUsers = await _context.Users.ToDictionaryAsync(x => x.UserName?.ToUpperInvariant() ?? string.Empty, cancellationToken);
                foreach (var u in users)
                {
                    sumIdentity.Processed++;
                    string uKey = (u.UserName ?? string.Empty).ToUpperInvariant();
                    if (!existingUsers.TryGetValue(uKey, out var existingUser))
                    {
                        var newUser = new ApplicationUser
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = u.UserName,
                            NormalizedUserName = u.UserName.ToUpperInvariant(),
                            Email = u.Email,
                            NormalizedEmail = (u.Email ?? string.Empty).ToUpperInvariant(),
                            FullName = u.FullName,
                            PhoneNumber = u.PhoneNumber,
                            Status = u.Status,
                            CreatedAt = u.CreatedAt,
                            CreatedBy = u.CreatedBy,
                            LastLoginAt = u.LastLoginAt,
                            SecurityStamp = Guid.NewGuid().ToString()
                        };
                        var ph = new PasswordHasher<ApplicationUser>();
                        newUser.PasswordHash = ph.HashPassword(newUser, "TradeFlow@123");
                        _context.Users.Add(newUser);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("User", u.Id, newUser.Id);
                        sumIdentity.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("User", u.Id, existingUser.Id);
                        var action = ResolveAction(sumIdentity.CategoryName, "User", u.UserName);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existingUser.FullName = u.FullName;
                            existingUser.PhoneNumber = u.PhoneNumber;
                            existingUser.Status = u.Status;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumIdentity.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumIdentity.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 9. UserRoles
                var uRoles = await ReadTableAsync<UserRoleBackupDto>("UserRoles");
                var existingUserRoles = await _context.UserRoles.ToListAsync(cancellationToken);
                foreach (var ur in uRoles)
                {
                    sumIdentity.Processed++;
                    string targetUserId = remapping.Get("User", ur.UserId) ?? ur.UserId;
                    string targetRoleId = remapping.Get("Role", ur.RoleId) ?? ur.RoleId;
                    bool exists = existingUserRoles.Any(x => x.UserId == targetUserId && x.RoleId == targetRoleId);
                    if (!exists)
                    {
                        _context.UserRoles.Add(new ApplicationUserRole { UserId = targetUserId, RoleId = targetRoleId });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumIdentity.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumIdentity.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // =========================================================================
                // TIER 2: REFERENCE & LOOKUPS
                // =========================================================================
                var sumLookups = GetSummary("Danh mục & Tham chiếu");

                // 10. ProductCategories
                var prodCats = await ReadTableAsync<ProductCategoryBackupDto>("ProductCategories");
                var sortedProdCats = prodCats.OrderBy(x => x.ParentCategoryId == null ? 0 : 1).ToList();
                var existingProdCats = await _context.ProductCategories.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var pc in sortedProdCats)
                {
                    sumLookups.Processed++;
                    if (!existingProdCats.TryGetValue(pc.Code, out var existing))
                    {
                        var newPc = new ProductCategory
                        {
                            Code = pc.Code,
                            Name = pc.Name,
                            Description = pc.Description,
                            DisplayOrder = pc.DisplayOrder,
                            IsActive = pc.IsActive,
                            ParentCategoryId = remapping.Get("ProductCategory", pc.ParentCategoryId)
                        };
                        _context.ProductCategories.Add(newPc);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("ProductCategory", pc.Id, newPc.Id);
                        sumLookups.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("ProductCategory", pc.Id, existing.Id);
                        var action = ResolveAction(sumLookups.CategoryName, "ProductCategory", pc.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = pc.Name;
                            existing.Description = pc.Description;
                            existing.DisplayOrder = pc.DisplayOrder;
                            existing.IsActive = pc.IsActive;
                            if (pc.ParentCategoryId.HasValue)
                                existing.ParentCategoryId = remapping.Get("ProductCategory", pc.ParentCategoryId);
                            await _context.SaveChangesAsync(cancellationToken);
                            sumLookups.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumLookups.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 11. UnitOfMeasures
                var uoms = await ReadTableAsync<UnitOfMeasureBackupDto>("UnitOfMeasures");
                var existingUoms = await _context.UnitOfMeasures.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var u in uoms)
                {
                    sumLookups.Processed++;
                    if (!existingUoms.TryGetValue(u.Code, out var existing))
                    {
                        var newUom = new UnitOfMeasure { Code = u.Code, Name = u.Name, Symbol = u.Symbol, IsActive = u.IsActive };
                        _context.UnitOfMeasures.Add(newUom);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("UnitOfMeasure", u.Id, newUom.Id);
                        sumLookups.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("UnitOfMeasure", u.Id, existing.Id);
                        var action = ResolveAction(sumLookups.CategoryName, "UnitOfMeasure", u.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = u.Name;
                            existing.Symbol = u.Symbol;
                            existing.IsActive = u.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumLookups.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumLookups.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 12. Currencies
                var currs = await ReadTableAsync<CurrencyBackupDto>("Currencies");
                var existingCurrs = await _context.Currencies.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var c in currs)
                {
                    sumLookups.Processed++;
                    if (!existingCurrs.TryGetValue(c.Code, out var existing))
                    {
                        var newCurr = new Currency
                        {
                            Code = c.Code,
                            Name = c.Name,
                            Symbol = c.Symbol,
                            ExchangeRate = c.ExchangeRate,
                            IsDefault = c.IsDefault,
                            IsActive = c.IsActive
                        };
                        _context.Currencies.Add(newCurr);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Currency", c.Id, newCurr.Id);
                        sumLookups.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Currency", c.Id, existing.Id);
                        var action = ResolveAction(sumLookups.CategoryName, "Currency", c.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = c.Name;
                            existing.Symbol = c.Symbol;
                            existing.ExchangeRate = c.ExchangeRate;
                            existing.IsDefault = c.IsDefault;
                            existing.IsActive = c.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumLookups.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumLookups.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 13. Departments
                var depts = await ReadTableAsync<DepartmentBackupDto>("Departments");
                var existingDepts = await _context.Departments.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var d in depts)
                {
                    sumLookups.Processed++;
                    if (!existingDepts.TryGetValue(d.Code, out var existing))
                    {
                        var newDept = new Department { Code = d.Code, Name = d.Name, Description = d.Description, IsActive = d.IsActive };
                        _context.Departments.Add(newDept);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Department", d.Id, newDept.Id);
                        sumLookups.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Department", d.Id, existing.Id);
                        var action = ResolveAction(sumLookups.CategoryName, "Department", d.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = d.Name;
                            existing.Description = d.Description;
                            existing.IsActive = d.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumLookups.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumLookups.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 14. Positions
                var poses = await ReadTableAsync<PositionBackupDto>("Positions");
                var existingPoses = await _context.Positions.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var p in poses)
                {
                    sumLookups.Processed++;
                    if (!existingPoses.TryGetValue(p.Code, out var existing))
                    {
                        var newPos = new Position { Code = p.Code, Name = p.Name, Description = p.Description, IsActive = p.IsActive };
                        _context.Positions.Add(newPos);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Position", p.Id, newPos.Id);
                        sumLookups.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Position", p.Id, existing.Id);
                        var action = ResolveAction(sumLookups.CategoryName, "Position", p.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = p.Name;
                            existing.Description = p.Description;
                            existing.IsActive = p.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumLookups.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumLookups.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // =========================================================================
                // TIER 3: MASTER DATA
                // =========================================================================
                var sumMaster = GetSummary("Dữ liệu nguồn (Master Data)");

                // 15. Warehouses
                var whs = await ReadTableAsync<WarehouseBackupDto>("Warehouses");
                var existingWhs = await _context.Warehouses.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var w in whs)
                {
                    sumMaster.Processed++;
                    if (!existingWhs.TryGetValue(w.Code, out var existing))
                    {
                        var newWh = new Warehouse { Code = w.Code, Name = w.Name, Address = w.Address, ManagerName = w.ManagerName, Phone = w.Phone, Notes = w.Notes, IsActive = w.IsActive };
                        _context.Warehouses.Add(newWh);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Warehouse", w.Id, newWh.Id);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Warehouse", w.Id, existing.Id);
                        var action = ResolveAction(sumMaster.CategoryName, "Warehouse", w.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = w.Name;
                            existing.Address = w.Address;
                            existing.ManagerName = w.ManagerName;
                            existing.Phone = w.Phone;
                            existing.Notes = w.Notes;
                            existing.IsActive = w.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumMaster.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumMaster.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 16. Customers
                var custs = await ReadTableAsync<CustomerBackupDto>("Customers");
                var existingCusts = await _context.Customers.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var c in custs)
                {
                    sumMaster.Processed++;
                    if (!existingCusts.TryGetValue(c.Code, out var existing))
                    {
                        var newCust = new Customer
                        {
                            Code = c.Code,
                            Name = c.Name,
                            CompanyName = c.CompanyName,
                            TaxCode = c.TaxCode,
                            ContactPerson = c.ContactPerson,
                            Phone = c.Phone,
                            Email = c.Email,
                            Address = c.Address,
                            Notes = c.Notes,
                            IsActive = c.IsActive
                        };
                        _context.Customers.Add(newCust);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Customer", c.Id, newCust.Id);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Customer", c.Id, existing.Id);
                        var action = ResolveAction(sumMaster.CategoryName, "Customer", c.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = c.Name;
                            existing.CompanyName = c.CompanyName;
                            existing.TaxCode = c.TaxCode;
                            existing.ContactPerson = c.ContactPerson;
                            existing.Phone = c.Phone;
                            existing.Email = c.Email;
                            existing.Address = c.Address;
                            existing.Notes = c.Notes;
                            existing.IsActive = c.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumMaster.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumMaster.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 17. Suppliers
                var supps = await ReadTableAsync<SupplierBackupDto>("Suppliers");
                var existingSupps = await _context.Suppliers.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var s in supps)
                {
                    sumMaster.Processed++;
                    if (!existingSupps.TryGetValue(s.Code, out var existing))
                    {
                        var newSupp = new Supplier
                        {
                            Code = s.Code,
                            Name = s.Name,
                            CompanyName = s.CompanyName,
                            TaxCode = s.TaxCode,
                            ContactPerson = s.ContactPerson,
                            Phone = s.Phone,
                            Email = s.Email,
                            Address = s.Address,
                            Notes = s.Notes,
                            IsActive = s.IsActive
                        };
                        _context.Suppliers.Add(newSupp);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Supplier", s.Id, newSupp.Id);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Supplier", s.Id, existing.Id);
                        var action = ResolveAction(sumMaster.CategoryName, "Supplier", s.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = s.Name;
                            existing.CompanyName = s.CompanyName;
                            existing.TaxCode = s.TaxCode;
                            existing.ContactPerson = s.ContactPerson;
                            existing.Phone = s.Phone;
                            existing.Email = s.Email;
                            existing.Address = s.Address;
                            existing.Notes = s.Notes;
                            existing.IsActive = s.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumMaster.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumMaster.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 18. Products
                var prods = await ReadTableAsync<ProductBackupDto>("Products");
                var existingProds = await _context.Products.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var p in prods)
                {
                    sumMaster.Processed++;
                    if (!existingProds.TryGetValue(p.Code, out var existing))
                    {
                        var newProd = new Product
                        {
                            Code = p.Code,
                            LegacyCode = p.LegacyCode,
                            Name = p.Name,
                            ShortName = p.ShortName,
                            CategoryId = remapping.Get("ProductCategory", p.CategoryId),
                            UnitId = remapping.Get("UnitOfMeasure", p.UnitId),
                            Description = p.Description,
                            Specifications = p.Specifications,
                            IsActive = p.IsActive
                        };
                        _context.Products.Add(newProd);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Product", p.Id, newProd.Id);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Product", p.Id, existing.Id);
                        var action = ResolveAction(sumMaster.CategoryName, "Product", p.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = p.Name;
                            existing.ShortName = p.ShortName;
                            existing.LegacyCode = p.LegacyCode;
                            if (p.CategoryId.HasValue) existing.CategoryId = remapping.Get("ProductCategory", p.CategoryId);
                            if (p.UnitId.HasValue) existing.UnitId = remapping.Get("UnitOfMeasure", p.UnitId);
                            existing.Description = p.Description;
                            existing.Specifications = p.Specifications;
                            existing.IsActive = p.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumMaster.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumMaster.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 19. ProductImages
                var prodImgs = await ReadTableAsync<ProductImageBackupDto>("ProductImages");
                foreach (var pi in prodImgs)
                {
                    sumMaster.Processed++;
                    int targetProductId = remapping.Resolve("Product", pi.ProductId);
                    bool exists = await _context.ProductImages.AnyAsync(x => x.ProductId == targetProductId && x.StorageReference == pi.StorageReference, cancellationToken);
                    if (!exists)
                    {
                        _context.ProductImages.Add(new ProductImage
                        {
                            ProductId = targetProductId,
                            FileName = pi.FileName,
                            StorageReference = pi.StorageReference,
                            ContentType = pi.ContentType,
                            FileSizeBytes = pi.FileSizeBytes,
                            IsPrimary = pi.IsPrimary,
                            SortOrder = pi.SortOrder,
                            IsActive = pi.IsActive
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumMaster.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 20. Employees
                var emps = await ReadTableAsync<EmployeeBackupDto>("Employees");
                var existingEmps = await _context.Employees.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var e in emps)
                {
                    sumMaster.Processed++;
                    if (!existingEmps.TryGetValue(e.Code, out var existing))
                    {
                        var newEmp = new Employee
                        {
                            Code = e.Code,
                            AttendanceCode = e.AttendanceCode,
                            FullName = e.FullName,
                            DateOfBirth = e.DateOfBirth,
                            Gender = e.Gender,
                            NationalId = e.NationalId,
                            Email = e.Email,
                            Phone = e.Phone,
                            Address = e.Address,
                            Facebook = e.Facebook,
                            MobileDevice = e.MobileDevice,
                            DepartmentId = remapping.Get("Department", e.DepartmentId),
                            PositionId = remapping.Get("Position", e.PositionId),
                            WorkingBranch = e.WorkingBranch,
                            PayrollBranch = e.PayrollBranch,
                            StartDate = e.StartDate,
                            TerminationDate = e.TerminationDate,
                            TerminationReason = e.TerminationReason,
                            Status = e.Status,
                            AvatarPath = e.AvatarPath,
                            BankInformation = e.BankInformation,
                            DebtAndAdvance = e.DebtAndAdvance,
                            Notes = e.Notes,
                            UserId = remapping.Get("User", e.UserId) ?? e.UserId
                        };
                        _context.Employees.Add(newEmp);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Employee", e.Id, newEmp.Id);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Employee", e.Id, existing.Id);
                        var action = ResolveAction(sumMaster.CategoryName, "Employee", e.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.FullName = e.FullName;
                            existing.Email = e.Email;
                            existing.Phone = e.Phone;
                            existing.Address = e.Address;
                            existing.NationalId = e.NationalId;
                            existing.Status = e.Status;
                            existing.AvatarPath = e.AvatarPath;
                            existing.Notes = e.Notes;
                            existing.AttendanceCode = e.AttendanceCode;
                            existing.WorkingBranch = e.WorkingBranch;
                            existing.PayrollBranch = e.PayrollBranch;
                            existing.StartDate = e.StartDate;
                            existing.BankInformation = e.BankInformation;
                            existing.DebtAndAdvance = e.DebtAndAdvance;
                            if (e.DepartmentId.HasValue) existing.DepartmentId = remapping.Get("Department", e.DepartmentId);
                            if (e.PositionId.HasValue) existing.PositionId = remapping.Get("Position", e.PositionId);
                            await _context.SaveChangesAsync(cancellationToken);
                            sumMaster.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumMaster.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 21. EmployeeRequests
                var empReqs = await ReadTableAsync<EmployeeRequestBackupDto>("EmployeeRequests");
                foreach (var er in empReqs)
                {
                    sumMaster.Processed++;
                    var newReq = new EmployeeRequest
                    {
                        FullName = er.FullName,
                        Code = er.Code,
                        AttendanceCode = er.AttendanceCode,
                        Phone = er.Phone,
                        Email = er.Email,
                        NationalId = er.NationalId,
                        DateOfBirth = er.DateOfBirth,
                        Gender = er.Gender,
                        Address = er.Address,
                        DepartmentId = remapping.Get("Department", er.DepartmentId),
                        PositionId = remapping.Get("Position", er.PositionId),
                        WorkingBranch = er.WorkingBranch,
                        PayrollBranch = er.PayrollBranch,
                        StartDate = er.StartDate,
                        BankInformation = er.BankInformation,
                        Notes = er.Notes,
                        ProposedUserId = remapping.Get("User", er.ProposedUserId) ?? er.ProposedUserId,
                        Status = er.Status,
                        RequestedBy = er.RequestedBy,
                        ApprovedBy = er.ApprovedBy,
                        ApprovedAt = er.ApprovedAt,
                        ReviewNotes = er.ReviewNotes,
                        CreatedEmployeeId = remapping.Get("Employee", er.CreatedEmployeeId)
                    };
                    _context.EmployeeRequests.Add(newReq);
                    await _context.SaveChangesAsync(cancellationToken);
                    sumMaster.Imported++;
                    result.NewRecordsImported++;
                }

                // 22. SignerIdentities
                var signIds = await ReadTableAsync<SignerIdentityBackupDto>("SignerIdentities");
                var existingSigners = await _context.SignerIdentities.ToListAsync(cancellationToken);
                foreach (var si in signIds)
                {
                    sumMaster.Processed++;
                    var existing = existingSigners.FirstOrDefault(x => x.CertificateSerialNumber == si.CertificateSerialNumber);
                    if (existing == null)
                    {
                        var newSi = new SignerIdentity
                        {
                            UserId = remapping.Get("User", si.UserId) ?? si.UserId,
                            UserName = si.UserName,
                            FullName = si.FullName,
                            Position = si.Position,
                            SignerRole = si.SignerRole,
                            Status = si.Status,
                            ProviderType = si.ProviderType,
                            CertificateSerialNumber = si.CertificateSerialNumber,
                            CertificateSubject = si.CertificateSubject,
                            CertificateIssuer = si.CertificateIssuer,
                            CertificateThumbprint = si.CertificateThumbprint,
                            ValidFrom = si.ValidFrom,
                            ValidTo = si.ValidTo
                        };
                        _context.SignerIdentities.Add(newSi);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("SignerIdentity", si.Id, newSi.Id);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("SignerIdentity", si.Id, existing.Id);
                        sumMaster.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                var signCodes = await ReadTableAsync<SignerEnrollmentCodeBackupDto>("SignerEnrollmentCodes");
                foreach (var sc in signCodes)
                {
                    sumMaster.Processed++;
                    var existing = await _context.SignerEnrollmentCodes.FirstOrDefaultAsync(x => x.CodeHash == sc.CodeHash, cancellationToken);
                    if (existing == null)
                    {
                        var newSc = new SignerEnrollmentCode
                        {
                            CodeHash = sc.CodeHash,
                            TargetUserId = remapping.Get("User", sc.TargetUserId) ?? sc.TargetUserId,
                            TargetUserName = sc.TargetUserName,
                            TargetFullName = sc.TargetFullName,
                            TargetPosition = sc.TargetPosition,
                            TargetSignerRole = sc.TargetSignerRole,
                            HasExpiration = sc.HasExpiration,
                            ExpiresAt = sc.ExpiresAt,
                            IsUsed = sc.IsUsed,
                            UsedAt = sc.UsedAt,
                            UsedBy = sc.UsedBy,
                            IsRevoked = sc.IsRevoked,
                            RevokedAt = sc.RevokedAt,
                            RevokedBy = sc.RevokedBy,
                            RevocationReason = sc.RevocationReason
                        };
                        _context.SignerEnrollmentCodes.Add(newSc);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("SignerEnrollmentCode", sc.Id, newSc.Id);
                        sumMaster.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("SignerEnrollmentCode", sc.Id, existing.Id);
                        sumMaster.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // =========================================================================
                // TIER 4: PRICING & TEMPLATES
                // =========================================================================
                var sumPricing = GetSummary("Bảng giá & Hợp đồng mẫu");

                // 23. PriceLists
                var pls = await ReadTableAsync<PriceListBackupDto>("PriceLists");
                var existingPls = await _context.PriceLists.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var pl in pls)
                {
                    sumPricing.Processed++;
                    if (!existingPls.TryGetValue(pl.Code, out var existing))
                    {
                        var newPl = new PriceList
                        {
                            Code = pl.Code,
                            Name = pl.Name,
                            QuotationNumber = pl.QuotationNumber,
                            QuotationDate = pl.QuotationDate,
                            EffectiveFrom = pl.EffectiveFrom,
                            EffectiveTo = pl.EffectiveTo,
                            Month = pl.Month,
                            Quarter = pl.Quarter,
                            Year = pl.Year,
                            ProgramTitle = pl.ProgramTitle,
                            PriceCondition = pl.PriceCondition,
                            VatNote = pl.VatNote,
                            Status = pl.Status,
                            OriginalFileName = pl.OriginalFileName,
                            OriginalFileStorageRef = pl.OriginalFileStorageRef,
                            TotalItems = pl.TotalItems,
                            Notes = pl.Notes
                        };
                        _context.PriceLists.Add(newPl);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("PriceList", pl.Id, newPl.Id);
                        sumPricing.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("PriceList", pl.Id, existing.Id);
                        var action = ResolveAction(sumPricing.CategoryName, "PriceList", pl.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Name = pl.Name;
                            existing.QuotationNumber = pl.QuotationNumber;
                            existing.QuotationDate = pl.QuotationDate;
                            existing.EffectiveFrom = pl.EffectiveFrom;
                            existing.EffectiveTo = pl.EffectiveTo;
                            existing.Month = pl.Month;
                            existing.Quarter = pl.Quarter;
                            existing.Year = pl.Year;
                            existing.ProgramTitle = pl.ProgramTitle;
                            existing.PriceCondition = pl.PriceCondition;
                            existing.VatNote = pl.VatNote;
                            existing.Status = pl.Status;
                            existing.OriginalFileName = pl.OriginalFileName;
                            existing.OriginalFileStorageRef = pl.OriginalFileStorageRef;
                            existing.TotalItems = pl.TotalItems;
                            existing.Notes = pl.Notes;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumPricing.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumPricing.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 24. PriceListItems
                var plis = await ReadTableAsync<PriceListItemBackupDto>("PriceListItems");
                foreach (var pli in plis)
                {
                    sumPricing.Processed++;
                    int targetPlId = remapping.Resolve("PriceList", pli.PriceListId);
                    int? targetProdId = remapping.Get("Product", pli.ProductId);
                    bool exists = await _context.PriceListItems.AnyAsync(x => x.PriceListId == targetPlId && (targetProdId == null || x.ProductId == targetProdId) && x.NewCode == pli.NewCode, cancellationToken);
                    if (!exists)
                    {
                        _context.PriceListItems.Add(new PriceListItem
                        {
                            PriceListId = targetPlId,
                            ProductId = targetProdId,
                            SortOrder = pli.SortOrder,
                            Group = pli.Group,
                            NewCode = pli.NewCode,
                            LegacyCode = pli.LegacyCode,
                            ProductInfo = pli.ProductInfo,
                            ImageStorageRef = pli.ImageStorageRef,
                            UnitPrice = pli.UnitPrice,
                            CurrencyCode = pli.CurrencyCode,
                            VatRate = pli.VatRate,
                            Note = pli.Note,
                            MatchStatus = pli.MatchStatus
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumPricing.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumPricing.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 25. ContractTemplates
                var cts = await ReadTableAsync<ContractTemplateBackupDto>("ContractTemplates");
                var existingCts = await _context.ContractTemplates.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var ct in cts)
                {
                    sumPricing.Processed++;
                    if (!existingCts.TryGetValue(ct.Code, out var existing))
                    {
                        var newCt = new ContractTemplate
                        {
                            Code = ct.Code,
                            Title = ct.Title,
                            Category = ct.Category,
                            TargetPartyType = ct.TargetPartyType,
                            Description = ct.Description,
                            TypicalUseCase = ct.TypicalUseCase,
                            LegalBasis = ct.LegalBasis,
                            DefaultClausesJson = ct.DefaultClausesJson,
                            IsActive = ct.IsActive,
                            DisplayOrder = ct.DisplayOrder
                        };
                        _context.ContractTemplates.Add(newCt);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("ContractTemplate", ct.Id, newCt.Id);
                        sumPricing.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("ContractTemplate", ct.Id, existing.Id);
                        var action = ResolveAction(sumPricing.CategoryName, "ContractTemplate", ct.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Title = ct.Title;
                            existing.Category = ct.Category;
                            existing.TargetPartyType = ct.TargetPartyType;
                            existing.Description = ct.Description;
                            existing.TypicalUseCase = ct.TypicalUseCase;
                            existing.LegalBasis = ct.LegalBasis;
                            existing.DefaultClausesJson = ct.DefaultClausesJson;
                            existing.IsActive = ct.IsActive;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumPricing.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumPricing.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // =========================================================================
                // TIER 5: TRANSACTIONS & DOCUMENTS
                // =========================================================================
                var sumTrans = GetSummary("Nghiệp vụ & Chứng từ giao dịch");

                // 26. Quotations
                var quots = await ReadTableAsync<QuotationBackupDto>("Quotations");
                var existingQuots = await _context.Quotations.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var q in quots)
                {
                    sumTrans.Processed++;
                    if (!existingQuots.TryGetValue(q.Code, out var existing))
                    {
                        var newQ = new Quotation
                        {
                            Code = q.Code,
                            QuotationDate = q.QuotationDate,
                            ExpiryDate = q.ExpiryDate,
                            CustomerId = remapping.Resolve("Customer", q.CustomerId),
                            CustomerName = q.CustomerName,
                            CustomerTaxCode = q.CustomerTaxCode,
                            CustomerAddress = q.CustomerAddress,
                            CustomerPhone = q.CustomerPhone,
                            CustomerContactPerson = q.CustomerContactPerson,
                            CustomerEmail = q.CustomerEmail,
                            SalespersonName = q.SalespersonName,
                            Notes = q.Notes,
                            Terms = q.Terms,
                            Status = q.Status,
                            SubTotal = q.SubTotal,
                            TotalDiscount = q.TotalDiscount,
                            GrandTotal = q.GrandTotal,
                            SalesOrderId = remapping.Get("SalesOrder", q.SalesOrderId)
                        };
                        _context.Quotations.Add(newQ);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Quotation", q.Id, newQ.Id);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Quotation", q.Id, existing.Id);
                        var action = ResolveAction(sumTrans.CategoryName, "Quotation", q.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.GrandTotal = q.GrandTotal;
                            existing.Status = q.Status;
                            existing.Notes = q.Notes;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumTrans.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumTrans.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 27. QuotationItems
                var quotiList = await ReadTableAsync<QuotationItemBackupDto>("QuotationItems");
                foreach (var qi in quotiList)
                {
                    sumTrans.Processed++;
                    int targetQId = remapping.Resolve("Quotation", qi.QuotationId);
                    int targetPId = remapping.Resolve("Product", qi.ProductId);
                    bool exists = await _context.QuotationItems.AnyAsync(x => x.QuotationId == targetQId && x.ProductId == targetPId, cancellationToken);
                    if (!exists)
                    {
                        _context.QuotationItems.Add(new QuotationItem
                        {
                            QuotationId = targetQId,
                            ProductId = targetPId,
                            ProductCode = qi.ProductCode,
                            ProductName = qi.ProductName,
                            UnitName = qi.UnitName,
                            Quantity = qi.Quantity,
                            UnitPrice = qi.UnitPrice,
                            PriceSource = qi.PriceSource,
                            DiscountRate = qi.DiscountRate,
                            DiscountAmount = qi.DiscountAmount,
                            LineTotal = qi.LineTotal,
                            SortOrder = qi.SortOrder,
                            Notes = qi.Notes
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumTrans.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 28. SalesOrders
                var sos = await ReadTableAsync<SalesOrderBackupDto>("SalesOrders");
                var existingSos = await _context.SalesOrders.ToDictionaryAsync(x => x.Code, cancellationToken);
                foreach (var so in sos)
                {
                    sumTrans.Processed++;
                    if (!existingSos.TryGetValue(so.Code, out var existing))
                    {
                        var newSo = new SalesOrder
                        {
                            Code = so.Code,
                            OrderDate = so.OrderDate,
                            DeliveryDate = so.DeliveryDate,
                            CustomerId = remapping.Resolve("Customer", so.CustomerId),
                            CustomerName = so.CustomerName,
                            CustomerTaxCode = so.CustomerTaxCode,
                            CustomerAddress = so.CustomerAddress,
                            CustomerPhone = so.CustomerPhone,
                            Notes = so.Notes,
                            Status = so.Status,
                            SubTotal = so.SubTotal,
                            TotalDiscount = so.TotalDiscount,
                            TotalTax = so.TotalTax,
                            GrandTotal = so.GrandTotal,
                            QuotationId = remapping.Get("Quotation", so.QuotationId)
                        };
                        _context.SalesOrders.Add(newSo);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("SalesOrder", so.Id, newSo.Id);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("SalesOrder", so.Id, existing.Id);
                        var action = ResolveAction(sumTrans.CategoryName, "SalesOrder", so.Code);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.GrandTotal = so.GrandTotal;
                            existing.Status = so.Status;
                            existing.Notes = so.Notes;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumTrans.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumTrans.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 29. SalesOrderItems
                var sois = await ReadTableAsync<SalesOrderItemBackupDto>("SalesOrderItems");
                foreach (var soi in sois)
                {
                    sumTrans.Processed++;
                    int targetSoId = remapping.Resolve("SalesOrder", soi.SalesOrderId);
                    int targetPId = remapping.Resolve("Product", soi.ProductId);
                    bool exists = await _context.SalesOrderItems.AnyAsync(x => x.SalesOrderId == targetSoId && x.ProductId == targetPId, cancellationToken);
                    if (!exists)
                    {
                        _context.SalesOrderItems.Add(new SalesOrderItem
                        {
                            SalesOrderId = targetSoId,
                            ProductId = targetPId,
                            ProductCode = soi.ProductCode,
                            ProductName = soi.ProductName,
                            UnitName = soi.UnitName,
                            Quantity = soi.Quantity,
                            UnitPrice = soi.UnitPrice,
                            PriceSource = soi.PriceSource,
                            DiscountAmount = soi.DiscountAmount,
                            TaxRate = soi.TaxRate,
                            TaxAmount = soi.TaxAmount,
                            LineTotal = soi.LineTotal,
                            Notes = soi.Notes
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumTrans.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 30. Invoices
                var invs = await ReadTableAsync<InvoiceBackupDto>("Invoices");
                var existingInvs = await _context.Invoices.ToDictionaryAsync(x => x.InvoiceNumber, cancellationToken);
                foreach (var inv in invs)
                {
                    sumTrans.Processed++;
                    if (!existingInvs.TryGetValue(inv.InvoiceNumber, out var existing))
                    {
                        var newInv = new Invoice
                        {
                            InvoiceNumber = inv.InvoiceNumber,
                            FormNumber = inv.FormNumber,
                            InvoiceSeries = inv.InvoiceSeries,
                            InvoiceNo = inv.InvoiceNo,
                            InvoiceDate = inv.InvoiceDate,
                            SalesOrderId = remapping.Get("SalesOrder", inv.SalesOrderId),
                            CustomerId = remapping.Get("Customer", inv.CustomerId),
                            CompanyName = inv.CompanyName,
                            CompanyTaxCode = inv.CompanyTaxCode,
                            CompanyAddress = inv.CompanyAddress,
                            CompanyPhone = inv.CompanyPhone,
                            CompanyEmail = inv.CompanyEmail,
                            CompanyBankAccount = inv.CompanyBankAccount,
                            CompanyBankName = inv.CompanyBankName,
                            CompanyLogoUrl = inv.CompanyLogoUrl,
                            CustomerName = inv.CustomerName,
                            CustomerCompanyName = inv.CustomerCompanyName,
                            CustomerTaxCode = inv.CustomerTaxCode,
                            CustomerAddress = inv.CustomerAddress,
                            CustomerEmail = inv.CustomerEmail,
                            CustomerBankAccount = inv.CustomerBankAccount,
                            CustomerBankName = inv.CustomerBankName,
                            PaymentMethod = inv.PaymentMethod,
                            Notes = inv.Notes,
                            Status = inv.Status,
                            Type = inv.Type,
                            SubTotal = inv.SubTotal,
                            TotalDiscount = inv.TotalDiscount,
                            TotalTax = inv.TotalTax,
                            GrandTotal = inv.GrandTotal,
                            TaxAuthorityCode = inv.TaxAuthorityCode,
                            QrCodeData = inv.QrCodeData,
                            SignatureStatus = inv.SignatureStatus,
                            SignedBy = inv.SignedBy,
                            SignedAt = inv.SignedAt,
                            SignatureValue = inv.SignatureValue,
                            CertificateSubject = inv.CertificateSubject,
                            CertificateSerialNumber = inv.CertificateSerialNumber,
                            SignerPosition = inv.SignerPosition,
                            SignerRole = inv.SignerRole,
                            SigningProvider = inv.SigningProvider,
                            DocumentHash = inv.DocumentHash,
                            SignerIdentityId = remapping.Get("SignerIdentity", inv.SignerIdentityId),
                            SignerHandwrittenSignatureImage = inv.SignerHandwrittenSignatureImage
                        };
                        _context.Invoices.Add(newInv);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Invoice", inv.Id, newInv.Id);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Invoice", inv.Id, existing.Id);
                        var action = ResolveAction(sumTrans.CategoryName, "Invoice", inv.InvoiceNumber);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.GrandTotal = inv.GrandTotal;
                            existing.Status = inv.Status;
                            existing.Notes = inv.Notes;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumTrans.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumTrans.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 31. InvoiceItems
                var invis = await ReadTableAsync<InvoiceItemBackupDto>("InvoiceItems");
                foreach (var invi in invis)
                {
                    sumTrans.Processed++;
                    int targetInvId = remapping.Resolve("Invoice", invi.InvoiceId);
                    int? targetPId = remapping.Get("Product", invi.ProductId);
                    bool exists = await _context.InvoiceItems.AnyAsync(x => x.InvoiceId == targetInvId && x.ProductCode == invi.ProductCode, cancellationToken);
                    if (!exists)
                    {
                        _context.InvoiceItems.Add(new InvoiceItem
                        {
                            InvoiceId = targetInvId,
                            ProductId = targetPId,
                            SortOrder = invi.SortOrder,
                            ProductCode = invi.ProductCode,
                            ProductName = invi.ProductName,
                            UnitName = invi.UnitName,
                            Quantity = invi.Quantity,
                            UnitPrice = invi.UnitPrice,
                            DiscountAmount = invi.DiscountAmount,
                            TaxRate = invi.TaxRate,
                            TaxAmount = invi.TaxAmount,
                            LineTotal = invi.LineTotal
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumTrans.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 32. PartnerPayments
                var pps = await ReadTableAsync<PartnerPaymentBackupDto>("PartnerPayments");
                var existingPps = await _context.PartnerPayments.ToDictionaryAsync(x => x.PaymentCode, cancellationToken);
                foreach (var pp in pps)
                {
                    sumTrans.Processed++;
                    if (!existingPps.TryGetValue(pp.PaymentCode, out var existing))
                    {
                        var newPp = new PartnerPayment
                        {
                            PaymentCode = pp.PaymentCode,
                            PartnerType = pp.PartnerType,
                            PartnerId = pp.PartnerType == 1 ? remapping.Resolve("Customer", pp.PartnerId) : remapping.Resolve("Supplier", pp.PartnerId),
                            CustomerId = remapping.Get("Customer", pp.CustomerId),
                            SupplierId = remapping.Get("Supplier", pp.SupplierId),
                            TransactionType = pp.TransactionType,
                            PaymentDate = pp.PaymentDate,
                            Amount = pp.Amount,
                            PaymentMethod = pp.PaymentMethod,
                            ReferenceCode = pp.ReferenceCode,
                            InvoiceId = remapping.Get("Invoice", pp.InvoiceId),
                            SalesOrderId = remapping.Get("SalesOrder", pp.SalesOrderId),
                            Notes = pp.Notes
                        };
                        _context.PartnerPayments.Add(newPp);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("PartnerPayment", pp.Id, newPp.Id);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("PartnerPayment", pp.Id, existing.Id);
                        var action = ResolveAction(sumTrans.CategoryName, "PartnerPayment", pp.PaymentCode);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            existing.Amount = pp.Amount;
                            existing.Notes = pp.Notes;
                            await _context.SaveChangesAsync(cancellationToken);
                            sumTrans.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumTrans.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 33. Contracts
                var cnts = await ReadTableAsync<ContractBackupDto>("Contracts");
                var existingCnts = await _context.Contracts.ToDictionaryAsync(x => x.ContractNumber, cancellationToken);
                foreach (var cnt in cnts)
                {
                    sumTrans.Processed++;
                    if (!existingCnts.TryGetValue(cnt.ContractNumber, out var existing))
                    {
                        var newCnt = new Contract();
                        MapContract(newCnt, cnt, remapping);
                        _context.Contracts.Add(newCnt);
                        await _context.SaveChangesAsync(cancellationToken);
                        remapping.Map("Contract", cnt.Id, newCnt.Id);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        remapping.Map("Contract", cnt.Id, existing.Id);
                        var action = ResolveAction(sumTrans.CategoryName, "Contract", cnt.ContractNumber);
                        if (action == ConflictResolutionAction.Overwrite || action == ConflictResolutionAction.Replace)
                        {
                            MapContract(existing, cnt, remapping);
                            await _context.SaveChangesAsync(cancellationToken);
                            sumTrans.Overwritten++;
                            result.RecordsOverwritten++;
                        }
                        else
                        {
                            sumTrans.Retained++;
                            result.ExistingRecordsRetained++;
                        }
                    }
                }

                // 34. ContractItems
                var ctis = await ReadTableAsync<ContractItemBackupDto>("ContractItems");
                foreach (var cti in ctis)
                {
                    sumTrans.Processed++;
                    int targetCntId = remapping.Resolve("Contract", cti.ContractId);
                    int? targetPId = remapping.Get("Product", cti.ProductId);
                    bool exists = await _context.ContractItems.AnyAsync(x => x.ContractId == targetCntId && x.ProductCode == cti.ProductCode, cancellationToken);
                    if (!exists)
                    {
                        _context.ContractItems.Add(new ContractItem
                        {
                            ContractId = targetCntId,
                            ProductId = targetPId,
                            ProductCode = cti.ProductCode,
                            ProductName = cti.ProductName,
                            Unit = cti.Unit,
                            Quantity = cti.Quantity,
                            UnitPrice = cti.UnitPrice,
                            DiscountAmount = cti.DiscountAmount,
                            VatRate = cti.VatRate,
                            TaxAmount = cti.TaxAmount,
                            LineTotal = cti.LineTotal,
                            Specifications = cti.Specifications,
                            SortOrder = cti.SortOrder,
                            Note = cti.Note
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumTrans.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 35. DocumentAttachments
                var docAtts = await ReadTableAsync<DocumentAttachmentBackupDto>("DocumentAttachments");
                foreach (var da in docAtts)
                {
                    sumTrans.Processed++;
                    bool exists = await _context.DocumentAttachments.AnyAsync(x => x.StoragePath == da.StoragePath, cancellationToken);
                    if (!exists)
                    {
                        _context.DocumentAttachments.Add(new DocumentAttachment
                        {
                            FileName = da.FileName,
                            StoragePath = da.StoragePath,
                            ContentType = da.ContentType,
                            FileSize = da.FileSize,
                            CategoryId = remapping.Get("DocumentCategory", da.CategoryId),
                            RelatedEntityType = da.RelatedEntityType,
                            RelatedEntityId = da.RelatedEntityId,
                            Description = da.Description,
                            Status = da.Status,
                            CreatedAt = da.CreatedAt,
                            CreatedBy = da.CreatedBy
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumTrans.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // 36. DocumentSignatureAudits
                var sigAuds = await ReadTableAsync<DocumentSignatureAuditBackupDto>("DocumentSignatureAudits");
                foreach (var sa in sigAuds)
                {
                    sumTrans.Processed++;
                    bool exists = await _context.DocumentSignatureAudits.AnyAsync(x => x.DocumentId == sa.DocumentId && x.SignatureValue == sa.SignatureValue, cancellationToken);
                    if (!exists)
                    {
                        _context.DocumentSignatureAudits.Add(new DocumentSignatureAudit
                        {
                            DocumentType = sa.DocumentType,
                            DocumentId = sa.DocumentId,
                            DocumentNumber = sa.DocumentNumber,
                            SignerIdentityId = remapping.Get("SignerIdentity", sa.SignerIdentityId),
                            SignerUserId = remapping.Get("User", sa.SignerUserId) ?? sa.SignerUserId,
                            SignerName = sa.SignerName,
                            SignerPosition = sa.SignerPosition,
                            SignerRole = sa.SignerRole,
                            SigningTime = sa.SigningTime,
                            SignatureStatus = sa.SignatureStatus,
                            DocumentHash = sa.DocumentHash,
                            SignatureValue = sa.SignatureValue,
                            CertificateSubject = sa.CertificateSubject,
                            CertificateSerialNumber = sa.CertificateSerialNumber,
                            ProviderType = sa.ProviderType,
                            IpAddress = sa.IpAddress,
                            UserAgent = sa.UserAgent,
                            VerificationResult = sa.VerificationResult,
                            LastVerifiedAt = sa.LastVerifiedAt,
                            LastVerifiedBy = sa.LastVerifiedBy,
                            ErrorMessage = sa.ErrorMessage
                        });
                        await _context.SaveChangesAsync(cancellationToken);
                        sumTrans.Imported++;
                        result.NewRecordsImported++;
                    }
                    else
                    {
                        sumTrans.Retained++;
                        result.ExistingRecordsRetained++;
                    }
                }

                // =========================================================================
                // TIER 6: AUDIT LOGS
                // =========================================================================
                var sumAudit = GetSummary("Nhật ký kiểm toán");
                var audits = await ReadTableAsync<AuditLogBackupDto>("AuditLogs");
                foreach (var al in audits)
                {
                    sumAudit.Processed++;
                    _context.AuditLogs.Add(new AuditLog(
                        al.EventType,
                        al.PerformedBy,
                        al.TargetEntity,
                        al.TargetId,
                        al.Details,
                        al.IpAddress,
                        al.UserAgent
                    ));
                    sumAudit.Imported++;
                    result.NewRecordsImported++;
                }
                await _context.SaveChangesAsync(cancellationToken);

                // =========================================================================
                // SYNCHRONIZE POSTGRESQL SEQUENCES
                // =========================================================================
                try
                {
                    string seqSyncSql = @"
DO $$
DECLARE
  seq text;
  tbl text;
  tbls text[] := ARRAY[
    'CompanySettings', 'DocumentCategories', 'SystemSequences',
    'ProductCategories', 'UnitOfMeasures', 'Currencies', 'Departments', 'Positions',
    'Warehouses', 'Customers', 'Suppliers', 'Products', 'ProductImages',
    'Employees', 'EmployeeRequests', 'SignerIdentities', 'SignerEnrollmentCodes',
    'PriceLists', 'PriceListItems', 'ContractTemplates',
    'Quotations', 'QuotationItems', 'SalesOrders', 'SalesOrderItems',
    'Invoices', 'InvoiceItems', 'PartnerPayments',
    'Contracts', 'ContractItems', 'DocumentAttachments', 'DocumentSignatureAudits'
  ];
BEGIN
  FOREACH tbl IN ARRAY tbls LOOP
    BEGIN
      seq := pg_get_serial_sequence('""' || tbl || '""', 'Id');
      IF seq IS NOT NULL THEN
        EXECUTE 'SELECT setval(''' || seq || ''', COALESCE((SELECT MAX(""Id"") FROM ""' || tbl || '""), 1))';
      END IF;
    EXCEPTION WHEN OTHERS THEN
      -- Bỏ qua nếu bảng không dùng standard serial sequence
    END;
  END LOOP;
END $$;";
                    await _context.Database.ExecuteSqlRawAsync(seqSyncSql, cancellationToken);
                }
                catch (Exception seqEx)
                {
                    _logger.LogWarning(seqEx, "Không thể đồng bộ PostgreSQL serial sequence: {Message}", seqEx.Message);
                }

                // =========================================================================
                // COMMIT TRANSACTION
                // =========================================================================
                await tx.CommitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Rollback toàn bộ giao dịch do lỗi trong quá trình khôi phục: {Message}", ex.Message);
                throw;
            }
        });

        // =========================================================================
        // RESTORE FILES (OUTSIDE DB TRANSACTION)
        // =========================================================================
        string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        string uploadsDir = Path.Combine(webRoot, "uploads");
        string stagingFilesDir = Path.Combine(stagingDir, "files");

        if (Directory.Exists(stagingFilesDir))
        {
            var files = Directory.GetFiles(stagingFilesDir, "*.*", SearchOption.AllDirectories);
            foreach (var sf in files)
            {
                string rel = Path.GetRelativePath(stagingFilesDir, sf);
                string destFile = Path.Combine(uploadsDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                if (!File.Exists(destFile))
                {
                    File.Copy(sf, destFile, true);
                    result.FilesImported++;
                }
                else
                {
                    var stagingInfo = new FileInfo(sf);
                    var destInfo = new FileInfo(destFile);

                    if (FilesAreEqual(stagingInfo, destInfo))
                    {
                        result.FilesRetained++;
                    }
                    else
                    {
                        if (plan.FileConflictPolicy == ConflictResolutionAction.Overwrite || plan.FileConflictPolicy == ConflictResolutionAction.Replace)
                        {
                            File.Copy(sf, destFile, true);
                            result.FilesImported++;
                        }
                        else
                        {
                            result.FilesRetained++;
                        }
                    }
                }
            }
        }

        // =========================================================================
        // RECORD AUDIT LOG & CLEANUP
        // =========================================================================
        sw.Stop();
        result.DurationMs = sw.ElapsedMilliseconds;
        result.TotalProcessed = result.SummaryByCategory.Values.Sum(s => s.Processed);

        await _auditService.LogAsync(
            AuditEventType.DatabaseBackupRestored,
            performedBy: user.Identity?.Name ?? "Administrator",
            targetEntity: "DatabaseBackup",
            targetId: plan.StagingId,
            details: JsonSerializer.Serialize(new
            {
                StagingId = plan.StagingId,
                DurationMs = result.DurationMs,
                TotalProcessed = result.TotalProcessed,
                NewRecordsImported = result.NewRecordsImported,
                ExistingRecordsRetained = result.ExistingRecordsRetained,
                RecordsOverwritten = result.RecordsOverwritten,
                FilesImported = result.FilesImported,
                Summary = result.SummaryByCategory
            }),
            cancellationToken: cancellationToken);

        _logger.LogInformation("Phục hồi dữ liệu hoàn tất thành công trong {Duration}ms. Nhập mới: {New}, Giữ nguyên: {Retained}, Ghi đè: {Over}",
            result.DurationMs, result.NewRecordsImported, result.ExistingRecordsRetained, result.RecordsOverwritten);

        return result;
    }

    private static void MapCompanySettings(CompanySettings dest, CompanySettingsBackupDto src)
    {
        dest.CompanyName = src.CompanyName;
        dest.TradingName = src.TradingName;
        dest.TaxCode = src.TaxCode;
        dest.Address = src.Address;
        dest.Phone = src.Phone;
        dest.Email = src.Email;
        dest.BankAccount = src.BankAccount;
        dest.BankName = src.BankName;
        dest.BankAccountHolder = src.BankAccountHolder;
        dest.Website = src.Website;
        dest.LogoPath = src.LogoPath;
        dest.SignaturePath = src.SignaturePath;
        dest.OrderQrCodePath = src.OrderQrCodePath;
        dest.DefaultOrderNote = src.DefaultOrderNote;
        dest.DefaultVatNote = src.DefaultVatNote;
        dest.OrderHotline = src.OrderHotline;
        dest.OrderFooterNote1 = src.OrderFooterNote1;
        dest.OrderFooterNote2 = src.OrderFooterNote2;
        dest.ShowLoadingScreen = src.ShowLoadingScreen;
        dest.ShowCompanyNameOnLoading = src.ShowCompanyNameOnLoading;
        dest.SpinnerColor = src.SpinnerColor;
        dest.SpinnerSpeed = src.SpinnerSpeed;
        dest.SpinnerOpacity = src.SpinnerOpacity;
    }

    private static void MapSecuritySettings(CompanySecuritySettings dest, CompanySecuritySettingsBackupDto src)
    {
        dest.RequireSignerPin = src.RequireSignerPin;
        dest.PinMinLength = src.PinMinLength;
        dest.EnrollmentCodeExpirationMinutes = src.EnrollmentCodeExpirationMinutes;
        dest.CertExpirationWarningDays = src.CertExpirationWarningDays;
        dest.AllowAdminSignerEnrollment = src.AllowAdminSignerEnrollment;
        dest.AutoRevokeOnTermination = src.AutoRevokeOnTermination;
        dest.MaxFailedSignAttempts = src.MaxFailedSignAttempts;
        dest.DefaultSigningProvider = src.DefaultSigningProvider;
        dest.XmlDsigCanonicalizationMethod = src.XmlDsigCanonicalizationMethod;
        dest.XmlDsigSignatureMethod = src.XmlDsigSignatureMethod;
        dest.XmlDsigDigestMethod = src.XmlDsigDigestMethod;
    }

    private static void MapPartyASettings(ContractPartyASettings dest, ContractPartyASettingsBackupDto src)
    {
        dest.CompanyName = src.CompanyName;
        dest.TradingName = src.TradingName;
        dest.TaxCode = src.TaxCode;
        dest.BusinessRegistrationNumber = src.BusinessRegistrationNumber;
        dest.RegisteredAddress = src.RegisteredAddress;
        dest.ContactAddress = src.ContactAddress;
        dest.RepresentativeName = src.RepresentativeName;
        dest.RepresentativePosition = src.RepresentativePosition;
        dest.RepresentativeAuthority = src.RepresentativeAuthority;
        dest.Phone = src.Phone;
        dest.Email = src.Email;
        dest.BankName = src.BankName;
        dest.BankAccount = src.BankAccount;
        dest.BankAccountHolder = src.BankAccountHolder;
        dest.BankBranch = src.BankBranch;
        dest.DefaultPaymentInstructions = src.DefaultPaymentInstructions;
        dest.DefaultDeliveryTerms = src.DefaultDeliveryTerms;
        dest.DefaultWarrantyTerms = src.DefaultWarrantyTerms;
    }

    private static void MapContract(Contract dest, ContractBackupDto src, IdRemappingContext remapping)
    {
        dest.ContractNumber = src.ContractNumber;
        dest.Title = src.Title;
        dest.TemplateCode = src.TemplateCode;
        dest.TemplateVersion = src.TemplateVersion;
        dest.Status = src.Status;
        dest.ContractDate = src.ContractDate;
        dest.ContractPlace = src.ContractPlace;
        dest.EffectiveDate = src.EffectiveDate;
        dest.ExpirationDate = src.ExpirationDate;
        dest.PartyBType = src.PartyBType;
        dest.PartnerId = src.PartyBType == ContractPartyBType.Individual || src.PartyBType == ContractPartyBType.Company
            ? remapping.Get("Customer", src.PartnerId)
            : remapping.Get("Supplier", src.PartnerId);
        dest.PartyAName = src.PartyAName;
        dest.PartyATaxCode = src.PartyATaxCode;
        dest.PartyAAddress = src.PartyAAddress;
        dest.PartyAContactAddress = src.PartyAContactAddress;
        dest.PartyARepresentative = src.PartyARepresentative;
        dest.PartyAPosition = src.PartyAPosition;
        dest.PartyAAuthority = src.PartyAAuthority;
        dest.PartyAPhone = src.PartyAPhone;
        dest.PartyAEmail = src.PartyAEmail;
        dest.PartyABankName = src.PartyABankName;
        dest.PartyABankAccount = src.PartyABankAccount;
        dest.PartyABankAccountHolder = src.PartyABankAccountHolder;
        dest.PartyABankBranch = src.PartyABankBranch;
        dest.PartyBName = src.PartyBName;
        dest.PartyBTaxCode = src.PartyBTaxCode;
        dest.PartyBIdCardNumber = src.PartyBIdCardNumber;
        dest.PartyBIdCardDate = src.PartyBIdCardDate;
        dest.PartyBIdCardPlace = src.PartyBIdCardPlace;
        dest.PartyBAddress = src.PartyBAddress;
        dest.PartyBDeliveryAddress = src.PartyBDeliveryAddress;
        dest.PartyBPhone = src.PartyBPhone;
        dest.PartyBEmail = src.PartyBEmail;
        dest.PartyBRepresentative = src.PartyBRepresentative;
        dest.PartyBPosition = src.PartyBPosition;
        dest.PartyBAuthority = src.PartyBAuthority;
        dest.PartyBBankName = src.PartyBBankName;
        dest.PartyBBankAccount = src.PartyBBankAccount;
        dest.SubTotal = src.SubTotal;
        dest.TotalDiscount = src.TotalDiscount;
        dest.TaxRate = src.TaxRate;
        dest.TaxAmount = src.TaxAmount;
        dest.TotalAmount = src.TotalAmount;
        dest.DepositAmount = src.DepositAmount;
        dest.RemainingAmount = src.RemainingAmount;
        dest.CurrencyCode = src.CurrencyCode;
        dest.AmountInWords = src.AmountInWords;
        dest.PaymentTerms = src.PaymentTerms;
        dest.DeliveryTerms = src.DeliveryTerms;
        dest.InstallationTerms = src.InstallationTerms;
        dest.WarrantyTerms = src.WarrantyTerms;
        dest.BreachPenaltyTerms = src.BreachPenaltyTerms;
        dest.DisputeResolutionTerms = src.DisputeResolutionTerms;
        dest.GeneralTerms = src.GeneralTerms;
        dest.CustomClausesJson = src.CustomClausesJson;
        dest.ExecutionMethod = src.ExecutionMethod;
        dest.PartyASignedAt = src.PartyASignedAt;
        dest.PartyASignedBy = src.PartyASignedBy;
        dest.PartyASignatureData = src.PartyASignatureData;
        dest.PartyBSignedAt = src.PartyBSignedAt;
        dest.PartyBSignedBy = src.PartyBSignedBy;
        dest.PartyBSignatureData = src.PartyBSignatureData;
        dest.DocumentHash = src.DocumentHash;
        dest.IsPaperPrinted = src.IsPaperPrinted;
        dest.PrintedAt = src.PrintedAt;
        dest.PrintedBy = src.PrintedBy;
        dest.CreatedByUserId = remapping.Get("User", src.CreatedByUserId) ?? src.CreatedByUserId;
        dest.CreatedByUserName = src.CreatedByUserName;
        dest.FinalizedAt = src.FinalizedAt;
        dest.FinalizedBy = src.FinalizedBy;
        dest.ParentContractId = remapping.Get("Contract", src.ParentContractId);
        dest.Notes = src.Notes;
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
