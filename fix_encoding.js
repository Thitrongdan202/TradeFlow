const fs = require('fs');

const fixes = {
    "Qu?n ly": "Quản lý",
    "b?ng gi": "bảng giá",
    "B?ng gi": "Bảng giá",
    "bo gi": "báo giá",
    "Bo gi": "Báo giá",
    "chuong trnh khuy?n mai": "chương trình khuyến mãi",
    "d?i ly theo k?": "đại lý theo kỳ",
    "KhA'ng th `?c": "Không thể đọc",
    "KhA'ng tAm thy": "Không tìm thấy",
    "bng tA-nh": "bảng tính",
    "chca": "chứa",
    "nAo": "nào",
    "dAng tiAu `?": "dòng tiêu đề",
    "hAng hA3a": "hàng hóa",
    "c n": "cần",
    "cTt": "cột",
    "MA HA?NG": "MÃ HÀNG",
    "THA\"NG TIN SN PH\"M": "THÔNG TIN SẢN PHẨM",
    "GIA?": "GIÁ",
    "NHA\"M": "NHÓM",
    "DANH M C": "DANH MỤC",
    "MsI": "MỚI",
    "C\"": "CŨ",
    "QUY CA?CH": "QUY CÁCH",
    "TASN": "TÊN",
    "?I LA?": "ĐẠI LÝ",
    "?N GIA?": "ĐƠN GIÁ",
    "ti file": "tải file",
    "G`c": "Gốc",
    "`ng xut": "đăng xuất",
    "Ng?i dA1ng": "Người dùng",
    "Thm msI": "Thêm mới",
    "Nm": "Năm",
    "Qu": "Quý",
    "Thng": "Tháng",
    "T`i Excel": "Tải Excel",
    "Xc nhn": "Xác nhận",
    "Nhp": "Nhập",
    "d l?u": "dữ liệu",
    "LsI": "Lỗi",
    "Cnh bo": "Cảnh báo"
};

const files = [
    "src/TradeFlow.Web/Components/Pages/Pricing/Compare.razor",
    "src/TradeFlow.Web/Components/Pages/Pricing/Detail.razor",
    "src/TradeFlow.Web/Components/Pages/Pricing/History.razor",
    "src/TradeFlow.Web/Components/Pages/Pricing/Import.razor",
    "src/TradeFlow.Web/Components/Pages/Pricing/List.razor",
    "src/TradeFlow.Infrastructure/Services/ExcelPricingService.cs",
    "src/TradeFlow.Web/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs",
    "src/TradeFlow.Web/Program.cs"
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
