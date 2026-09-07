# TradeFlow

Hệ thống quản lý thương mại toàn diện (ERP/CRM) xây dựng trên nền tảng .NET 10, ASP.NET Core, Blazor và PostgreSQL.

## Lộ trình phát triển (Roadmap)

- [x] **Phase 1**: Nền tảng .NET / Blazor / PostgreSQL
- [x] **Phase 1.5**: UI/UX Foundation
- [x] **Phase 2**: Authentication + User + Role + Permission (Kiến trúc phân quyền động, fix lỗi cookie bloat, fix lỗi Circuit NullReferenceException)
- [x] **Phase 3A**: Chuẩn hóa Master Data + mã hệ thống
- [x] **Phase 3B**: Kiểm tra quan hệ dữ liệu + UX
- [x] **Phase 4**: Bảng giá + Excel + hình ảnh + lịch sử giá
- [ ] **Phase 5**: Báo giá + tài liệu/chứng từ + PDF
- [ ] **Phase 6**: Đơn bán hàng + hóa đơn
- [ ] **Phase 7**: Mua hàng
- [ ] **Phase 8**: Kho
- [ ] **Phase 9**: XNK
- [ ] **Phase 10**: Báo cáo

---

## Các tính năng hoàn thành trong Phase 4

Phase 4 tập trung vào nghiệp vụ Bảng giá và xử lý file Excel dung lượng lớn.

*   **Quản lý Bảng giá (Price List)**: Hỗ trợ tạo mới, chỉnh sửa, và xem danh sách bảng giá / báo giá với đầy đủ siêu dữ liệu (Số báo giá, Ngày hiệu lực, Tháng/Quý/Năm áp dụng).
*   **Lịch sử giá**: Đảm bảo các hồ sơ giá trong quá khứ là bất biến (immutable). Hỗ trợ tra cứu lịch sử thay đổi giá của một sản phẩm qua nhiều kỳ.
*   **So sánh bảng giá**: Tính năng so sánh giá kỳ trước và kỳ sau, làm nổi bật trạng thái Tăng giá, Giảm giá, hoặc Không đổi.
*   **Nhập dữ liệu từ Excel (Excel Import)**:
    *   Hỗ trợ cấu hình tải lên file Excel khổng lồ với **giới hạn 200 MB**.
    *   Sử dụng luồng (stream) trực tiếp xuống đĩa vật lý (`TempFileReference`) để tránh tràn RAM khi đọc file lớn.
    *   Hỗ trợ **Dry Run**: Phân tích file, bắt lỗi cấu trúc, tìm dòng tiêu đề thông minh bằng từ khóa (NO, MÃ HÀNG MỚI, THÔNG TIN SẢN PHẨM, GIÁ...), và kiểm tra tính hợp lệ trước khi xác nhận.
*   **Xử lý Hình ảnh từ Excel**: Tự động giải nén file `.xlsx` dưới dạng ZIP (OpenXML), trích xuất và phân tích các relationship XML để ánh xạ hình ảnh nhúng vào đúng dòng chứa sản phẩm tương ứng. Hình ảnh được lưu trữ an toàn bằng `IFileStorageService`.
*   **Tải File Gốc & Xuất Excel**:
    *   **Tải Excel Gốc**: Khách hàng luôn có thể tải lại chính xác file Excel nguyên bản ban đầu đã dùng để import.
    *   **Xuất Excel (Export)**: Sinh file Excel báo giá chuẩn hóa trực tiếp từ dữ liệu trong PostgreSQL qua thư viện ClosedXML.
*   **RBAC & Audit**: Mọi hành vi (tạo bảng giá, nhập Excel, xuất Excel, tải file gốc) đều được phân quyền chặt chẽ bằng Role-Based Access Control tại server và ghi nhận đầy đủ vào hệ thống Nhật ký hoạt động (Audit Log).

## Tối ưu hóa Import Excel & Tiến trình\n\n*   **Tiến trình thời gian thực**: Giao diện hiển thị trực tiếp các bước xử lý (đọc file, phân tích Excel, trích xuất dữ liệu, trích xuất hình ảnh, đối chiếu danh mục). Bỏ qua việc trích xuất hình ảnh của các dòng trống/không hợp lệ.\n*   **Hủy bỏ an toàn (Cancellation)**: Hỗ trợ thời gian timeout an toàn để chống treo ứng dụng (5 phút cho đọc file, 2 phút cho lưu DB).\n\n## Quản lý Storage & Dữ liệu Riêng tư\n\n*   **Dữ liệu riêng tư**: Mọi file Excel tải lên đều được lưu bên ngoài thư mục được theo dõi bởi Git để bảo vệ dữ liệu công ty.\n*   **Dọn dẹp hệ thống (Cleanup)**: Hệ thống cho phép người dùng Hủy bỏ trong quá trình import hoặc Xóa file gốc sau khi đã import thành công để giải phóng dung lượng đĩa mà không ảnh hưởng đến dữ liệu đã lưu trong cơ sở dữ liệu.\n\n## Trải nghiệm Người dùng (UI/UX) và Khắc phục lỗi

*   **Menu Chức năng (Sidebar)**:
    *   Hiển thị đúng các nghiệp vụ của Phase 3/4 (Tổng quan, Danh mục, Đối tác, Kho hàng, Bảng giá, Cài đặt).
    *   Hỗ trợ mở rộng/thu gọn nhóm linh hoạt. Nút nhóm mẹ không bị "active" sai lệch khi ở trang khác.
*   **Menu Người dùng (Top-right Dropdown)**:
    *   Hiển thị thông tin người dùng đang đăng nhập.
    *   Dropdown mở/đóng mượt mà.
    *   Chứa các liên kết "Tài khoản của tôi" (Hồ sơ), "Đổi mật khẩu", và "Đăng xuất".
*   **Quy trình Đăng nhập / Đăng xuất an toàn**: Đã xử lý triệt để lỗi mất Context khi render trang Đăng nhập tĩnh (SSR). Đăng xuất sẽ xóa cookie và chuyển hướng an toàn về trang Đăng nhập.

---

## Lệnh khởi chạy & Kiểm thử (Commands)

Các lệnh sau đã được chạy thử và xác nhận hoạt động ổn định trên môi trường Windows.

**1. Khởi chạy ứng dụng:**
```powershell
# Chạy dự án Web với môi trường Development
$env:ASPNETCORE_ENVIRONMENT = 'Development'; dotnet run --project src/TradeFlow.Web --launch-profile "https"
```

**2. Biên dịch dự án (Build):**
```powershell
# Kiểm tra không có lỗi cú pháp hoặc cảnh báo nghiêm trọng
dotnet build --configuration Release
```

**3. Chạy kiểm thử tự động (Test):**
```powershell
# Chạy 61 Unit Tests và Integration Tests
dotnet test --configuration Release
```

**4. Áp dụng Cập nhật Cơ sở dữ liệu (Migrations):**
```powershell
dotnet ef database update --project src/TradeFlow.Infrastructure --startup-project src/TradeFlow.Web
```

---

## Cấu hình Kỹ thuật Cốt lõi

*   **Cơ sở dữ liệu**: PostgreSQL
*   **ORM**: Entity Framework Core 10
*   **Web Framework**: ASP.NET Core 10 Blazor (Per-Page Interactivity)
*   **Xử lý Excel**: ClosedXML + System.IO.Compression (OpenXML Image Extraction)
*   **File Storage**: Lưu trữ an toàn ngoài mã nguồn chính (không commit file người dùng lên Git).

## Quản trị Dữ liệu mẫu (Database Seeding)

Khi chạy ở môi trường Development, `DatabaseSeeder.cs` sẽ tự động tạo dữ liệu mẫu và các tài khoản kiểm thử nếu chúng chưa tồn tại:
*   Tài khoản Admin: `admin` / `tradecore123`
*   Các vai trò và phân quyền cơ bản đã được thiết lập sẵn. Mọi tài khoản mới có mật khẩu mặc định là `tradecore123`.

