using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.DependencyInjection;

public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddTradeFlowAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Register a policy for each Resource and Action
            foreach (ResourceType resource in Enum.GetValues(typeof(ResourceType)))
            {
                foreach (PermissionAction action in Enum.GetValues(typeof(PermissionAction)))
                {
                    string policyName = $"Permission:{resource}:{action}";
                    options.AddPolicy(policyName, policy =>
                        policy.RequireClaim("Permission", $"{resource}:{action}"));
                }
            }
        });

        return services;
    }
}
