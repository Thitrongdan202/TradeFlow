# TradeFlow

Hệ thống quản lý kinh doanh nội bộ — Internal Business Management System

Built with **C# / .NET 10 / Blazor Server / Entity Framework Core / PostgreSQL**

---

## Trạng thái triển khai / Implementation Status

| Phase | Nội dung | Trạng thái |
|-------|---------|-----------|
| **Phase 1** | Solution scaffold, EF Core, Identity, PostgreSQL | ✅ Hoàn thành |
| **Phase 1.5** | UI Foundation — Design system, Login, App shell | ✅ Hoàn thành |
| **Phase 2** | Authentication flows, Users, Roles, Permissions, Audit, Profile, Seeding | ✅ Hoàn thành |
| Phase 3 | Master data (Products, Customers, Suppliers…) | 🔄 Kế hoạch |
| Phase 4 | Price lists, Excel import/export | 🔄 Kế hoạch |
| Phase 5 | Sales — Quotations, Orders, Invoices | 🔄 Kế hoạch |
| Phase 6 | Purchasing | 🔄 Kế hoạch |
| Phase 7 | Warehouse management | 🔄 Kế hoạch |
| Phase 8 | Reports | 🔄 Kế hoạch |

---

## Yêu cầu / Prerequisites

| Công cụ | Phiên bản | Ghi chú |
|---------|-----------|---------|
| .NET SDK | 10.0.400+ | [Tải về](https://dotnet.microsoft.com/download/dotnet/10.0) |
| PostgreSQL | 15+ | [Tải về](https://www.postgresql.org/download/) |
| Git | bất kỳ | |

Kiểm tra phiên bản .NET đang cài:
```powershell
dotnet --version
# Phải trả về 10.0.xxx
```

---

## Cài đặt / Installation

### 1. Clone repository

```bash
git clone <repository-url>
cd TradeFlow
```

### 2. Cấu hình PostgreSQL

```sql
-- Tạo database (chạy trong psql hoặc pgAdmin)
CREATE DATABASE tradeflow_dev;
```

### 3. Cấu hình connection string

Sử dụng .NET User Secrets (KHÔNG commit vào Git):

```powershell
cd src/TradeFlow.Web

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=tradeflow_dev;Username=postgres;Password=<your-password>"
```

Kiểm tra user secrets:
```powershell
dotnet user-secrets list
```

> **Không bao giờ commit** connection string hoặc mật khẩu database vào Git.

### 4. Cài EF Core Tools

```powershell
dotnet tool install --global dotnet-ef
```

### 5. Chạy migration

```powershell
# Từ thư mục gốc TradeFlow/
dotnet ef database update `
  --project src/TradeFlow.Infrastructure `
  --startup-project src/TradeFlow.Web

# Hoặc dùng đường dẫn đầy đủ đến dotnet-ef nếu PATH chưa cập nhật:
$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe database update `
  --project src/TradeFlow.Infrastructure `
  --startup-project src/TradeFlow.Web
```

---

## Khởi động ứng dụng / Starting the Application

```powershell
# Restore dependencies
dotnet restore TradeFlow.slnx

# Chạy ứng dụng
dotnet run --project src/TradeFlow.Web

# Hoặc với hot-reload (phát triển)
dotnet watch --project src/TradeFlow.Web
```

Ứng dụng chạy tại: `http://localhost:5000` hoặc `https://localhost:5001`

> Khi khởi động ở môi trường **Development**, ứng dụng tự động:
> 1. Chạy migration EF Core
> 2. Seed database với dữ liệu mặc định (idempotent — an toàn khi chạy nhiều lần)

### Dừng ứng dụng

```powershell
# Nhấn Ctrl+C trong terminal đang chạy ứng dụng
```

---

## Build và Test

```powershell
# Restore
dotnet restore TradeFlow.slnx

# Build
dotnet build TradeFlow.slnx

# Test
dotnet test TradeFlow.slnx

# Kết quả hiện tại (Phase 2)
# Build succeeded. 0 Warning(s), 0 Error(s)
# Test Run Successful. Total: 25+, Passed: 25+, Failed: 0
```

---

## 🔑 Tài khoản phát triển / Development Accounts

> ⚠️ **CHỈ DÙNG CHO MÔI TRƯỜNG PHÁT TRIỂN / LOCAL**
>
> Các tài khoản và mật khẩu này **KHÔNG ĐƯỢC DÙNG** trong môi trường staging hoặc production.
> Không commit tài khoản thật hay mật khẩu production vào README.

### Đăng nhập

Truy cập: `http://localhost:5000/Account/Login`

Nhập **Tên đăng nhập** (không cần dùng email đầy đủ) và **Mật khẩu**.

### Danh sách tài khoản

| Tên đăng nhập | Mật khẩu | Vai trò | Quyền |
|---------------|----------|---------|-------|
| `admin` | `tradecore123` | Administrator | Toàn quyền |
| `quanly01` | `tradecore123` | Manager | Xem, Phê duyệt |
| `kinhdoanh01` | `tradecore123` | Sales | Bán hàng |
| `muahang01` | `tradecore123` | Purchase | Mua hàng |
| `kho01` | `tradecore123` | Warehouse | Kho hàng |
| `xnk01` | `tradecore123` | Import-Export | Xuất nhập khẩu |

> Các tài khoản này được tự động tạo khi app khởi động ở môi trường Development (idempotent).
> Nếu tài khoản đã tồn tại, seeder sẽ đảm bảo chúng đang ở trạng thái Active và không bị khóa.

### Thay đổi mật khẩu sau khi đăng nhập

Sau khi đăng nhập → click avatar → **Đổi mật khẩu** (hoặc truy cập `/tai-khoan/doi-mat-khau`).

---

## Tính năng đăng nhập / Login Features

### Hiện / ẩn mật khẩu (Eye Icon)

Trang đăng nhập có nút **👁** để hiện/ẩn mật khẩu:

- Click nút 👁 → mật khẩu hiển thị dạng text
- Click lại → trở về dạng ẩn (`••••••••`)
- Nút có aria-label: "Hiển thị mật khẩu" / "Ẩn mật khẩu"
- Hoạt động bằng JavaScript thuần (không cần Blazor interactive mode)
- Không submit form, không xóa giá trị mật khẩu

### Hỗ trợ đăng nhập bằng tên đăng nhập HOẶC email

```
# Cả hai đều hoạt động:
Tên đăng nhập: admin
Tên đăng nhập: admin@tradeflow.local
```

### Thông báo lỗi

| Trường hợp | Thông báo |
|------------|-----------|
| Tên đăng nhập không tồn tại | "Tên đăng nhập hoặc mật khẩu không chính xác." |
| Mật khẩu sai | "Mật khẩu không chính xác." |
| Tài khoản bị khóa (Lockout) | Chuyển đến `/Account/Lockout` |
| Tài khoản bị vô hiệu hóa | "Tài khoản của bạn đang bị vô hiệu hóa..." |
| Chưa được kích hoạt | "Tài khoản của bạn chưa được kích hoạt..." |

---

## Xử lý sự cố đăng nhập / Authentication Troubleshooting

### Không đăng nhập được

1. **Kiểm tra database đã có dữ liệu chưa:**
   ```sql
   SELECT "UserName", "Status" FROM "Users";
   ```
2. **Seed chưa chạy?** Đảm bảo ứng dụng chạy ở môi trường `Development`:
   ```powershell
   ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/TradeFlow.Web
   ```
3. **Kiểm tra password hash đã có trong DB:**
   ```sql
   SELECT "UserName", LENGTH("PasswordHash") as pw_len FROM "Users";
   -- pw_len phải > 0 (thường là 84)
   ```
4. **Kiểm tra tài khoản không bị khóa:**
   ```sql
   SELECT "UserName", "LockoutEnd", "Status" FROM "Users";
   -- LockoutEnd phải là NULL
   -- Status phải là 1 (Active)
   ```
5. **Kiểm tra vai trò đã gán:**
   ```sql
   SELECT u."UserName", r."Name" FROM "UserRoles" ur
   JOIN "Users" u ON ur."UserId" = u."Id"
   JOIN "Roles" r ON ur."RoleId" = r."Id";
   ```

### Password hashing

TradeFlow sử dụng `UserManager<TUser>.CreateAsync(user, password)` của ASP.NET Core Identity.

Mật khẩu được lưu với PBKDF2 (không bao giờ plaintext). Không có master password hoặc backdoor.

---

## Database Migrations

```powershell
# Xem migration hiện tại
dotnet ef migrations list `
  --project src/TradeFlow.Infrastructure `
  --startup-project src/TradeFlow.Web

# Thêm migration mới
dotnet ef migrations add <TênMigration> `
  --project src/TradeFlow.Infrastructure `
  --startup-project src/TradeFlow.Web

# Cập nhật database
dotnet ef database update `
  --project src/TradeFlow.Infrastructure `
  --startup-project src/TradeFlow.Web
```

---

## Kiến trúc UI / UI Architecture

### Design System

Toàn bộ design được xây dựng với **CSS Custom Properties (Design Tokens)**.

File chính: [`src/TradeFlow.Web/wwwroot/app.css`](src/TradeFlow.Web/wwwroot/app.css)

#### Design Tokens

| Token | Giá trị | Ý nghĩa |
|-------|---------|---------|
| `--tf-primary` | `#4338ca` (Indigo-700) | Màu chủ đạo — brand |
| `--tf-success` | `#16a34a` | Trạng thái thành công / dương |
| `--tf-warning` | `#d97706` | Cảnh báo |
| `--tf-error` | `#dc2626` | Lỗi / nguy hiểm |
| `--tf-sidebar-bg` | `#0f172a` | Sidebar — dark navy |
| `--tf-sidebar-width` | `240px` | Độ rộng sidebar |
| `--tf-topbar-height` | `56px` | Chiều cao topbar |

#### Typography

- **Font**: `Inter` → `Segoe UI` → system-ui
- **Base size**: 14px
- **Scale**: xs(11) → sm(13) → base(14) → md(15) → lg(16) → xl(18) → 2xl(22) → 3xl(28)

---

### App Shell Architecture

```
Browser
  └── App.razor (lang="vi")
        └── Routes.razor
              ├── LoginLayout (for /Account/* pages)
              │     └── Login.razor  ← no sidebar/topbar
              └── MainLayout (for all other pages)
                    ├── Sidebar: NavMenu.razor
                    ├── TopBar: search + notifications + UserMenu.razor
                    └── Main content: @Body
```

### Layout Components

| Component | File | Mô tả |
|-----------|------|-------|
| `MainLayout` | `Layout/MainLayout.razor` | App shell với sidebar + topbar |
| `LoginLayout` | `Layout/LoginLayout.razor` | Layout tối giản cho trang login |
| `NavMenu` | `Layout/NavMenu.razor` | Sidebar navigation với collapsible groups |
| `UserMenu` | `Layout/UserMenu.razor` | User dropdown — hiển thị FullName thật từ DB |

---

### Cấu trúc dự án / Project Structure

```
TradeFlow/
├── TradeFlow.slnx
├── .gitignore
├── README.md
│
├── src/
│   ├── TradeFlow.Domain/
│   │   ├── Common/            Entity<TId>, AuditableEntity<TId>
│   │   ├── Entities/          AuditLog, CompanySettings
│   │   └── Enums/             UserStatus, PermissionAction, ResourceType, AuditEventType
│   │
│   ├── TradeFlow.Application/
│   │   ├── Common/Interfaces/ IApplicationDbContext, ICurrentUserService, IAuditService
│   │   └── DependencyInjection/  AddApplication()
│   │
│   ├── TradeFlow.Infrastructure/
│   │   ├── Persistence/       TradeFlowDbContext, ApplicationUser, ApplicationRole, RolePermission
│   │   │   ├── DatabaseSeeder.cs       ← Dev accounts seed (idempotent)
│   │   │   └── CustomUserClaimsPrincipalFactory.cs ← Injects permissions into claims
│   │   ├── Services/          AuditService
│   │   └── DependencyInjection/  AddInfrastructure()
│   │
│   └── TradeFlow.Web/
│       ├── Components/
│       │   ├── Account/Pages/Login.razor   ← Username+email support, JS eye icon
│       │   ├── Layout/UserMenu.razor       ← Real FullName from DB/claims
│       │   ├── Pages/Admin/Users/
│       │   ├── Pages/Admin/Roles/
│       │   ├── Pages/Admin/Settings/
│       │   ├── Pages/Admin/AuditLogs/
│       │   └── Pages/Account/Profile.razor, ChangePassword.razor
│       ├── wwwroot/app.css
│       └── Program.cs
│
└── tests/
    ├── TradeFlow.UnitTests/
    │   └── Authentication/AuthenticationTests.cs
    └── TradeFlow.IntegrationTests/
```

---

## Navigation / Điều hướng

### Sidebar modules

| Module | Route | Trạng thái |
|--------|-------|-----------|
| Tổng quan | `/` | ✅ Hoàn thành |
| Người dùng | `/cai-dat/nguoi-dung` | ✅ Phase 2 |
| Vai trò | `/cai-dat/vai-tro` | ✅ Phase 2 |
| Thông tin công ty | `/cai-dat/cong-ty` | ✅ Phase 2 |
| Nhật ký hoạt động | `/cai-dat/nhat-ky` | ✅ Phase 2 |
| Hỗ trợ kỹ thuật | `/cai-dat/ho-tro` | ✅ Phase 2 |
| Tài khoản của tôi | `/tai-khoan` | ✅ Phase 2 |
| Đổi mật khẩu | `/tai-khoan/doi-mat-khau` | ✅ Phase 2 |
| Báo giá | `/ban-hang/bao-gia` | 🔄 Phase 5 |
| Đơn bán hàng | `/ban-hang/don-hang` | 🔄 Phase 5 |
| Hóa đơn | `/ban-hang/hoa-don` | 🔄 Phase 5 |
| Đơn mua hàng | `/mua-hang/don-hang` | 🔄 Phase 6 |
| Tồn kho | `/kho/ton-kho` | 🔄 Phase 7 |
| Nhập kho | `/kho/nhap-kho` | 🔄 Phase 7 |
| Xuất kho | `/kho/xuat-kho` | 🔄 Phase 7 |
| Khách hàng | `/doi-tac/khach-hang` | 🔄 Phase 3 |
| Nhà cung cấp | `/doi-tac/nha-cung-cap` | 🔄 Phase 3 |
| Báo cáo | `/bao-cao` | 🔄 Phase 8 |

---

## Phụ thuộc dự án / Dependency Architecture

```
TradeFlow.Domain (no dependencies)
    ↑
TradeFlow.Application → TradeFlow.Domain
    ↑
TradeFlow.Infrastructure → TradeFlow.Application, TradeFlow.Domain
    ↑
TradeFlow.Web → TradeFlow.Application, TradeFlow.Infrastructure
```

---

## NuGet Packages

| Project | Package | Version |
|---------|---------|---------|
| Application | FluentValidation | 12.1.1 |
| Application | MediatR | 14.2.0 |
| Infrastructure | Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.11 |
| Infrastructure | Microsoft.EntityFrameworkCore | 10.0.11 |
| Infrastructure | **Npgsql.EntityFrameworkCore.PostgreSQL** | **10.0.3** |
| Web | Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore | 10.0.11 |
| Tests | FluentAssertions, Moq | latest |

> **Ghi chú**: Npgsql sử dụng versioning riêng — `10.0.3` (không phải `10.0.11`).

---

## Database Seeding

Seeder chạy tự động khi ứng dụng khởi động ở môi trường `Development`.

**Seeder là idempotent** — an toàn để chạy nhiều lần:
- Kiểm tra tồn tại trước khi tạo (không tạo trùng)
- Reset lockout/status cho dev accounts nếu bị thay đổi
- Xóa tài khoản legacy nếu tồn tại

Seeder tạo:
1. **6 vai trò**: Administrator, Manager, Sales, Purchase, Warehouse, Import-Export
2. **Toàn bộ quyền** cho vai trò Administrator
3. **6 tài khoản dev**: admin, quanly01, kinhdoanh01, muahang01, kho01, xnk01
4. **CompanySettings** mặc định (singleton)

---

## Bảo mật / Security

- Mật khẩu được lưu bằng ASP.NET Core Identity (PBKDF2 + salt ngẫu nhiên)
- **Không bao giờ commit** connection string thật hoặc encryption key vào Git
- Audit log không bao giờ ghi plaintext mật khẩu hoặc token
- Server-side authorization enforcement (UI filtering không đủ)
- Support access: giới hạn thời gian, kiểm toán đầy đủ, có thể thu hồi ngay
- Không có master password hoặc backdoor ẩn

---

*Cập nhật lần cuối: Phase 2 — Authentication + Administration hoàn thành*
