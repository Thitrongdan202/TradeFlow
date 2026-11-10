using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Support;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Giao diện xử lý nghiệp vụ Báo cáo sự cố & Hỗ trợ nội bộ trong TradeFlow
/// </summary>
public interface ISupportService
{
    /// <summary>
    /// Gửi một yêu cầu hỗ trợ mới kèm tệp đính kèm và tự động ghi nhận metadata hệ thống
    /// </summary>
    Task<SupportTicketDetailDto> CreateTicketAsync(CreateSupportTicketCommand command, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Lấy danh sách báo cáo hỗ trợ theo bộ lọc và phân quyền RBAC
    /// </summary>
    Task<List<SupportTicketSummaryDto>> GetTicketsAsync(SupportTicketFilter filter, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết báo cáo hỗ trợ theo ID (bảo vệ quyền truy cập và ẩn ghi chú nội bộ với nhân viên)
    /// </summary>
    Task<SupportTicketDetailDto?> GetTicketByIdAsync(int id, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Lấy chi tiết báo cáo hỗ trợ theo mã code (SUP-2026-000001)
    /// </summary>
    Task<SupportTicketDetailDto?> GetTicketByCodeAsync(string code, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Thêm phản hồi / bình luận hoặc ghi chú nội bộ vào báo cáo
    /// </summary>
    Task<SupportCommentDto> AddCommentAsync(AddSupportCommentCommand command, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật trạng thái yêu cầu hỗ trợ (Tiếp nhận, Đang xử lý, Chờ phản hồi, Giải quyết, Đóng, Từ chối)
    /// </summary>
    Task<bool> UpdateTicketStatusAsync(UpdateSupportTicketStatusCommand command, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Phân công người xử lý yêu cầu (dành cho Manager / Admin)
    /// </summary>
    Task<bool> AssignTicketAsync(AssignSupportTicketCommand command, ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Thống kê tổng quan số lượng báo cáo theo phạm vi quyền hạn của người dùng
    /// </summary>
    Task<SupportStatsDto> GetStatsAsync(ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Lấy số lượng báo cáo cần hành động / phản hồi mới để hiển thị badge trên Header
    /// </summary>
    Task<int> GetBadgeCountAsync(ClaimsPrincipal user, CancellationToken ct = default);

    /// <summary>
    /// Lấy thông tin tệp đính kèm và kiểm tra quyền trước khi tải xuống / preview
    /// </summary>
    Task<(SupportAttachmentDto? Attachment, System.IO.Stream? Stream)> GetAttachmentStreamAsync(int attachmentId, ClaimsPrincipal user, CancellationToken ct = default);
}
