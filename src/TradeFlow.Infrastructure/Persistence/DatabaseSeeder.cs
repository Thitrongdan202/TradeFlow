using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ILogger<DatabaseSeeder> _logger;
    private readonly TradeFlowDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

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

            await SeedRolesAndPermissionsAsync();
            await SeedAdministratorAsync();
            await SeedCompanySettingsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedRolesAndPermissionsAsync()
    {
        const string adminRoleName = "Administrator";

        if (!await _roleManager.RoleExistsAsync(adminRoleName))
        {
            _logger.LogInformation("Creating Administrator role.");
            var adminRole = new ApplicationRole(adminRoleName)
            {
                Description = "Quản trị viên hệ thống (có toàn quyền)",
                IsSystem = true, // Cannot be deleted
                CreatedBy = "System"
            };
            await _roleManager.CreateAsync(adminRole);
        }

        var role = await _roleManager.FindByNameAsync(adminRoleName);
        if (role != null)
        {
            // Seed ALL permissions for the Administrator role
            bool permissionsAdded = false;
            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                foreach (PermissionAction action in Enum.GetValues(typeof(PermissionAction)))
                {
                    var exists = await _context.RolePermissions
                        .AnyAsync(p => p.RoleId == role.Id && p.Resource == resource && p.Action == action);

                    if (!exists)
                    {
                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            Resource = resource,
                            Action = action,
                            IsGranted = true,
                            GrantedBy = "System"
                        });
                        permissionsAdded = true;
                    }
                }
            }

            if (permissionsAdded)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Administrator role permissions updated.");
            }
        }
    }

    private async Task SeedAdministratorAsync()
    {
        const string adminEmail = "admin@tradeflow.local";

        if (await _userManager.FindByEmailAsync(adminEmail) == null)
        {
            _logger.LogInformation("Creating default Administrator user.");
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Quản trị viên",
                EmailConfirmed = true,
                Status = UserStatus.Active,
                CreatedBy = "System"
            };

            var result = await _userManager.CreateAsync(adminUser, "TradeFlow@2026");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(adminUser, "Administrator");
            }
            else
            {
                _logger.LogError("Failed to create default administrator: {Errors}", 
                    string.Join(", ", result.Errors.Select(e => e.Description)));
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
}
