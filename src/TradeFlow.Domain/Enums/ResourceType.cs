namespace TradeFlow.Domain.Enums;

/// <summary>Tài nguyên hệ thống được phân quyền</summary>
public enum ResourceType
{
    // === Quản trị hệ thống ===

    /// <summary>Người dùng</summary>
    Users = 1,

    /// <summary>Vai trò</summary>
    Roles = 2,

    /// <summary>Thông tin công ty</summary>
    CompanySettings = 3,

    /// <summary>Nhật ký hoạt động</summary>
    AuditLog = 4,

    /// <summary>Hỗ trợ kỹ thuật</summary>
    TechnicalSupport = 5,

    // === Danh mục chính ===

    /// <summary>Sản phẩm</summary>
    Products = 10,

    /// <summary>Danh mục sản phẩm</summary>
    ProductCategories = 11,

    /// <summary>Đơn vị tính</summary>
    UnitOfMeasures = 12,

    /// <summary>Loại tiền</summary>
    Currencies = 13,

    /// <summary>Khách hàng</summary>
    Customers = 14,

    /// <summary>Nhà cung cấp</summary>
    Suppliers = 15,

    /// <summary>Kho hàng</summary>
    Warehouses = 16,

    // === Bảng giá ===

    /// <summary>Bảng giá</summary>
    PriceLists = 20,

    // === Bán hàng (Phase 5+) ===

    /// <summary>Báo giá</summary>
    Quotations = 30,

    /// <summary>Đơn bán hàng</summary>
    SalesOrders = 31,

    /// <summary>Hóa đơn</summary>
    Invoices = 32,
}