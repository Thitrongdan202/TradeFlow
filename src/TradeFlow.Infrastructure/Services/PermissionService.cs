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

            cachedPerms = LoadPermissionsFromDbAsync(userId).GetAwaiter().GetResult();

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

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Any())
        {
            return result;
        }

        // Fast path: Administrator có toàn bộ quyền
        if (roles.Contains("Administrator"))
        {
            foreach (ResourceType res in Enum.GetValues<ResourceType>())
            {
                foreach (PermissionAction act in Enum.GetValues<PermissionAction>())
                {
                    result.Add($"{res}:{act}");
                    result.Add($"{(int)res}:{(int)act}");
                }
            }
            return result;
        }

        var activeRoleIds = await context.Roles
            .Where(r => roles.Contains(r.Name!) && r.IsActive)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (!activeRoleIds.Any())
        {
            return result;
        }

        var grantedPermissions = await context.RolePermissions
            .Where(rp => activeRoleIds.Contains(rp.RoleId) && rp.IsGranted)
            .Select(rp => new { rp.Resource, rp.Action })
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var p in grantedPermissions)
        {
            result.Add($"{p.Resource}:{p.Action}");
            result.Add($"{(int)p.Resource}:{(int)p.Action}");
        }

        return result;
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