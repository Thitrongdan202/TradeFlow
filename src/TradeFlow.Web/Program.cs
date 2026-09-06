using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.DependencyInjection;
using TradeFlow.Domain.Enums;
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

builder.Services.AddTradeFlowAuthorization();

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
    
    // Seed database on startup in Development
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<TradeFlow.Infrastructure.Persistence.DatabaseSeeder>();
    await seeder.SeedAsync();
}
else
{
    //app.UseExceptionHandler("/loi", createScopeForErrors: true);
    app.UseHsts();
}

//app.UseStatusCodePagesWithReExecute("/loi/{0}");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseStaticFiles();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Identity endpoints (login, logout, etc.)
app.MapAdditionalIdentityEndpoints();

// Pricing Excel Export & Original File Downloads
app.MapGet("/api/pricing/{id:int}/export-excel", async (int id, IExcelPricingService excelService, TradeFlowDbContext dbContext) =>
{
    var list = await dbContext.PriceLists.FindAsync(id);
    if (list == null) return Results.NotFound();
    var bytes = await excelService.ExportPriceListAsync(id);
    return Results.File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BangGia_{list.Code}_{DateTime.Now:yyyyMMdd}.xlsx");
}).RequireAuthorization("Permission:PriceLists:View");

app.MapGet("/api/pricing/{id:int}/download-original", async (int id, IFileStorageService fileStorage, TradeFlowDbContext dbContext, IAuditService auditService, ICurrentUserService user) =>
{
    var list = await dbContext.PriceLists.FindAsync(id);
    if (list == null || string.IsNullOrEmpty(list.OriginalFileStorageRef)) return Results.NotFound();
    var stream = await fileStorage.GetFileAsync(list.OriginalFileStorageRef);
    if (stream == null) return Results.NotFound();
    await auditService.LogAsync(AuditEventType.PriceListOriginalDownloaded, user.UserName ?? "System", "PriceList", list.Id.ToString(), $"Tải file Excel gốc: {list.OriginalFileName}");
    return Results.File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", list.OriginalFileName ?? $"BangGia_{list.Code}_Goc.xlsx");
}).RequireAuthorization("Permission:PriceLists:View");

app.Run();

// Make Program class accessible for integration tests
public partial class Program { }
