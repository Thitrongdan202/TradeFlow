using Microsoft.AspNetCore.Identity;

namespace TradeFlow.Infrastructure.Persistence;

/// <summary>
/// Extended ApplicationUser with additional profile fields.
/// Extends ASP.NET Core Identity's IdentityUser.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Họ và tên đầy đủ</summary>
    public string? FullName { get; set; }

    /// <summary>Trạng thái tài khoản</summary>
    public Domain.Enums.UserStatus Status { get; set; } = Domain.Enums.UserStatus.Active;

    /// <summary>Ngày tạo tài khoản (UTC)</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Người tạo (username)</summary>
    public string? CreatedBy { get; set; }

    /// <summary>Lần đăng nhập gần nhất (UTC)</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Mã định danh hồ sơ nhân viên liên kết (1-1 tùy chọn).
    /// </summary>
    public int? EmployeeId { get; set; }

    /// <summary>
    /// Cờ bắt buộc đổi mật khẩu khi đăng nhập lần đầu hoặc sau khi được cấp lại mật khẩu.
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    /// <summary>
    /// Thời điểm đổi mật khẩu lần gần nhất (UTC).
    /// </summary>
    public DateTime? PasswordChangedAt { get; set; }

    /// <summary>
    /// Mật khẩu được mã hóa đối xứng AES-256 an toàn phục vụ kho thông tin xác thực ROOT.
    /// Tuyệt đối KHÔNG lưu trữ plaintext.
    /// </summary>
    public string? EncryptedPassword { get; set; }

    // Navigation
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
}
