path = r'C:\Users\thitr\.gemini\antigravity\scratch\TradeFlow\src\TradeFlow.Infrastructure\Persistence\DatabaseSeeder.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Update RoleDefinitions
old_defs = """    private static readonly (string Name, string Description, bool IsSystem)[] RoleDefinitions =
    [
        ("Administrator", "Quản trị viên hệ thống (toàn quyền)", true),
        ("Manager",       "Quản lý (xem và phê duyệt)",          false),
        ("HR",            "Quản lý nhân sự & Chấm công",         false),
        ("Sales",         "Nhân viên kinh doanh & Bán hàng",     false),
        ("Purchase",      "Nhân viên mua hàng",                   false),
        ("Warehouse",     "Nhân viên kho",                        false),
        ("Accountant",    "Kế toán & Hóa đơn",                   false),
        ("Import-Export", "Nhân viên xuất nhập khẩu",             false),
        ("Employee",      "Nhân viên tiêu chuẩn (tự phục vụ)",     false),
    ];"""

new_defs = """    private static readonly (string Name, string Description, bool IsSystem, string ColorHex, string Icon)[] RoleDefinitions =
    [
        ("Administrator", "Quản trị viên hệ thống (toàn quyền)", true,  "#ef4444", "shield"),
        ("Manager",       "Quản lý (xem và phê duyệt)",          false, "#8b5cf6", "user-check"),
        ("HR",            "Quản lý nhân sự & Chấm công",         false, "#ec4899", "users"),
        ("Sales",         "Nhân viên kinh doanh & Bán hàng",     false, "#3b82f6", "shopping-cart"),
        ("Purchase",      "Nhân viên mua hàng",                   false, "#06b6d4", "truck"),
        ("Warehouse",     "Nhân viên kho",                        false, "#f59e0b", "archive"),
        ("Accountant",    "Kế toán & Hóa đơn",                   false, "#10b981", "calculator"),
        ("Import-Export", "Nhân viên xuất nhập khẩu",             false, "#6366f1", "globe"),
        ("Employee",      "Nhân viên tiêu chuẩn (tự phục vụ)",     false, "#64748b", "user"),
    ];"""

text = text.replace(old_defs.replace('\r\n', '\n'), new_defs.replace('\r\n', '\n'))
text = text.replace(old_defs, new_defs)

# 2. Update SeedRolesAsync to include ColorHex and Icon
old_seed = """        foreach (var (name, description, isSystem) in RoleDefinitions)
        {
            if (!await _roleManager.RoleExistsAsync(name))
            {
                _logger.LogInformation("Creating role: {RoleName}", name);
                var role = new ApplicationRole(name)
                {
                    Description = description,
                    IsSystem = isSystem,
                    IsActive = true,
                    CreatedBy = "System"
                };"""

new_seed = """        foreach (var (name, description, isSystem, colorHex, icon) in RoleDefinitions)
        {
            if (!await _roleManager.RoleExistsAsync(name))
            {
                _logger.LogInformation("Creating role: {RoleName}", name);
                var role = new ApplicationRole(name)
                {
                    Description = description,
                    IsSystem = isSystem,
                    IsActive = true,
                    ColorHex = colorHex,
                    Icon = icon,
                    CreatedBy = "System"
                };"""

text = text.replace(old_seed.replace('\r\n', '\n'), new_seed.replace('\r\n', '\n'))
text = text.replace(old_seed, new_seed)

# 3. Remove Customers from Employee in SeedDefaultRolePermissionsAsync
old_emp = """            ["Employee"] =
            [
                (ResourceType.Attendance, PermissionAction.View),
                (ResourceType.Attendance, PermissionAction.Create),
                (ResourceType.Attendance, PermissionAction.Edit),
                (ResourceType.SalesOrders, PermissionAction.Create),
                (ResourceType.SalesOrders, PermissionAction.View),
                (ResourceType.Products, PermissionAction.View),
                (ResourceType.Customers, PermissionAction.View),
                (ResourceType.Customers, PermissionAction.Create),
                (ResourceType.PriceLists, PermissionAction.View),
                (ResourceType.Warehouses, PermissionAction.View),
            ],"""

new_emp = """            ["Employee"] =
            [
                (ResourceType.Attendance, PermissionAction.View),
                (ResourceType.Attendance, PermissionAction.Create),
                (ResourceType.Attendance, PermissionAction.Edit),
                (ResourceType.SalesOrders, PermissionAction.Create),
                (ResourceType.SalesOrders, PermissionAction.View),
                (ResourceType.Products, PermissionAction.View),
                (ResourceType.PriceLists, PermissionAction.View),
                (ResourceType.Warehouses, PermissionAction.View),
            ],"""

text = text.replace(old_emp.replace('\r\n', '\n'), new_emp.replace('\r\n', '\n'))
text = text.replace(old_emp, new_emp)

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("DatabaseSeeder patched successfully!")
