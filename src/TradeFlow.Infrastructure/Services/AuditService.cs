using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Domain.Entities.Users;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Infrastructure.Services;

/// <summary>
/// Concrete implementation of IAuditService.
/// Writes audit log entries to the database.
/// IMPORTANT: Never logs plaintext passwords.
/// </summary>
public class AuditService : IAuditService
{
    private readonly Persistence.TradeFlowDbContext _dbContext;
    private readonly IUserActivityBroadcaster? _broadcaster;

    public AuditService(
        Persistence.TradeFlowDbContext dbContext,
        IUserActivityBroadcaster? broadcaster = null)
    {
        _dbContext = dbContext;
        _broadcaster = broadcaster;
    }

    public async Task LogAsync(
        AuditEventType eventType,
        string? performedBy = null,
        string? targetEntity = null,
        string? targetId = null,
        string? details = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLog(
            eventType,
            performedBy,
            targetEntity,
            targetId,
            details,
            ipAddress,
            userAgent);

        _dbContext.AuditLogs.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Broadcast non-sensitive operational activity in real time
        if (_broadcaster != null && IsOperationalEvent(eventType))
        {
            var title = GetEventTitle(eventType);
            var desc = GetEventDescription(entry);
            var targetUrl = GetTargetUrl(targetEntity);

            _broadcaster.Broadcast(new ActivityToastNotification(
                EventId: entry.Id.ToString(),
                PerformedBy: performedBy ?? "Người dùng hệ thống",
                EventType: eventType,
                Title: title,
                Description: desc,
                TargetUrl: targetUrl,
                OccurredAt: entry.OccurredAt
            ));
        }
    }

    private static bool IsOperationalEvent(AuditEventType type)
    {
        return type switch
        {
            AuditEventType.SalesOrderCreated or
            AuditEventType.SalesOrderConfirmed or
            AuditEventType.SalesOrderUpdated or
            AuditEventType.InvoiceCreated or
            AuditEventType.InvoiceIssued or
            AuditEventType.QuotationCreated or
            AuditEventType.QuotationConverted or
            AuditEventType.ProductCreated or
            AuditEventType.CustomerCreated or
            AuditEventType.SupplierCreated or
            AuditEventType.ContractCreated or
            AuditEventType.ContractSigned or
            AuditEventType.ContractFinalized or
            AuditEventType.EmployeeRequestSubmitted or
            AuditEventType.EmployeeRequestApproved or
            AuditEventType.AttendanceAdjustmentSubmitted or
            AuditEventType.AttendanceAdjustmentApproved or
            AuditEventType.AttendanceAdjustmentRejected or
            AuditEventType.LeaveRequestSubmitted or
            AuditEventType.LeaveRequestApproved or
            AuditEventType.LeaveRequestRejected or
            AuditEventType.ShiftChangeSubmitted or
            AuditEventType.ShiftChangeApproved or
            AuditEventType.RoleCreated or
            AuditEventType.RoleUpdated or
            AuditEventType.RoleDeleted or
            AuditEventType.WarehouseCreated or
            AuditEventType.PriceListCreated or
            AuditEventType.PriceListApproved or
            AuditEventType.PasswordReset or
            AuditEventType.PasswordChanged or
            AuditEventType.TemporaryPasswordIssued or
            AuditEventType.ForcedPasswordChange or
            AuditEventType.SessionRevoked or
            AuditEventType.AccountDisabled or
            AuditEventType.SuspiciousLogin or
            AuditEventType.UserLocked or
            AuditEventType.UserUnlocked => true,
            _ => false
        };
    }

    private static string GetEventTitle(AuditEventType type)
    {
        return type switch
        {
            AuditEventType.SalesOrderCreated => "Đơn bán hàng mới",
            AuditEventType.SalesOrderConfirmed => "Đơn bán hàng đã xác nhận",
            AuditEventType.SalesOrderUpdated => "Cập nhật đơn bán hàng",
            AuditEventType.InvoiceCreated => "Hóa đơn mới được tạo",
            AuditEventType.InvoiceIssued => "Hóa đơn đã phát hành",
            AuditEventType.QuotationCreated => "Báo giá mới được tạo",
            AuditEventType.QuotationConverted => "Báo giá đã chuyển thành đơn hàng",
            AuditEventType.ProductCreated => "Sản phẩm mới trong danh mục",
            AuditEventType.CustomerCreated => "Khách hàng mới được thêm",
            AuditEventType.SupplierCreated => "Nhà cung cấp mới",
            AuditEventType.ContractCreated => "Hợp đồng kinh tế mới",
            AuditEventType.ContractSigned => "Hợp đồng đã ký điện tử",
            AuditEventType.ContractFinalized => "Hợp đồng đã hoàn tất",
            AuditEventType.EmployeeRequestSubmitted => "Đề xuất nhân sự mới",
            AuditEventType.EmployeeRequestApproved => "Đề xuất nhân sự đã duyệt",
            AuditEventType.WarehouseCreated => "Kho hàng mới được thêm",
            AuditEventType.PriceListCreated => "Bảng giá mới được tạo",
            AuditEventType.PriceListApproved => "Bảng giá đã được duyệt",
            AuditEventType.AttendanceClockIn => "Chấm công vào ca",
            AuditEventType.AttendanceClockOut => "Chấm công ra ca",
            AuditEventType.AttendanceAdjustmentSubmitted => "Yêu cầu điều chỉnh công mới",
            AuditEventType.AttendanceAdjustmentApproved => "Yêu cầu điều chỉnh công đã duyệt",
            AuditEventType.AttendanceAdjustmentRejected => "Yêu cầu điều chỉnh công bị từ chối",
            AuditEventType.LeaveRequestSubmitted => "Đơn xin nghỉ phép mới",
            AuditEventType.LeaveRequestApproved => "Đơn xin nghỉ phép đã duyệt",
            AuditEventType.LeaveRequestRejected => "Đơn xin nghỉ phép bị từ chối",
            AuditEventType.LeaveBalanceAdjusted => "Điều chỉnh số dư phép",
            AuditEventType.ShiftChangeSubmitted => "Đơn xin đổi ca mới",
            AuditEventType.ShiftChangeApproved => "Đơn xin đổi ca đã duyệt",
            AuditEventType.ShiftChangeRejected => "Đơn xin đổi ca bị từ chối",
            AuditEventType.RoleCreated => "Tạo vai trò mới",
            AuditEventType.RoleUpdated => "Cập nhật vai trò & quyền",
            AuditEventType.RoleDeleted => "Xóa vai trò",
            AuditEventType.PermissionAssigned => "Gán quyền vai trò",
            AuditEventType.PermissionRevoked => "Thu hồi quyền vai trò",
            AuditEventType.PasswordReset => "Đặt lại mật khẩu tài khoản",
            AuditEventType.PasswordChanged => "Mật khẩu tài khoản đã thay đổi",
            AuditEventType.TemporaryPasswordIssued => "Cấp mật khẩu tạm thời",
            AuditEventType.ForcedPasswordChange => "Yêu cầu buộc đổi mật khẩu",
            AuditEventType.SessionRevoked => "Thu hồi phiên đăng nhập",
            AuditEventType.UserLocked => "Khóa tài khoản người dùng",
            AuditEventType.UserUnlocked => "Mở khóa tài khoản người dùng",
            AuditEventType.AccountDisabled => "Vô hiệu hóa tài khoản",
            AuditEventType.LoginFailed => "Cảnh báo đăng nhập thất bại",
            AuditEventType.SuspiciousLogin => "Cảnh báo bảo mật: Đăng nhập đáng ngờ",
            _ => "Hoạt động hệ thống mới"
        };
    }

    private static string GetEventDescription(AuditLog entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.Details))
        {
            var text = entry.Details.Trim();
            if (text.Length > 90) return text[..87] + "...";
            return text;
        }

        if (!string.IsNullOrEmpty(entry.TargetEntity) && !string.IsNullOrEmpty(entry.TargetId))
        {
            return $"{entry.TargetEntity} #{entry.TargetId}";
        }

        return "Vừa phát sinh giao dịch mới trên hệ thống.";
    }

    private static string? GetTargetUrl(string? targetEntity)
    {
        if (string.IsNullOrEmpty(targetEntity)) return null;

        return targetEntity.ToLowerInvariant() switch
        {
            "salesorder" => "/ban-hang/don-ban-hang",
            "invoice" => "/ban-hang/hoa-don",
            "quotation" => "/ban-hang/bao-gia",
            "product" => "/danh-muc/san-pham",
            "customer" => "/doi-tac/khach-hang",
            "supplier" => "/doi-tac/nha-cung-cap",
            "contract" => "/hop-dong",
            "warehouse" or "warehousevoucher" => "/kho/phieu-kho",
            "employeerequest" or "employee" => "/nhan-vien/duyet-ho-so",
            "pricelist" => "/danh-muc/bang-gia",
            "attendance" or "attendancerecord" or "attendanceadjustmentrequest" or "leaverequest" or "shiftchangerequest" => "/cham-cong",
            "role" or "rolepermission" => "/cai-dat/vai-tro",
            "user" or "applicationuser" => "/cai-dat/nguoi-dung",
            _ => null
        };
    }
}
