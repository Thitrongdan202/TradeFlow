path = r'C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Domain\Enums\AuditEventType.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target = "    ShiftChangeRejected = 261\n}"
replacement = """    ShiftChangeRejected = 261,

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
"""

if target in text:
    text = text.replace(target, replacement)
    with open(path, 'w', encoding='utf-8', newline='\n') as f:
        f.write(text)
    print("Successfully patched AuditEventType.cs")
else:
    print("Target not found!")
