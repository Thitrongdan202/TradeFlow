const fs = require('fs');

const fixes = {
    "Tá»•ng quan": "Tổng quan",
    "Danh má»¥c sáº£n pháº©m": "Danh mục sản phẩm",
    "Danh má»¥c": "Danh mục",
    "Sáº£n pháº©m": "Sản phẩm",
    "Ä á»‘i tÃ¡c": "Đối tác",
    "Báº£ng giÃ¡": "Bảng giá",
    "CÃ i Ä‘áº·t": "Cài đặt",
    "KhÃ¡ch hÃ ng": "Khách hàng",
    "NhÃ  cung cáº¥p": "Nhà cung cấp",
    "Kho hÃ ng": "Kho hàng",
    "Lá»‹ch sá»­ giÃ¡ SP": "Lịch sử giá SP",
    "Danh sÃ¡ch báº£ng giÃ¡": "Danh sách bảng giá",
    "Nháº­p tá»« Excel": "Nhập từ Excel",
    "So sÃ¡nh báº£ng giÃ¡": "So sánh bảng giá",
    "Ä Æ¡n vá»‹ tÃ­nh": "Đơn vị tính",
    "Loáº¡i tiá» n": "Loại tiền",
    "Quáº£n lÃ½ thÆ°Æ¡ng máº¡i": "Quản lý thương mại",
    "TÃ i khoáº£n cÃ¡ nhÃ¢n": "Tài khoản cá nhân",
    "TÃ i khoáº£n cá»§a tÃ´i": "Tài khoản của tôi",
    "Ä á»•i máº­t kháº©u": "Đổi mật khẩu",
    "Ä Äƒng xuáº¥t": "Đăng xuất",
    "Báº¡n cÃ³ cháº¯c cháº¯n muá»‘n Ä‘Äƒng xuáº¥t khá» i há»‡ thá»‘ng?": "Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?"
};

const files = [
    "src/TradeFlow.Web/Components/Layout/NavMenu.razor",
    "src/TradeFlow.Web/Components/Layout/UserMenu.razor"
];

files.forEach(file => {
    if (fs.existsSync(file)) {
        let content = fs.readFileSync(file, 'utf8');
        for (const [key, value] of Object.entries(fixes)) {
            content = content.split(key).join(value);
        }
        fs.writeFileSync(file, content, 'utf8');
        console.log("Fixed", file);
    }
});
