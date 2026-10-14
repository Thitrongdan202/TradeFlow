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
- [x] **Phase 6**: Báo giá + tài liệu/chứng từ (Đã dời từ Phase 5)
- [x] **Phase 6.5**: Quản lý Hợp đồng Thương mại & Ký điện tử (Vietnamese Contract Management & E-Sign)
- [ ] **Phase 7**: Mua hàng
- [ ] **Phase 8**: Kho
- [x] **Phase 8.5**: Quản lý Nhân sự & Nhân viên (Employee Management)
- [x] **Phase 9**: Nhận diện thương hiệu & Màn hình tải toàn cục (Company Branding & Global Loading Screen)
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

## Quản lý Nhân sự & Nhân viên (Phase 8 - Employee Management)

Module quản lý nhân viên chuyên biệt cho TradeFlow, bám sát nghiệp vụ và giao diện chuẩn ERP/POS:
* **Điều hướng (Navigation)**:
  * Thêm mục "Nhân viên" trên thanh điều hướng bên trái với menu fly-out chuyên nghiệp gồm:
    * "Danh sách nhân viên" (`/nhan-vien`)
    * "Duyệt yêu cầu" (`/nhan-vien?tab=requests`)
* **Danh sách nhân viên (`/nhan-vien`)**:
  * **Bộ lọc bên trái**: Lọc theo trạng thái ("Đang làm việc" vs "Đã nghỉ"), Phòng ban (kèm nút "+ Tạo mới"), Chức danh (kèm nút "+ Tạo mới").
  * **Bảng danh sách**: Hiển thị ảnh đại diện (Avatar), Mã NV (`NV00001`), Mã chấm công, Tên NV, SĐT, CMND/CCCD, Nợ & tạm ứng, Ghi chú.
  * **Mở rộng & Chỉnh sửa trực tiếp (Inline Expansion & Editing)**:
    * Bấm vào dòng nhân viên để mở rộng vùng chi tiết 3 cột đầy đủ (Thông tin nhân sự, Vị trí & tổ chức, Hệ thống & tài khoản TradeFlow liên kết).
    * Hỗ trợ nút "Chỉnh sửa" để kích hoạt chế độ chỉnh sửa ngay trong vùng mở rộng, lưu trực tiếp mà không cần rời trang.
* **Quy trình Yêu cầu & Phê duyệt (Employee Creation Requests)**:
  * Cho phép người dùng gửi "Yêu cầu thêm nhân viên" đề xuất nhân sự mới.
  * Màn hình / Modal "Duyệt yêu cầu" cho cấp quản lý để duyệt (tự động tạo hồ sơ nhân viên `Active`) hoặc từ chối kèm lý do.
* **Quy trình Thôi việc (Employee Offboarding)**:
  * Hỗ trợ chuyển trạng thái nhân viên sang "Đã nghỉ", lưu ngày thôi việc và lý do.
  * Tích hợp với `SecurityService.OffboardEmployeeAsync`: Tự động vô hiệu hóa tài khoản liên kết, thu hồi phiên đăng nhập và chứng thư chữ ký số nội bộ.
* **Tạo nhanh danh mục (Quick Create)**: Hỗ trợ tạo nhanh Phòng ban và Chức danh ngay trên màn hình thao tác.

---

## Nhận diện Thương hiệu & Màn hình Tải Toàn cục (Phase 9 - Company Branding & Global Loading Screen)

Tính năng tùy biến thương hiệu và màn hình tải ứng dụng mang đậm phong cách nhận diện doanh nghiệp:

* **Màn hình tải toàn cục mang thương hiệu (Company-Branded Loading Screen)**:
  * **Logo công ty cố định ở vị trí trung tâm (Stationary Center Logo)**: Sử dụng logo thực tế của doanh nghiệp (mặc định là logo LACASA). Bảo toàn 100% độ trong suốt của ảnh gốc, không bị biến dạng tỷ lệ (`object-fit: contain`), không thêm khối vuông màu trắng ép buộc. Khi chưa có ảnh logo, hệ thống tự động sinh chữ viết tắt thương hiệu tinh tế.
  * **Vòng xoay mềm mại bao quanh (Rotating Ring Animation)**: Vòng tròn vector SVG xoay êm ái quanh logo với các đầu nét bo tròn mềm mại (`stroke-linecap="round"`), đường nét mảnh thanh lịch (~3.5px), kết hợp đường ray mờ mờ tinh tế bên dưới. Hỗ trợ chuẩn trợ năng `prefers-reduced-motion` tự động giảm chuyển động khi người dùng yêu cầu.
  * **Lớp phủ nền làm mờ (Global Overlay)**: Phủ mờ toàn màn hình với màu trắng bán trong suốt `rgba(255, 255, 255, 0.82)` kết hợp hiệu ứng kính mờ `backdrop-filter: blur(5px)`.
  * **Dịch vụ điều khiển trạng thái (`ILoadingService`)**: Phạm vi Scoped trên Blazor circuit cung cấp các hàm `Show(text)` / `Hide()` và `ShowAsync` / `HideAsync`. Tự động đếm tác vụ lồng nhau (nested operations) và tự động kích hoạt mượt mà khi ứng dụng khởi chạy lần đầu (~750ms).
* **Trang Quản lý Thương hiệu & Màn hình Tải (`/cai-dat/thuong-hieu`)**:
  * **Quản lý Tệp Logo**: Hỗ trợ định dạng PNG, SVG, WebP, JPG với dung lượng tối đa 5MB. Xác thực nghiêm ngặt Magic Bytes của tệp tại server để chống tệp giả mạo. Lưu trữ file an toàn trong `wwwroot/uploads/branding/` và không commit lên Git.
  * **Tùy biến Nhận diện & Trạng thái**:
    * Nhập Tên doanh nghiệp.
    * Tùy chọn bật/tắt hiển thị Tên công ty bên dưới vòng xoay.
    * Tùy chọn bật/tắt toàn bộ Màn hình tải toàn cục (Global Loading Screen).
  * **Tùy biến Vòng xoay (Spinner Settings)**:
    * Bảng chọn màu (Color Picker) + Nhập mã màu Hex trực tiếp + 6 Bảng màu thiết kế sẵn (Emerald #10b981, Deep Teal #0f766e, Royal Blue #2563eb, Indigo #6366f1, Purple #8b5cf6, Coral Orange #f97316).
    * Thanh trượt điều chỉnh Độ trong suốt (Opacity: 10% – 100%).
    * Lựa chọn Tốc độ xoay: Chậm (2.4s), Bình thường (1.4s), Nhanh (0.8s).
  * **Xem trước Trực tiếp Thời gian thực (Live Interactive Preview)**:
    * Khu vực xem trước ngay trên giao diện cài đặt mô phỏng môi trường làm việc ERP thật.
    * Tự động cập nhật tức thì (100% Real-time) theo từng thao tác kéo thanh trượt, đổi mã màu, nhập tên hoặc thay logo mà chưa cần nhấn Lưu.
    * Nút "Chạy thử toàn màn hình" kích hoạt lớp phủ toàn cục trong 2 giây để kiểm tra trải nghiệm người dùng thực tế.
  * **Cơ chế Cache & Nhật ký Kiểm toán (Audit Logging)**:
    * Tích hợp `IMemoryCache` lưu đệm cấu hình thương hiệu (10 phút) để tối ưu hiệu năng trang web, tự động xóa cache khi có cập nhật mới.
    * Tự động ghi nhận mọi thao tác tải lên, cập nhật, xóa logo vào hệ thống Nhật ký hoạt động (`AuditLog`).

---

## Nâng cấp Giao diện & Trải nghiệm Người dùng (UI/UX Upgrade — Responsive Navigation, Product Management, Excel Import & Filters)

Bản nâng cấp toàn diện giao diện và trải nghiệm tương tác lấy cảm hứng từ các chuẩn mực ERP/POS thương mại hiện đại (KiotViet):

1. **Thanh điều hướng Đa nền tảng & Co giãn thông minh (Responsive & Collapsible Navigation)**:
   - **Chế độ Mở rộng (Expanded)**: Hiển thị đầy đủ logo, nhận diện thương hiệu, tên phân hệ tiếng Việt, biểu tượng SVG đồng bộ và menu con dạng fly-out hoặc accordion.
   - **Chế độ Thu gọn (Compact Collapsed - 68px)**: Nút bật/tắt (toggle) rõ ràng ở thanh trên cùng (Topbar) và chân sidebar (Bottom dock), tự động căn giữa các icon phân hệ, hỗ trợ tooltip tiếng Việt và hiển thị fly-out menu nổi bên cạnh khi rê chuột.
   - **Lưu trữ tùy chọn người dùng (User Preference Persistence)**: Tự động ghi nhớ trạng thái thu gọn/mở rộng trong `localStorage` qua JavaScript helper (`window.tradeflow`).
   - **Trải nghiệm trên màn hình di động/tablet (Mobile Drawer)**: Tự động thu gọn thanh menu thành drawer ẩn, trượt ra mượt mà với lớp phủ mờ tinh tế (`.tf-sidebar-backdrop`), nút đóng `✕`, và lắng nghe phím `Escape` để đóng menu nhanh chóng.

2. **Đơn giản hóa Quản lý Sản phẩm (Streamlined Product Management)**:
   - Loại bỏ tính bắt buộc của Đơn vị tính (`UnitOfMeasure`) và Tiền tệ (`Currency`) trong tạo/sửa sản phẩm. Nếu người dùng không chỉ định, hệ thống tự động gán đơn vị mặc định ("Cái").
   - Tích hợp trực tiếp trường nhập **Đơn giá trước VAT** ngay trên form tạo và chỉnh sửa sản phẩm; tự động liên kết và lưu trữ vào Bảng giá hiện hành (`PriceList` / `PriceListItem`) trong cơ sở dữ liệu.
   - Tinh gọn thanh menu: loại bỏ mục "Bảng giá" độc lập khỏi menu thông thường nhằm tập trung trải nghiệm tại phân hệ Sản phẩm, trong khi bảo toàn 100% mô hình dữ liệu và logic tính giá của hệ thống.

3. **Màn hình Danh sách Sản phẩm chuẩn POS/ERP (KiotViet-style Product List)**:
   - **Bố cục 2 cột linh hoạt**: Cột trái là Bộ lọc (`FilterSidebar`), cột phải là bảng dữ liệu chính.
   - **Ô tìm kiếm nhanh**: Bố trí ngay cạnh tiêu đề trang, hỗ trợ tìm tức thì theo tên sản phẩm, mã hệ thống, mã mới, mã cũ và quy cách kỹ thuật.
   - **Thanh công cụ gọn gàng**: Các nút hành động chính "+ Thêm mới", "Import Excel", cùng thanh thao tác hàng loạt khi chọn nhiều sản phẩm.
   - **Bảng dữ liệu cô đọng & Ảnh thu nhỏ (Thumbnails)**: Hiển thị ảnh đại diện sản phẩm (40x40px) trích xuất từ kho ảnh hoặc Excel, mã hàng mới nổi bật, đơn giá trước VAT màu xanh rõ nét, phân trang tiện lợi (20 dòng/trang).

4. **Vùng Chi tiết Sản phẩm Mở rộng Trực tiếp (Expandable Product Details)**:
   - Cho phép nhấp vào dòng sản phẩm để mở rộng bảng thông tin chi tiết ngay bên dưới (tương tự trải nghiệm tại phân hệ Nhân sự).
   - **Chế độ Xem (View)**: Hiển thị ảnh độ phân giải cao hoặc bộ sưu tập ảnh phụ, lưới thông tin 3 cột (Thông tin định danh, Chính sách giá & thuế VAT, Quy cách kỹ thuật & mô tả).
   - **Chế độ Sửa trực tiếp (Inline Edit)**: Nút "Chỉnh sửa" chuyển đổi các trường sang ô nhập liệu ngay tại chỗ (Tên, Mã mới, Mã cũ, Loại hàng, ĐVT, Đơn giá chưa VAT, Quy cách, Thay ảnh mới) và lưu trực tiếp với nút "Lưu thay đổi" mà không phải chuyển trang.
   - **Ngừng sử dụng / Kích hoạt lại**: Thao tác an toàn với 1 chạm trực tiếp trên dòng sản phẩm.

5. **Cửa sổ Nhập dữ liệu & Bảng giá từ Excel (Modal Excel Import Window)**:
   - Thực hiện qua Modal thứ cấp trực tiếp từ trang Sản phẩm mà không rời khỏi ngữ cảnh làm việc.
   - Hỗ trợ tệp `.xlsx` dung lượng lên đến **200MB**.
   - Tự động trích xuất hình ảnh vẽ nhúng trong ô Excel qua OpenXML drawings và lưu trữ an toàn.
   - **Đối soát & Xem trước thay đổi (Diff & Review)**: Thống kê số lượng hàng mới, hàng trùng khớp, hàng có thay đổi; bảng đối soát trực quan cho phép người dùng chọn "Cập nhật mới" hoặc "Giữ giá cũ" cho từng sản phẩm (hoặc áp dụng hàng loạt).
   - Đảm bảo đơn giá nhập khẩu là **Đơn giá đại lý CHƯA VAT**; tự động tính thuế VAT dự kiến để đối soát; tự động làm mới danh sách sản phẩm sau khi hoàn tất.

6. **Mẫu Thành phần Bộ lọc Tái sử dụng (Reusable Filter Component)**:
   - Thành phần dùng chung `FilterSidebar.razor` được áp dụng đồng bộ cho các phân hệ: **Sản phẩm**, **Khách hàng**, **Nhà cung cấp**, **Kho hàng**.
   - Hỗ trợ co giãn linh hoạt trên thiết bị di động, đếm số lượng bản ghi theo từng trạng thái/danh mục, và nút bấm "Xóa bộ lọc" khi có điều kiện lọc đang kích hoạt.

---

## Kiến trúc Giao diện Hiện đại hóa (UI Modernization Architecture)

Dự án áp dụng phương pháp hiện đại hóa giao diện có kiểm soát, bảo toàn tuyệt đối 100% logic nghiệp vụ, tính năng, quyền hạn và đường dẫn hiện có:

### 1. Nền tảng Styling Kết hợp (Bootstrap 5 + Scoped CSS + Tailwind CSS)
- **Bảo toàn Bootstrap 5**: Giữ nguyên `lib/bootstrap/dist/css/bootstrap.min.css` làm nền tảng layout gốc; không gỡ bỏ để tránh gãy đổ bất kỳ thành phần nào.
- **Tích hợp Tailwind CSS v3 an toàn**:
  - **Tiền tố bắt buộc `tw-`**: Mọi utility class của Tailwind đều sử dụng tiền tố `tw-` (ví dụ: `tw-flex`, `tw-p-3.5`, `tw-font-mono`, `tw-bg-blue-50/80`), loại bỏ 100% nguy cơ trùng tên class với Bootstrap (`.modal`, `.dropdown`, `.collapse`, `.show`, `.btn`, `.card`...) và hệ thống CSS của dự án.
  - **Vô hiệu hóa Preflight (`preflight: false`)**: Ngăn chặn hoàn toàn việc Tailwind reset kiểu dáng mặc định của thẻ HTML (bảng, tiêu đề, nút bấm, input).
  - **Biên dịch nội bộ (Local Build) & Tree-shaking**: File CSS đầu ra `wwwroot/css/tailwind.min.css` được biên dịch trực tiếp từ `package.json` và chỉ chứa các class thực tế được sử dụng (~1.5 KB), hoàn toàn độc lập và không phụ thuộc vào CDN bên ngoài.
  - **Tự động hóa với MSBuild**: File `.csproj` tích hợp target `BuildTailwindCss` tự động biên dịch khi chạy `dotnet build` (kèm điều kiện kiểm tra tồn tại `node_modules` an toàn).
- **Thứ tự nạp CSS chuẩn xác trong `App.razor`**:
  1. `lib/bootstrap/dist/css/bootstrap.min.css` (Lớp nền cơ sở)
  2. `app.css` (Quy chuẩn thiết kế `tf-*` của TradeFlow)
  3. `TradeFlow.Web.styles.css` (Blazor Scoped CSS)
  4. `css/tailwind.min.css` (Lớp tiện ích `tw-*`)

### 2. Thư viện Thành phần Dùng chung (Shared UI Primitives — `Components/Shared/`)
Tất cả thành phần dùng chung được khai báo toàn cục tại `_Imports.razor`:
- `<TfButton />`: Nút bấm đa năng hỗ trợ Variant (Primary, Secondary, Danger, Ghost), kích cỡ, icon, và trạng thái quay vòng khi đang xử lý (Loading).
- `<TfBadge />`: Huy hiệu trạng thái chuẩn ERP hỗ trợ biến thể màu (success, warning, error, info, gray) và chỉ báo chấm tròn (Dot indicator).
- `<TfPagination />`: Thanh phân trang mật độ cao chuẩn ERP/POS (KiotViet style), cho phép chọn kích thước trang (10, 20, 50, 100), nhảy trang và tự động thu gọn dấu ba chấm (`...`).
- `<TfModal />`: Cửa sổ Modal hiện đại, hỗ trợ phím `Escape`, đóng khi bấm ngoài nền, kích thước linh hoạt (sm, md, lg, xl) cùng các vùng mẫu `HeaderContent`, `BodyContent`, `FooterContent`.
- `<TfMasterDetailLayout />`: Bố cục chia 2 cột chuẩn mực cho trang ERP: cột lọc bên trái (Sidebar) và lưới dữ liệu thao tác bên phải (Main).

### 3. Danh sách Màn hình Đã Chuẩn hóa
- **Danh mục hệ thống**: Đơn vị tính (`/danh-muc/don-vi-tinh`), Loại tiền (`/danh-muc/loai-tien`), Danh mục SP (`/danh-muc/danh-muc-san-pham`), Kho hàng (`/kho/kho-hang`), Sản phẩm (`/danh-muc/san-pham`).
- **Đối tác**: Khách hàng (`/doi-tac/khach-hang`), Nhà cung cấp (`/doi-tac/nha-cung-cap`).
- **Bảng giá**: Danh sách bảng giá & báo giá (`/bang-gia`).
- **Bán hàng**: Báo giá (`/ban-hang/bao-gia`), Đơn bán hàng (`/ban-hang/don-ban-hang`), Hóa đơn (`/hoa-don`).
- **Quản trị**: Người dùng (`/cai-dat/nguoi-dung`), Vai trò (`/cai-dat/vai-tro`).

### 4. Kiểm thử Hồi quy & Đảm bảo Chất lượng (Regression Testing)
- 100% Unit Tests & Integration Tests vượt qua thành công: **201/201 Passed** (100% thành công).
- 0 lỗi biên dịch C# / Blazor.
- Giữ nguyên vẹn 100% cơ chế bảo mật RBAC, chính sách truy cập, endpoint API và giao thức lưu trữ dữ liệu.

---

## Quản lý Hợp đồng Thương mại & Ký điện tử (Phase 6.5 — Vietnamese Contract Management & E-Sign)

Phân hệ Hợp đồng cung cấp giải pháp toàn diện cho việc lập, quản lý, ký kết điện tử, xuất bản in PDF và lưu trữ chứng từ hợp đồng mua bán thiết bị vệ sinh cao cấp LACASA, tuân thủ nghiêm ngặt hệ thống pháp luật Việt Nam.

### 1. Căn cứ Pháp lý & Chuẩn mực Soạn thảo
- **Bộ luật Dân sự số 91/2015/QH13**: Quy định về hợp đồng dân sự/thương mại, năng lực chủ thể (Pháp nhân vs Cá nhân/Hộ gia đình), giao kết hợp đồng, quyền và nghĩa vụ các bên, giải quyết tranh chấp.
- **Luật Thương mại số 36/2005/QH11**: Áp dụng cho các quan hệ mua bán hàng hóa và cung ứng dịch vụ giữa các thương nhân (B2B); **áp dụng giới hạn mức phạt vi phạm hợp đồng tối đa 8%** giá trị phần nghĩa vụ hợp đồng bị vi phạm theo Điều 301.
- **Luật Giao dịch điện tử số 20/2023/QH15** (Hiệu lực từ 01/07/2024): Thừa nhận giá trị pháp lý của hợp đồng điện tử, chữ ký điện tử nội bộ, chứng nhận thời gian và mã băm toàn vẹn SHA-256 bảo đảm văn bản không bị sửa đổi sau khi ký.
- **Nghị định 30/2020/NĐ-CP**: Quy chuẩn về thể thức văn bản hành chính; áp dụng mặc định phông chữ **Times New Roman** trên toàn bộ văn bản xem trước và bản xuất PDF khổ A4.

### 2. Thư viện 7 Mẫu Hợp đồng Chuẩn hóa Ngành Thiết bị Vệ sinh LACASA
1. **Hợp đồng Mua bán Thiết bị Vệ sinh (B2B)** (`B2B_SALE`): Dành cho đại lý, nhà thầu xây dựng, đối tác thương mại; bao gồm điều khoản thanh toán, bảo hành men sứ 10 năm, phụ kiện 3 năm, phạt vi phạm tối đa 8%.
2. **Hợp đồng Mua bán Lẻ Thiết bị Vệ sinh (B2C)** (`B2C_RETAIL`): Dành cho khách hàng cá nhân, chủ nhà; xác nhận theo số CCCD/ngày cấp/nơi cấp; cam kết bảo hành chính hãng và bảo vệ quyền lợi người tiêu dùng.
3. **Hợp đồng Cung cấp & Lắp đặt Thiết bị Vệ sinh** (`SUPPLY_INSTALL`): Dành cho các công trình, dự án căn hộ, biệt thự; quy định chi tiết nghiệm thu kỹ thuật, kiểm tra cấp thoát nước, chạy thử không rò rỉ.
4. **Hợp đồng Nguyên tắc Mua bán Hàng hóa** (`PRINCIPLE_AGREEMENT`): Khung thỏa thuận dài hạn cho đại lý/nhà phân phối cấp 1; từng lô hàng cụ thể sẽ căn cứ theo Đơn đặt hàng (PO).
5. **Thỏa thuận Đặt cọc Mua Thiết bị Vệ sinh** (`DEPOSIT_AGREEMENT`): Căn cứ Điều 328 BLDS 2015; xác định số tiền cọc, mục đích bảo đảm giao kết hợp đồng mua bán và xử lý tiền cọc khi vi phạm.
6. **Biên bản Thanh lý Hợp đồng** (`CONTRACT_LIQUIDATION`): Xác nhận hoàn tất 100% nghĩa vụ giao nhận hàng, quyết toán tài chính, thanh toán dứt điểm công nợ và chuyển sang giai đoạn bảo hành.
7. **Phụ lục Hợp đồng** (`CONTRACT_APPENDIX`): Điều chỉnh phát sinh số lượng, chủng loại sản phẩm, gia hạn tiến độ giao nhận hoặc thay đổi điều khoản thanh toán.

### 3. Các Tính năng & Quy trình Nghiệp vụ Cốt lõi
- **Thanh điều hướng Độc lập**: Mục menu **"Hợp đồng"** nằm độc lập trực tiếp giữa **"Đối tác"** và **"Chứng từ"** trên thanh Sidebar; nhấp chuột trực tiếp mở `/hop-dong` mà không mở menu con phiền phức.
- **Thiết lập Bên A Mặc định (`PartyASettingsModal`)**: Cấu hình thông tin pháp lý doanh nghiệp LACASA (Tên công ty, MST, số ĐKKD, địa chỉ trụ sở, người đại diện theo pháp luật, chức vụ, tài khoản ngân hàng, điều khoản giao nhận và bảo hành mặc định). Lưu trữ trong cơ sở dữ liệu và tự động snapshot bất biến vào từng hợp đồng tạo mới.
- **Quy trình Soạn thảo Hợp đồng 5 Bước (`/hop-dong/tao-moi`)**:
  - *Bước 1: Chọn Mẫu*: Chọn trực quan dạng thẻ bài từ 7 mẫu pháp lý.
  - *Bước 2: Bên A*: Tự động điền và cho phép điều chỉnh thông tin đơn vị bán.
  - *Bước 3: Bên B*: Hỗ trợ chọn nhanh khách hàng từ danh bạ ERP hoặc nhập mới (phân tách Pháp nhân Doanh nghiệp và Cá nhân có CCCD).
  - *Bước 4: Hàng hóa & Điều khoản*: Thêm dòng sản phẩm thiết bị vệ sinh, tự động tính tiền hàng, thuế VAT, tổng giá trị hợp đồng và chuyển đổi thành chữ tiếng Việt (`VietnameseNumberToWords`).
  - *Bước 5: Xem trước Văn bản*: Hiển thị trực quan theo chuẩn thể thức văn bản Việt Nam với phông chữ **Times New Roman**, quốc hiệu, tiêu ngữ, bảng hàng hóa và khung ký tên.
- **Xác nhận Ký kết & Ký Điện tử (`ElectronicSignatureModal`)**:
  - Hỗ trợ ký điện tử với HTML5 Canvas vẽ tay hoặc tự động tạo nét ký doanh nhân.
  - Tự động sinh mã băm SHA-256 toàn vẹn văn bản (`IntegrityHash`), lưu trữ dấu thời gian (`SignedAt`), họ tên người ký, chức vụ và số định danh.
  - Hỗ trợ ghi nhận ký trên bản in giấy truyền thống (`RecordPrintedAsync`).
- **Xuất PDF Khổ A4 Chuẩn Pháp lý (`/api/contracts/{id}/export-pdf`)**:
  - Tích hợp QuestPDF với phông chữ **Times New Roman**, hỗ trợ ngắt trang thông minh, bảng biểu hàng hóa rõ ràng, điều khoản thương mại đầy đủ và chữ ký số.
- **Tích hợp Module Chứng từ (`/tai-lieu`)**:
  - Tự động tạo bản ghi đính kèm chứng từ (`DocumentAttachment`) khi hợp đồng được khởi tạo và ký kết.
  - Bổ sung nút "Hợp đồng đã tạo" và liên kết hai chiều giữa module Chứng từ và trang chi tiết hợp đồng `/hop-dong/{id}`.
- **Kiểm toán Hoàn chỉnh (Audit Trail)**: Mọi thao tác lập, cập nhật, chốt duyệt, ký điện tử, ghi nhận bản in, thanh lý và xóa hợp đồng đều được lưu vết đầy đủ trong `AuditLog` với `ResourceType.Contracts`.

### 4. Giao diện Bộ sưu tập Xem trước Mẫu Hợp đồng A4 Chuẩn Mực (Real A4 Contract Document Preview Gallery)
- **Bản Xem trước Tài liệu A4 Thu nhỏ Trực quan (Real Miniature A4 Contract Sheet)**: Mỗi thẻ hợp đồng hiển thị một trang tài liệu giấy trắng khổ A4 (tỷ lệ chuẩn 210:297) chân thực, thể hiện chính xác bố cục văn bản hành chính Việt Nam thay vì sử dụng các biểu tượng đồ họa trừu tượng.
- **Giao diện Nền Sáng Hiện đại (Clean & Professional Light Theme)**:
  - Khung giao diện, thanh công cụ tìm kiếm và cửa sổ modal sử dụng gam màu trắng và xám sáng thanh lịch (`#ffffff` và `#f8fafc`).
  - Đường viền xám nhẹ tinh tế (`#e2e8f0`) cùng hiệu ứng đổ bóng giấy tự nhiên (`natural paper shadow`), mang lại cảm giác chân thực như đang duyệt tập hồ sơ văn bản A4.
- **Nội dung Văn bản Thực tế Chuẩn Phông Times New Roman**:
  - Từng mẫu hợp đồng hiển thị đúng Quốc hiệu ("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM"), Tiêu ngữ, ngày tháng địa điểm.
  - Tên hợp đồng in hoa đậm (Vd: *HỢP ĐỒNG MUA BÁN THIẾT BỊ VỆ SINH (B2B)*, *HỢP ĐỒNG BÁN LẺ THIẾT BỊ VỆ SINH (B2C)*...).
  - Các căn cứ pháp lý tương ứng (Bộ luật Dân sự 2015, Luật Thương mại 2005, Luật Bảo vệ quyền lợi người tiêu dùng 2023...).
  - Thông tin Bên A (LACASA) và Bên B tương ứng với đối tượng khách hàng (Doanh nghiệp hoặc Cá nhân có CCCD).
  - Bảng kê sản phẩm / công tác kỹ thuật mẫu với các dòng kẻ bảng rõ ràng.
  - Các điều khoản được đánh số (Điều 1, Điều 2, Điều 3...) và khung ký tên hai bên ở cuối trang.
- **Công nghệ Co giãn Đơn vị Container Queries (`cqw`)**:
  - Toàn bộ cỡ chữ và khoảng cách trong trang A4 được tính toán theo tỷ lệ chiều rộng khung chứa (`cqw`), tự động co giãn sắc nét trên mọi độ phân giải mà không bị nhòe vỡ hay phụ thuộc vào JavaScript.
- **Tương tác Di chuột Thông minh & Không Che khuất Văn bản (Non-Obtrusive Hover Interaction)**:
  - Khi di chuột (Hover): Thẻ bài nâng nhẹ (`transform: translateY(-4px)`), viền chuyển sang màu xanh thương hiệu (`#3b82f6`).
  - Thanh thao tác nhanh trượt lên tinh tế ở mép dưới trang tài liệu gồm: **"👁 Xem mẫu"** và **"Soạn hợp đồng →"** mà vẫn giữ trọn vẹn khả năng đọc toàn bộ nội dung văn bản bên dưới.
- **Cửa sổ Xem trước Toàn văn Khổ lớn (High-Resolution A4 Inspection Modal)**:
  - Nhấp vào **"Xem mẫu"** sẽ mở cửa sổ modal kích thước lớn hiển thị toàn văn tài liệu A4 với phông chữ Times New Roman 14.5pt sắc nét, cho phép người dùng đọc kỹ từng câu chữ, điều khoản pháp lý và bảng giá mẫu trước khi bấm **"Soạn hợp đồng từ mẫu này"**.
- **Thanh Công cụ Lọc & Tìm kiếm Nhanh**:
  - Hộp tìm kiếm tức thì theo từ khóa tên hoặc mã hợp đồng, hỗ trợ nút xóa nhanh 1 chạm.
  - Thanh nút lọc danh mục (Pill filter buttons) với bộ đếm số lượng mẫu tương ứng cho từng nhóm.
- **Độ nhạy Cảm ứng & Khả năng Tiếp cận Toàn diện (Accessibility & Responsive)**:
  - Tự động thích ứng trên mọi màn hình: 5–6 thẻ trên Desktop rộng ($\ge 1280$px), 4 thẻ trên Desktop chuẩn (992–1279px), 3 thẻ trên Tablet (640–991px), và 2 thẻ trên Mobile ($< 640$px).
  - Hỗ trợ thao tác cảm ứng tự nhiên trên điện thoại/máy tính bảng và phím tắt bàn phím hoàn chỉnh (`tabindex="0"`, phím `Enter` / `Space` để chọn, phím `Escape` để hủy lựa chọn).
  - Tự động tắt hoạt ảnh chuyển động khi người dùng kích hoạt cấu hình hệ thống `prefers-reduced-motion`.
- **Tích hợp Đồng bộ Khắp Hệ thống**:
  - Tái sử dụng trong Hộp thoại chọn mẫu (`TemplateSelectorModal.razor`) mở từ màn hình Danh sách Hợp đồng `/hop-dong`.
  - Nhúng trực tiếp tại Bước 1 của Trang Soạn thảo Hợp đồng `/hop-dong/tao-moi`.
  - Cung cấp trang Thư viện Mẫu độc lập tại đường dẫn `/hop-dong/thu-vien-mau` (hoặc `/hop-dong/mau-hop-dong`).






