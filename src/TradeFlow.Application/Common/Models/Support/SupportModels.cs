using System;
using System.Collections.Generic;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Models.Support;

/// <summary>
/// DTO tải lên tệp đính kèm trong báo cáo sự cố hoặc bình luận
/// </summary>
public class SupportFileUploadDto
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public byte[] Data { get; set; } = [];
}

/// <summary>
/// Lệnh gửi báo cáo sự cố / yêu cầu hỗ trợ mới
/// </summary>
public class CreateSupportTicketCommand
{
    public SupportCategory Category { get; set; } = SupportCategory.SystemError;
    public string Title { get; set; } = string.Empty;
    public SupportRelatedModule RelatedModule { get; set; } = SupportRelatedModule.Other;
    public string Description { get; set; } = string.Empty;
    public SupportPriority Priority { get; set; } = SupportPriority.Normal;
    public string? CurrentRoute { get; set; }
    public List<SupportFileUploadDto> Files { get; set; } = [];
}

/// <summary>
/// Thông tin tóm tắt báo cáo hỗ trợ hiển thị trên bảng danh sách
/// </summary>
public class SupportTicketSummaryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public SupportCategory Category { get; set; }
    public string CategoryName => Category.ToDisplayName();
    public string Title { get; set; } = string.Empty;
    public SupportRelatedModule RelatedModule { get; set; }
    public string RelatedModuleName => RelatedModule.ToDisplayName();
    public SupportPriority Priority { get; set; }
    public string PriorityName => Priority.ToDisplayName();
    public SupportStatus Status { get; set; }
    public string StatusName => Status.ToDisplayName();
    public string CreatedByUserId { get; set; } = string.Empty;
    public string CreatedByUserName { get; set; } = string.Empty;
    public string? SubmitterFullName { get; set; }
    public string? SubmitterRole { get; set; }
    public string? SubmitterDepartment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int AttachmentCount { get; set; }
    public int CommentCount { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string? AssignedToFullName { get; set; }
}

/// <summary>
/// Chi tiết toàn bộ báo cáo hỗ trợ bao gồm trao đổi, tệp đính kèm và kiểm toán
/// </summary>
public class SupportTicketDetailDto : SupportTicketSummaryDto
{
    public string Description { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public string? CurrentRoute { get; set; }
    public string? AppVersion { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    public List<SupportAttachmentDto> Attachments { get; set; } = [];
    public List<SupportCommentDto> Comments { get; set; } = [];
    public List<SupportAuditLogDto> AuditLogs { get; set; } = [];
}

/// <summary>
/// DTO thông tin tệp đính kèm
/// </summary>
public class SupportAttachmentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int? CommentId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string UploadedByUserId { get; set; } = string.Empty;
    public string UploadedByUserName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string DownloadUrl => $"/api/support/attachments/{Id}/download";
    public string PreviewUrl => $"/api/support/attachments/{Id}/preview";
    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    public string FormattedSize
    {
        get
        {
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
            return $"{FileSizeBytes / (1024.0 * 1024.0):F2} MB";
        }
    }
}

/// <summary>
/// DTO phản hồi / bình luận / ghi chú nội bộ
/// </summary>
public class SupportCommentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string AuthorUserId { get; set; } = string.Empty;
    public string AuthorUserName { get; set; } = string.Empty;
    public string AuthorFullName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternalNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<SupportAttachmentDto> Attachments { get; set; } = [];
}

/// <summary>
/// Lệnh gửi bình luận / phản hồi mới
/// </summary>
public class AddSupportCommentCommand
{
    public int TicketId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternalNote { get; set; } = false;
    public List<SupportFileUploadDto> Files { get; set; } = [];
}

/// <summary>
/// DTO nhật ký kiểm toán yêu cầu hỗ trợ
/// </summary>
public class SupportAuditLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedByUserName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Lệnh cập nhật trạng thái yêu cầu
/// </summary>
public class UpdateSupportTicketStatusCommand
{
    public int TicketId { get; set; }
    public SupportStatus NewStatus { get; set; }
    public string? ReasonOrNotes { get; set; }
}

/// <summary>
/// Lệnh phân công xử lý yêu cầu
/// </summary>
public class AssignSupportTicketCommand
{
    public int TicketId { get; set; }
    public string AssignToUserId { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

/// <summary>
/// Thống kê tổng hợp số lượng báo cáo theo trạng thái
/// </summary>
public class SupportStatsDto
{
    public int TotalTickets { get; set; }
    public int NewTickets { get; set; }
    public int InProgressTickets { get; set; }
    public int WaitingCustomerTickets { get; set; }
    public int ResolvedTickets { get; set; }
    public int ClosedTickets { get; set; }
    public int RejectedTickets { get; set; }
}

/// <summary>
/// Bộ lọc danh sách báo cáo
/// </summary>
public class SupportTicketFilter
{
    public string? SearchKeyword { get; set; }
    public SupportCategory? Category { get; set; }
    public SupportRelatedModule? RelatedModule { get; set; }
    public SupportPriority? Priority { get; set; }
    public SupportStatus? Status { get; set; }
    public string? Department { get; set; }
    public bool OnlyMine { get; set; } = false;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? CreatedByUserId { get; set; }
    public string? SortBy { get; set; }
}

/// <summary>
/// Tiện ích chuyển đổi Enum sang tên hiển thị Tiếng Việt
/// </summary>
public static class SupportEnumExtensions
{
    public static string ToDisplayName(this SupportCategory category) => category switch
    {
        SupportCategory.SystemError => "Lỗi hệ thống",
        SupportCategory.OperationError => "Lỗi thao tác",
        SupportCategory.UiDisplayError => "Lỗi hiển thị giao diện",
        SupportCategory.DataInaccurate => "Dữ liệu không chính xác",
        SupportCategory.FeatureInaccessible => "Không truy cập được chức năng",
        SupportCategory.UsageSupport => "Yêu cầu hỗ trợ sử dụng",
        SupportCategory.ImprovementProposal => "Đề xuất cải tiến",
        SupportCategory.Other => "Khác",
        _ => "Khác"
    };

    public static string ToDisplayName(this SupportRelatedModule module) => module switch
    {
        SupportRelatedModule.Attendance => "Chấm công",
        SupportRelatedModule.Sales => "Bán hàng",
        SupportRelatedModule.Invoices => "Hóa đơn",
        SupportRelatedModule.Products => "Sản phẩm",
        SupportRelatedModule.Warehouse => "Kho hàng",
        SupportRelatedModule.Partners => "Đối tác",
        SupportRelatedModule.Contracts => "Hợp đồng",
        SupportRelatedModule.Documents => "Chứng từ",
        SupportRelatedModule.Employees => "Nhân sự",
        SupportRelatedModule.Settings => "Cài đặt",
        SupportRelatedModule.Other => "Khác",
        _ => "Khác"
    };

    public static string ToDisplayName(this SupportPriority priority) => priority switch
    {
        SupportPriority.Normal => "Bình thường",
        SupportPriority.Medium => "Cần xử lý sớm",
        SupportPriority.Urgent => "Khẩn cấp",
        _ => "Bình thường"
    };

    public static string ToDisplayName(this SupportStatus status) => status switch
    {
        SupportStatus.New => "Mới gửi",
        SupportStatus.Acknowledged => "Đã tiếp nhận",
        SupportStatus.InProgress => "Đang xử lý",
        SupportStatus.WaitingForCustomer => "Chờ phản hồi từ người gửi",
        SupportStatus.Resolved => "Đã giải quyết",
        SupportStatus.Closed => "Đã đóng",
        SupportStatus.Rejected => "Từ chối tiếp nhận",
        _ => "Mới gửi"
    };
}
