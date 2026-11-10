import os

path = r'src/TradeFlow.Infrastructure/Persistence/DatabaseSeeder.cs'
with open(path, 'r', encoding='utf-8') as f:
    c = f.read()

# 1. Add using if needed, inject ICredentialVaultService
target_ctor = """    private readonly RoleManager<ApplicationRole> _roleManager;

    private const string DevPassword = "tradecore123";"""

repl_ctor = """    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly TradeFlow.Application.Common.Interfaces.ICredentialVaultService _vaultService;

    private const string DevPassword = "tradecore123";"""

target_init = """    public DatabaseSeeder(
        ILogger<DatabaseSeeder> logger,
        TradeFlowDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }"""

repl_init = """    public DatabaseSeeder(
        ILogger<DatabaseSeeder> logger,
        TradeFlowDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        TradeFlow.Application.Common.Interfaces.ICredentialVaultService vaultService)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _vaultService = vaultService;
    }"""

target_new_user = """                Status = UserStatus.Active,
                CreatedBy = "System"
            };"""

repl_new_user = """                Status = UserStatus.Active,
                CreatedBy = "System",
                EncryptedPassword = _vaultService.EncryptPassword(DevPassword),
                PasswordChangedAt = DateTime.UtcNow
            };"""

target_existing = """            if (!await _userManager.CheckPasswordAsync(existing, DevPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(existing);
                await _userManager.ResetPasswordAsync(existing, token, DevPassword);
                _logger.LogInformation("Reset password to default for dev account: {Username}", username);
            }

            if (changed)"""

repl_existing = """            if (string.IsNullOrEmpty(existing.EncryptedPassword))
            {
                existing.EncryptedPassword = _vaultService.EncryptPassword(DevPassword);
                existing.PasswordChangedAt ??= DateTime.UtcNow;
                changed = true;
            }

            if (changed)"""

c = c.replace(target_ctor, repl_ctor, 1).replace(target_init, repl_init, 1).replace(target_new_user, repl_new_user, 1).replace(target_existing, repl_existing, 1)

with open(path, 'w', encoding='utf-8') as f:
    f.write(c)

print('SUCCESS')
