using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TradeFlow.Application.Common.Interfaces;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService supporting both HTTP requests and Blazor circuits.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
    }

    private ClaimsPrincipal? Principal
    {
        get
        {
            var httpUser = _httpContextAccessor.HttpContext?.User;
            if (httpUser?.Identity?.IsAuthenticated == true)
            {
                return httpUser;
            }

            try
            {
                var authStateProvider = _serviceProvider.GetService<AuthenticationStateProvider>();
                if (authStateProvider != null)
                {
                    var task = authStateProvider.GetAuthenticationStateAsync();
                    if (task.IsCompleted)
                    {
                        return task.Result.User;
                    }
                    return task.GetAwaiter().GetResult().User;
                }
            }
            catch
            {
                // Fallback to HTTP context
            }

            return httpUser;
        }
    }

    public string? UserName => Principal?.Identity?.Name;

    public string? UserId => Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<Claim> Claims => Principal?.Claims ?? Enumerable.Empty<Claim>();

    public bool HasPermission(string resource, string action)
    {
        if (Principal == null || !IsAuthenticated) return false;

        if (Principal.IsInRole("Administrator")) return true;

        if (string.IsNullOrEmpty(UserId)) return false;

        var permissionService = _serviceProvider.GetService<IPermissionService>();
        if (permissionService == null) return false;

        return permissionService.HasPermission(UserId, resource, action);
    }
}