using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Constants;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Entities.Users;
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
        if (!await _context.CompanySettings.AnyAsync())
        {
            _logger.LogInformation("Seeding default company settings.");
            _context.CompanySettings.Add(new CompanySettings("Công ty TNHH TradeFlow")
            {
                TaxCode = "0123456789",
                Address = "Hà Nội, Việt Nam",
                Phone = "024 1234 5678",
                Email = "contact@tradeflow.local",
                Website = "https://tradeflow.local"
            });
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedSystemSequencesAsync()
    {
        _logger.LogInformation("Seeding system sequences...");
        var now = DateTime.UtcNow;

        foreach (var (key, (prefix, description)) in SystemCodeConstants.Defaults)
        {
            if (!await _context.SystemSequences.AnyAsync(s => s.SequenceKey == key))
            {
                _context.SystemSequences.Add(new SystemSequence(key, prefix, "{Prefix}{Number:D6}", description)
                {
                    CurrentNumber = 0,
                    Step = 1,
                    CreatedAt = now,
                    CreatedBy = "System"
                });
            }
        }

        await _context.SaveChangesAsync();
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
}