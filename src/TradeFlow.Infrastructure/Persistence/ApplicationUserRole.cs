using Microsoft.AspNetCore.Identity;

namespace TradeFlow.Infrastructure.Persistence;

/// <summary>
/// Many-to-many join between ApplicationUser and ApplicationRole,
/// with navigation properties to both sides.
/// </summary>
public class ApplicationUserRole : IdentityUserRole<string>
{
    public ApplicationUser User { get; set; } = null!;
    public ApplicationRole Role { get; set; } = null!;
}
