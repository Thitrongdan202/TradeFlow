using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using TradeFlow.Application.DependencyInjection;
using TradeFlow.Infrastructure.DependencyInjection;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Web.Components;
using TradeFlow.Web.Components.Account;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Infrastructure & Application layers
// ============================================================
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// ============================================================
// Blazor / Razor Components
// ============================================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================
// ASP.NET Core Identity + Authentication
// ============================================================
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddScoped<SignInManager<ApplicationUser>>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// No email sender needed for internal system (no email confirmation)
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// ============================================================
// Database developer page (dev only)
// ============================================================
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ============================================================
// Build application
// ============================================================
var app = builder.Build();

// ============================================================
// HTTP pipeline
// ============================================================
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/loi", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/loi/{0}");
app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Identity endpoints (login, logout, etc.)
app.MapAdditionalIdentityEndpoints();

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
