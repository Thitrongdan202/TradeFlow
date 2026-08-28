using Microsoft.AspNetCore.Identity;

namespace TradeFlow.Infrastructure.Persistence;

/// <summary>
/// Extended IdentityRole with dynamic role support.
/// Roles are NOT hard-coded - administrators can create any role name.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }

    /// <summary>Mô tả vai trò</summary>
    public string? Description { get; set; }

    /// <summary>Vai trò hệ thống (không thể xóa)</summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Ngày tạo (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Người tạo (username)</summary>
    public string? CreatedBy { get; set; }

    // Navigation
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];

    /// <summary>Quyền được gán cho vai trò này</summary>
    public ICollection<RolePermission> Permissions { get; set; } = [];
}
