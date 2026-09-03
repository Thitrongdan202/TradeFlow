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

---

## Cấu hình / Setup

1. Clone repository.
2. Thiết lập chuỗi kết nối (Connection String) cho PostgreSQL:

**Tùy chọn 1: Dùng \ppsettings.Development.json\ (Không khuyên dùng nếu code công khai)**
\\\json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=TradeFlow;Username=postgres;Password=your_password"
  }
}
\\\

**Tùy chọn 2: Dùng .NET User Secrets (Khuyên dùng)**
Mở terminal tại thư mục \src/TradeFlow.Web\:
\\\powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=TradeFlow;Username=postgres;Password=your_password"
\\\

---

## Tính năng khả dụng trong User Menu

- **Tài khoản của tôi** (\/tai-khoan\) — Xem thông tin cá nhân
- **Đổi mật khẩu** (\/tai-khoan/doi-mat-khau\) — Đổi mật khẩu
- **Quản lý người dùng** (\/cai-dat/nguoi-dung\) — (Dựa trên quyền \Users:View\)
- **Đăng xuất** (POST \/Account/Logout\)
  - Sử dụng Form Post tiêu chuẩn (không qua AJAX) để tương tác trực tiếp với API Auth của Identity.
  - Huỷ session bảo mật và xóa cookies.
  - Chuyển hướng người dùng về trang Đăng nhập.
  - Ngăn chặn triệt để quyền truy cập vào các trang bảo mật.
  - Ghi Audit Log tự động sự kiện \Logout\.

---

## 🧭 Cấu trúc điều hướng / Navigation Structure

Thanh điều hướng bên trái (Sidebar) phản ánh các module của TradeFlow.
**Lưu ý:** Các chức năng chưa hoàn thiện trong Phase 3+ (Bán hàng, Mua hàng, Kho, Đối tác, Báo cáo) được ẩn khỏi UI để tránh tình trạng link chết (Dead links), nhưng cấu trúc HTML vẫn được duy trì trong source code để tiếp tục phát triển.

**Tính năng hiển thị theo vai trò (Role-based Navigation):**
Các mục trong **Hệ thống / Cài đặt** sẽ chỉ hiển thị nếu người dùng có quyền tương ứng:
- Người dùng (Yêu cầu \Permission:Users:View\)
- Vai trò (Yêu cầu \Permission:Roles:View\)
- Thông tin công ty (Yêu cầu \Permission:CompanySettings:View\)
- Nhật ký hoạt động (Yêu cầu \Permission:AuditLog:View\)
- Hỗ trợ kỹ thuật (Yêu cầu \Permission:TechnicalSupport:View\)

### Cài đặt chung

Sidebar chứa nút **Cài đặt** dẫn đến hub trung tâm tại \/cai-dat\. Click vào Text/Icon "Cài đặt" sẽ chuyển hướng tới \/cai-dat\. Click vào biểu tượng mũi tên sẽ mở rộng/thu gọn danh sách menu con.

## 🛡️ Bảo vệ định tuyến / Protected Routes

Toàn bộ ứng dụng sử dụng \<AuthorizeRouteView>\ để kiểm soát truy cập:
- Truy cập khi chưa đăng nhập → Chuyển hướng đến \/Account/Login\
- Truy cập URL không có quyền → Thông báo Access Denied
- Sau khi nhấn Đăng xuất → Mọi trang bảo vệ đều yêu cầu đăng nhập lại (dù có nhấn Back trên trình duyệt).

---

## 🛠 Lệnh khởi chạy & Kiểm thử / Commands

Các lệnh thao tác dự án (đã được kiểm thử trong môi trường thực tế):

**Khởi chạy dự án:**
\\\powershell
dotnet run --project src/TradeFlow.Web
\\\

**Biên dịch (Build):**
\\\powershell
dotnet build TradeFlow.slnx
\\\

**Chạy kiểm thử (Test):**
\\\powershell
dotnet test TradeFlow.slnx --no-build
\\\

**Dừng ứng dụng (Shutdown):**
- Trên terminal đang chạy ứng dụng, nhấn \Ctrl + C\.

---

## Database Seeding

Seeder chạy tự động khi ứng dụng khởi động ở môi trường \Development\.

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
