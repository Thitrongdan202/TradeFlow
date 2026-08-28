using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace TradeFlow.Infrastructure.Persistence;

public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    private readonly TradeFlowDbContext _context;

    public CustomUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor,
        TradeFlowDbContext context)
        : base(userManager, roleManager, optionsAccessor)
    {
        _context = context;
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        // Include FullName
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            identity.AddClaim(new Claim("FullName", user.FullName));
        }

        // Include Status
        identity.AddClaim(new Claim("Status", user.Status.ToString()));

        // Add granular permissions from all roles the user belongs to
        var roles = await UserManager.GetRolesAsync(user);
        if (roles.Any())
        {
            var roleIds = await _context.Roles
                .Where(r => roles.Contains(r.Name!))
                .Select(r => r.Id)
                .ToListAsync();

            var permissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsGranted)
                .Select(rp => new { rp.Resource, rp.Action })
                .Distinct()
                .ToListAsync();

            foreach (var perm in permissions)
            {
                // Format: Permission:Resource:Action
                identity.AddClaim(new Claim("Permission", $"{perm.Resource}:{perm.Action}"));
            }
        }

        return identity;
    }
}
