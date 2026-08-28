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
}
