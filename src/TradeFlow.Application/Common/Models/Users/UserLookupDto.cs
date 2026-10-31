namespace TradeFlow.Application.Common.Models.Users;

/// <summary>
/// DTO nhẹ đại diện người dùng cho các danh sách lựa chọn (dropdown) liên kết tài khoản.
/// Tránh nạp toàn bộ thực thể IdentityUser/ApplicationUser cùng các trường nhạy cảm/nặng.
/// </summary>
public class UserLookupDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
