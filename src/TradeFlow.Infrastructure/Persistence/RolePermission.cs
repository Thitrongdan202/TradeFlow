using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Persistence;

/// <summary>
/// Granular permission assigned to a role.
/// Permissions are scoped to a resource + action pair.
/// Not a fixed enum — new resources can be added without code changes.
/// </summary>
public class RolePermission
{
    public int Id { get; set; }

    /// <summary>FK: Vai trò được gán quyền</summary>
    public string RoleId { get; set; } = string.Empty;
    public ApplicationRole Role { get; set; } = null!;

    /// <summary>Tài nguyên (resource)</summary>
    public ResourceType Resource { get; set; }

    /// <summary>Hành động (action)</summary>
    public PermissionAction Action { get; set; }

    /// <summary>Cho phép hay không</summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>Ngày gán quyền (UTC)</summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Người gán quyền</summary>
    public string? GrantedBy { get; set; }
}
