using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Security;
using TradeFlow.Domain.Entities.Security;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services.Security;

public class RoleManagementService : IRoleManagementService
{
    private readonly TradeFlowDbContext _context;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPermissionService _permissionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<RoleManagementService> _logger;

    public RoleManagementService(
        TradeFlowDbContext context,
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IPermissionService permissionService,
        IAuditService auditService,
        ILogger<RoleManagementService> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _permissionService = permissionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<RoleOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        int totalRoles = await _context.Roles.CountAsync(cancellationToken);

        // Distinct users assigned to at least one active role
        var permanentAssignedUserIds = await _context.UserRoles
            .Select(ur => ur.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var temporaryAssignedUserIds = await _context.TemporaryRoleAssignments
            .Where(t => t.Status == TemporaryRoleAssignmentStatus.Active && t.StartsAt <= now && t.ExpiresAt > now)
            .Select(t => t.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        int totalAssignedUsers = permanentAssignedUserIds.Union(temporaryAssignedUserIds).Count();

        int activeTemporaryRoles = await _context.TemporaryRoleAssignments
            .CountAsync(t => t.Status == TemporaryRoleAssignmentStatus.Active && t.StartsAt <= now && t.ExpiresAt > now, cancellationToken);

        // Count sensitive permissions currently granted across active roles (Delete, ManageConfig, ManagePermissions, OverridePrice, Export)
        var sensitiveActions = new[]
        {
            PermissionAction.Delete,
            PermissionAction.ManageConfig,
            PermissionAction.ManagePermissions,
            PermissionAction.OverridePrice,
            PermissionAction.Export
        };

        int attentionPermissions = await _context.RolePermissions
            .Where(rp => rp.IsGranted && sensitiveActions.Contains(rp.Action))
            .CountAsync(cancellationToken);

        // Recent RBAC audit events
        var rbacEventTypes = new[]
        {
            AuditEventType.RoleCreated,
            AuditEventType.RoleUpdated,
            AuditEventType.RoleDeleted,
            AuditEventType.RoleCloned,
            AuditEventType.RolePermissionsUpdated,
            AuditEventType.UserRoleAssigned,
            AuditEventType.UserRoleRevoked,
            AuditEventType.TemporaryRoleGranted,
            AuditEventType.TemporaryRoleRevoked,
            AuditEventType.TemporaryRoleExpired,
            AuditEventType.DataScopeChanged,
            AuditEventType.UnauthorizedAccessAttempt
        };

        var recentLogs = await _context.AuditLogs
            .AsNoTracking()
            .Where(a => rbacEventTypes.Contains(a.EventType))
            .OrderByDescending(a => a.OccurredAt)
            .Take(10)
            .ToListAsync(cancellationToken);

        var activities = recentLogs.Select(a => new PermissionActivityDto
        {
            Id = (int)a.Id,
            Actor = a.PerformedBy ?? "System",
            Target = $"{a.TargetEntity}: {a.TargetId}",
            EventType = a.EventType,
            EventTypeName = a.EventType.ToString(),
            Description = a.Details ?? string.Empty,
            Timestamp = a.OccurredAt,
            Succeeded = !a.Details?.Contains("từ chối", StringComparison.OrdinalIgnoreCase) ?? true
        }).ToList();

        return new RoleOverviewDto
        {
            TotalRoles = totalRoles,
            TotalAssignedUsers = totalAssignedUsers,
            ActiveTemporaryRoles = activeTemporaryRoles,
            AttentionPermissionsCount = attentionPermissions,
            RecentActivities = activities
        };
    }

    public async Task<List<RoleListItemDto>> GetRolesAsync(string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Roles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var pattern = $"%{term}%";
            query = query.Where(r => EF.Functions.ILike(r.Name!, pattern) || (r.Description != null && EF.Functions.ILike(r.Description, pattern)));
        }

        var roles = await query
            .OrderByDescending(r => r.IsSystem)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(r => r.Id).ToList();

        // Count granted permissions per role
        var permissionCounts = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsGranted)
            .GroupBy(rp => rp.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        // Count permanent active users per role
        var userCounts = await _context.UserRoles
            .Where(ur => roleIds.Contains(ur.RoleId))
            .GroupBy(ur => ur.RoleId)
            .Select(g => new { RoleId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RoleId, x => x.Count, cancellationToken);

        return roles.Select(r => new RoleListItemDto
        {
            Id = r.Id,
            Name = r.Name ?? string.Empty,
            Description = r.Description,
            ColorHex = r.ColorHex ?? "#4f46e5",
            Icon = r.Icon ?? "shield",
            IsActive = r.IsActive,
            IsSystem = r.IsSystem,
            CreatedAt = r.CreatedAt,
            CreatedBy = r.CreatedBy,
            GrantedPermissionsCount = permissionCounts.GetValueOrDefault(r.Id, 0),
            ActiveUsersCount = userCounts.GetValueOrDefault(r.Id, 0)
        }).ToList();
    }

    public async Task<RoleDetailDto?> GetRoleByIdAsync(string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
        if (role == null) return null;

        var permissions = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new RolePermissionItemDto
            {
                Resource = rp.Resource,
                Action = rp.Action,
                IsGranted = rp.IsGranted,
                Scope = rp.Scope
            })
            .ToListAsync(cancellationToken);

        var permanentUserIds = await _context.UserRoles
            .Where(ur => ur.RoleId == roleId)
            .Select(ur => ur.UserId)
            .ToListAsync(cancellationToken);

        var employees = await _context.Employees
            .Include(e => e.Department)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var empByUserId = employees.Where(e => !string.IsNullOrEmpty(e.UserId)).ToDictionary(e => e.UserId!, e => e);
        var empById = employees.ToDictionary(e => e.Id, e => e);

        var pUsers = await _context.Users
            .AsNoTracking()
            .Where(u => permanentUserIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        var users = pUsers.Select(u =>
        {
            var emp = (u.EmployeeId.HasValue && empById.TryGetValue(u.EmployeeId.Value, out var e1)) ? e1 : (empByUserId.TryGetValue(u.Id, out var e2) ? e2 : null);
            return new UserSummaryDto
            {
                UserId = u.Id,
                UserName = u.UserName ?? string.Empty,
                FullName = u.FullName ?? string.Empty,
                DepartmentName = emp?.Department?.Name,
                IsTemporary = false,
                ExpiresAt = null
            };
        }).ToList();

        // Also check temporary users currently assigned
        var now = DateTime.UtcNow;
        var tempAssignments = await _context.TemporaryRoleAssignments
            .AsNoTracking()
            .Where(t => t.RoleId == roleId && t.Status == TemporaryRoleAssignmentStatus.Active && t.StartsAt <= now && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        if (tempAssignments.Any())
        {
            var tempUserIds = tempAssignments.Select(t => t.UserId).Distinct().ToList();
            var tempUsers = await _context.Users
                .AsNoTracking()
                .Where(u => tempUserIds.Contains(u.Id) && !permanentUserIds.Contains(u.Id))
                .ToListAsync(cancellationToken);

            foreach (var tu in tempUsers)
            {
                var assign = tempAssignments.FirstOrDefault(a => a.UserId == tu.Id);
                var emp = (tu.EmployeeId.HasValue && empById.TryGetValue(tu.EmployeeId.Value, out var e1)) ? e1 : (empByUserId.TryGetValue(tu.Id, out var e2) ? e2 : null);
                users.Add(new UserSummaryDto
                {
                    UserId = tu.Id,
                    UserName = tu.UserName ?? string.Empty,
                    FullName = tu.FullName ?? string.Empty,
                    DepartmentName = emp?.Department?.Name,
                    IsTemporary = true,
                    ExpiresAt = assign?.ExpiresAt
                });
            }
        }

        return new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name ?? string.Empty,
            Description = role.Description,
            ColorHex = role.ColorHex ?? "#4f46e5",
            Icon = role.Icon ?? "shield",
            IsActive = role.IsActive,
            IsSystem = role.IsSystem,
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy,
            Permissions = permissions,
            AssignedUsers = users
        };
    }

    public async Task<(bool Success, string? Error, string? RoleId)> CreateRoleAsync(
        CreateRoleCommand command,
        string createdBy,
        CancellationToken cancellationToken = default)
    {
        string name = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Tên vai trò không được để trống.", null);
        }

        if (await _roleManager.RoleExistsAsync(name))
        {
            return (false, $"Vai trò có tên '{name}' đã tồn tại trong hệ thống.", null);
        }

        var role = new ApplicationRole(name)
        {
            Description = command.Description?.Trim(),
            ColorHex = !string.IsNullOrWhiteSpace(command.ColorHex) ? command.ColorHex.Trim() : "#4f46e5",
            Icon = !string.IsNullOrWhiteSpace(command.Icon) ? command.Icon.Trim() : "shield",
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            var err = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"Lỗi khi tạo vai trò: {err}", null);
        }

        // If inheriting permissions from a template role
        if (!string.IsNullOrWhiteSpace(command.InheritFromRoleId))
        {
            var sourcePermissions = await _context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == command.InheritFromRoleId)
                .ToListAsync(cancellationToken);

            foreach (var sp in sourcePermissions)
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    Resource = sp.Resource,
                    Action = sp.Action,
                    IsGranted = sp.IsGranted,
                    Scope = sp.Scope,
                    GrantedAt = DateTime.UtcNow,
                    GrantedBy = createdBy
                });
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        await _auditService.LogAsync(
            AuditEventType.RoleCreated,
            createdBy,
            "Role",
            role.Name,
            $"Tạo vai trò mới '{role.Name}' (Màu: {role.ColorHex}, Biểu tượng: {role.Icon}){(string.IsNullOrWhiteSpace(command.InheritFromRoleId) ? "" : " kế thừa quyền từ vai trò mẫu")}"
        );

        _permissionService.InvalidateAllPermissions();
        return (true, null, role.Id);
    }

    public async Task<(bool Success, string? Error)> UpdateRoleAsync(
        UpdateRoleCommand command,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(command.Id);
        if (role == null)
        {
            return (false, "Không tìm thấy vai trò cần cập nhật.");
        }

        // System role protection: Administrator name and active status cannot be altered
        if (role.IsSystem && role.Name == "Administrator")
        {
            if (command.Name != "Administrator" || !command.IsActive)
            {
                return (false, "Không thể đổi tên hoặc vô hiệu hóa vai trò Quản trị viên tối cao (Administrator).");
            }
        }

        string newName = command.Name.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            return (false, "Tên vai trò không được để trống.");
        }

        // Check name uniqueness if changed
        if (!string.Equals(role.Name, newName, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _roleManager.FindByNameAsync(newName);
            if (existing != null && existing.Id != role.Id)
            {
                return (false, $"Vai trò có tên '{newName}' đã tồn tại.");
            }
            role.Name = newName;
            role.NormalizedName = newName.ToUpperInvariant();
        }

        role.Description = command.Description?.Trim();
        role.ColorHex = !string.IsNullOrWhiteSpace(command.ColorHex) ? command.ColorHex.Trim() : "#4f46e5";
        role.Icon = !string.IsNullOrWhiteSpace(command.Icon) ? command.Icon.Trim() : "shield";
        role.IsActive = command.IsActive;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            var err = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"Lỗi cập nhật vai trò: {err}");
        }

        await _auditService.LogAsync(
            AuditEventType.RoleUpdated,
            updatedBy,
            "Role",
            role.Name!,
            $"Cập nhật thông tin vai trò '{role.Name}' (Trạng thái: {(role.IsActive ? "Hoạt động" : "Vô hiệu hóa")})"
        );

        _permissionService.InvalidateAllPermissions();
        return (true, null);
    }

    public async Task<(bool Success, string? Error, string? NewRoleId)> CloneRoleAsync(
        string sourceRoleId,
        string newName,
        string? newDescription,
        string clonedBy,
        CancellationToken cancellationToken = default)
    {
        var sourceRole = await _roleManager.FindByIdAsync(sourceRoleId);
        if (sourceRole == null)
        {
            return (false, "Không tìm thấy vai trò nguồn để sao chép.", null);
        }

        string trimmedName = newName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (false, "Tên vai trò mới không được để trống.", null);
        }

        if (await _roleManager.RoleExistsAsync(trimmedName))
        {
            return (false, $"Vai trò có tên '{trimmedName}' đã tồn tại.", null);
        }

        var newRole = new ApplicationRole(trimmedName)
        {
            Description = newDescription?.Trim() ?? $"Sao chép từ {sourceRole.Name}",
            ColorHex = sourceRole.ColorHex ?? "#4f46e5",
            Icon = sourceRole.Icon ?? "shield",
            IsActive = true,
            IsSystem = false,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = clonedBy
        };

        var result = await _roleManager.CreateAsync(newRole);
        if (!result.Succeeded)
        {
            var err = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, $"Lỗi khi sao chép vai trò: {err}", null);
        }

        // Copy all permissions from source role
        var sourcePerms = await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == sourceRoleId)
            .ToListAsync(cancellationToken);

        foreach (var sp in sourcePerms)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = newRole.Id,
                Resource = sp.Resource,
                Action = sp.Action,
                IsGranted = sp.IsGranted,
                Scope = sp.Scope,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = clonedBy
            });
        }
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.RoleCloned,
            clonedBy,
            "Role",
            newRole.Name,
            $"Sao chép vai trò từ '{sourceRole.Name}' sang vai trò mới '{newRole.Name}' (sao chép {sourcePerms.Count} quyền)"
        );

        _permissionService.InvalidateAllPermissions();
        return (true, null, newRole.Id);
    }

    public async Task<(bool Success, string? Error)> ToggleRoleStatusAsync(
        string roleId,
        bool isActive,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return (false, "Không tìm thấy vai trò.");

        if (role.IsSystem && role.Name == "Administrator" && !isActive)
        {
            return (false, "Không thể vô hiệu hóa vai trò Quản trị viên tối cao (Administrator).");
        }

        role.IsActive = isActive;
        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            return (false, "Không thể cập nhật trạng thái vai trò.");
        }

        await _auditService.LogAsync(
            AuditEventType.RoleUpdated,
            updatedBy,
            "Role",
            role.Name!,
            $"{(isActive ? "Kích hoạt" : "Vô hiệu hóa")} vai trò '{role.Name}'"
        );

        _permissionService.InvalidateAllPermissions();
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> DeleteRoleAsync(
        string roleId,
        string deletedBy,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return (false, "Không tìm thấy vai trò.");

        // Rule: Protect system roles
        if (role.IsSystem || role.Name == "Administrator")
        {
            return (false, "Không thể xóa vai trò hệ thống mặc định (Administrator/Owner).");
        }

        // Rule: Cannot delete role if assigned to any accounts
        int assignedUsersCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == roleId, cancellationToken);
        if (assignedUsersCount > 0)
        {
            return (false, $"Không thể xóa vai trò '{role.Name}' vì đang có {assignedUsersCount} tài khoản được gán vai trò này. Vui lòng chuyển các tài khoản sang vai trò khác trước.");
        }

        int activeTempCount = await _context.TemporaryRoleAssignments
            .CountAsync(t => t.RoleId == roleId && t.Status == TemporaryRoleAssignmentStatus.Active, cancellationToken);
        if (activeTempCount > 0)
        {
            return (false, $"Không thể xóa vai trò '{role.Name}' vì đang có {activeTempCount} lượt cấp quyền tạm thời đang hiệu lực.");
        }

        // Remove role permissions first
        var permissions = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        _context.RolePermissions.RemoveRange(permissions);
        await _context.SaveChangesAsync(cancellationToken);

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            return (false, "Không thể xóa vai trò khỏi cơ sở dữ liệu.");
        }

        await _auditService.LogAsync(
            AuditEventType.RoleDeleted,
            deletedBy,
            "Role",
            role.Name!,
            $"Xóa vai trò '{role.Name}' và toàn bộ quyền đã cấp"
        );

        _permissionService.InvalidateAllPermissions();
        return (true, null);
    }

    public async Task<List<RolePermissionItemDto>> GetRolePermissionsAsync(string roleId, CancellationToken cancellationToken = default)
    {
        return await _context.RolePermissions
            .AsNoTracking()
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => new RolePermissionItemDto
            {
                Resource = rp.Resource,
                Action = rp.Action,
                IsGranted = rp.IsGranted,
                Scope = rp.Scope
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(bool Success, string? Error)> UpdateRolePermissionsAsync(
        string roleId,
        List<RolePermissionItemDto> permissions,
        string updatedBy,
        CancellationToken cancellationToken = default)
    {
        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return (false, "Không tìm thấy vai trò.");

        // Clear existing permissions for role
        var existing = await _context.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        _context.RolePermissions.RemoveRange(existing);

        // Add new permissions
        foreach (var p in permissions)
        {
            _context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                Resource = p.Resource,
                Action = p.Action,
                IsGranted = p.IsGranted,
                Scope = p.Scope,
                GrantedAt = DateTime.UtcNow,
                GrantedBy = updatedBy
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.RolePermissionsUpdated,
            updatedBy,
            "Role",
            role.Name!,
            $"Cập nhật ma trận quyền cho vai trò '{role.Name}' ({permissions.Count(p => p.IsGranted)} quyền được cấp)"
        );

        _permissionService.InvalidateAllPermissions();
        return (true, null);
    }

    public async Task<List<UserRoleAssignmentDto>> GetUserRoleAssignmentsAsync(
        string? searchTerm = null,
        string? roleFilter = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var usersQuery = _context.Users
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            var pattern = $"%{term}%";
            usersQuery = usersQuery.Where(u => EF.Functions.ILike(u.UserName!, pattern)
                                            || (u.FullName != null && EF.Functions.ILike(u.FullName, pattern))
                                            || (u.Email != null && EF.Functions.ILike(u.Email, pattern)));
        }

        var users = await usersQuery.OrderBy(u => u.UserName).ToListAsync(cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();

        var employees = await _context.Employees
            .Include(e => e.Department)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var empByUserId = employees.Where(e => !string.IsNullOrEmpty(e.UserId)).ToDictionary(e => e.UserId!, e => e);
        var empById = employees.ToDictionary(e => e.Id, e => e);

        // Get permanent roles mapping
        var userRoles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new
            {
                ur.UserId,
                RoleId = r.Id,
                RoleName = r.Name ?? string.Empty,
                Color = r.ColorHex ?? "#4f46e5",
                r.IsSystem
            })
            .ToListAsync(cancellationToken);

        // Get active/pending temporary role grants
        var tempGrants = await _context.TemporaryRoleAssignments
            .AsNoTracking()
            .Where(t => userIds.Contains(t.UserId) && (t.Status == TemporaryRoleAssignmentStatus.Active || t.Status == TemporaryRoleAssignmentStatus.Pending))
            .OrderByDescending(t => t.StartsAt)
            .ToListAsync(cancellationToken);

        var allRoles = await _context.Roles.AsNoTracking().ToDictionaryAsync(r => r.Id, r => r, cancellationToken);

        var result = new List<UserRoleAssignmentDto>();

        foreach (var u in users)
        {
            var pRoles = userRoles.Where(ur => ur.UserId == u.Id).ToList();

            // Determine base role:
            // If user has Administrator -> Administrator is base
            // Else prioritize Employee role if present
            var baseRoleInfo = pRoles.FirstOrDefault(r => r.RoleName == "Administrator")
                            ?? pRoles.FirstOrDefault(r => r.RoleName == "Employee")
                            ?? pRoles.FirstOrDefault();

            string baseRoleId = baseRoleInfo?.RoleId ?? string.Empty;
            string baseRoleName = baseRoleInfo?.RoleName ?? "Employee";
            string baseRoleColor = baseRoleInfo?.Color ?? "#64748b";

            // Determine secondary role:
            // Check active temporary role first
            var tempGrant = tempGrants.FirstOrDefault(t => t.UserId == u.Id && t.ExpiresAt > now && t.Status == TemporaryRoleAssignmentStatus.Active);
            if (tempGrant == null)
            {
                // Check pending temporary grant
                tempGrant = tempGrants.FirstOrDefault(t => t.UserId == u.Id && t.Status == TemporaryRoleAssignmentStatus.Pending);
            }

            string? secRoleId = null;
            string? secRoleName = null;
            string? secRoleColor = null;
            bool isSecondaryTemp = false;
            int? tempAssignId = null;
            DateTime? secStartsAt = null;
            DateTime? secExpiresAt = null;
            TemporaryRoleAssignmentStatus? tempStatus = null;
            string? grantedBy = null;
            DateTime? grantedAt = null;
            string? reason = null;

            if (tempGrant != null && allRoles.TryGetValue(tempGrant.RoleId, out var tRole))
            {
                secRoleId = tRole.Id;
                secRoleName = tRole.Name;
                secRoleColor = tRole.ColorHex;
                isSecondaryTemp = true;
                tempAssignId = tempGrant.Id;
                secStartsAt = tempGrant.StartsAt;
                secExpiresAt = tempGrant.ExpiresAt;
                tempStatus = tempGrant.Status;
                grantedBy = tempGrant.GrantedBy;
                grantedAt = tempGrant.GrantedAt;
                reason = tempGrant.Reason;
            }
            else
            {
                // Check second permanent role
                var secPerm = pRoles.FirstOrDefault(r => r.RoleId != baseRoleId);
                if (secPerm != null)
                {
                    secRoleId = secPerm.RoleId;
                    secRoleName = secPerm.RoleName;
                    secRoleColor = secPerm.Color;
                    isSecondaryTemp = false;
                }
            }

            // Apply role filter if requested
            if (!string.IsNullOrWhiteSpace(roleFilter))
            {
                if (!string.Equals(baseRoleName, roleFilter, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(secRoleName, roleFilter, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
            }

            var emp = (u.EmployeeId.HasValue && empById.TryGetValue(u.EmployeeId.Value, out var e1)) ? e1 : (empByUserId.TryGetValue(u.Id, out var e2) ? e2 : null);

            result.Add(new UserRoleAssignmentDto
            {
                UserId = u.Id,
                UserName = u.UserName ?? string.Empty,
                FullName = u.FullName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                EmployeeCode = emp?.Code,
                DepartmentName = emp?.Department?.Name,
                BaseRoleId = baseRoleId,
                BaseRoleName = baseRoleName,
                BaseRoleColor = baseRoleColor,
                SecondaryRoleId = secRoleId,
                SecondaryRoleName = secRoleName,
                SecondaryRoleColor = secRoleColor,
                IsSecondaryTemporary = isSecondaryTemp,
                TemporaryAssignmentId = tempAssignId,
                SecondaryStartsAt = secStartsAt,
                SecondaryExpiresAt = secExpiresAt,
                TemporaryStatus = tempStatus,
                GrantedBy = grantedBy,
                GrantedAt = grantedAt,
                Reason = reason
            });
        }

        return result;
    }

    public async Task<(bool Success, string? Error)> AssignSecondaryRoleAsync(
        string userId,
        string roleId,
        string assignedBy,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "Không tìm thấy người dùng.");

        var role = await _roleManager.FindByIdAsync(roleId);
        if (role == null) return (false, "Không tìm thấy vai trò bổ sung.");

        if (role.Name == "Administrator")
        {
            return (false, "Không thể gán vai trò Administrator làm vai trò bổ sung.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains("Administrator"))
        {
            return (false, "Tài khoản Administrator là tài khoản quản trị tối cao, không cần gán thêm vai trò.");
        }

        // Enforce maximum 2 active roles rule:
        var now = DateTime.UtcNow;
        var activeTempCount = await _context.TemporaryRoleAssignments
            .CountAsync(t => t.UserId == userId && t.Status == TemporaryRoleAssignmentStatus.Active && t.ExpiresAt > now, cancellationToken);

        // If user already has 2 active roles (permanent or temporary)
        if (currentRoles.Count + activeTempCount >= 2)
        {
            return (false, "Tài khoản đã có đủ 2 vai trò đang hoạt động (vai trò nền và vai trò bổ sung). Vui lòng thu hồi vai trò bổ sung hiện tại trước khi gán vai trò mới.");
        }

        // Ensure Employee base role exists for normal employees
        if (!currentRoles.Contains("Employee"))
        {
            if (await _roleManager.RoleExistsAsync("Employee"))
            {
                await _userManager.AddToRoleAsync(user, "Employee");
            }
        }

        // Add secondary role
        if (!currentRoles.Contains(role.Name!))
        {
            var addResult = await _userManager.AddToRoleAsync(user, role.Name!);
            if (!addResult.Succeeded)
            {
                return (false, "Lỗi khi gán vai trò: " + string.Join("; ", addResult.Errors.Select(e => e.Description)));
            }
        }

        await _auditService.LogAsync(
            AuditEventType.UserRoleAssigned,
            assignedBy,
            "User",
            user.UserName!,
            $"Cấp vai trò bổ sung vĩnh viễn '{role.Name}' cho tài khoản '{user.UserName}'"
        );

        _permissionService.InvalidateUserPermissions(userId);
        await _userManager.UpdateSecurityStampAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RemoveSecondaryRoleAsync(
        string userId,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return (false, "Không tìm thấy người dùng.");

        var currentRoles = await _userManager.GetRolesAsync(user);
        var secondaryRoles = currentRoles.Where(r => r != "Employee" && r != "Administrator").ToList();

        if (!secondaryRoles.Any())
        {
            return (false, "Tài khoản không có vai trò bổ sung vĩnh viễn nào để thu hồi.");
        }

        foreach (var r in secondaryRoles)
        {
            await _userManager.RemoveFromRoleAsync(user, r);
            await _auditService.LogAsync(
                AuditEventType.UserRoleRevoked,
                revokedBy,
                "User",
                user.UserName!,
                $"Thu hồi vai trò bổ sung vĩnh viễn '{r}' của tài khoản '{user.UserName}'"
            );
        }

        _permissionService.InvalidateUserPermissions(userId);
        await _userManager.UpdateSecurityStampAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> GrantTemporaryRoleAsync(
        GrantTemporaryRoleCommand command,
        string grantedBy,
        CancellationToken cancellationToken = default)
    {
        // 1. Validate hard limit: Max 2 hours (120 minutes)
        if (command.DurationMinutes <= 0)
        {
            return (false, "Thời hạn cấp quyền tạm thời phải lớn hơn 0.");
        }

        if (command.DurationMinutes > 120)
        {
            return (false, "Thời gian cấp quyền tạm thời tối đa là 2 giờ (120 phút). Không được vượt quá giới hạn an toàn.");
        }

        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            return (false, "Vui lòng nhập lý do cấp quyền tạm thời.");
        }

        var user = await _userManager.FindByIdAsync(command.UserId);
        if (user == null) return (false, "Không tìm thấy người dùng.");

        var role = await _roleManager.FindByIdAsync(command.RoleId);
        if (role == null) return (false, "Không tìm thấy vai trò để cấp quyền.");

        if (role.Name == "Administrator")
        {
            return (false, "Không thể cấp quyền tạm thời vai trò Administrator.");
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains("Administrator"))
        {
            return (false, "Tài khoản Administrator là Quản trị viên tối cao, không cần cấp quyền tạm thời.");
        }

        // 2. Enforce maximum 2 active roles rule
        var now = DateTime.UtcNow;
        var activeTempCount = await _context.TemporaryRoleAssignments
            .CountAsync(t => t.UserId == command.UserId && t.Status == TemporaryRoleAssignmentStatus.Active && t.ExpiresAt > now, cancellationToken);

        if (currentRoles.Count + activeTempCount >= 2)
        {
            return (false, "Tài khoản hiện đã có đủ 2 vai trò đang hoạt động. Không thể cấp thêm quyền tạm thời khi chưa thu hồi hoặc hết hạn vai trò bổ sung hiện tại.");
        }

        // 3. Calculate start and expiration times
        var startsAt = command.StartsAt.HasValue && command.StartsAt.Value > now
            ? DateTime.SpecifyKind(command.StartsAt.Value, DateTimeKind.Utc)
            : now;

        var expiresAt = startsAt.AddMinutes(command.DurationMinutes);

        var assignment = new TemporaryRoleAssignment
        {
            UserId = command.UserId,
            RoleId = command.RoleId,
            StartsAt = startsAt,
            ExpiresAt = expiresAt,
            Reason = command.Reason.Trim(),
            Status = startsAt <= now ? TemporaryRoleAssignmentStatus.Active : TemporaryRoleAssignmentStatus.Pending,
            GrantedAt = now,
            GrantedBy = grantedBy
        };

        _context.TemporaryRoleAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            AuditEventType.TemporaryRoleGranted,
            grantedBy,
            "User",
            user.UserName!,
            $"Cấp quyền tạm thời vai trò '{role.Name}' trong {command.DurationMinutes} phút (Hiệu lực: {startsAt:dd/MM/yyyy HH:mm} - {expiresAt:dd/MM/yyyy HH:mm} UTC). Lý do: {command.Reason.Trim()}"
        );

        _permissionService.InvalidateUserPermissions(command.UserId);
        await _userManager.UpdateSecurityStampAsync(user);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> RevokeTemporaryRoleAsync(
        int assignmentId,
        string revokedBy,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _context.TemporaryRoleAssignments.FirstOrDefaultAsync(t => t.Id == assignmentId, cancellationToken);
        if (assignment == null) return (false, "Không tìm thấy bản ghi cấp quyền tạm thời.");

        if (assignment.Status == TemporaryRoleAssignmentStatus.Revoked || assignment.Status == TemporaryRoleAssignmentStatus.Expired)
        {
            return (true, null);
        }

        assignment.Status = TemporaryRoleAssignmentStatus.Revoked;
        assignment.RevokedAt = DateTime.UtcNow;
        assignment.RevokedBy = revokedBy;
        await _context.SaveChangesAsync(cancellationToken);

        var role = await _roleManager.FindByIdAsync(assignment.RoleId);
        var roleName = role?.Name ?? assignment.RoleId;

        var user = await _userManager.FindByIdAsync(assignment.UserId);
        var username = user?.UserName ?? assignment.UserId;

        await _auditService.LogAsync(
            AuditEventType.TemporaryRoleRevoked,
            revokedBy,
            "User",
            username,
            $"Thu hồi chủ động quyền tạm thời vai trò '{roleName}' của tài khoản '{username}'"
        );

        _permissionService.InvalidateUserPermissions(assignment.UserId);
        if (user != null)
        {
            await _userManager.UpdateSecurityStampAsync(user);
        }

        return (true, null);
    }

    public async Task<List<TemporaryRoleAssignmentItemDto>> GetTemporaryRoleAssignmentsAsync(CancellationToken cancellationToken = default)
    {
        var assignments = await _context.TemporaryRoleAssignments
            .AsNoTracking()
            .OrderByDescending(t => t.GrantedAt)
            .ToListAsync(cancellationToken);

        if (!assignments.Any()) return [];

        var userIds = assignments.Select(t => t.UserId).Distinct().ToList();
        var roleIds = assignments.Select(t => t.RoleId).Distinct().ToList();

        var users = await _context.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u, cancellationToken);
        var roles = await _context.Roles.AsNoTracking().Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r, cancellationToken);

        var now = DateTime.UtcNow;

        return assignments.Select(t =>
        {
            var user = users.GetValueOrDefault(t.UserId);
            var role = roles.GetValueOrDefault(t.RoleId);

            var status = t.Status;
            // On-the-fly status calculation if expired in real time
            if (status == TemporaryRoleAssignmentStatus.Active && t.ExpiresAt <= now)
            {
                status = TemporaryRoleAssignmentStatus.Expired;
            }

            return new TemporaryRoleAssignmentItemDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = user?.UserName ?? t.UserId,
                FullName = user?.FullName ?? string.Empty,
                RoleId = t.RoleId,
                RoleName = role?.Name ?? t.RoleId,
                RoleColor = role?.ColorHex ?? "#4f46e5",
                StartsAt = t.StartsAt,
                ExpiresAt = t.ExpiresAt,
                DurationMinutes = (int)Math.Round((t.ExpiresAt - t.StartsAt).TotalMinutes),
                Reason = t.Reason,
                Status = status,
                GrantedAt = t.GrantedAt,
                GrantedBy = t.GrantedBy,
                RevokedAt = t.RevokedAt,
                RevokedBy = t.RevokedBy
            };
        }).ToList();
    }
}
