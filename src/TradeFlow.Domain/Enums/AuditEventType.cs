namespace TradeFlow.Domain.Enums;

/// <summary>Loại sự kiện nhật ký hoạt động</summary>
public enum AuditEventType
{
    // === Xác thực ===
    Login = 1,
    LoginFailed = 2,
    Logout = 3,

    // === Quản lý người dùng & Bảo mật ROOT ===
    UserCreated = 10,
    UserUpdated = 11,
    UserLocked = 12,
    AccountLocked = 12,
    UserUnlocked = 13,
    AccountUnlocked = 13,
    PasswordReset = 14,
    PasswordChanged = 15,
    PasswordViewed = 16,
    PasswordRecoveryRequested = 17,
    TemporaryPasswordIssued = 18,
    ForcedPasswordChange = 19,
    SessionRevoked = 26,
    AccountDisabled = 27,
    SuspiciousLogin = 28,

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
    DatabaseBackupCreated = 33,
    DatabaseBackupRestored = 34,

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

    // === Bán hàng & Hóa đơn ===
    SalesOrderCreated = 110,
    SalesOrderUpdated = 111,
    SalesOrderConfirmed = 112,
    SalesOrderCancelled = 113,
    SalesOrderDeleted = 114,

    InvoiceCreated = 120,
    InvoiceUpdated = 127,
    InvoiceIssued = 121,
    InvoiceAdjusted = 122,
    InvoiceReplaced = 123,
    InvoiceCancelled = 124,
    InvoiceDeleted = 125,
    InvoiceExportedPdf = 126,

    // === Báo giá (Phase 6) ===
    QuotationCreated = 130,
    QuotationUpdated = 131,
    QuotationStatusChanged = 132,
    QuotationConverted = 133,
    QuotationDeleted = 134,
    QuotationExportedPdf = 135,

    // === Tài liệu & Chứng từ (Phase 6) ===
    DocumentUploaded = 140,
    DocumentDeleted = 141,
    DocumentDownloaded = 142,

    // === An ninh & Chữ ký số (Phase 7) ===
    SignerEnrolled = 150,
    SignerRevoked = 151,
    SignerCodeIssued = 152,
    SignerCodeRevoked = 153,
    SignatureCreated = 154,
    SignatureVerified = 155,
    SignatureFailed = 156,
    DocumentTampered = 157,
    UserOffboarded = 158,
    SecuritySettingsUpdated = 159,
    SignerPinChanged = 160,
    SignerPinReset = 161,
    SignerLockedOut = 162,
    SignerProfileCreated = 163,
    SignerProfileUpdated = 164,

    // === Quản lý nhân viên (Phase 8 - Nhân viên) ===
    EmployeeCreated = 170,
    EmployeeUpdated = 171,
    EmployeeStatusChanged = 172,
    EmployeeRetired = 173,
    EmployeeRequestSubmitted = 174,
    EmployeeRequestApproved = 175,
    EmployeeRequestRejected = 176,
    EmployeeLinkedToUser = 177,
    EmployeeUnlinkedFromUser = 178,
    DepartmentCreated = 179,
    PositionCreated = 180,
    DepartmentUpdated = 181,
    DepartmentDeactivated = 182,
    DepartmentReactivated = 183,
    DepartmentDeleted = 184,
    PositionUpdated = 185,
    PositionDeactivated = 186,
    PositionReactivated = 187,
    PositionDeleted = 188,
    EmployeeRequestAdditionalDocsRequested = 189,
    EmployeeRequestResubmitted = 194,

    // === Quản lý Công nợ & Thanh toán Đối tác ===
    DebtPaymentRecorded = 190,
    DebtAdjustmentRecorded = 191,
    DebtDiscountRecorded = 192,
    DebtExported = 193,

    // === Quản lý Hợp đồng & Phụ lục ===
    ContractCreated = 200,
    ContractUpdated = 201,
    ContractFinalized = 202,
    ContractSigned = 203,
    ContractPrinted = 204,
    ContractCancelled = 205,
    ContractLiquidated = 206,
    ContractExportedPdf = 207,
    ContractSettingsUpdated = 208,
    ContractPartyASettingsUpdated = 208,
    ContractTemplateUpdated = 209,
    ContractDeleted = 210,

    // === Quản lý Kho, Kiểm kê & Hàng trả ===
    StocktakeCreated = 220,
    StocktakeUpdated = 221,
    StocktakeSubmitted = 222,
    StocktakeApproved = 223,
    StocktakeRejected = 224,
    StocktakeCancelled = 225,
    WarehouseVoucherCreated = 226,
    WarehouseVoucherApproved = 227,
    WarehouseVoucherCancelled = 228,
    WarehouseReturnCreated = 229,
    WarehouseReturnInspected = 230,
    WarehouseReturnApproved = 231,
    WarehouseReturnCompleted = 232,
    DefectiveStockQuarantined = 233,
    DefectiveStockDisposed = 234,

    // === Quản lý Hợp đồng lao động ===
    EmploymentContractCreated = 240,
    EmploymentContractUpdated = 241,
    EmploymentContractFinalized = 242,
    EmploymentContractSigned = 243,
    EmploymentContractRenewed = 244,
    EmploymentContractTerminated = 245,
    EmploymentContractCancelled = 246,
    EmploymentContractExportedPdf = 247,
    EmploymentContractDeleted = 248,

    // === Chấm công & Nghỉ phép (Phase 8+) ===
    AttendanceClockIn = 250,
    AttendanceClockOut = 251,
    AttendanceAdjustmentSubmitted = 252,
    AttendanceAdjustmentApproved = 253,
    AttendanceAdjustmentRejected = 254,
    LeaveRequestSubmitted = 255,
    LeaveRequestApproved = 256,
    LeaveRequestRejected = 257,
    LeaveBalanceAdjusted = 258,
    ShiftChangeSubmitted = 259,
    ShiftChangeApproved = 260,
    ShiftChangeRejected = 261,

    // === Phân quyền & Quản lý Truy cập (Custom RBAC & Temporary Access) ===
    RoleCloned = 265,
    RolePermissionsUpdated = 266,
    UserRoleAssigned = 267,
    UserRoleRevoked = 268,
    TemporaryRoleGranted = 269,
    TemporaryRoleRevoked = 270,
    TemporaryRoleExpired = 271,
    DataScopeChanged = 272,
    UnauthorizedAccessAttempt = 273
}
