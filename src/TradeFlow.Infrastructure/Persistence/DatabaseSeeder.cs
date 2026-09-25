using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Persistence;

/// <summary>
/// Seeds the development database with default roles, permissions, sequences, and test accounts.
/// Safe to run multiple times - idempotent by design.
/// </summary>
public class DatabaseSeeder
{
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly TradeFlowDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    private const string DevPassword = "tradecore123";

    private static readonly (string Username, string Email, string FullName, string RoleName)[] DevAccounts =
    [
        ("admin",        "admin@tradeflow.local",        "Quản trị viên",      "Administrator"),
        ("quanly01",     "quanly01@tradeflow.local",     "Quản Lý 01",         "Manager"),
        ("kinhdoanh01",  "kinhdoanh01@tradeflow.local",  "Kinh Doanh 01",      "Sales"),
        ("muahang01",    "muahang01@tradeflow.local",    "Mua Hàng 01",        "Purchase"),
        ("kho01",        "kho01@tradeflow.local",        "Kho 01",             "Warehouse"),
        ("xnk01",        "xnk01@tradeflow.local",        "Xuất Nhập Khẩu 01",  "Import-Export"),
    ];

    private static readonly (string Name, string Description, bool IsSystem)[] RoleDefinitions =
    [
        ("Administrator", "Quản trị viên hệ thống (toàn quyền)", true),
        ("Manager",       "Quản lý (xem và phê duyệt)",          false),
        ("Sales",         "Nhân viên kinh doanh",                 false),
        ("Purchase",      "Nhân viên mua hàng",                   false),
        ("Warehouse",     "Nhân viên kho",                        false),
        ("Import-Export", "Nhân viên xuất nhập khẩu",             false),
    ];

    public DatabaseSeeder(
        ILogger<DatabaseSeeder> logger,
        TradeFlowDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task SeedAsync()
    {
        try
        {
            if (_context.Database.IsNpgsql())
            {
                await _context.Database.MigrateAsync();
            }

            await CleanupLegacyAccountsAsync();
            await SeedRolesAsync();
            await SeedAdminPermissionsAsync();
            await SeedDevAccountsAsync();
            await SeedCompanySettingsAsync();
            await SeedSystemSequencesAsync();
            await SeedReferenceDataAsync();
            await SeedDocumentCategoriesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task CleanupLegacyAccountsAsync()
    {
        var legacy = await _userManager.FindByNameAsync("admin@tradeflow.local");
        if (legacy != null)
        {
            _logger.LogInformation("Removing legacy account admin@tradeflow.local (superseded by 'admin').");
            await _userManager.DeleteAsync(legacy);
        }
    }

    private async Task SeedRolesAsync()
    {
        foreach (var (name, description, isSystem) in RoleDefinitions)
        {
            if (!await _roleManager.RoleExistsAsync(name))
            {
                _logger.LogInformation("Creating role: {RoleName}", name);
                var role = new ApplicationRole(name)
                {
                    Description = description,
                    IsSystem = isSystem,
                    IsActive = true,
                    CreatedBy = "System"
                };
                var result = await _roleManager.CreateAsync(role);
                if (!result.Succeeded)
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}",
                        name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
        }
    }

    private async Task SeedAdminPermissionsAsync()
    {
        var adminRole = await _roleManager.FindByNameAsync("Administrator");
        if (adminRole == null)
        {
            _logger.LogWarning("Administrator role not found - skipping permission seed.");
            return;
        }

        bool anyAdded = false;
        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            foreach (PermissionAction action in Enum.GetValues<PermissionAction>())
            {
                var exists = await _context.RolePermissions
                    .AnyAsync(p => p.RoleId == adminRole.Id
                               && p.Resource == resource
                               && p.Action == action);

                if (!exists)
                {
                    _context.RolePermissions.Add(new RolePermission
                    {
                        RoleId = adminRole.Id,
                        Resource = resource,
                        Action = action,
                        IsGranted = true,
                        GrantedBy = "System"
                    });
                    anyAdded = true;
                }
            }
        }

        if (anyAdded)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Administrator role permissions seeded/updated.");
        }
    }

    private async Task SeedDevAccountsAsync()
    {
        foreach (var (username, email, fullName, roleName) in DevAccounts)
        {
            await EnsureDevUserAsync(username, email, fullName, roleName);
        }
    }

    private async Task EnsureDevUserAsync(
        string username, string email, string fullName, string roleName)
    {
        var existing = await _userManager.FindByNameAsync(username);

        if (existing == null)
        {
            _logger.LogInformation("Creating dev account: {Username}", username);

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true,
                LockoutEnabled = false,
                Status = UserStatus.Active,
                CreatedBy = "System"
            };

            var createResult = await _userManager.CreateAsync(user, DevPassword);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create dev account {Username}: {Errors}",
                    username,
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            existing = user;
        }
        else
        {
            _logger.LogDebug("Dev account already exists: {Username}", username);

            bool changed = false;

            if (existing.Status != UserStatus.Active)
            {
                existing.Status = UserStatus.Active;
                changed = true;
            }

            if (existing.LockoutEnd.HasValue)
            {
                await _userManager.SetLockoutEndDateAsync(existing, null);
                changed = true;
            }

            if (!await _userManager.CheckPasswordAsync(existing, DevPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(existing);
                await _userManager.ResetPasswordAsync(existing, token, DevPassword);
                _logger.LogInformation("Reset password to default for dev account: {Username}", username);
            }

            if (changed)
            {
                await _userManager.UpdateAsync(existing);
                _logger.LogInformation("Reset status/lockout for dev account: {Username}", username);
            }
        }

        var currentRoles = await _userManager.GetRolesAsync(existing);
        if (!currentRoles.Contains(roleName))
        {
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(existing, currentRoles);
            }

            if (await _roleManager.RoleExistsAsync(roleName))
            {
                await _userManager.AddToRoleAsync(existing, roleName);
                _logger.LogInformation("Assigned role {Role} to {Username}", roleName, username);
            }
            else
            {
                _logger.LogWarning("Role {Role} does not exist - cannot assign to {Username}", roleName, username);
            }
        }
    }

    private async Task SeedCompanySettingsAsync()
    {
        var company = await _context.CompanySettings.FirstOrDefaultAsync();
        if (company == null)
        {
            _logger.LogInformation("Seeding default company settings.");
            _context.CompanySettings.Add(new CompanySettings("TỔNG KHO THIẾT BỊ VỆ SINH LACASA")
            {
                TaxCode = "0123456789",
                Address = "Hà Nội, Việt Nam",
                Phone = "0369.074.789",
                Email = "tongkhothietbivesinh@gmail.com",
                Website = "https://tradeflow.local",
                BankAccountHolder = "TRẦN VĂN TUẤN",
                BankAccount = "4987.9177",
                BankName = "NGÂN HÀNG Á CHÂU (ACB)",
                OrderQrCodePath = "/images/lacasa_qr.png",
                DefaultVatNote = "Đơn giá trên chưa bao gồm thuế GTGT (8%).",
                OrderHotline = "0369.074.789 - Hotline",
                OrderFooterNote1 = "Quý khách kiểm tra hàng hóa đúng số lượng trên hóa đơn và kiểm hàng trước khi rời khỏi kho Lacasa, bể vỡ Lacasa không chịu trách nhiệm.",
                OrderFooterNote2 = "Hàng hóa mua không nhận trả hàng ngoại trừ hàng bị lỗi do nhà sản xuất, đổi trả trong vòng 10 ngày kể từ ngày xuất kho."
            });
            await _context.SaveChangesAsync();
        }
        else
        {
            bool modified = false;
            if (string.IsNullOrEmpty(company.BankAccountHolder))
            {
                company.BankAccountHolder = "TRẦN VĂN TUẤN";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.BankAccount))
            {
                company.BankAccount = "4987.9177";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.BankName))
            {
                company.BankName = "NGÂN HÀNG Á CHÂU (ACB)";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.OrderQrCodePath))
            {
                company.OrderQrCodePath = "/images/lacasa_qr.png";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.DefaultVatNote))
            {
                company.DefaultVatNote = "Đơn giá trên chưa bao gồm thuế GTGT (8%).";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.OrderHotline))
            {
                company.OrderHotline = "0369.074.789 - Hotline";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.OrderFooterNote1))
            {
                company.OrderFooterNote1 = "Quý khách kiểm tra hàng hóa đúng số lượng trên hóa đơn và kiểm hàng trước khi rời khỏi kho Lacasa, bể vỡ Lacasa không chịu trách nhiệm.";
                modified = true;
            }
            else if (company.OrderFooterNote1.Contains("kẻ vỡ"))
            {
                company.OrderFooterNote1 = company.OrderFooterNote1.Replace("kẻ vỡ", "bể vỡ");
                modified = true;
            }
            if (string.IsNullOrEmpty(company.OrderFooterNote2))
            {
                company.OrderFooterNote2 = "Hàng hóa mua không nhận trả hàng ngoại trừ hàng bị lỗi do nhà sản xuất, đổi trả trong vòng 10 ngày kể từ ngày xuất kho.";
                modified = true;
            }

            if (modified)
            {
                await _context.SaveChangesAsync();
            }
        }
    }

    private async Task SeedSystemSequencesAsync()
    {
        _logger.LogInformation("Seeding system sequences...");
        var now = DateTime.UtcNow;

        var existingSequences = await _context.SystemSequences.ToListAsync();
        var existingMap = existingSequences.ToDictionary(s => s.SequenceKey, StringComparer.OrdinalIgnoreCase);

        var sequenceDefinitions = new List<(string Key, string Prefix, string FormatPattern, string Description, long DefaultCurrentNumber)>();

        foreach (var (key, (prefix, description)) in SystemCodeConstants.Defaults)
        {
            if (string.Equals(key, "Quotation", StringComparison.OrdinalIgnoreCase))
                continue;

            sequenceDefinitions.Add((key, prefix, "{Prefix}{Number:D6}", description, 0));
        }

        // Document sequences with custom formats
        sequenceDefinitions.Add(("Quotation", "BG-", "{Prefix}{Year}-{Number:D4}", "Mã báo giá hệ thống", 1));
        sequenceDefinitions.Add(("SalesOrder", "SO-", "{Prefix}{Year}-{Number:D4}", "Mã đơn hàng bán", 1));
        sequenceDefinitions.Add(("Invoice", "INV-", "{Prefix}{Year}-{Number:D4}", "Mã hóa đơn nội bộ", 1));
        sequenceDefinitions.Add(("LegalInvoiceNo", "", "{Number:D8}", "Số hóa đơn GTGT / Bán hàng", 1));

        bool hasChanges = false;

        foreach (var def in sequenceDefinitions)
        {
            if (existingMap.TryGetValue(def.Key, out var existing))
            {
                // Sequence already exists: preserve current counter and do NOT insert a duplicate row.
                // If Quotation previously had the master 6-digit pattern ("BG" / "{Prefix}{Number:D6}"), normalize it.
                if (string.Equals(def.Key, "Quotation", StringComparison.OrdinalIgnoreCase))
                {
                    if (existing.Prefix == "BG" && existing.FormatPattern == "{Prefix}{Number:D6}")
                    {
                        _logger.LogInformation("Normalizing Quotation sequence format pattern to {Pattern}", def.FormatPattern);
                        existing.Prefix = def.Prefix;
                        existing.FormatPattern = def.FormatPattern;
                        existing.Description ??= def.Description;
                        if (existing.CurrentNumber == 0)
                        {
                            existing.CurrentNumber = 1;
                        }
                        hasChanges = true;
                    }
                }
            }
            else if (!_context.SystemSequences.Local.Any(s => string.Equals(s.SequenceKey, def.Key, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogInformation("Adding system sequence: {SequenceKey} (Prefix: {Prefix})", def.Key, def.Prefix);
                var newSeq = new SystemSequence(def.Key, def.Prefix, def.FormatPattern, def.Description)
                {
                    CurrentNumber = def.DefaultCurrentNumber,
                    Step = 1,
                    CreatedAt = now,
                    CreatedBy = "System"
                };

                _context.SystemSequences.Add(newSeq);
                existingMap[def.Key] = newSeq;
                hasChanges = true;
            }
        }

        if (hasChanges)
        {
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedReferenceDataAsync()
    {
        _logger.LogInformation("Seeding reference data...");
        var now = DateTime.UtcNow;

        if (!await _context.Currencies.AnyAsync(c => c.Code == "VND"))
        {
            _context.Currencies.Add(new Currency { Code = "VND", Name = "Việt Nam đồng", Symbol = "₫", ExchangeRate = 1, IsDefault = true, IsActive = true, CreatedAt = now, CreatedBy = "System" });
        }
        if (!await _context.Currencies.AnyAsync(c => c.Code == "USD"))
        {
            _context.Currencies.Add(new Currency { Code = "USD", Name = "Đô la Mỹ", Symbol = "$", ExchangeRate = 25000, IsDefault = false, IsActive = true, CreatedAt = now, CreatedBy = "System" });
        }

        var units = new[]
        {
            new UnitOfMeasure { Code = "CAI", Name = "Cái", Symbol = "cái", IsActive = true, CreatedAt = now, CreatedBy = "System" },
            new UnitOfMeasure { Code = "BO", Name = "Bộ", Symbol = "bộ", IsActive = true, CreatedAt = now, CreatedBy = "System" },
            new UnitOfMeasure { Code = "CHIEC", Name = "Chiếc", Symbol = "chiếc", IsActive = true, CreatedAt = now, CreatedBy = "System" },
            new UnitOfMeasure { Code = "THUNG", Name = "Thùng", Symbol = "thùng", IsActive = true, CreatedAt = now, CreatedBy = "System" },
            new UnitOfMeasure { Code = "KG", Name = "Kilogram", Symbol = "kg", IsActive = true, CreatedAt = now, CreatedBy = "System" },
            new UnitOfMeasure { Code = "M", Name = "Mét", Symbol = "m", IsActive = true, CreatedAt = now, CreatedBy = "System" }
        };

        foreach (var unit in units)
        {
            if (!await _context.UnitOfMeasures.AnyAsync(u => u.Code == unit.Code))
            {
                _context.UnitOfMeasures.Add(unit);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedDocumentCategoriesAsync()
    {
        _logger.LogInformation("Seeding document categories...");
        var now = DateTime.UtcNow;

        var defaultCategories = new (string Code, string Name, DocumentTypeCategory Group, string Description, int Order)[]
        {
            ("BAO_GIA",      "Báo giá",              DocumentTypeCategory.Commercial, "Bảng báo giá thương mại gửi khách hàng", 1),
            ("DON_DAT_HANG", "Đơn đặt hàng",         DocumentTypeCategory.Commercial, "Đơn đặt hàng / Đơn bán hàng", 2),
            ("HOP_DONG",     "Hợp đồng kinh tế",     DocumentTypeCategory.Accounting, "Hợp đồng kinh tế, phụ lục hợp đồng", 3),
            ("HOA_DON",      "Hóa đơn",              DocumentTypeCategory.Accounting, "Hóa đơn điện tử, hóa đơn GTGT / bán hàng", 4),
            ("PHIEU_GIAO",   "Phiếu giao hàng",      DocumentTypeCategory.Internal,   "Phiếu giao nhận hàng hóa, biên bản bàn giao", 5),
            ("PHIEU_XUAT",   "Phiếu xuất kho",       DocumentTypeCategory.Internal,   "Chứng từ xuất kho hàng hóa", 6),
            ("PHIEU_NHAP",   "Phiếu nhập kho",       DocumentTypeCategory.Internal,   "Chứng từ nhập kho hàng hóa", 7),
            ("BIEN_BAN",     "Biên bản nghiệm thu",  DocumentTypeCategory.Internal,   "Biên bản bàn giao, kiểm định, nghiệm thu", 8),
            ("CHUNG_TU",     "Chứng từ kế toán",     DocumentTypeCategory.Accounting, "Chứng từ thu chi, ủy nhiệm chi, đối chiếu công nợ", 9),
            ("HO_SO",        "Hồ sơ pháp lý / CO-CQ",DocumentTypeCategory.Attachment, "Chứng chỉ xuất xứ, chất lượng, hồ sơ năng lực", 10),
            ("KHAC",         "Tài liệu khác",        DocumentTypeCategory.Attachment, "Các tài liệu, hình ảnh hoặc đính kèm khác", 99)
        };

        foreach (var cat in defaultCategories)
        {
            if (!await _context.DocumentCategories.AnyAsync(c => c.Code == cat.Code))
            {
                _context.DocumentCategories.Add(new DocumentCategory
                {
                    Code = cat.Code,
                    Name = cat.Name,
                    Group = cat.Group,
                    Description = cat.Description,
                    DisplayOrder = cat.Order,
                    IsActive = true,
                    IsSystem = true,
                    CreatedAt = now,
                    CreatedBy = "System"
                });
            }
        }

        await _context.SaveChangesAsync();
    }
}