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
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options => { options.MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024; });
builder.WebHost.ConfigureKestrel(serverOptions => { serverOptions.Limits.MaxRequestBodySize = 10L * 1024 * 1024 * 1024; });
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 64 * 1024 * 1024; // 64 MB
        options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
        options.HandshakeTimeout = TimeSpan.FromMinutes(2);
    });

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 64 * 1024 * 1024; // 64 MB
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
    options.HandshakeTimeout = TimeSpan.FromMinutes(2);
});

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

// Quotations & Documents Endpoints
app.MapGet("/api/quotations/{id:int}/export-pdf", async (int id, IQuotationService quotationService) =>
{
    var quotation = await quotationService.GetQuotationByIdAsync(id);
    if (quotation == null) return Results.NotFound();
    var bytes = await quotationService.GeneratePdfAsync(id);
    return Results.File(bytes, "application/pdf", $"BaoGia_{quotation.Code}.pdf");
}).RequireAuthorization("Permission:Quotations:View");

app.MapGet("/api/contracts/{id:int}/export-pdf", async (int id, IContractService contractService) =>
{
    var contract = await contractService.GetContractByIdAsync(id);
    if (contract == null) return Results.NotFound();
    var bytes = await contractService.GenerateContractPdfAsync(id);
    return Results.File(bytes, "application/pdf", $"HopDong_{contract.ContractNumber}.pdf");
}).RequireAuthorization("Permission:Contracts:View");

app.MapGet("/api/documents/{id:int}/download", async (int id, IDocumentService documentService) =>
{
    var (stream, contentType, fileName) = await documentService.GetFileStreamAsync(id);
    if (stream == null) return Results.NotFound();
    return Results.File(stream, contentType, fileName);
}).RequireAuthorization("Permission:Documents:View");

app.MapGet("/api/documents/{id:int}/preview", async (int id, IDocumentService documentService) =>
{
    var (stream, contentType, fileName) = await documentService.GetFileStreamAsync(id);
    if (stream == null) return Results.NotFound();
    return Results.File(stream, contentType, enableRangeProcessing: true);
}).RequireAuthorization("Permission:Documents:View");

// Data Backup Download Endpoints
app.MapGet("/api/admin/backup/download-prepared", async (
    string id,
    IDataBackupService backupService,
    System.Security.Claims.ClaimsPrincipal user,
    Microsoft.AspNetCore.Authorization.IAuthorizationService authService) =>
{
    var authResult = await authService.AuthorizeAsync(user, null, "Permission:CompanySettings:View");
    if (!authResult.Succeeded && !user.IsInRole("Administrator"))
    {
        return Results.Forbid();
    }

    var stream = await backupService.GetPreparedBackupStreamAsync(id, user);
    if (stream == null)
    {
        return Results.NotFound("Tệp sao lưu không tồn tại hoặc đã hết hạn.");
    }

    string fileName = $"tradeflow_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
    return Results.File(stream, "application/zip", fileName);
}).RequireAuthorization();

app.MapGet("/api/admin/backup/download", async (
    IDataBackupService backupService,
    System.Security.Claims.ClaimsPrincipal user,
    Microsoft.AspNetCore.Authorization.IAuthorizationService authService) =>
{
    var authResult = await authService.AuthorizeAsync(user, null, "Permission:CompanySettings:View");
    if (!authResult.Succeeded && !user.IsInRole("Administrator"))
    {
        return Results.Forbid();
    }

    var result = await backupService.CreateBackupAsync(new TradeFlow.Application.Common.Models.Settings.BackupOptions(), user);
    if (!result.Succeeded || string.IsNullOrEmpty(result.BackupId))
    {
        return Results.Problem(result.ErrorMessage ?? "Không thể tạo bản sao lưu dữ liệu.");
    }

    var stream = await backupService.GetPreparedBackupStreamAsync(result.BackupId, user);
    if (stream == null)
    {
        return Results.NotFound();
    }

    return Results.File(stream, "application/zip", result.FileName ?? $"tradeflow_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip");
}).RequireAuthorization();

app.MapPost("/api/admin/backup/upload-archive", async (
    IFormFile file,
    IDataBackupService backupService,
    System.Security.Claims.ClaimsPrincipal user,
    Microsoft.AspNetCore.Authorization.IAuthorizationService authService) =>
{
    var authResult = await authService.AuthorizeAsync(user, null, "Permission:CompanySettings:View");
    if (!authResult.Succeeded && !user.IsInRole("Administrator"))
    {
        return Results.Forbid();
    }

    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "Vui lòng chọn tệp ZIP sao lưu hợp lệ." });
    }

    using var stream = file.OpenReadStream();
    var result = await backupService.ValidateAndPreviewArchiveAsync(stream, file.FileName, user);
    return Results.Ok(result);
}).DisableAntiforgery().RequireAuthorization();

app.MapPost("/api/admin/backup/upload-folder", async (
    HttpRequest request,
    IDataBackupService backupService,
    System.Security.Claims.ClaimsPrincipal user,
    Microsoft.AspNetCore.Authorization.IAuthorizationService authService) =>
{
    var authResult = await authService.AuthorizeAsync(user, null, "Permission:CompanySettings:View");
    if (!authResult.Succeeded && !user.IsInRole("Administrator"))
    {
        return Results.Forbid();
    }

    var form = await request.ReadFormAsync();
    var files = form.Files;
    if (files == null || files.Count == 0)
    {
        return Results.BadRequest(new { message = "Không tìm thấy tệp nào trong thư mục đã chọn." });
    }

    string stagingDir = Path.Combine(Path.GetTempPath(), "TradeFlow_Restore_Staging", $"folder_{Guid.NewGuid():N}");
    Directory.CreateDirectory(stagingDir);

    foreach (var file in files)
    {
        string relPath = form[file.FileName].FirstOrDefault() ?? file.FileName;
        var parts = relPath.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        string targetSubPath = parts.Length > 1 ? string.Join(Path.DirectorySeparatorChar, parts.Skip(1)) : parts[0];
        string destPath = Path.Combine(stagingDir, targetSubPath);
        Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
        using var destStream = new FileStream(destPath, FileMode.Create);
        await file.CopyToAsync(destStream);
    }

    var result = await backupService.ValidateAndPreviewExtractedFolderAsync(stagingDir, user);
    return Results.Ok(result);
}).DisableAntiforgery().RequireAuthorization();

app.MapPost("/api/admin/backup/execute-restore", async (
    [Microsoft.AspNetCore.Mvc.FromBody] TradeFlow.Application.Common.Models.Settings.ImportExecutionPlan plan,
    IDataBackupService backupService,
    System.Security.Claims.ClaimsPrincipal user,
    Microsoft.AspNetCore.Authorization.IAuthorizationService authService) =>
{
    var authResult = await authService.AuthorizeAsync(user, null, "Permission:CompanySettings:View");
    if (!authResult.Succeeded && !user.IsInRole("Administrator"))
    {
        return Results.Forbid();
    }

    var result = await backupService.ExecuteRestoreAsync(plan, user);
    return Results.Ok(result);
}).DisableAntiforgery().RequireAuthorization();

app.MapPost("/api/admin/backup/cleanup-staging", async (
    [Microsoft.AspNetCore.Mvc.FromBody] TradeFlow.Web.StagingCleanupRequest req,
    IDataBackupService backupService,
    System.Security.Claims.ClaimsPrincipal user) =>
{
    if (!string.IsNullOrEmpty(req?.StagingPath))
    {
        await backupService.CleanupStagingAsync(req.StagingPath);
    }
    return Results.Ok();
}).DisableAntiforgery().RequireAuthorization();

app.MapGet("/test-import", async (TradeFlow.Application.Common.Interfaces.IExcelPricingService excelService) => 
{ 
    try 
    { 
        var path = System.IO.Directory.GetFiles(@"wwwroot/uploads/pricing_originals", "*.xlsx")[0]; 
        using var stream = System.IO.File.OpenRead(path); 
        var result = await excelService.AnalyzeAndDryRunAsync(stream, "test.xlsx"); 
        var commitReq = new TradeFlow.Application.Common.Models.Pricing.ExcelImportCommitRequest 
        { 
            OriginalFileName = result.FileName, 
            TempFileReference = result.TempFileReference, 
            AutoCreateProducts = true, 
            Items = result.Items 
        }; 
        var priceList = await excelService.CommitImportAsync(commitReq, "admin"); 
        return Microsoft.AspNetCore.Http.Results.Ok(priceList); 
    } 
    catch (Exception ex) 
    { 
        return Microsoft.AspNetCore.Http.Results.Ok(ex.ToString()); 
    } 
});

app.Run();

namespace TradeFlow.Web
{
    public record StagingCleanupRequest(string? StagingPath);
}

// Make Program class accessible for integration tests
public partial class Program { }
