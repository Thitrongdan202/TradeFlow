using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Domain.Entities.Contracts;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Entities.Documents;
using TradeFlow.Domain.Entities.Employees;
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
            await SeedSecuritySettingsAsync();
            await SeedPreProvisionedSignersAsync();
            await SeedEmployeeMasterDataAsync();
            await SeedSampleEmployeesAsync();
            await SeedContractDataAsync();
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

        var existingPerms = await _context.RolePermissions
            .Where(p => p.RoleId == adminRole.Id)
            .Select(p => new { p.Resource, p.Action })
            .ToListAsync();
        var existingSet = new HashSet<(ResourceType, PermissionAction)>(
            existingPerms.Select(p => (p.Resource, p.Action)));

        bool anyAdded = false;
        foreach (ResourceType resource in Enum.GetValues<ResourceType>())
        {
            foreach (PermissionAction action in Enum.GetValues<PermissionAction>())
            {
                if (!existingSet.Contains((resource, action)))
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
                LogoPath = "/uploads/branding/lacasa_logo.png",
                ShowLoadingScreen = true,
                ShowCompanyNameOnLoading = true,
                SpinnerColor = "#10b981",
                SpinnerOpacity = 0.8,
                SpinnerSpeed = "normal",
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
            if (string.IsNullOrEmpty(company.LogoPath))
            {
                company.LogoPath = "/uploads/branding/lacasa_logo.png";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.SpinnerColor))
            {
                company.SpinnerColor = "#10b981";
                modified = true;
            }
            if (string.IsNullOrEmpty(company.SpinnerSpeed))
            {
                company.SpinnerSpeed = "normal";
                modified = true;
            }
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
            if (string.Equals(key, "Quotation", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Employee", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(key, "Contract", StringComparison.OrdinalIgnoreCase))
                continue;

            sequenceDefinitions.Add((key, prefix, "{Prefix}{Number:D6}", description, 0));
        }

        // Document sequences with custom formats
        sequenceDefinitions.Add(("Employee", "NV", "{Prefix}{Number:D5}", "Mã nhân viên hệ thống", 6));
        sequenceDefinitions.Add(("Quotation", "BG-", "{Prefix}{Year}-{Number:D4}", "Mã báo giá hệ thống", 1));
        sequenceDefinitions.Add(("SalesOrder", "SO-", "{Prefix}{Year}-{Number:D4}", "Mã đơn hàng bán", 1));
        sequenceDefinitions.Add(("Invoice", "INV-", "{Prefix}{Year}-{Number:D4}", "Mã hóa đơn nội bộ", 1));
        sequenceDefinitions.Add(("LegalInvoiceNo", "", "{Number:D8}", "Số hóa đơn GTGT / Bán hàng", 1));
        sequenceDefinitions.Add(("Contract", "HD-", "{Prefix}{Year}-{Number:D4}", "Mã hợp đồng thương mại", 1));

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

        // Seed sample suppliers if none exist
        if (!await _context.Suppliers.AnyAsync())
        {
            _logger.LogInformation("Seeding sample suppliers...");
            var sampleSuppliers = new[]
            {
                new Supplier
                {
                    Code = "NCC000001",
                    Name = "Công ty CP Xây Dựng Miền Tây",
                    CompanyName = "Công ty CP Xây Dựng Miền Tây",
                    TaxCode = "1801234567",
                    Phone = "02923888999",
                    Email = "mientay.corp@gmail.com",
                    Address = "Đại lộ Hòa Bình, Q. Ninh Kiều, Cần Thơ",
                    ContactPerson = "Nguyễn Văn Hùng",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new Supplier
                {
                    Code = "NCC000002",
                    Name = "Tổng Công ty Thiết Bị Viglacera",
                    CompanyName = "Tổng Công ty CP Thiết Bị Vệ Sinh Viglacera",
                    TaxCode = "0100112233",
                    Phone = "02438887777",
                    Email = "kinhdoanh@viglacera.com.vn",
                    Address = "Tòa nhà Viglacera, Mễ Trì, Nam Từ Liêm, Hà Nội",
                    ContactPerson = "Trần Thị Mai",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new Supplier
                {
                    Code = "NCC000003",
                    Name = "Công ty TNHH Sen Vòi INAX",
                    CompanyName = "Công ty TNHH LIXIL - INAX Việt Nam",
                    TaxCode = "0300998877",
                    Phone = "02839996666",
                    Email = "lienhe@inax.com.vn",
                    Address = "Khu Công Nghiệp Biên Hòa 2, Đồng Nai",
                    ContactPerson = "Lê Hoàng Quân",
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = "System"
                }
            };
            _context.Suppliers.AddRange(sampleSuppliers);
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

    private async Task SeedSecuritySettingsAsync()
    {
        var settings = await _context.CompanySecuritySettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            _logger.LogInformation("Seeding default company security settings.");
            _context.CompanySecuritySettings.Add(new TradeFlow.Domain.Entities.Security.CompanySecuritySettings
            {
                RequireSignerPin = true,
                PinMinLength = 6,
                EnrollmentCodeExpirationMinutes = 30,
                CertExpirationWarningDays = 30,
                AllowAdminSignerEnrollment = true,
                AutoRevokeOnTermination = true,
                MaxFailedSignAttempts = 5,
                DefaultSigningProvider = TradeFlow.Domain.Enums.SigningProviderType.SoftwareRsa,
                CreatedBy = "System"
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedPreProvisionedSignersAsync()
    {
        _logger.LogInformation("Seeding pre-provisioned senior authorized signers...");
        var now = DateTime.UtcNow;

        var managerUser = await _userManager.FindByNameAsync("quanly01");
        var adminUser = await _userManager.FindByNameAsync("admin");

        const string directorSignatureSvg = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 300 100' width='300' height='100'><path d='M20 70 Q 60 10, 100 60 T 180 40 T 260 70 M50 50 Q 120 80, 200 20 M140 30 L 220 85' fill='none' stroke='%23002f6c' stroke-width='3' stroke-linecap='round'/></svg>";
        const string deputySignatureSvg = "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 300 100' width='300' height='100'><path d='M30 65 Q 80 15, 120 55 T 190 35 T 270 65 M70 45 Q 150 75, 230 25' fill='none' stroke='%231a365d' stroke-width='2.5' stroke-linecap='round'/></svg>";

        // 1. Giám đốc profile
        if (!await _context.SignerIdentities.AnyAsync(s => s.SignerRole == SignerRole.Director))
        {
            _context.SignerIdentities.Add(new TradeFlow.Domain.Entities.Security.SignerIdentity
            {
                UserId = managerUser?.Id ?? "pre_director",
                UserName = managerUser?.UserName ?? "quanly01",
                FullName = "Nguyễn Văn Giám Đốc",
                Position = "Giám đốc",
                SignerRole = SignerRole.Director,
                Status = SignerStatus.Active,
                ProviderType = SigningProviderType.SoftwareRsa,
                HandwrittenSignatureImage = directorSignatureSvg,
                CertificateSerialNumber = null,
                CertificateSubject = null,
                CertificateIssuer = null,
                CertificateThumbprint = null,
                EncryptedPrivateKey = null,
                KeySalt = null,
                PinVerificationHash = null,
                FailedPinAttempts = 0,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        // 2. Phó giám đốc profile
        if (!await _context.SignerIdentities.AnyAsync(s => s.SignerRole == SignerRole.DeputyDirector))
        {
            _context.SignerIdentities.Add(new TradeFlow.Domain.Entities.Security.SignerIdentity
            {
                UserId = adminUser?.Id ?? "pre_deputy",
                UserName = adminUser?.UserName ?? "admin",
                FullName = "Trần Thị Phó Giám Đốc",
                Position = "Phó giám đốc",
                SignerRole = SignerRole.DeputyDirector,
                Status = SignerStatus.Active,
                ProviderType = SigningProviderType.SoftwareRsa,
                HandwrittenSignatureImage = deputySignatureSvg,
                CertificateSerialNumber = null,
                CertificateSubject = null,
                CertificateIssuer = null,
                CertificateThumbprint = null,
                EncryptedPrivateKey = null,
                KeySalt = null,
                PinVerificationHash = null,
                FailedPinAttempts = 0,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedEmployeeMasterDataAsync()
    {
        _logger.LogInformation("Seeding employee departments and positions...");
        var now = DateTime.UtcNow;

        var departments = new (string Code, string Name, string Description)[]
        {
            ("KHO",        "Kho vận",             "Bộ phận quản lý và vận hành kho bãi, xuất nhập hàng"),
            ("KINHDOANH",  "Kinh doanh",          "Bộ phận phát triển kinh doanh, bán hàng và chăm sóc khách hàng"),
            ("KETOAN",     "Kế toán",             "Bộ phận tài chính kế toán, thu chi và công nợ"),
            ("XNK",        "Xuất nhập khẩu",      "Bộ phận điều phối logistics, chứng từ xuất nhập khẩu"),
            ("IT",         "Công nghệ thông tin", "Bộ phận quản trị hệ thống kỹ thuật và phần mềm")
        };

        foreach (var d in departments)
        {
            if (!await _context.Departments.AnyAsync(dept => dept.Code == d.Code))
            {
                _context.Departments.Add(new Department
                {
                    Code = d.Code,
                    Name = d.Name,
                    Description = d.Description,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = "System"
                });
            }
        }

        var positions = new (string Code, string Name, string Description)[]
        {
            ("GIAMDOC",     "Giám đốc",      "Điều hành chung hoạt động công ty"),
            ("TRUONGPHONG", "Trưởng phòng",  "Quản lý điều hành các bộ phận nghiệp vụ"),
            ("TRUONGNHOM",  "Trưởng nhóm",   "Quản lý tổ nhóm chuyên môn"),
            ("NHANVIEN",    "Nhân viên",     "Chuyên viên tác nghiệp thực tế"),
            ("THUCTAP",     "Thực tập sinh", "Học việc và hỗ trợ dự án")
        };

        foreach (var p in positions)
        {
            if (!await _context.Positions.AnyAsync(pos => pos.Code == p.Code))
            {
                _context.Positions.Add(new Position
                {
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description,
                    IsActive = true,
                    CreatedAt = now,
                    CreatedBy = "System"
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedSampleEmployeesAsync()
    {
        _logger.LogInformation("Seeding synthetic sample employees and requests...");
        var now = DateTime.UtcNow;

        var deptKho = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "KHO");
        var deptKd = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "KINHDOANH");
        var deptKt = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "KETOAN");
        var deptXnk = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "XNK");

        var posTp = await _context.Positions.FirstOrDefaultAsync(p => p.Code == "TRUONGPHONG");
        var posTn = await _context.Positions.FirstOrDefaultAsync(p => p.Code == "TRUONGNHOM");
        var posNv = await _context.Positions.FirstOrDefaultAsync(p => p.Code == "NHANVIEN");

        var userKd = await _userManager.FindByNameAsync("kinhdoanh01");
        var userKho = await _userManager.FindByNameAsync("kho01");
        var userXnk = await _userManager.FindByNameAsync("xnk01");

        // 1. NV00001 - Active, linked to user kinhdoanh01
        if (!await _context.Employees.AnyAsync(e => e.Code == "NV00001"))
        {
            _context.Employees.Add(new Employee
            {
                Code = "NV00001",
                AttendanceCode = "CC001",
                FullName = "Nguyễn Văn An",
                NationalId = "001090012345",
                Phone = "0901112233",
                Email = "an.nguyen@tradeflow.local",
                DateOfBirth = new DateTime(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nam",
                Address = "Số 12, Phố Tràng Tiền, Quận Hoàn Kiếm, Hà Nội",
                DepartmentId = deptKd?.Id,
                PositionId = posTp?.Id,
                WorkingBranch = "Chi nhánh trung tâm",
                PayrollBranch = "Chi nhánh trung tâm",
                StartDate = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc),
                Status = EmployeeStatus.Active,
                BankInformation = "1903456789012, Techcombank, NGUYEN VAN AN",
                DebtAndAdvance = 0,
                Notes = "Phụ trách đội kinh doanh dự án B2B",
                UserId = userKd?.Id,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        // 2. NV00002 - Active, no user account
        if (!await _context.Employees.AnyAsync(e => e.Code == "NV00002"))
        {
            _context.Employees.Add(new Employee
            {
                Code = "NV00002",
                AttendanceCode = "CC002",
                FullName = "Trần Thị Mai",
                NationalId = "001192023456",
                Phone = "0912223344",
                Email = "mai.tran@tradeflow.local",
                DateOfBirth = new DateTime(1994, 8, 20, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nữ",
                Address = "Số 45, Đường Lê Duẩn, Quận Hải Châu, TP Đà Nẵng",
                DepartmentId = deptKt?.Id,
                PositionId = posTn?.Id,
                WorkingBranch = "Chi nhánh trung tâm",
                PayrollBranch = "Chi nhánh trung tâm",
                StartDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = EmployeeStatus.Active,
                BankInformation = "0071001234567, Vietcombank, TRAN THI MAI",
                DebtAndAdvance = 0,
                Notes = "Kế toán thanh toán & ngân hàng",
                UserId = null,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        // 3. NV00003 - Active, linked to user kho01
        if (!await _context.Employees.AnyAsync(e => e.Code == "NV00003"))
        {
            _context.Employees.Add(new Employee
            {
                Code = "NV00003",
                AttendanceCode = "CC003",
                FullName = "Lê Hoàng Khoa",
                NationalId = "079088034567",
                Phone = "0983334455",
                Email = "khoa.le@tradeflow.local",
                DateOfBirth = new DateTime(1988, 11, 12, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nam",
                Address = "Khu công nghiệp Tân Bình, Quận Tân Phú, TP Hồ Chí Minh",
                DepartmentId = deptKho?.Id,
                PositionId = posTn?.Id,
                WorkingBranch = "Kho tổng phía Nam",
                PayrollBranch = "Chi nhánh trung tâm",
                StartDate = new DateTime(2023, 8, 15, 0, 0, 0, DateTimeKind.Utc),
                Status = EmployeeStatus.Active,
                BankInformation = "10287654321, VietinBank, LE HOANG KHOA",
                DebtAndAdvance = 0,
                Notes = "Điều phối kho và xuất nhập hàng hóa",
                UserId = userKho?.Id,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        // 4. NV00004 - Active, linked to user xnk01
        if (!await _context.Employees.AnyAsync(e => e.Code == "NV00004"))
        {
            _context.Employees.Add(new Employee
            {
                Code = "NV00004",
                AttendanceCode = "CC004",
                FullName = "Phạm Quốc Dũng",
                NationalId = "001095045678",
                Phone = "0974445566",
                Email = "dung.pham@tradeflow.local",
                DateOfBirth = new DateTime(1995, 2, 28, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nam",
                Address = "Tòa nhà Keangnam, Mễ Trì, Quận Nam Từ Liêm, Hà Nội",
                DepartmentId = deptXnk?.Id,
                PositionId = posNv?.Id,
                WorkingBranch = "Chi nhánh trung tâm",
                PayrollBranch = "Chi nhánh trung tâm",
                StartDate = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                Status = EmployeeStatus.Active,
                BankInformation = "21510001234567, BIDV, PHAM QUOC DUNG",
                DebtAndAdvance = 0,
                Notes = "Phụ trách chứng từ hải quan và vận đơn",
                UserId = userXnk?.Id,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        // 5. NV00005 - Retired / Đã nghỉ
        if (!await _context.Employees.AnyAsync(e => e.Code == "NV00005"))
        {
            _context.Employees.Add(new Employee
            {
                Code = "NV00005",
                AttendanceCode = "CC005",
                FullName = "Vũ Đình Tuấn",
                NationalId = "031093056789",
                Phone = "0965556677",
                Email = "tuan.vu@tradeflow.local",
                DateOfBirth = new DateTime(1993, 7, 4, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nam",
                Address = "Số 88, Đường Láng, Quận Đống Đa, Hà Nội",
                DepartmentId = deptKd?.Id,
                PositionId = posNv?.Id,
                WorkingBranch = "Chi nhánh trung tâm",
                PayrollBranch = "Chi nhánh trung tâm",
                StartDate = new DateTime(2023, 1, 15, 0, 0, 0, DateTimeKind.Utc),
                Status = EmployeeStatus.Retired,
                TerminationDate = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                TerminationReason = "Chuyển công tác theo nguyện vọng cá nhân",
                BankInformation = "1902998877665, Techcombank, VU DINH TUAN",
                DebtAndAdvance = 0,
                Notes = "Đã bàn giao đầy đủ hồ sơ và thiết bị",
                UserId = null,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        // 6. NV00006 - Retired / Đã nghỉ
        if (!await _context.Employees.AnyAsync(e => e.Code == "NV00006"))
        {
            _context.Employees.Add(new Employee
            {
                Code = "NV00006",
                AttendanceCode = "CC006",
                FullName = "Hoàng Ngọc Bích",
                NationalId = "036196067890",
                Phone = "0936667788",
                Email = "bich.hoang@tradeflow.local",
                DateOfBirth = new DateTime(1996, 9, 18, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nữ",
                Address = "Số 25, Phố Huế, Quận Hai Bà Trưng, Hà Nội",
                DepartmentId = deptKt?.Id,
                PositionId = posNv?.Id,
                WorkingBranch = "Chi nhánh trung tâm",
                PayrollBranch = "Chi nhánh trung tâm",
                StartDate = new DateTime(2023, 5, 20, 0, 0, 0, DateTimeKind.Utc),
                Status = EmployeeStatus.Retired,
                TerminationDate = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc),
                TerminationReason = "Hết hạn hợp đồng lao động",
                BankInformation = "0451000987654, Vietcombank, HOANG NGOC BICH",
                DebtAndAdvance = 0,
                Notes = "Đã tất toán công nợ và sổ bảo hiểm",
                UserId = null,
                CreatedAt = now,
                CreatedBy = "System"
            });
        }

        await _context.SaveChangesAsync();

        // Seed Sample Employee Request (Pending)
        if (!await _context.EmployeeRequests.AnyAsync(r => r.FullName == "Đỗ Minh Khang" && r.Status == EmployeeRequestStatus.Pending))
        {
            _context.EmployeeRequests.Add(new EmployeeRequest
            {
                FullName = "Đỗ Minh Khang",
                Code = "NV00007",
                AttendanceCode = "CC007",
                Phone = "0947778899",
                Email = "khang.do@tradeflow.local",
                NationalId = "001099078901",
                DateOfBirth = new DateTime(1998, 4, 12, 0, 0, 0, DateTimeKind.Utc),
                Gender = "Nam",
                Address = "Số 15, Ngõ 91, Đường Nguyễn Chí Thanh, Quận Đống Đa, Hà Nội",
                DepartmentId = deptKho?.Id,
                PositionId = posNv?.Id,
                WorkingBranch = "Chi nhánh trung tâm",
                PayrollBranch = "Chi nhánh trung tâm",
                StartDate = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc),
                BankInformation = "10199887766, VietinBank, DO MINH KHANG",
                Notes = "Tuyển bổ sung nhân viên bốc xếp và kiểm đếm kho",
                Status = EmployeeRequestStatus.Pending,
                RequestedBy = "quanly01",
                CreatedAt = now,
                CreatedBy = "quanly01"
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedContractDataAsync()
    {
        _logger.LogInformation("Seeding contract module settings and templates...");
        var now = DateTime.UtcNow;

        // 1. Bên A Settings mặc định
        if (!await _context.ContractPartyASettings.AnyAsync())
        {
            _context.ContractPartyASettings.Add(new ContractPartyASettings
            {
                CompanyName = "CÔNG TY TNHH THIẾT BỊ VỆ SINH LACASA VIỆT NAM",
                TradingName = "LACASA SANITARY WARE",
                TaxCode = "0316899999",
                BusinessRegistrationNumber = "0316899999",
                RegisteredAddress = "Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ",
                ContactAddress = "Showroom LACASA: Số 123 Đường Nguyễn Văn Cừ, Phường An Khánh, Quận Ninh Kiều, TP. Cần Thơ",
                RepresentativeName = "Nguyễn Văn An",
                RepresentativePosition = "Giám đốc",
                RepresentativeAuthority = "Người đại diện theo pháp luật",
                Phone = "0909 123 456",
                Email = "hopdong@lacasavietnam.vn",
                BankName = "Ngân hàng TMCP Ngoại thương Việt Nam (Vietcombank)",
                BankAccount = "0071000999888",
                BankAccountHolder = "CONG TY TNHH THIET BI VE SINH LACASA VIET NAM",
                BankBranch = "Chi nhánh Cần Thơ",
                DefaultPaymentInstructions = "Thanh toán bằng hình thức chuyển khoản vào tài khoản Công ty hoặc tiền mặt tại văn phòng / showroom LACASA. Nội dung chuyển khoản ghi rõ: [Số Hợp đồng] - [Tên Bên B] thanh toán.",
                DefaultDeliveryTerms = "Giao hàng miễn phí trong phạm vi bán kính 20km tính từ kho trung tâm LACASA. Hàng hóa được giao tận chân công trình hoặc sàn tầng 1 theo thỏa thuận hai bên.",
                DefaultWarrantyTerms = "Bảo hành chính hãng 10 năm đối với phần thân sứ vệ sinh (men sứ chống bám bẩn Nano); 03 năm đối với phụ kiện van xả, nắp êm, sen vòi, bộ trộn nóng lạnh; 01 năm đối với phụ kiện dây cấp và vật tư tiêu hao.",
                CreatedAt = now,
                CreatedBy = "System"
            });
            await _context.SaveChangesAsync();
        }

        // 2. Mẫu hợp đồng chuẩn
        if (!await _context.ContractTemplates.AnyAsync())
        {
            var templates = new List<ContractTemplate>
            {
                new ContractTemplate
                {
                    Code = "B2B_SALE",
                    Title = "Hợp đồng mua bán thiết bị vệ sinh (B2B)",
                    Category = "Mua bán thương mại",
                    Description = "Hợp đồng cung cấp thiết bị vệ sinh cao cấp số lượng lớn cho chủ đầu tư, tổng thầu xây dựng hoặc đại lý ủy quyền.",
                    TypicalUseCase = "Dành cho giao dịch mua bán hàng hóa giữa hai pháp nhân doanh nghiệp, có giá trị đơn hàng lớn.",
                    TargetPartyType = ContractPartyBType.Company,
                    RequiredInfo = "Mã số thuế, Giấy phép ĐKKD, Đại diện pháp luật, Địa chỉ nhận hàng, Danh mục hàng hóa & tiến độ thanh toán",
                    Icon = "building",
                    Version = "1.0",
                    DisplayOrder = 1,
                    IsActive = true,
                    LegalBasis = "Căn cứ Bộ luật Dân sự số 91/2015/QH13; Căn cứ Luật Thương mại số 36/2005/QH11 (Điều 24, Điều 301 về mức phạt vi phạm không quá 8% giá trị nghĩa vụ); Căn cứ Luật Giao dịch điện tử số 20/2023/QH15.",
                    KnownLimitations = "Mức phạt vi phạm nghĩa vụ hợp đồng tối đa 8% theo Điều 301 Luật Thương mại 2005 đối với quan hệ thương mại giữa các thương nhân.",
                    IsReviewedByLegal = true,
                    DefaultClausesJson = JsonSerializer.Serialize(new[]
                    {
                        new { ClauseNumber = "Điều 1", Title = "Đối tượng hợp đồng và Danh mục hàng hóa", Content = "Bên A đồng ý bán và Bên B đồng ý mua các thiết bị vệ sinh, phụ kiện và vật tư cao cấp theo danh mục, chủng loại, số lượng, quy cách và đơn giá chi tiết tại Bảng kê hàng hóa đính kèm Hợp đồng này." },
                        new { ClauseNumber = "Điều 2", Title = "Giá trị hợp đồng và Phương thức thanh toán", Content = "Giá trị hợp đồng đã bao gồm thuế GTGT (VAT), chi phí đóng gói, bảo hiểm vận chuyển đến chân công trình. Thanh toán chia làm 03 đợt: Đợt 1 đặt cọc 30% ngay sau khi ký; Đợt 2 thanh toán 50% trước khi giao hàng; Đợt 3 thanh toán 20% còn lại trong vòng 07 ngày sau khi nghiệm thu." },
                        new { ClauseNumber = "Điều 3", Title = "Thời gian, địa điểm và Quy cách giao nhận", Content = "Bên A giao hàng trong vòng 07-15 ngày làm việc kể từ ngày nhận đủ tiền đợt 1. Địa điểm giao hàng tại công trình hoặc kho của Bên B. Hai bên ký Biên bản giao nhận và phiếu kiểm định chất lượng." },
                        new { ClauseNumber = "Điều 4", Title = "Tiêu chuẩn chất lượng và Xuất xứ hàng hóa", Content = "Hàng hóa do Bên A cung cấp là hàng mới 100%, chính hãng LACASA, đạt tiêu chuẩn chất lượng TCVN và có đầy đủ CO/CQ từ nhà sản xuất." },
                        new { ClauseNumber = "Điều 5", Title = "Trách nhiệm và Bảo hành thiết bị", Content = "Bên A bảo hành 10 năm cho phần men sứ và 03 năm cho linh kiện phụ trợ. Mọi hư hỏng do lỗi sản xuất sẽ được đổi mới miễn phí trong vòng 48 giờ." },
                        new { ClauseNumber = "Điều 6", Title = "Phạt vi phạm và Bồi thường thiệt hại", Content = "Bên nào vi phạm nghĩa vụ hợp đồng sẽ phải chịu phạt vi phạm với mức 8% giá trị phần nghĩa vụ bị vi phạm theo đúng quy định tại Điều 301 Luật Thương mại 2005 và bồi thường thiệt hại thực tế phát sinh." },
                        new { ClauseNumber = "Điều 7", Title = "Giải quyết tranh chấp và Điều khoản thi hành", Content = "Mọi tranh chấp phát sinh sẽ được ưu tiên giải quyết thông qua thương lượng, hòa giải. Trường hợp không hòa giải được sẽ đưa ra Tòa án nhân dân có thẩm quyền tại TP. Cần Thơ để giải quyết." }
                    }),
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new ContractTemplate
                {
                    Code = "B2C_RETAIL",
                    Title = "Hợp đồng bán lẻ thiết bị vệ sinh (B2C)",
                    Category = "Bán lẻ tiêu dùng",
                    Description = "Hợp đồng mua bán thiết bị vệ sinh trọn gói phục vụ khách hàng cá nhân, hộ gia đình, biệt thự, căn hộ chung cư.",
                    TypicalUseCase = "Dành cho khách hàng cá nhân mua sắm thiết bị phòng tắm hoàn thiện.",
                    TargetPartyType = ContractPartyBType.Individual,
                    RequiredInfo = "Họ tên, CCCD/CMND, Số điện thoại, Địa chỉ giao hàng & lắp đặt",
                    Icon = "user",
                    Version = "1.0",
                    DisplayOrder = 2,
                    IsActive = true,
                    LegalBasis = "Căn cứ Bộ luật Dân sự số 91/2015/QH13; Căn cứ Luật Bảo vệ quyền lợi người tiêu dùng số 19/2023/QH15; Căn cứ Luật Giao dịch điện tử số 20/2023/QH15.",
                    KnownLimitations = "Tuân thủ chặt chẽ các điều khoản bảo vệ người tiêu dùng, chính sách đổi trả hàng hóa và bảo hành minh bạch.",
                    IsReviewedByLegal = true,
                    DefaultClausesJson = JsonSerializer.Serialize(new[]
                    {
                        new { ClauseNumber = "Điều 1", Title = "Hàng hóa đặt mua", Content = "Bên A cung cấp cho Bên B các sản phẩm thiết bị vệ sinh LACASA chính hãng theo đơn đặt hàng chi tiết." },
                        new { ClauseNumber = "Điều 2", Title = "Giá cả và Thanh toán", Content = "Bên B đặt cọc 20-30% khi xác nhận đơn và thanh toán số tiền còn lại ngay khi nhận hàng và kiểm tra nguyên vẹn tại nhà." },
                        new { ClauseNumber = "Điều 3", Title = "Giao hàng và Kiểm tra", Content = "Bên A giao hàng tận nơi theo lịch hẹn trước với Bên B. Bên B có trách nhiệm kiểm tra ngoại quan men sứ, linh kiện kèm theo trước khi ký biên bản giao nhận." },
                        new { ClauseNumber = "Điều 4", Title = "Chính sách bảo hành và Đổi trả", Content = "Áp dụng chính sách 1 đổi 1 trong vòng 30 ngày nếu phát hiện lỗi kỹ thuật từ nhà sản xuất. Bảo hành thân sứ 10 năm theo số điện thoại kích hoạt bảo hành điện tử." },
                        new { ClauseNumber = "Điều 5", Title = "Hiệu lực hợp đồng", Content = "Hợp đồng có hiệu lực kể từ ngày ký và chấm dứt khi hai bên hoàn thành nghĩa vụ giao nhận, thanh toán." }
                    }),
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new ContractTemplate
                {
                    Code = "SUPPLY_INSTALL",
                    Title = "Hợp đồng cung cấp và lắp đặt thiết bị vệ sinh",
                    Category = "Cung cấp & Lắp đặt",
                    Description = "Hợp đồng trọn gói từ cung cấp thiết bị đến thi công lắp đặt, đấu nối kỹ thuật, thử nghiệm áp lực nước và bàn giao hoàn thiện.",
                    TypicalUseCase = "Dành cho công trình khách sạn, resort, chung cư, biệt thự yêu cầu kiểm tra kỹ thuật lắp đặt khắt khe.",
                    TargetPartyType = null,
                    RequiredInfo = "Địa điểm công trình, Thông số áp lực nước, Kế hoạch nghiệm thu, An toàn lao động",
                    Icon = "wrench",
                    Version = "1.0",
                    DisplayOrder = 3,
                    IsActive = true,
                    LegalBasis = "Căn cứ Bộ luật Dân sự 2015; Luật Thương mại 2005; Luật Xây dựng số 50/2014/QH13 sửa đổi bổ sung số 62/2020/QH14; Tiêu chuẩn quốc gia TCVN 4513:1988.",
                    KnownLimitations = "Bắt buộc có biên bản nghiệm thu kỹ thuật và thử nghiệm áp lực đường ống nước trước khi bàn giao hoàn công.",
                    IsReviewedByLegal = true,
                    DefaultClausesJson = JsonSerializer.Serialize(new[]
                    {
                        new { ClauseNumber = "Điều 1", Title = "Phạm vi cung cấp và Thi công lắp đặt", Content = "Bên A chịu trách nhiệm cung ứng toàn bộ thiết bị vệ sinh, phụ kiện đấu nối và bố trí đội ngũ kỹ thuật viên có chứng chỉ nghề thực hiện lắp đặt hoàn thiện theo bản vẽ thiết kế đã duyệt." },
                        new { ClauseNumber = "Điều 2", Title = "Tiêu chuẩn kỹ thuật và Nghiệm thu thử áp lực nước", Content = "Công tác lắp đặt phải tuân thủ nghiêm ngặt TCVN 4513:1988. Sau khi lắp đặt hoàn thiện, hai bên tiến hành thử áp lực nước tối thiểu 3-5 bar trong 30 phút, kiểm tra độ kín nước, chống rò rỉ và thoát nước êm trước khi ký Biên bản nghiệm thu kỹ thuật." },
                        new { ClauseNumber = "Điều 3", Title = "An toàn lao động và Vệ sinh môi trường", Content = "Bên A cam kết trang bị đầy đủ bảo hộ lao động cho công nhân, tuân thủ nội quy công trường và dọn dẹp sạch sẽ mặt bằng sau khi hoàn thành công việc." },
                        new { ClauseNumber = "Điều 4", Title = "Giá trị hợp đồng và Phương thức nghiệm thu thanh toán", Content = "Thanh toán theo 3 đợt: 30% tạm ứng, 40% khi tập kết vật tư đến công trường, 30% còn lại sau khi ký Biên bản nghiệm thu bàn giao và bàn giao phiếu bảo hành." },
                        new { ClauseNumber = "Điều 5", Title = "Bảo hành công trình và Thiết bị", Content = "Bảo hành kỹ thuật lắp đặt 24 tháng; bảo hành thiết bị chính hãng theo tiêu chuẩn LACASA (10 năm sứ, 03 năm sen vòi)." }
                    }),
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new ContractTemplate
                {
                    Code = "PRINCIPLE_AGREEMENT",
                    Title = "Hợp đồng nguyên tắc cung ứng hàng hóa",
                    Category = "Khung hợp tác dài hạn",
                    Description = "Hợp đồng khung định kỳ quy định nguyên tắc giao dịch, mức chiết khấu, hạn mức công nợ cho các đơn đặt hàng (PO) phát sinh trong kỳ.",
                    TypicalUseCase = "Ký kết định kỳ 1 năm hoặc nhiều năm với các nhà thầu cơ điện, tổng thầu xây dựng hoặc hệ thống đại lý phân phối.",
                    TargetPartyType = ContractPartyBType.Company,
                    RequiredInfo = "Hạn mức tín dụng, Thời hạn công nợ, Chính sách thưởng doanh số năm, Quy trình gửi nhận Đơn đặt hàng (PO)",
                    Icon = "layers",
                    Version = "1.0",
                    DisplayOrder = 4,
                    IsActive = true,
                    LegalBasis = "Căn cứ Bộ luật Dân sự 2015; Luật Thương mại 2005; Luật Doanh nghiệp 2020.",
                    KnownLimitations = "Mỗi đợt mua bán cụ thể phải phát hành Đơn đặt hàng (PO) hoặc Phụ lục chi tiết xác định chủng loại, số lượng và đơn giá áp dụng.",
                    IsReviewedByLegal = true,
                    DefaultClausesJson = JsonSerializer.Serialize(new[]
                    {
                        new { ClauseNumber = "Điều 1", Title = "Nguyên tắc hợp tác và Đặt hàng", Content = "Hợp đồng này xác định các nguyên tắc chung về việc Bên A cung cấp thiết bị vệ sinh cho Bên B. Từng lần mua bán cụ thể sẽ được thực hiện bằng Đơn đặt hàng (PO) được xác nhận bởi đại diện có thẩm quyền của hai bên." },
                        new { ClauseNumber = "Điều 2", Title = "Chính sách giá và Chiết khấu thương mại", Content = "Đơn giá được áp dụng theo Bảng giá niêm yết hiện hành của Bên A trừ đi tỷ lệ chiết khấu theo phân hạng đối tác được quy định chi tiết trong phụ lục hợp tác." },
                        new { ClauseNumber = "Điều 3", Title = "Hạn mức công nợ và Thời hạn thanh toán", Content = "Bên A cấp cho Bên B hạn mức tín dụng công nợ tối đa là 500.000.000 VNĐ với thời hạn thanh toán không quá 30 ngày kể từ ngày xuất hóa đơn GTGT." },
                        new { ClauseNumber = "Điều 4", Title = "Thời hạn hiệu lực của Hợp đồng nguyên tắc", Content = "Hợp đồng này có hiệu lực 01 năm kể từ ngày ký và tự động gia hạn thêm 01 năm tiếp theo nếu không bên nào có văn bản chấm dứt trước 30 ngày." }
                    }),
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new ContractTemplate
                {
                    Code = "DEPOSIT_AGREEMENT",
                    Title = "Thỏa thuận đặt cọc giữ hàng / sản xuất",
                    Category = "Biện pháp bảo đảm",
                    Description = "Văn bản thỏa thuận đặt cọc để giữ giá, đảm bảo tiến độ giữ lô hàng sứ/sen vòi số lượng giới hạn hoặc sản xuất theo thiết kế riêng.",
                    TypicalUseCase = "Khách hàng muốn giữ mẫu hàng đặc thù hoặc nhập khẩu riêng trước khi ký hợp đồng mua bán chính thức.",
                    TargetPartyType = null,
                    RequiredInfo = "Số tiền đặt cọc, Thời hạn đặt cọc, Cam kết xử lý tiền cọc khi bên nào vi phạm",
                    Icon = "shield",
                    Version = "1.0",
                    DisplayOrder = 5,
                    IsActive = true,
                    LegalBasis = "Căn cứ Điều 328 Bộ luật Dân sự số 91/2015/QH13 về Đặt cọc; Căn cứ Luật Thương mại 2005.",
                    KnownLimitations = "Nếu bên đặt cọc từ chối giao kết hợp đồng thì tài sản đặt cọc thuộc về bên nhận đặt cọc; nếu bên nhận đặt cọc từ chối thì phải trả lại tiền đặt cọc và bồi thường một khoản tương đương giá trị đặt cọc (Điều 328 BLDS 2015).",
                    IsReviewedByLegal = true,
                    DefaultClausesJson = JsonSerializer.Serialize(new[]
                    {
                        new { ClauseNumber = "Điều 1", Title = "Mục đích đặt cọc", Content = "Bên B tự nguyện đặt cọc cho Bên A một khoản tiền để đảm bảo quyền ưu tiên mua và giữ giá lô thiết bị vệ sinh LACASA theo danh mục đính kèm trong thời gian chuẩn bị ký kết Hợp đồng mua bán chính thức." },
                        new { ClauseNumber = "Điều 2", Title = "Số tiền đặt cọc và Phương thức giao nhận", Content = "Số tiền đặt cọc được xác định tương ứng 10% - 20% tổng giá trị dự kiến của lô hàng, được chuyển khoản trực tiếp vào tài khoản ngân hàng của Bên A." },
                        new { ClauseNumber = "Điều 3", Title = "Thời hạn đặt cọc và Ký hợp đồng chính thức", Content = "Thời hạn đặt cọc có hiệu lực trong vòng 30 ngày kể từ ngày Bên A nhận được tiền cọc. Hai bên cam kết ký kết Hợp đồng mua bán chính thức trước khi hết hạn đặt cọc." },
                        new { ClauseNumber = "Điều 4", Title = "Xử lý tiền đặt cọc (Theo Điều 328 Bộ luật Dân sự)", Content = "Khi Hợp đồng chính thức được giao kết, tiền đặt cọc sẽ được cấn trừ vào đợt thanh toán đầu tiên. Nếu Bên B từ chối ký hợp đồng thì mất tiền đặt cọc; nếu Bên A từ chối bán hoặc không có hàng cung cấp theo thỏa thuận thì phải trả lại tiền đặt cọc và nộp phạt một khoản tiền tương đương giá trị tiền cọc cho Bên B." }
                    }),
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new ContractTemplate
                {
                    Code = "CONTRACT_LIQUIDATION",
                    Title = "Biên bản thanh lý hợp đồng",
                    Category = "Kết thúc hợp đồng",
                    Description = "Biên bản xác nhận hai bên đã hoàn thành đầy đủ các quyền và nghĩa vụ giao nhận, nghiệm thu, thanh toán công nợ và chấm dứt hiệu lực hợp đồng.",
                    TypicalUseCase = "Lập khi hoàn tất toàn bộ công việc, quyết toán tài chính và giải phóng các nghĩa vụ liên quan.",
                    TargetPartyType = null,
                    RequiredInfo = "Số hợp đồng gốc, Giá trị thực tế đã thực hiện, Số tiền đã thanh toán, Số tiền còn lại phải thanh toán dứt điểm",
                    Icon = "check-circle",
                    Version = "1.0",
                    DisplayOrder = 6,
                    IsActive = true,
                    LegalBasis = "Căn cứ Điều 422, Điều 424 Bộ luật Dân sự số 91/2015/QH13 về Chấm dứt hợp đồng; Căn cứ các Biên bản giao nhận và Hồ sơ thanh quyết toán công nợ.",
                    KnownLimitations = "Cần đối chiếu sổ phụ công nợ và biên bản giao nhận thực tế trước khi ký kết biên bản thanh lý.",
                    IsReviewedByLegal = true,
                    DefaultClausesJson = JsonSerializer.Serialize(new[]
                    {
                        new { ClauseNumber = "Điều 1", Title = "Căn cứ và Phạm vi thanh lý", Content = "Hai bên cùng xác nhận đã thực hiện đầy đủ nghĩa vụ cung cấp, lắp đặt và nghiệm thu toàn bộ khối lượng thiết bị vệ sinh theo Hợp đồng gốc đã ký kết." },
                        new { ClauseNumber = "Điều 2", Title = "Quyết toán giá trị hợp đồng và Công nợ", Content = "Tổng giá trị hợp đồng thực tế sau quyết toán là [Số tiền]. Bên B đã thanh toán đầy đủ 100% giá trị hợp đồng cho Bên A. Hai bên xác nhận không còn bất kỳ tranh chấp công nợ nào." },
                        new { ClauseNumber = "Điều 3", Title = "Nghĩa vụ bảo hành sau thanh lý", Content = "Việc thanh lý hợp đồng không làm chấm dứt trách nhiệm bảo hành thiết bị của Bên A theo đúng thời hạn và chính sách bảo hành đã cam kết." },
                        new { ClauseNumber = "Điều 4", Title = "Chấm dứt hiệu lực hợp đồng", Content = "Kể từ ngày ký Biên bản này, Hợp đồng gốc chính thức hết hiệu lực thi hành, hai bên không còn bất kỳ khiếu nại hay ràng buộc pháp lý nào khác đối với nhau." }
                    }),
                    CreatedAt = now,
                    CreatedBy = "System"
                },
                new ContractTemplate
                {
                    Code = "CONTRACT_APPENDIX",
                    Title = "Phụ lục hợp đồng (Bổ sung/Điều chỉnh)",
                    Category = "Bổ sung & Điều chỉnh",
                    Description = "Phụ lục sửa đổi, bổ sung khối lượng thiết bị, điều chỉnh đơn giá, gia hạn tiến độ giao nhận hoặc cập nhật các điều khoản của hợp đồng gốc.",
                    TypicalUseCase = "Công trình phát sinh hạng mục thiết bị vệ sinh bổ sung hoặc thay đổi thiết kế phòng tắm.",
                    TargetPartyType = null,
                    RequiredInfo = "Số hợp đồng gốc, Nội dung các điều khoản sửa đổi/bổ sung, Giá trị hợp đồng điều chỉnh",
                    Icon = "paperclip",
                    Version = "1.0",
                    DisplayOrder = 7,
                    IsActive = true,
                    LegalBasis = "Căn cứ Điều 403 Bộ luật Dân sự số 91/2015/QH13 về Phụ lục hợp đồng; Phụ lục hợp đồng là bộ phận không thể tách rời của hợp đồng chính.",
                    KnownLimitations = "Nội dung phụ lục không được trái với nội dung của hợp đồng chính trừ khi có thỏa thuận thay thế rõ ràng.",
                    IsReviewedByLegal = true,
                    DefaultClausesJson = JsonSerializer.Serialize(new[]
                    {
                        new { ClauseNumber = "Điều 1", Title = "Căn cứ lập phụ lục hợp đồng", Content = "Phụ lục này được lập căn cứ theo Hợp đồng gốc đã ký kết giữa hai bên và nhu cầu thực tế về việc bổ sung/điều chỉnh chủng loại, số lượng thiết bị vệ sinh của Bên B." },
                        new { ClauseNumber = "Điều 2", Title = "Nội dung bổ sung và Điều chỉnh", Content = "Bổ sung danh mục thiết bị và điều chỉnh khối lượng theo Bảng chi tiết đính kèm. Tiến độ giao hàng đối với phần khối lượng bổ sung là trong vòng 10 ngày kể từ ngày ký phụ lục này." },
                        new { ClauseNumber = "Điều 3", Title = "Giá trị phụ lục và Tiến độ thanh toán", Content = "Giá trị phát sinh bổ sung theo Phụ lục này là [Số tiền]. Bên B tiến hành thanh toán tương ứng theo tiến độ giao nhận phần hàng hóa bổ sung." },
                        new { ClauseNumber = "Điều 4", Title = "Hiệu lực của Phụ lục", Content = "Phụ lục này là một bộ phận cấu thành không thể tách rời của Hợp đồng gốc. Các điều khoản khác không được đề cập trong Phụ lục này vẫn giữ nguyên giá trị pháp lý." }
                    }),
                    CreatedAt = now,
                    CreatedBy = "System"
                }
            };

            _context.ContractTemplates.AddRange(templates);
            await _context.SaveChangesAsync();
        }
    }
}