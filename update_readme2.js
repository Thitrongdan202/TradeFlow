const fs = require('fs');
let c = fs.readFileSync('README.md', 'utf8');

if (!c.includes('Xóa dữ liệu (Delete Functionality)')) {
    const text = `
## Xóa dữ liệu (Delete Functionality)
- **Master Data**: Đã thêm chức năng Xóa an toàn tại cả danh sách (List) và biểu mẫu (Form).
- **Nguyên tắc**: Xóa vật lý các bản ghi chưa sử dụng. Nếu dữ liệu đang bị ràng buộc (ví dụ Sản phẩm trong Đơn hàng), hệ thống sẽ vô hiệu hóa (soft-delete) hoặc chặn lệnh xóa kèm theo thông báo lỗi rõ ràng.
- **Bảng giá, Đơn bán hàng, Hóa đơn**: Áp dụng quy tắc tương tự, không xóa các chứng từ đã xác nhận/phát hành.
- **Bảo mật**: Sử dụng RBAC server-side xác thực quyền người dùng. Mọi thao tác xóa thành công đều được lưu vết qua AuditService.
`;
    c += text;
    fs.writeFileSync('README.md', c, 'utf8');
}
