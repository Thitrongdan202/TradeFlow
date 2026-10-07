using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Constants;
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
                string.Equals(key, "Employee", StringComparison.OrdinalIgnoreCase))
                continue;

            sequenceDefinitions.Add((key, prefix, "{Prefix}{Number:D6}", description, 0));
        }

        // Document sequences with custom formats
        sequenceDefinitions.Add(("Employee", "NV", "{Prefix}{Number:D5}", "Mã nhân viên hệ thống", 6));
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
}