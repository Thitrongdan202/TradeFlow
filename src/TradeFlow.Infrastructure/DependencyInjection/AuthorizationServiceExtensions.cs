using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Authorization;
using TradeFlow.Infrastructure.Services;

namespace TradeFlow.Infrastructure.DependencyInjection;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddTradeFlowAuthorization(this IServiceCollection services)
    {
        // Bộ nhớ đệm cho quyền hạn hiệu lực
        services.AddMemoryCache();

        // Dịch vụ phân quyền server-side
        services.AddScoped<IPermissionService, PermissionService>();

        // Handler kiểm tra PermissionRequirement
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            // Đăng ký policy cho từng cặp Resource và Action với PermissionRequirement
            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                foreach (PermissionAction action in Enum.GetValues(typeof(PermissionAction)))
                {
                    string policyName = $"Permission:{resource}:{action}";
                    options.AddPolicy(policyName, policy =>
                    {
                        policy.RequireAuthenticatedUser();
                        policy.Requirements.Add(new PermissionRequirement(resource, action));
                    });
                }
            }
        });

        return services;
    }
}