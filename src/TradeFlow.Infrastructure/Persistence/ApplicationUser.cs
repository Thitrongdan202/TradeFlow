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
    /// Mật khẩu được mã hóa đảo ngược để quản trị viên xem được.
    /// KHÔNG bao giờ lưu mật khẩu plaintext.
    /// Key mã hóa phải đến từ cấu hình bảo mật, không lưu trong DB.
    /// </summary>
    public string? EncryptedPassword { get; set; }

    // Navigation
    public ICollection<ApplicationUserRole> UserRoles { get; set; } = [];
}
