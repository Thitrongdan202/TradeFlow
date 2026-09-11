# TradeFlow

Hệ thống quản lý thương mại toàn diện (ERP/CRM) xây dựng trên nền tảng .NET 10, ASP.NET Core, Blazor và PostgreSQL.

## Lộ trình phát triển (Roadmap)

- [x] **Phase 1**: Nền tảng .NET / Blazor / PostgreSQL
- [x] **Phase 1.5**: UI/UX Foundation
- [x] **Phase 2**: Authentication + User + Role + Permission (Kiến trúc phân quyền động, fix lỗi cookie bloat, fix lỗi Circuit NullReferenceException)
- [x] **Phase 3A**: Chuẩn hóa Master Data + mã hệ thống
- [x] **Phase 3B**: Kiểm tra quan hệ dữ liệu + UX
- [x] **Phase 4**: Bảng giá + Excel + hình ảnh + lịch sử giá
- [x] **Phase 5**: Bán hàng + Đơn bán hàng + Hóa đơn + PDF
- [ ] **Phase 6**: Báo giá + tài liệu/chứng từ (Đã dời từ Phase 5)
- [ ] **Phase 7**: Mua hàng
- [ ] **Phase 8**: Kho
- [ ] **Phase 9**: XNK
- [ ] **Phase 10**: Báo cáo

---

## Các tính năng hoàn thành trong Phase 4

Phase 4 tập trung vào nghiệp vụ Bảng giá và xử lý file Excel dung lượng lớn.

*   **Thao tác xóa bảng giá an toàn**: Tách biệt rõ ràng 2 hành động: "Hủy bảng giá" (chuyển trạng thái Đã hủy, giữ nguyên lịch sử) và "Xóa vĩnh viễn". Việc xóa vĩnh viễn được bảo vệ bởi **Reference Integrity**: nếu bảng giá đang được sử dụng bởi các Đơn bán hàng (Sales Order) hoặc Hóa đơn đã chốt, hệ thống sẽ chặn xóa vật lý để đảm bảo an toàn dữ liệu.
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


## Cac tinh nang hoan thanh trong Phase 5 (Ban hang & Hoa don)
* **Tao Don ban hang (Sales Order)**:
  * Ho tro tim kiem, them san pham va tu dong ap dung gia ban moi nhat tu PriceList hien tai (Phase 4).
  * Tu dong tinh toan tong tien, chiet khau va thue.
* **Hoa don (Invoice)**:
  * Tu dong sinh hoa don khi Don ban hang duoc xac nhan.
  * Hoa don bao ton thong tin (Snapshot) tai thoi diem xuat de khong bi thay doi boi Master Data sau nay.
* **Xuat PDF Hoa don**: Mo phong xuat PDF hoa don truc tiep tren trinh duyet.
* **Audit Logging**: Moi thao tac tao, cap nhat, huy don hang va hoa don deu duoc ghi lai.

## Xóa dữ liệu (Delete Functionality)
- **Master Data**: Đã thêm chức năng Xóa an toàn tại cả danh sách (List) và biểu mẫu (Form).
- **Nguyên tắc**: Xóa vật lý các bản ghi chưa sử dụng. Nếu dữ liệu đang bị ràng buộc (ví dụ Sản phẩm trong Đơn hàng), hệ thống sẽ vô hiệu hóa (soft-delete) hoặc chặn lệnh xóa kèm theo thông báo lỗi rõ ràng.
- **Bảng giá, Đơn bán hàng, Hóa đơn**: Áp dụng quy tắc tương tự, không xóa các chứng từ đã xác nhận/phát hành.
- **Bảo mật**: Sử dụng RBAC server-side xác thực quyền người dùng. Mọi thao tác xóa thành công đều được lưu vết qua AuditService.


### Cập nhật cấu trúc Giá bán (Pricing Flow)
- **Giá sản phẩm là tùy chọn**: Một Sản phẩm (Product) có thể không cần thiết lập giá cố định. Khi thêm vào đơn hàng nếu chưa có giá, hệ thống sẽ báo "Chưa có giá".
- **Lấy giá từ Bảng giá (PriceList)**: Người dùng có thể chủ động chọn nguồn giá là một Bảng giá cụ thể (ví dụ: Q3/2026), hệ thống sẽ tự động điền đơn giá.
- **Nhập giá thủ công**: Hỗ trợ nhập giá thủ công (chỉnh sửa trực tiếp đơn giá trên đơn bán hàng). Giá này chỉ áp dụng riêng cho dòng đơn hàng đó, không ảnh hưởng đến dữ liệu Master.
- **Hóa đơn giữ giá cuối**: Hóa đơn (Invoice) luôn luôn sử dụng chính xác đơn giá cuối cùng đã được lưu trên Đơn bán hàng (bao gồm cả giá thủ công), không tra cứu lại bảng giá.

## Hóa đơn (Phase 5)

Tính năng Hóa đơn được thiết kế chuyên nghiệp, hỗ trợ các quy chuẩn thực tế (01/GTGT và 02/BH):
* **Tự động sinh từ Đơn bán hàng**: Khi xác nhận một Đơn bán hàng, Hóa đơn sẽ được tự động tạo với toàn bộ dữ liệu (Sản phẩm, Khách hàng, Giá bán) được sao chép nguyên trạng (Snapshot), đảm bảo dữ liệu hóa đơn không bị ảnh hưởng nếu Master Data thay đổi sau này.
* **Tạo Hóa đơn thủ công (Manual Invoice)**:
  * Hỗ trợ tạo hóa đơn trực tiếp mà không cần thông qua Đơn bán hàng (Nút "+ Tạo hóa đơn").
  * Hỗ trợ tìm kiếm khách hàng/sản phẩm có sẵn HOẶC nhập tay khách hàng vãng lai (không tự động lưu vào Master Data để tránh rác dữ liệu).
  * Hỗ trợ thêm các dòng hàng hóa/dịch vụ thủ công không có trong danh mục.
  * Hỗ trợ nhập trực tiếp đơn giá, thuế suất, và tính toán thành tiền tự động.
* **Loại Hóa đơn**:
  * **Hóa đơn GTGT (01/GTGT)**: Hiển thị đầy đủ các cột thuế suất, tiền thuế, và tổng hợp trước/sau thuế.
  * **Hóa đơn Bán hàng (02/BH)**: Giao diện tối giản, ẩn các cột thuế theo quy định.
* **Web Preview & Xuất PDF**:
  * Sử dụng thư viện QuestPDF tạo file PDF vector chất lượng cao (A4), hiển thị hoàn hảo tiếng Việt (UTF-8).
  * Bản xem trước trên Web sử dụng chính template PDF để đảm bảo tính đồng nhất 100% giữa nội dung xem trước và bản in.
* **Xác nhận của Công ty**: Hỗ trợ hiển thị vùng "Xác nhận của công ty (Signature Valid)" dựa trên dữ liệu cấu hình trong hệ thống (CompanySettings).
