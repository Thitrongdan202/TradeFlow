using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Support;

/// <summary>
/// Yêu cầu hỗ trợ / Báo cáo sự cố nội bộ trong TradeFlow
/// </summary>
public class SupportTicket : AuditableEntity<int>
{
    public SupportTicket() { }

    /// <summary>Mã yêu cầu hỗ trợ duy nhất (ví dụ: SUP-2026-000001)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Loại yêu cầu</summary>
    public SupportCategory Category { get; set; } = SupportCategory.SystemError;

    /// <summary>Tiêu đề ngắn gọn</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Phân hệ / Module liên quan</summary>
    public SupportRelatedModule RelatedModule { get; set; } = SupportRelatedModule.Other;

    /// <summary>Mô tả chi tiết nội dung sự cố, các bước tái hiện, kết quả thực tế và mong muốn</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Mức độ ưu tiên</summary>
    public SupportPriority Priority { get; set; } = SupportPriority.Normal;

    /// <summary>Trạng thái xử lý hiện tại</summary>
    public SupportStatus Status { get; set; } = SupportStatus.New;

    /// <summary>Lý do từ chối tiếp nhận (nếu có)</summary>
    public string? RejectionReason { get; set; }

    // === Tự động ghi nhận thông tin hệ thống ===

    /// <summary>ID tài khoản người gửi (Identity UserId)</summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>Tên tài khoản người gửi (Username)</summary>
    public string CreatedByUserName { get; set; } = string.Empty;

    /// <summary>Họ tên nhân viên liên kết</summary>
    public string? SubmitterFullName { get; set; }

    /// <summary>Vai trò / Quyền hạn người gửi tại thời điểm tạo đơn</summary>
    public string? SubmitterRole { get; set; }

    /// <summary>Phòng ban của nhân viên gửi (nếu có)</summary>
    public string? SubmitterDepartment { get; set; }

    /// <summary>ID nhân viên liên kết (nếu có)</summary>
    public int? EmployeeId { get; set; }

    /// <summary>Route / Đường dẫn trang xảy ra sự cố</summary>
    public string? CurrentRoute { get; set; }

    /// <summary>Phiên bản ứng dụng</summary>
    public string? AppVersion { get; set; } = "TradeFlow v2.4 Enterprise";

    // === Thời điểm vòng đời ===

    /// <summary>Thời điểm giải quyết (UTC)</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Thời điểm đóng (UTC)</summary>
    public DateTime? ClosedAt { get; set; }

    // === Phân công xử lý ===

    /// <summary>ID người được phân công xử lý (Manager / Admin)</summary>
    public string? AssignedToUserId { get; set; }

    /// <summary>Username người được phân công</summary>
    public string? AssignedToUserName { get; set; }

    /// <summary>Họ tên người được phân công</summary>
    public string? AssignedToFullName { get; set; }

    // === Navigation Collections ===

    /// <summary>Danh sách tệp đính kèm trong báo cáo</summary>
    public ICollection<SupportAttachment> Attachments { get; set; } = [];

    /// <summary>Lịch sử trao đổi / phản hồi / ghi chú nội bộ</summary>
    public ICollection<SupportComment> Comments { get; set; } = [];

    /// <summary>Nhật ký kiểm toán thay đổi trạng thái và hành động</summary>
    public ICollection<SupportAuditLog> AuditLogs { get; set; } = [];
}
