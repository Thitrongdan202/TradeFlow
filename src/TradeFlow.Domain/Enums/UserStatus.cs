namespace TradeFlow.Domain.Enums;

/// <summary>Trạng thái tài khoản người dùng</summary>
public enum UserStatus
{
    /// <summary>Đang hoạt động</summary>
    Active = 1,

    /// <summary>Bị khóa</summary>
    Locked = 2,

    /// <summary>Vô hiệu hóa</summary>
    Disabled = 3,

    /// <summary>Chờ đổi mật khẩu lần đầu</summary>
    MustChangePassword = 4,

    /// <summary>Đã thôi việc / Chấm dứt hợp đồng</summary>
    Terminated = 5
}

