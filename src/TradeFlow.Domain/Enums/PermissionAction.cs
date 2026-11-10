namespace TradeFlow.Domain.Enums;

/// <summary>Hành động quyền truy cập trên một tài nguyên</summary>
public enum PermissionAction
{
    /// <summary>Xem</summary>
    View = 1,

    /// <summary>Thêm</summary>
    Create = 2,

    /// <summary>Sửa</summary>
    Edit = 3,

    /// <summary>Xóa</summary>
    Delete = 4,

    /// <summary>Phê duyệt</summary>
    Approve = 5,

    /// <summary>Nhập dữ liệu (import)</summary>
    Import = 6,

    /// <summary>Xuất dữ liệu (export)</summary>
    Export = 7,

    /// <summary>Ghi đè giá</summary>
    OverridePrice = 8,

    /// <summary>Xem chi tiết</summary>
    ViewDetail = 9,

    /// <summary>In tài liệu</summary>
    Print = 10,

    /// <summary>Từ chối</summary>
    Reject = 11,

    /// <summary>Quản lý cấu hình</summary>
    ManageConfig = 12,

    /// <summary>Quản lý quyền</summary>
    ManagePermissions = 13
}
