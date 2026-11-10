namespace TradeFlow.Domain.Enums;

/// <summary>
/// Loại yêu cầu / sự cố hỗ trợ kỹ thuật
/// </summary>
public enum SupportCategory
{
    /// <summary>Lỗi hệ thống</summary>
    SystemError = 1,

    /// <summary>Lỗi thao tác</summary>
    OperationError = 2,

    /// <summary>Lỗi hiển thị giao diện</summary>
    UiDisplayError = 3,

    /// <summary>Dữ liệu không chính xác</summary>
    DataInaccurate = 4,

    /// <summary>Không truy cập được chức năng</summary>
    FeatureInaccessible = 5,

    /// <summary>Yêu cầu hỗ trợ sử dụng</summary>
    UsageSupport = 6,

    /// <summary>Đề xuất cải tiến</summary>
    ImprovementProposal = 7,

    /// <summary>Khác</summary>
    Other = 8
}

/// <summary>
/// Phân hệ / Module liên quan trong TradeFlow
/// </summary>
public enum SupportRelatedModule
{
    /// <summary>Chấm công</summary>
    Attendance = 1,

    /// <summary>Bán hàng</summary>
    Sales = 2,

    /// <summary>Hóa đơn</summary>
    Invoices = 3,

    /// <summary>Sản phẩm</summary>
    Products = 4,

    /// <summary>Kho hàng</summary>
    Warehouse = 5,

    /// <summary>Đối tác</summary>
    Partners = 6,

    /// <summary>Hợp đồng</summary>
    Contracts = 7,

    /// <summary>Chứng từ</summary>
    Documents = 8,

    /// <summary>Nhân sự</summary>
    Employees = 9,

    /// <summary>Cài đặt</summary>
    Settings = 10,

    /// <summary>Khác</summary>
    Other = 11
}

/// <summary>
/// Mức độ ưu tiên của báo cáo hỗ trợ
/// </summary>
public enum SupportPriority
{
    /// <summary>Bình thường</summary>
    Normal = 1,

    /// <summary>Cần xử lý sớm</summary>
    Medium = 2,

    /// <summary>Khẩn cấp</summary>
    Urgent = 3
}

/// <summary>
/// Trạng thái vòng đời của yêu cầu hỗ trợ
/// </summary>
public enum SupportStatus
{
    /// <summary>Mới gửi</summary>
    New = 1,

    /// <summary>Đã tiếp nhận</summary>
    Acknowledged = 2,

    /// <summary>Đang xử lý</summary>
    InProgress = 3,

    /// <summary>Chờ phản hồi từ người gửi</summary>
    WaitingForCustomer = 4,

    /// <summary>Đã giải quyết</summary>
    Resolved = 5,

    /// <summary>Đã đóng</summary>
    Closed = 6,

    /// <summary>Từ chối tiếp nhận</summary>
    Rejected = 7
}
