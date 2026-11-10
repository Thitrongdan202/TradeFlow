using System;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Security;

/// <summary>
/// Thực thể cấp quyền truy cập tạm thời cho tài khoản (giới hạn tối đa 2 giờ).
/// Được kiểm tra trực tiếp khi tính quyền hiệu lực và tự động xử lý bởi BackgroundService.
/// </summary>
public class TemporaryRoleAssignment
{
    public int Id { get; set; }

    /// <summary>ID người dùng được cấp quyền tạm thời</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>ID vai trò được cấp</summary>
    public string RoleId { get; set; } = string.Empty;

    /// <summary>Thời điểm bắt đầu có hiệu lực (UTC)</summary>
    public DateTime StartsAt { get; set; }

    /// <summary>Thời điểm hết hạn (UTC, tối đa 2 giờ kể từ StartsAt)</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Lý do cấp quyền tạm thời</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Trạng thái phân quyền tạm thời</summary>
    public TemporaryRoleAssignmentStatus Status { get; set; } = TemporaryRoleAssignmentStatus.Active;

    /// <summary>Thời điểm cấp (UTC)</summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Người cấp quyền (username)</summary>
    public string? GrantedBy { get; set; }

    /// <summary>Thời điểm bị thu hồi nếu Quản trị viên thu hồi thủ công (UTC)</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Người thu hồi quyền (username)</summary>
    public string? RevokedBy { get; set; }
}
