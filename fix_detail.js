const fs = require('fs');

let c = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Detail.razor', 'utf8');

if (!c.includes('@inject IJSRuntime JS')) {
    c = c.replace('@inject NavigationManager Nav', '@inject NavigationManager Nav\n@inject IJSRuntime JS');
}

const deleteButton = `
                <a href="/api/pricing/@Id/download-original" class="tf-btn tf-btn-secondary" target="_blank">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                    Tải file gốc
                </a>
                <button type="button" class="tf-btn tf-btn-secondary" style="color: #ef4444; border-color: #fecaca; background: #fef2f2;" @onclick="DeleteOriginalFile">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M3 6h18M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"/></svg>
                    Xóa file gốc
                </button>`;

c = c.replace(
    /<a href="\/api\/pricing\/@Id\/download-original"[\s\S]*?Tải file gốc\s*<\/a>/,
    deleteButton
);

const deleteMethod = `
    private async Task ApproveThisPriceList()
    {
        var result = await PriceListService.ApprovePriceListAsync(Id);
        if (result)
        {
            await LoadDataAsync();
        }
    }

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
`;

c = c.replace(
    /private async Task ApproveThisPriceList\(\)[\s\S]*?await LoadDataAsync\(\);\s*\}\s*\}/,
    deleteMethod
);

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Detail.razor', c, 'utf8');
