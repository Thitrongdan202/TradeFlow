using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Security;

/// <summary>
/// Mã kích hoạt danh tính người ký số sử dụng một lần (One-Time Activation Credential)
/// </summary>
public class SignerEnrollmentCode : AuditableEntity<int>
{
    public SignerEnrollmentCode() { }

    /// <summary>Mã băm SHA-256 của mã kích hoạt (Tuyệt đối KHÔNG lưu mã plaintext trong DB!)</summary>
    public string CodeHash { get; set; } = string.Empty;

    /// <summary>ID người dùng được cấp quyền (ApplicationUser.Id)</summary>
    public string TargetUserId { get; set; } = string.Empty;

    /// <summary>Tên tài khoản người dùng được cấp quyền</summary>
    public string TargetUserName { get; set; } = string.Empty;

    /// <summary>Họ và tên người được cấp quyền</summary>
    public string TargetFullName { get; set; } = string.Empty;

    /// <summary>Chức vụ được chỉ định</summary>
    public string TargetPosition { get; set; } = string.Empty;

    /// <summary>Vai trò thẩm quyền ký được chỉ định</summary>
    public SignerRole TargetSignerRole { get; set; } = SignerRole.AuthorizedSigner;

    /// <summary>Thời điểm hết hạn của mã (UTC)</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Đã được sử dụng chưa</summary>
    public bool IsUsed { get; set; }

    /// <summary>Thời điểm sử dụng (UTC)</summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>Người sử dụng mã</summary>
    public string? UsedBy { get; set; }

    /// <summary>Mã đã bị hủy bỏ/thu hồi</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Thời điểm hủy mã</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>Người hủy mã</summary>
    public string? RevokedBy { get; set; }

    /// <summary>Lý do hủy mã</summary>
    public string? RevocationReason { get; set; }
}
