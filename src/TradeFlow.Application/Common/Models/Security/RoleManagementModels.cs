using System;
using System.Collections.Generic;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Security;

public class RoleOverviewDto
{
    public int TotalRoles { get; set; }
    public int TotalAssignedUsers { get; set; }
    public int ActiveTemporaryRoles { get; set; }
    public int AttentionPermissionsCount { get; set; }
    public List<PermissionActivityDto> RecentActivities { get; set; } = [];
}

public class PermissionActivityDto
{
    public int Id { get; set; }
    public string Actor { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public AuditEventType EventType { get; set; }
    public string EventTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Succeeded { get; set; } = true;
}

public class RoleListItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ColorHex { get; set; } = "#4f46e5";
    public string Icon { get; set; } = "shield";
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public int GrantedPermissionsCount { get; set; }
    public int ActiveUsersCount { get; set; }
}

public class RoleDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ColorHex { get; set; } = "#4f46e5";
    public string Icon { get; set; } = "shield";
    public bool IsActive { get; set; } = true;
    public bool IsSystem { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public List<RolePermissionItemDto> Permissions { get; set; } = [];
    public List<UserSummaryDto> AssignedUsers { get; set; } = [];
}

public class UserSummaryDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public bool IsTemporary { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateRoleCommand
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ColorHex { get; set; } = "#4f46e5";
    public string Icon { get; set; } = "shield";
    public string? InheritFromRoleId { get; set; }
}

public class UpdateRoleCommand
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ColorHex { get; set; } = "#4f46e5";
    public string Icon { get; set; } = "shield";
    public bool IsActive { get; set; } = true;
}

public class RolePermissionItemDto
{
    public ResourceType Resource { get; set; }
    public PermissionAction Action { get; set; }
    public bool IsGranted { get; set; } = true;
    public DataScope Scope { get; set; } = DataScope.All;
}

public class UserRoleAssignmentDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? EmployeeCode { get; set; }
    public string? DepartmentName { get; set; }
    public string BaseRoleId { get; set; } = string.Empty;
    public string BaseRoleName { get; set; } = "Employee";
    public string BaseRoleColor { get; set; } = "#64748b";
    public string? SecondaryRoleId { get; set; }
    public string? SecondaryRoleName { get; set; }
    public string? SecondaryRoleColor { get; set; }
    public bool IsSecondaryTemporary { get; set; }
    public int? TemporaryAssignmentId { get; set; }
    public DateTime? SecondaryStartsAt { get; set; }
    public DateTime? SecondaryExpiresAt { get; set; }
    public TemporaryRoleAssignmentStatus? TemporaryStatus { get; set; }
    public string? GrantedBy { get; set; }
    public DateTime? GrantedAt { get; set; }
    public string? Reason { get; set; }
}

public class GrantTemporaryRoleCommand
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 60; // Max 120 mins
    public string Reason { get; set; } = string.Empty;
    public DateTime? StartsAt { get; set; }
}

public class TemporaryRoleAssignmentItemDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string RoleColor { get; set; } = "#4f46e5";
    public DateTime StartsAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int DurationMinutes { get; set; }
    public string Reason { get; set; } = string.Empty;
    public TemporaryRoleAssignmentStatus Status { get; set; }
    public DateTime GrantedAt { get; set; }
    public string? GrantedBy { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? RevokedBy { get; set; }
}
