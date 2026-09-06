using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace TradeFlow.Infrastructure.Persistence;

/// <summary>
/// Thêm các claim định danh cần thiết vào ClaimsIdentity (FullName, Status).
/// Không lưu ma trận quyền hạn vào cookie để đảm bảo cookie luôn nhẹ (< 1 KB) và không gây lỗi HTTP 431.
/// </summary>
public class CustomUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public CustomUserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> optionsAccessor)
        : base(userManager, roleManager, optionsAccessor)
    {
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

        return identity;
    }
}