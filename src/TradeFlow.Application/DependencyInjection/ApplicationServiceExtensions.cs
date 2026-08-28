using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace TradeFlow.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all Application layer services into the DI container.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);
        });

        return services;
    }
}
