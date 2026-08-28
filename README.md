# TradeFlow

Hệ thống quản lý kinh doanh nội bộ — Internal Business Management System

Built with **C# / .NET 10 / Blazor Server / Entity Framework Core / PostgreSQL**

---

## Trạng thái triển khai / Implementation Status

| Phase | Nội dung | Trạng thái |
|-------|---------|-----------|
| **Phase 1** | Solution scaffold, EF Core, Identity, PostgreSQL | ✅ Hoàn thành |
| **Phase 1.5** | UI Foundation — Design system, Login, App shell | ✅ Hoàn thành |
| Phase 2 | Authentication flows, Users, Roles, Permissions | 🔄 Kế hoạch |
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

---

## Cài đặt / Installation

### 1. Clone repository

```bash
git clone <repository-url>
cd TradeFlow
```

### 2. Cấu hình PostgreSQL

```sql
CREATE DATABASE tradeflow_dev;
```

### 3. Cấu hình connection string

```powershell
cd src/TradeFlow.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Database=tradeflow_dev;Username=postgres;Password=<your-password>"
```

### 4. Cài EF Core Tools

```powershell
dotnet tool install --global dotnet-ef
```

### 5. Chạy migration

```powershell
dotnet ef database update --project src/TradeFlow.Infrastructure --startup-project src/TradeFlow.Web
```

---

## Chạy ứng dụng / Run

```powershell
dotnet run --project src/TradeFlow.Web

# Hoặc với hot-reload
dotnet watch --project src/TradeFlow.Web
```

Ứng dụng chạy tại `https://localhost:5001` (HTTPS) hoặc `http://localhost:5000`.

---

## Build và Test

```powershell
# Restore
dotnet restore TradeFlow.slnx

# Build
dotnet build TradeFlow.slnx

# Test
dotnet test TradeFlow.slnx

# Kết quả hiện tại
# Build succeeded. 0 Warning(s), 0 Error(s)
# Test Run Successful. Total: 2, Passed: 2, Failed: 0
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

#### Màu sắc

Toàn bộ màu sắc được định nghĩa trong `:root` CSS variables.
Không có màu hard-coded trong component styles.

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
| `UserMenu` | `Layout/UserMenu.razor` | User dropdown — avatar, tên, logout |

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
│   │   ├── Common/Interfaces/ IApplicationDbContext, ICurrentUserService, IAuditService, IPasswordEncryptionService
│   │   └── DependencyInjection/  AddApplication()
│   │
│   ├── TradeFlow.Infrastructure/
│   │   ├── Persistence/       TradeFlowDbContext, ApplicationUser, ApplicationRole, ApplicationUserRole, RolePermission
│   │   │   └── Configurations/  EF Core fluent configs
│   │   ├── Services/          AuditService
│   │   └── DependencyInjection/  AddInfrastructure()
│   │
│   └── TradeFlow.Web/
│       ├── Components/
│       │   ├── App.razor               ← html lang="vi"
│       │   ├── Routes.razor            ← routing with auth redirect
│       │   ├── _Imports.razor          ← global using
│       │   ├── Account/                ← ASP.NET Core Identity UI (Vietnamese)
│       │   │   ├── Pages/Login.razor   ← NEW: full design
│       │   │   ├── Pages/Lockout.razor ← Vietnamese
│       │   │   └── Pages/AccessDenied.razor ← Vietnamese
│       │   ├── Layout/
│       │   │   ├── MainLayout.razor    ← App shell
│       │   │   ├── LoginLayout.razor   ← Login-only layout
│       │   │   ├── NavMenu.razor       ← Sidebar with all modules
│       │   │   └── UserMenu.razor      ← User dropdown
│       │   └── Pages/
│       │       ├── Home.razor          ← Dashboard (Tổng quan)
│       │       └── Error.razor         ← Vietnamese error page
│       ├── wwwroot/
│       │   └── app.css                 ← Design system (design tokens + components)
│       ├── appsettings.json
│       └── Program.cs
│
└── tests/
    ├── TradeFlow.UnitTests/
    └── TradeFlow.IntegrationTests/
```

---

## Navigation / Điều hướng

### Sidebar modules

| Module | Route | Trạng thái |
|--------|-------|-----------|
| Tổng quan | `/` | ✅ Shell hoàn thành |
| Báo giá | `/ban-hang/bao-gia` | 🔄 Phase 5 |
| Đơn bán hàng | `/ban-hang/don-hang` | 🔄 Phase 5 |
| Hóa đơn | `/ban-hang/hoa-don` | 🔄 Phase 5 |
| Đơn mua hàng | `/mua-hang/don-hang` | 🔄 Phase 6 |
| Đề nghị mua | `/mua-hang/de-nghi-mua` | 🔄 Phase 6 |
| Tồn kho | `/kho/ton-kho` | 🔄 Phase 7 |
| Nhập kho | `/kho/nhap-kho` | 🔄 Phase 7 |
| Xuất kho | `/kho/xuat-kho` | 🔄 Phase 7 |
| Khách hàng | `/doi-tac/khach-hang` | 🔄 Phase 3 |
| Nhà cung cấp | `/doi-tac/nha-cung-cap` | 🔄 Phase 3 |
| Báo cáo | `/bao-cao` | 🔄 Phase 8 |
| Người dùng | `/cai-dat/nguoi-dung` | 🔄 Phase 2 |
| Vai trò | `/cai-dat/vai-tro` | 🔄 Phase 2 |
| Thông tin công ty | `/cai-dat/cong-ty` | 🔄 Phase 2 |
| Nhật ký hoạt động | `/cai-dat/nhat-ky` | 🔄 Phase 2 |

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
| Tests | FluentAssertions, Moq, Microsoft.AspNetCore.Mvc.Testing | latest |

> **Ghi chú**: Npgsql sử dụng versioning riêng — `10.0.3` (không phải `10.0.11`).

---

## Database Migrations

```powershell
# Thêm migration mới
dotnet ef migrations add <MigrationName> \
  --project src/TradeFlow.Infrastructure \
  --startup-project src/TradeFlow.Web

# Cập nhật database
dotnet ef database update \
  --project src/TradeFlow.Infrastructure \
  --startup-project src/TradeFlow.Web
```

---

## Bảo mật / Security

- Encryption key: cung cấp qua User Secrets hoặc environment variable
- **Không bao giờ commit** connection string thật hoặc encryption key vào Git
- Audit log không bao giờ ghi plaintext mật khẩu
- Server-side authorization enforcement (frontend navigation filtering không đủ)

---

*Cập nhật lần cuối: Phase 1 UI Foundation — Design System + App Shell + Login*
