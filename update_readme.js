const fs = require('fs');
let file = 'README.md';
let content = fs.readFileSync(file, 'utf8');

// I will just append the new Phase 5 Invoice documentation to the end
let newDocs = `
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
`;

content = content + newDocs;
fs.writeFileSync(file, content, 'utf8');
