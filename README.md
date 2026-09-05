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

## Database Seeding

Seeder chạy tự động khi ứng dụng khởi động ở môi trường Development:
- Các tài khoản mặc định: `admin`, `quanly01`, `kinhdoanh01`, `muahang01`, `kho01`, `xnk01` với mật khẩu `tradecore123`.
- Khởi tạo 6 chuỗi cấp mã hệ thống mặc định (`SP`, `KH`, `NCC`, `KHO`, `DM`, `DV`).
- Khởi tạo đơn vị tiền tệ (`VND`, `USD`) và đơn vị tính tiêu chuẩn (`Cái`, `Bộ`, `Chiếc`, `Thùng`, `Kilogram`, `Mét`).