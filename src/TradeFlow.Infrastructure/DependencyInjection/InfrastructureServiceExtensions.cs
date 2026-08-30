using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradeFlow.Application.Common.Interfaces;

namespace TradeFlow.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all Infrastructure layer services into the DI container.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. " +
                "Set it in appsettings.json, environment variables, or user secrets.");

        services.AddDbContext<Persistence.TradeFlowDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

#if DEBUG
            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors();
#endif
        });

        // Register the DbContext as IApplicationDbContext
        services.AddScoped<IApplicationDbContext>(
            sp => sp.GetRequiredService<Persistence.TradeFlowDbContext>());

        // ASP.NET Core Identity with dynamic roles
        services.AddIdentityCore<Persistence.ApplicationUser>(options =>
            {
                // Password policy
                // tradecore123 satisfies: digit + lowercase + length>=8
                // Uppercase NOT required so simple dev passwords work
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;

                // Lockout policy
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // Don't require confirmed email for internal system
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddRoles<Persistence.ApplicationRole>()
            .AddEntityFrameworkStores<Persistence.TradeFlowDbContext>()
            .AddClaimsPrincipalFactory<Persistence.CustomUserClaimsPrincipalFactory>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        // Register audit service
        services.AddScoped<IAuditService, Services.AuditService>();
        
        // Register database seeder
        services.AddScoped<Persistence.DatabaseSeeder>();

        return services;
    }
}
