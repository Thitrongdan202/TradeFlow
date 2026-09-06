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
| **Phase 3** | Quản lý danh mục (Master Data: Sản phẩm, Đối tác, Kho hàng) | ✅ Hoàn thành |
| **Phase 3A** | Tinh chỉnh kiến trúc & Master Data Hardening (System Sequences, Business Codes, File Storage Abstraction) | ✅ Hoàn thành |
| Phase 4 | Bảng giá & Chiết khấu (Price lists, Excel import/export) | 🔄 Kế hoạch |
| Phase 5 | Bán hàng (Sales — Quotations, Orders, Invoices) | 🔄 Kế hoạch |
| Phase 6 | Mua hàng (Purchasing) | 🔄 Kế hoạch |
| Phase 7 | Quản lý kho (Warehouse transactions) | 🔄 Kế hoạch |
| Phase 8 | Báo cáo & Phân tích (Reports) | 🔄 Kế hoạch |

---

## 🏛️ Kiến trúc Tinh chỉnh Phase 3A / Architecture Refinements

### 1. Phân định Mã hệ thống (System Identifier) và Mã nghiệp vụ (Business Code)
- **Mã hệ thống (System Code)**: `SP000001`, `KH000001`, `NCC000001`, `KHO000001`, `DM000001`, `DV000001` được TradeFlow tự động sinh thông qua dịch vụ `ISystemCodeGenerator`.
  - Được lưu trữ tại trường `Code` của mỗi thực thể.
  - Mang tính duy nhất toàn cục, tuần tự và hiển thị dạng **chỉ đọc (Read-only)** trên giao diện người dùng. Người dùng không phải tự nhập mã hệ thống.
- **Mã nghiệp vụ (Business Codes)**:
  - `Product`: Phân tách rõ ràng giữa `NewCode` ("Mã hàng mới", vd: `TL2138`) phục vụ kinh doanh mới và `LegacyCode` ("Mã hàng cũ") để tra cứu lịch sử.
  - `Customer`, `Supplier`, `Warehouse`: Hỗ trợ trường `BusinessCode` ("Mã nội bộ") độc lập cho từng đối tác/kho.

### 2. Bộ sinh mã tuần tự an toàn đa luồng (System Code Generator)
- Dựa trên bảng cơ sở dữ liệu `SystemSequences` trong PostgreSQL.
- Quản lý bộ đếm số (`CurrentNumber`), bước nhảy (`Step`), tiền tố (`Prefix`) và mẫu định dạng (`FormatPattern` - mặc định `{Prefix}{Number:D6}`).
- Cơ chế đồng bộ bảo đảm an toàn đa luồng, không sinh mã ngẫu nhiên hoặc UUID làm mất tính tuần tự kế toán.

### 3. Cách ly Bảng giá & Chuẩn bị Hình ảnh sản phẩm
- Giá nhập và giá bán được loại bỏ hoàn toàn khỏi thực thể `Product`, sẵn sàng cho mô hình đa bảng giá (Phase 4 `PriceList` / `PriceListItem`).
- Thực thể `ProductImage` lưu trữ siêu dữ liệu hình ảnh trong PostgreSQL, kết hợp với giao diện trừu tượng `IFileStorageService` (`LocalFileStorageService`) lưu file vật lý bên ngoài CSDL để tối ưu hiệu năng.

---

## Yêu cầu / Prerequisites

| Công cụ | Phiên bản | Ghi chú |
|---------|-----------|---------|
| .NET SDK | 10.0.400+ | [Tải về](https://dotnet.microsoft.com/download/dotnet/10.0) |
| PostgreSQL | 15+ | [Tải về](https://www.postgresql.org/download/) |

---

## Cấu hình / Setup

1. Clone repository.
2. Thiết lập chuỗi kết nối (Connection String) cho PostgreSQL:

**Tùy chọn 1: Dùng appsettings.Development.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=TradeFlow;Username=postgres;Password=your_password"
  }
}
```

**Tùy chọn 2: Dùng .NET User Secrets (Khuyên dùng)**
Mở terminal tại thư mục src/TradeFlow.Web:
```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=TradeFlow;Username=postgres;Password=your_password"
```

---

## Tính năng khả dụng trong User Menu

- **Tài khoản của tôi** (`/tai-khoan`) — Xem thông tin cá nhân
- **Đổi mật khẩu** (`/tai-khoan/doi-mat-khau`) — Đổi mật khẩu
- **Quản lý người dùng** (`/cai-dat/nguoi-dung`) — (Dựa trên quyền Users:View)
- **Đăng xuất** (POST `/Account/Logout`)
  - Sử dụng Form Post tiêu chuẩn để tương tác trực tiếp với API Auth của Identity.
  - Huỷ session bảo mật và xóa cookies.
  - Chuyển hướng người dùng về trang Đăng nhập.
  - Ngăn chặn triệt để quyền truy cập vào các trang bảo mật.
  - Ghi Audit Log tự động sự kiện Logout.

---

## 🧭 Cấu trúc điều hướng / Navigation Structure

Thanh điều hướng bên trái (Sidebar) phản ánh các module của TradeFlow:

### Các nhóm menu hoạt động (Phase 1, 2, 3, 3A)

1. **Tổng quan** (`/`) — Dashboard quản trị và hoạt động gần đây.
2. **Danh mục** (Nhóm có thể mở rộng/thu gọn):
   - **Sản phẩm** (`/danh-muc/san-pham`) — Hiển thị cột Mã hệ thống, Mã hàng mới, Mã hàng cũ, Tên, Danh mục, ĐVT, Trạng thái. Tìm kiếm đa trường.
   - **Danh mục sản phẩm** (`/danh-muc/danh-muc-san-pham`, alias: `/danh-muc/nhom-san-pham`) — Quản lý cây danh mục sản phẩm.
   - **Đơn vị tính** (`/danh-muc/don-vi-tinh`) — Quản lý đơn vị tính (Cái, Bộ, Thùng, Kg, Mét...).
   - **Loại tiền** (`/danh-muc/loai-tien`) — Quản lý các loại tiền tệ (VND, USD...).
3. **Đối tác** (Nhóm có thể mở rộng/thu gọn):
   - **Khách hàng** (`/doi-tac/khach-hang`) — Quản lý hồ sơ khách hàng, mã hệ thống, mã nội bộ.
   - **Nhà cung cấp** (`/doi-tac/nha-cung-cap`) — Quản lý hồ sơ nhà cung cấp, mã hệ thống, mã nội bộ.
4. **Kho hàng** (Nhóm có thể mở rộng/thu gọn):
   - **Kho hàng** (`/kho/kho-hang`, alias: `/kho-hang`) — Quản lý danh sách kho, thủ kho, mã nội bộ.
5. **Hệ thống** (Cài đặt — Hub trung tâm tại `/cai-dat`):
   - **Người dùng** (`/cai-dat/nguoi-dung`) — Phân quyền theo Permission:Users:View.
   - **Vai trò** (`/cai-dat/vai-tro`) — Phân quyền theo Permission:Roles:View.
   - **Thông tin công ty** (`/cai-dat/cong-ty`) — Phân quyền theo Permission:CompanySettings:View.
   - **Nhật ký hoạt động** (`/cai-dat/nhat-ky`) — Phân quyền theo Permission:AuditLog:View.
   - **Hỗ trợ kỹ thuật** (`/cai-dat/ho-tro`) — Phân quyền theo Permission:TechnicalSupport:View.

---

## 🗺️ Danh sách Route Master Data

| Module | URL List | URL Thêm mới | URL Chỉnh sửa | Component |
|--------|----------|--------------|---------------|-----------|
| Sản phẩm | `/danh-muc/san-pham` | `/danh-muc/san-pham/them-moi` | `/danh-muc/san-pham/sua/{id}` | Pages/MasterData/Products/ |
| Danh mục SP | `/danh-muc/danh-muc-san-pham` (alias: `/danh-muc/nhom-san-pham`) | `/danh-muc/danh-muc-san-pham/them-moi` | `/danh-muc/danh-muc-san-pham/sua/{id}` | Pages/MasterData/Categories/ |
| Đơn vị tính | `/danh-muc/don-vi-tinh` | `/danh-muc/don-vi-tinh/them-moi` | `/danh-muc/don-vi-tinh/sua/{id}` | Pages/MasterData/Units/ |
| Loại tiền | `/danh-muc/loai-tien` | `/danh-muc/loai-tien/them-moi` | `/danh-muc/loai-tien/sua/{id}` | Pages/MasterData/Currencies/ |
| Khách hàng | `/doi-tac/khach-hang` | `/doi-tac/khach-hang/them-moi` | `/doi-tac/khach-hang/sua/{id}` | Pages/Partners/Customers/ |
| Nhà cung cấp | `/doi-tac/nha-cung-cap` | `/doi-tac/nha-cung-cap/them-moi` | `/doi-tac/nha-cung-cap/sua/{id}` | Pages/Partners/Suppliers/ |
| Kho hàng | `/kho/kho-hang` (alias: `/kho-hang`) | `/kho/kho-hang/them-moi` | `/kho/kho-hang/sua/{id}` | Pages/Warehouses/ |

---

## 🛠 Lệnh khởi chạy & Kiểm thử / Commands

Các lệnh thao tác dự án (đã được kiểm thử trong môi trường thực tế):

**Khởi chạy dự án:**
```powershell
dotnet run --project src/TradeFlow.Web
```

**Biên dịch (Build):**
```powershell
dotnet build TradeFlow.slnx
```

**Chạy kiểm thử (Test):**
```powershell
dotnet test TradeFlow.slnx
```

---

## Database Seeding & Danh sách tài khoản thử nghiệm

Seeder (`DatabaseSeeder.cs`) chạy tự động khi ứng dụng khởi động ở môi trường `Development`:
- Khởi tạo 6 chuỗi cấp mã hệ thống mặc định (`SP`, `KH`, `NCC`, `KHO`, `DM`, `DV`).
- Khởi tạo tiền tệ chuẩn (`VND`, `USD`) và đơn vị tính tiêu chuẩn (`Cái`, `Bộ`, `Chiếc`, `Thùng`, `Kilogram`, `Mét`).
- Đảm bảo đầy đủ các tài khoản mẫu cho từng vai trò nghiệp vụ với mật khẩu mặc định `tradecore123`. Tự động đặt lại mật khẩu nếu tài khoản đã tồn tại với hash cũ:

| Tên đăng nhập | Mật khẩu | Họ và tên | Vai trò | Mô tả quyền hạn |
|---------------|----------|-----------|---------|-----------------|
| `admin` | `tradecore123` | Quản trị viên | `Administrator` | Toàn quyền hệ thống (bỏ qua kiểm tra quyền tĩnh, truy cập mọi tính năng) |
| `quanly01` | `tradecore123` | Trần Quản Lý | `Manager` | Quản lý kinh doanh, xem và chỉnh sửa danh mục, đối tác, kho, báo cáo |
| `kinhdoanh01` | `tradecore123` | Nguyễn Văn Kinh Doanh | `Sales` | Xem danh mục sản phẩm, quản lý khách hàng, thao tác bán hàng |
| `muahang01` | `tradecore123` | Lê Thị Mua Hàng | `Purchase` | Xem danh mục sản phẩm, quản lý nhà cung cấp, mua hàng |
| `kho01` | `tradecore123` | Phạm Thủ Kho | `Warehouse` | Quản lý kho hàng, xem sản phẩm, thực hiện các giao dịch kho |
| `xnk01` | `tradecore123` | Hoàng Xuất Nhập Khẩu | `Import-Export` | Quản lý danh mục hàng hóa, đối tác xuất nhập khẩu, ngoại tệ |

---

## 🔐 Kiến trúc Xác thực & Phân quyền / Authentication & Permission Architecture

TradeFlow sử dụng mô hình xác thực dựa trên cookie của ASP.NET Core Identity kết hợp với cơ chế phân quyền động phía máy chủ (Server-side Permission Evaluation):

### 1. Cơ chế phòng chống phình Cookie (Cookie Bloat Prevention — HTTP 431 / 400 Fix)
- **Vấn đề trước đây**: Khi người dùng đăng nhập, nếu nhồi toàn bộ ma trận quyền (32 tài nguyên × 4 thao tác = 128 claims) vào `ClaimsPrincipal`, cookie xác thực `.AspNetCore.Identity.Application` bị phình to (8–12 KB), bị phân mảnh thành nhiều cookie (`ApplicationC1`, `ApplicationC2`), và kích hoạt lỗi HTTP 431 (*Request Header Fields Too Large*) hoặc HTTP 400 từ Kestrel.
- **Giải pháp TradeFlow**:
  - `CustomUserClaimsPrincipalFactory` chỉ lưu các claim nhận diện cơ bản: `UserId`, `UserName`, `Email`, `FullName`, `Status` và danh sách vai trò (`Role`).
  - Toàn bộ ma trận quyền được loại bỏ khỏi cookie, giữ cho cookie xác thực luôn ở mức siêu nhẹ (~920–950 bytes), không bị phân mảnh và tuyệt đối an toàn với HTTP headers.
  - Phân quyền được đánh giá động tại máy chủ thông qua dịch vụ `IPermissionService`.

### 2. Dịch vụ Phân quyền Máy chủ & Bộ nhớ đệm (`IPermissionService`)
- Khi một trang hoặc thành phần Blazor kiểm tra quyền (thông qua `@attribute [Authorize(Policy = "...")]` hoặc `<AuthorizeView Policy="...">`), `PermissionAuthorizationHandler` sẽ gọi `IPermissionService.HasPermissionAsync`:
  - Người dùng thuộc vai trò `Administrator` được duyệt ngay lập tức (Fast-path).
  - Với các vai trò khác, danh sách quyền được nạp từ cơ sở dữ liệu (`RolePermissions`) và lưu vào `IMemoryCache` trong 15 phút.
  - Khi quản trị viên cập nhật quyền trong form `RoleForm` hoặc `UserForm`, cache được tự động làm mới ngay lập tức qua `InvalidateAllPermissions()` hoặc `InvalidateUserPermissions(userId)`.

### 3. An toàn đa luồng EF Core trong Blazor Pre-rendering
- Trong quá trình Blazor SSR / Interactive pre-rendering, nhiều thành phần `<AuthorizeView>` trên cùng một trang được đánh giá song song. Vì `DbContext` của Entity Framework Core không hỗ trợ truy cập đồng thời, `PermissionService` tạo phạm vi dịch vụ độc lập (`IServiceScopeFactory.CreateAsyncScope()`) kết hợp với `SemaphoreSlim` để điều phối tải dữ liệu, ngăn chặn triệt để ngoại lệ `InvalidOperationException: A second operation was started on this context instance`.

### 4. Nút Ẩn/Hiện mật khẩu thuần JavaScript (Client-side Password Toggle)
- Nút con mắt xem mật khẩu trong trang đăng nhập (`PasswordToggleInput.razor`) hoạt động hoàn toàn bằng JavaScript ở client (`type="button"`, gọi hàm `tfTogglePassword`).
- Không sử dụng `@rendermode InteractiveServer` lồng bên trong SSR Form, đảm bảo:
  - Form đăng nhập tĩnh hoạt động độc lập không cần SignalR circuit.
  - Bấm nút xem mật khẩu không kích hoạt submit form hay tải lại trang.
  - Giá trị mật khẩu được giữ nguyên hoàn toàn khi chuyển đổi kiểu hiển thị.

---

## 🔄 Quy trình Đăng nhập & Đăng xuất / Login & Logout Flow

### Quy trình Đăng nhập (`/Account/Login`):
1. Người dùng gửi thông tin đăng nhập (Tên đăng nhập hoặc Email cùng Mật khẩu).
2. Form SSR POST đến `/Account/Login` có gắn mã chống giả mạo `__RequestVerificationToken`.
3. Kiểm tra trạng thái tài khoản: nếu `Locked` sẽ điều hướng đến `/Account/Lockout`; nếu không phải `Active` sẽ thông báo tài khoản bị vô hiệu hóa.
4. `SignInManager.PasswordSignInAsync` xác thực thông tin, cấp 1 cookie xác thực duy nhất (< 1 KB) và phản hồi HTTP 302 Found chuyển hướng về trang chủ (`/`) hoặc `ReturnUrl`.

### Quy trình Đăng xuất (POST `/Account/Logout`):
1. Giao diện người dùng sử dụng form POST tiêu chuẩn đến `/Account/Logout`.
2. Hệ thống thu hồi phiên đăng nhập, xóa sạch cookie `.AspNetCore.Identity.Application` (đặt `Expires` về quá khứ).
3. Ghi vết `AuditLog` (sự kiện Logout) và chuyển hướng về `/Account/Login`.
4. Mọi yêu cầu tiếp theo đến các trang bảo mật (`/cai-dat`, `/danh-muc/*`, `/kho/*`) đều bị chặn và tự động chuyển hướng về trang Đăng nhập.

---

## 🛠 Hướng dẫn khắc phục sự cố / Troubleshooting

### 1. Lỗi HTTP 431 hoặc HTTP 400 khi Đăng nhập
- **Nguyên nhân**: Kích thước request header vượt quá giới hạn của Kestrel do cookie xác thực bị phình to.
- **Khắc phục**: Đảm bảo không ghi đè claims permission vào `CustomUserClaimsPrincipalFactory`. Xác minh kích thước cookie luôn < 1 KB bằng `curl` hoặc Chrome DevTools.

### 2. Mật khẩu tài khoản mẫu không đăng nhập được
- **Nguyên nhân**: Database đã có tài khoản mẫu từ phiên bản trước nhưng hash mật khẩu cũ không khớp.
- **Khắc phục**: `DatabaseSeeder.cs` tự động gọi `ResetPasswordAsync` về `tradecore123` mỗi khi ứng dụng khởi động ở môi trường Development. Chỉ cần restart ứng dụng để đồng bộ lại.

### 3. DbContext Concurrency Error khi tải trang có nhiều AuthorizeView
- **Nguyên nhân**: Dùng chung một scoped `DbContext` qua nhiều tác vụ phân quyền bất đồng bộ chạy đồng thời.
- **Khắc phục**: Luôn truy vấn cơ sở dữ liệu phân quyền thông qua `IServiceScopeFactory.CreateAsyncScope()` như đã hiện thực trong `PermissionService.cs`.

### 4. Loi #blazor-error-ui (Da xay ra loi khong mong doi) ngay sau khi dang nhap
- **Nguyen nhan**: Thieu middleware UseAuthentication/UseAuthorization trong HTTP pipeline khien circuit WebSocket cua Blazor InteractiveServer khong the tai dung claims dang nhap. Dong thoi viec su dung @rendermode InteractiveServer long nhau sai cach (vi du tren UserMenu.razor) tao ra cac interactive islands doc lap lam dut gay CascadingParameter Task<AuthenticationState>.
- **Khac phuc**: Middleware bao mat da duoc dua vao dung thu tu trong Program.cs. @rendermode chi dung o muc Routes (App.razor) hoac trang doc lap; cac component con (nhu UserMenu) se ke thua render mode tu cha.
