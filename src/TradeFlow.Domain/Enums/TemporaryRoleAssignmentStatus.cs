namespace TradeFlow.Domain.Enums;

/// <summary>
/// Trạng thái của quyền truy cập tạm thời
/// </summary>
public enum TemporaryRoleAssignmentStatus
{
    /// <summary>Chờ hiệu lực (thời điểm bắt đầu trong tương lai)</summary>
    Pending = 1,

    /// <summary>Đang hiệu lực</summary>
    Active = 2,

    /// <summary>Đã hết hạn tự động</summary>
    Expired = 3,

    /// <summary>Đã bị thu hồi chủ động bởi Quản trị viên</summary>
    Revoked = 4
}
