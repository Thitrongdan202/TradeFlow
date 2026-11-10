using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Triển khai dịch vụ phân quyền phía máy chủ với cơ chế lưu cache trong bộ nhớ và an toàn đa luồng.
/// </summary>
public class PermissionService : IPermissionService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    private const string CacheKeyPrefix = "UserEffectivePerms_";
    private static readonly TimeSpan CacheSlidingExpiration = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CacheAbsoluteExpiration = TimeSpan.FromHours(1);

    public PermissionService(
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<PermissionService> logger)
    {
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(string userId, ResourceType resource, PermissionAction action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var permissions = await GetEffectivePermissionsAsync(userId, cancellationToken);
        var key = $"{(int)resource}:{(int)action}";
        var nameKey = $"{resource}:{action}";
        return permissions.Contains(nameKey) || permissions.Contains(key);
    }

    public async Task<bool> HasPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var permissions = await GetEffectivePermissionsAsync(userId, cancellationToken);
        var key = $"{resource}:{action}";
        return permissions.Contains(key);
    }

    public bool HasPermission(string userId, string resource, string action)
    {
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var permissions = GetEffectivePermissions(userId);
        var key = $"{resource}:{action}";
        return permissions.Contains(key);
    }

    public async Task<HashSet<string>> GetEffectivePermissionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        var cacheKey = $"{CacheKeyPrefix}{userId}";
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedPerms) && cachedPerms != null)
        {
            return cachedPerms;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cachedPerms) && cachedPerms != null)
            {
                return cachedPerms;
            }

            cachedPerms = await LoadPermissionsFromDbAsync(userId, cancellationToken);

            _cache.Set(cacheKey, cachedPerms, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheSlidingExpiration,
                AbsoluteExpirationRelativeToNow = CacheAbsoluteExpiration
            });

            return cachedPerms;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public HashSet<string> GetEffectivePermissions(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return [];
        }

        var cacheKey = $"{CacheKeyPrefix}{userId}";
        if (_cache.TryGetValue(cacheKey, out HashSet<string>? cachedPerms) && cachedPerms != null)
        {
            return cachedPerms;
        }

        _semaphore.Wait();
        try
        {
            if (_cache.TryGetValue(cacheKey, out cachedPerms) && cachedPerms != null)
            {
                return cachedPerms;
            }

            cachedPerms = Task.Run(async () => await LoadPermissionsFromDbAsync(userId)).GetAwaiter().GetResult();

            _cache.Set(cacheKey, cachedPerms, new MemoryCacheEntryOptions
            {
                SlidingExpiration = CacheSlidingExpiration,
                AbsoluteExpirationRelativeToNow = CacheAbsoluteExpiration
            });

            return cachedPerms;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<HashSet<string>> LoadPermissionsFromDbAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = scope.ServiceProvider.GetRequiredService<TradeFlowDbContext>();

        var user = await userManager.FindByIdAsync(userId);
        if (user == null || user.Status != UserStatus.Active)
        {
            return result;
        }

        var now = DateTime.UtcNow;

        // 1. Permanent roles assigned via Identity
        var permanentRoleNames = await userManager.GetRolesAsync(user);
        var permanentActiveRoles = await context.Roles
            .Where(r => permanentRoleNames.Contains(r.Name!) && r.IsActive)
            .ToListAsync(cancellationToken);

        // 2. Active temporary role grants (on-the-fly expiration check: StartsAt <= now && ExpiresAt > now && Status == Active)
        var activeTemporaryGrants = await context.TemporaryRoleAssignments
            .Where(t => t.UserId == userId && t.Status == TemporaryRoleAssignmentStatus.Active && t.StartsAt <= now && t.ExpiresAt > now)
            .OrderByDescending(t => t.StartsAt)
            .ToListAsync(cancellationToken);

        var tempRoleIds = activeTemporaryGrants.Select(t => t.RoleId).Distinct().ToList();
        var tempActiveRoles = await context.Roles
            .Where(r => tempRoleIds.Contains(r.Id) && r.IsActive)
            .ToListAsync(cancellationToken);

        // Fast path: Administrator có toàn bộ quyền
        if (permanentActiveRoles.Any(r => r.Name == "Administrator") || tempActiveRoles.Any(r => r.Name == "Administrator"))
        {
            foreach (ResourceType res in Enum.GetValues<ResourceType>())
            {
                foreach (PermissionAction act in Enum.GetValues<PermissionAction>())
                {
                    result.Add($"{res}:{act}");
                    result.Add($"{(int)res}:{(int)act}");
                    result.Add($"{res}:{act}:3");
                    result.Add($"{(int)res}:{(int)act}:3");
                }
            }
            return result;
        }

        // 3. Enforce strictly MAXIMUM 2 ACTIVE ROLES rule:
        // Role 1 (Base role): Prioritize Employee role or first active permanent role
        var effectiveRoleIds = new List<string>();
        var baseRole = permanentActiveRoles.FirstOrDefault(r => r.Name == "Employee") ?? permanentActiveRoles.FirstOrDefault();
        if (baseRole != null)
        {
            effectiveRoleIds.Add(baseRole.Id);
        }

        // Role 2 (Secondary role): Active temporary role takes precedence, then second permanent role
        if (tempActiveRoles.Any() && effectiveRoleIds.Count < 2)
        {
            var secondaryTemp = tempActiveRoles.First();
            if (!effectiveRoleIds.Contains(secondaryTemp.Id))
            {
                effectiveRoleIds.Add(secondaryTemp.Id);
            }
        }

        if (effectiveRoleIds.Count < 2)
        {
            var secondaryPerm = permanentActiveRoles.FirstOrDefault(r => !effectiveRoleIds.Contains(r.Id));
            if (secondaryPerm != null)
            {
                effectiveRoleIds.Add(secondaryPerm.Id);
            }
        }

        if (!effectiveRoleIds.Any())
        {
            return result;
        }

        // 4. Retrieve union of granted permissions across effective roles
        var grantedPermissions = await context.RolePermissions
            .Where(rp => effectiveRoleIds.Contains(rp.RoleId) && rp.IsGranted)
            .Select(rp => new { rp.Resource, rp.Action, rp.Scope })
            .ToListAsync(cancellationToken);

        // Group by Resource + Action and compute highest scope (All > Department > Self)
        var grouped = grantedPermissions
            .GroupBy(p => new { p.Resource, p.Action })
            .Select(g => new { g.Key.Resource, g.Key.Action, MaxScope = g.Max(x => (int)x.Scope) });

        foreach (var p in grouped)
        {
            result.Add($"{p.Resource}:{p.Action}");
            result.Add($"{(int)p.Resource}:{(int)p.Action}");
            result.Add($"{p.Resource}:{p.Action}:{p.MaxScope}");
            result.Add($"{(int)p.Resource}:{(int)p.Action}:{p.MaxScope}");
        }

        return result;
    }

    public async Task<DataScope> GetDataScopeAsync(string userId, ResourceType resource, PermissionAction action, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return DataScope.Self;

        var permissions = await GetEffectivePermissionsAsync(userId, cancellationToken);
        if (permissions.Contains($"{resource}:{action}:3") || permissions.Contains($"{(int)resource}:{(int)action}:3"))
        {
            return DataScope.All;
        }
        if (permissions.Contains($"{resource}:{action}:2") || permissions.Contains($"{(int)resource}:{(int)action}:2"))
        {
            return DataScope.Department;
        }
        if (permissions.Contains($"{resource}:{action}:1") || permissions.Contains($"{(int)resource}:{(int)action}:1"))
        {
            return DataScope.Self;
        }
        return DataScope.Self;
    }

    public void InvalidateUserPermissions(string userId)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var cacheKey = $"{CacheKeyPrefix}{userId}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("Invalidated cached permissions for user {UserId}", userId);
        }
    }

    public void InvalidateAllPermissions()
    {
        if (_cache is MemoryCache mc)
        {
            mc.Clear();
            _logger.LogInformation("Cleared all cached user permissions.");
        }
    }
}