const fs = require('fs');
let c = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Detail.razor', 'utf8');
const method = `
    private async Task DeleteOriginalFile()
    {
        bool confirm = await JS.InvokeAsync<bool>("confirm", "Bạn có chắc chắn muốn xóa file Excel gốc đã upload không? Hành động này sẽ dọn dẹp dung lượng và không xóa dữ liệu bảng giá trong hệ thống.");
        if (confirm)
        {
            var result = await PriceListService.DeleteOriginalFileAsync(Id);
            if (result)
            {
                await LoadDataAsync();
            }
        }
    }

    private async Task ApproveThisPriceList()`;

c = c.replace(/private async Task ApproveThisPriceList\(\)/g, method);
fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Detail.razor', c, 'utf8');
