using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Entities.Users;

namespace TradeFlow.Infrastructure.Persistence;

/// <summary>
/// TradeFlow application DbContext.
/// Inherits from IdentityDbContext to include ASP.NET Core Identity tables.
/// </summary>
public class TradeFlowDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, string,
        IdentityUserClaim<string>, ApplicationUserRole, IdentityUserLogin<string>,
        IdentityRoleClaim<string>, IdentityUserToken<string>>,
      IApplicationDbContext
{
    public TradeFlowDbContext(DbContextOptions<TradeFlowDbContext> options)
        : base(options)
    {
    }

    // === Audit & Settings ===
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<SystemSequence> SystemSequences => Set<SystemSequence>();

    // === Phase 3 — Master Data ===
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<UnitOfMeasure> UnitOfMeasures => Set<UnitOfMeasure>();
    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> found in this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradeFlowDbContext).Assembly);

        // Rename Identity tables to English but keep them clear
        modelBuilder.Entity<ApplicationUser>().ToTable("Users");
        modelBuilder.Entity<ApplicationRole>().ToTable("Roles");
        modelBuilder.Entity<ApplicationUserRole>().ToTable("UserRoles");
        modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        modelBuilder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
    }
}