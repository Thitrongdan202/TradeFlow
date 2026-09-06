using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Authorization;
using TradeFlow.Infrastructure.Persistence;
using TradeFlow.Infrastructure.Services;

namespace TradeFlow.UnitTests.Authentication;

public class PermissionServiceTests : IDisposable
{
    private readonly TradeFlowDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly IMemoryCache _cache;
    private readonly PermissionService _sut;

    public PermissionServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<TradeFlowDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new TradeFlowDbContext(dbOptions);

        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _cache = new MemoryCache(new MemoryCacheOptions());

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSingleton(_context);
        services.AddSingleton(_userManagerMock.Object);
        var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<Microsoft.Extensions.DependencyInjection.IServiceScopeFactory>();

        _sut = new PermissionService(
            scopeFactory,
            _cache,
            NullLogger<PermissionService>.Instance);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public void PermissionRequirement_Properties_ShouldBeSetCorrectly()
    {
        var req = new PermissionRequirement(ResourceType.Products, PermissionAction.View);
        req.Resource.Should().Be(ResourceType.Products);
        req.Action.Should().Be(PermissionAction.View);
    }

    [Fact]
    public async Task Administrator_HasAllPermissions()
    {
        var adminUser = new ApplicationUser { Id = "admin-1", UserName = "admin", Status = UserStatus.Active };
        _userManagerMock.Setup(m => m.FindByIdAsync("admin-1")).ReturnsAsync(adminUser);
        _userManagerMock.Setup(m => m.GetRolesAsync(adminUser)).ReturnsAsync(["Administrator"]);

        var canViewProducts = await _sut.HasPermissionAsync("admin-1", ResourceType.Products, PermissionAction.View);
        var canDeleteUsers = await _sut.HasPermissionAsync("admin-1", ResourceType.Users, PermissionAction.Delete);
        var canApproveSales = await _sut.HasPermissionAsync("admin-1", ResourceType.SalesOrders, PermissionAction.Approve);

        canViewProducts.Should().BeTrue("Administrator should have all permissions");
        canDeleteUsers.Should().BeTrue("Administrator should have all permissions");
        canApproveSales.Should().BeTrue("Administrator should have all permissions");
    }

    [Fact]
    public async Task StandardRole_HasOnlyGrantedPermissions()
    {
        var salesUser = new ApplicationUser { Id = "sales-1", UserName = "kinhdoanh01", Status = UserStatus.Active };
        _userManagerMock.Setup(m => m.FindByIdAsync("sales-1")).ReturnsAsync(salesUser);
        _userManagerMock.Setup(m => m.GetRolesAsync(salesUser)).ReturnsAsync(["Sales"]);

        var salesRole = new ApplicationRole { Id = "role-sales", Name = "Sales", IsActive = true };
        _context.Roles.Add(salesRole);
        _context.RolePermissions.AddRange(
            new RolePermission { RoleId = "role-sales", Resource = ResourceType.Customers, Action = PermissionAction.View, IsGranted = true },
            new RolePermission { RoleId = "role-sales", Resource = ResourceType.Customers, Action = PermissionAction.Create, IsGranted = true },
            new RolePermission { RoleId = "role-sales", Resource = ResourceType.Users, Action = PermissionAction.Delete, IsGranted = false }
        );
        await _context.SaveChangesAsync();

        var canViewCustomers = await _sut.HasPermissionAsync("sales-1", ResourceType.Customers, PermissionAction.View);
        var canCreateCustomers = await _sut.HasPermissionAsync("sales-1", ResourceType.Customers, PermissionAction.Create);
        var canDeleteUsers = await _sut.HasPermissionAsync("sales-1", ResourceType.Users, PermissionAction.Delete);
        var canEditCurrencies = await _sut.HasPermissionAsync("sales-1", ResourceType.Currencies, PermissionAction.Edit);

        canViewCustomers.Should().BeTrue();
        canCreateCustomers.Should().BeTrue();
        canDeleteUsers.Should().BeFalse();
        canEditCurrencies.Should().BeFalse();
    }

    [Fact]
    public async Task InactiveUser_HasNoPermissions()
    {
        var lockedUser = new ApplicationUser { Id = "locked-1", UserName = "locked", Status = UserStatus.Locked };
        _userManagerMock.Setup(m => m.FindByIdAsync("locked-1")).ReturnsAsync(lockedUser);

        var canView = await _sut.HasPermissionAsync("locked-1", ResourceType.Products, PermissionAction.View);
        canView.Should().BeFalse("Locked users should have no effective permissions");
    }

    [Fact]
    public async Task InactiveRole_DoesNotGrantPermissions()
    {
        var user = new ApplicationUser { Id = "user-inactive-role", UserName = "test", Status = UserStatus.Active };
        _userManagerMock.Setup(m => m.FindByIdAsync("user-inactive-role")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["OldRole"]);

        var inactiveRole = new ApplicationRole { Id = "role-inactive", Name = "OldRole", IsActive = false };
        _context.Roles.Add(inactiveRole);
        _context.RolePermissions.Add(
            new RolePermission { RoleId = "role-inactive", Resource = ResourceType.Products, Action = PermissionAction.View, IsGranted = true });
        await _context.SaveChangesAsync();

        var canView = await _sut.HasPermissionAsync("user-inactive-role", ResourceType.Products, PermissionAction.View);
        canView.Should().BeFalse("Inactive roles must not grant permissions");
    }

    [Fact]
    public async Task Caching_ReturnsCachedPermissions_OnSubsequentCalls()
    {
        var user = new ApplicationUser { Id = "user-cache", UserName = "cacheuser", Status = UserStatus.Active };
        _userManagerMock.Setup(m => m.FindByIdAsync("user-cache")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Warehouse"]);

        var role = new ApplicationRole { Id = "role-wh", Name = "Warehouse", IsActive = true };
        _context.Roles.Add(role);
        _context.RolePermissions.Add(
            new RolePermission { RoleId = "role-wh", Resource = ResourceType.Warehouses, Action = PermissionAction.View, IsGranted = true });
        await _context.SaveChangesAsync();

        // First call: populates cache
        var perms1 = await _sut.GetEffectivePermissionsAsync("user-cache");
        perms1.Should().Contain($"{(int)ResourceType.Warehouses}:{(int)PermissionAction.View}");

        // Now modify DB directly without invalidating cache
        _context.RolePermissions.RemoveRange(_context.RolePermissions);
        await _context.SaveChangesAsync();

        // Second call: should still return cached permissions
        var perms2 = await _sut.GetEffectivePermissionsAsync("user-cache");
        perms2.Should().Contain($"{(int)ResourceType.Warehouses}:{(int)PermissionAction.View}");

        // After invalidation: should query fresh and be empty
        _sut.InvalidateUserPermissions("user-cache");
        var perms3 = await _sut.GetEffectivePermissionsAsync("user-cache");
        perms3.Should().BeEmpty();
    }

    [Fact]
    public void StringAndEnumKeys_ShouldResolveConsistently()
    {
        var user = new ApplicationUser { Id = "user-sync", UserName = "syncuser", Status = UserStatus.Active };
        _userManagerMock.Setup(m => m.FindByIdAsync("user-sync")).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(["Manager"]);

        var role = new ApplicationRole { Id = "role-mgr", Name = "Manager", IsActive = true };
        _context.Roles.Add(role);
        _context.RolePermissions.Add(
            new RolePermission { RoleId = "role-mgr", Resource = ResourceType.Users, Action = PermissionAction.View, IsGranted = true });
        _context.SaveChanges();

        // Sync HasPermission by string
        var canViewSync = _sut.HasPermission("user-sync", "Users", "View");
        canViewSync.Should().BeTrue();
    }
}