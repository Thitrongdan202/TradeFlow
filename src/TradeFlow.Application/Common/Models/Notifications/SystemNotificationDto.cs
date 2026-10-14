using System;

namespace TradeFlow.Application.Common.Models.Notifications;

public class SystemNotificationDto
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "system"; // system, order, invoice, warning, info
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }
    public string? TargetUrl { get; set; }

    // Approval Workflow Details
    public bool IsApprovalRequest { get; set; }
    public string? RequestType { get; set; } // "EmployeeRequest", "PriceList"
    public string? RequestTypeName { get; set; } // "Đề xuất thêm nhân sự", "Bảng giá chờ duyệt"
    public int? EntityId { get; set; }
    public string? RequestCode { get; set; }
    public string? Requester { get; set; }
    public string? Status { get; set; }
    public bool CanApprove { get; set; }
    public bool IsSelfRequest { get; set; }
}
