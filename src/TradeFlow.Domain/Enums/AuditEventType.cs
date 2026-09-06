namespace TradeFlow.Domain.Enums;

/// <summary>Loại sự kiện nhật ký hoạt động</summary>
public enum AuditEventType
{
    // === Xác thực ===
    Login = 1,
    LoginFailed = 2,
    Logout = 3,

    // === Quản lý người dùng ===
    UserCreated = 10,
    UserUpdated = 11,
    UserLocked = 12,
    UserUnlocked = 13,
    PasswordReset = 14,
    PasswordChanged = 15,
    PasswordViewed = 16,

    // === Quản lý vai trò ===
    RoleCreated = 20,
    RoleUpdated = 21,
    RoleDeleted = 22,
    PermissionAssigned = 23,
    PermissionRevoked = 24,

    // === Dữ liệu ===
    DataImported = 30,
    DataExported = 31,
    FileUploaded = 32,

    // === Cài đặt ===
    CompanySettingsChanged = 40,

    // === Hỗ trợ kỹ thuật ===
    SupportAccessEnabled = 50,
    SupportAccessRevoked = 51,
    SupportActionPerformed = 52,

    // === Danh mục chính ===
    ProductCreated = 60,
    ProductUpdated = 61,
    ProductDeleted = 62,
    CategoryCreated = 63,
    CategoryUpdated = 64,
    CategoryDeleted = 65,
    UnitCreated = 66,
    UnitUpdated = 67,
    UnitDeleted = 68,
    CurrencyCreated = 69,
    CurrencyUpdated = 70,
    CurrencyDeleted = 71,
    CustomerCreated = 72,
    CustomerUpdated = 73,
    CustomerDeleted = 74,
    SupplierCreated = 75,
    SupplierUpdated = 76,
    SupplierDeleted = 77,
    WarehouseCreated = 78,
    WarehouseUpdated = 79,
    WarehouseDeleted = 80,

    // === Bảng giá (Phase 4) ===
    PriceListCreated = 90,
    PriceListUpdated = 91,
    PriceListDeleted = 92,
    PriceListApproved = 93,
    PriceListImported = 94,
    PriceListExported = 95,
    PriceListOriginalDownloaded = 96,
    PriceChanged = 97,
    ProductImageMapped = 98,
}