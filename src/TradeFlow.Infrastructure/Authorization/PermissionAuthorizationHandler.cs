using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Authorization;

/// <summary>
/// Handler xử lý kiểm tra PermissionRequirement tại server-side.
/// Tra cứu quyền hiệu lực thông qua IPermissionService và tối ưu đường tắt cho Administrator.
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionService _permissionService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionAuthorizationHandler(
        IPermissionService permissionService,
        UserManager<ApplicationUser> userManager)
    {
        _permissionService = permissionService;
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Fast path: Administrator luôn có toàn quyền
        if (context.User.IsInRole("Administrator"))
        {
            context.Succeed(requirement);
            return;
        }

        var userId = _userManager.GetUserId(context.User);
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var hasPermission = await _permissionService.HasPermissionAsync(userId, requirement.Resource, requirement.Action);
        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}