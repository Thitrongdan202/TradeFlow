const fs = require('fs');

const files = [
    'src/TradeFlow.Web/Components/Pages/MasterData/Categories/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Currencies/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Products/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Units/List.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Customers/List.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Suppliers/List.razor',
    'src/TradeFlow.Web/Components/Pages/Warehouses/List.razor',
    'src/TradeFlow.Web/Components/Pages/Sales/Orders/List.razor',
    'src/TradeFlow.Web/Components/Pages/Sales/Invoices/List.razor'
];

const modalHtml = `
@if (_itemToDeleteId != null)
{
    <div style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 1000; display: flex; align-items: center; justify-content: center;">
        <div style="background: white; padding: 24px; border-radius: 8px; width: 400px; max-width: 90%; box-shadow: 0 10px 25px rgba(0,0,0,0.2);">
            <h3 style="margin-top: 0; margin-bottom: 16px; font-size: 18px; color: var(--tf-danger);">Xóa dữ liệu?</h3>
            <p style="margin-bottom: 24px;">Bạn có chắc chắn muốn xóa @_itemToDeleteName?</p>
            @if (!string.IsNullOrEmpty(_deleteError))
            {
                <div class="tf-alert tf-alert-danger" style="margin-bottom: 16px;">
                    @_deleteError
                </div>
            }
            <div style="display: flex; justify-content: flex-end; gap: 12px;">
                <button type="button" class="tf-btn tf-btn-outline-secondary" @onclick="CancelDelete">Hủy</button>
                <button type="button" class="tf-btn tf-btn-danger" @onclick="ExecuteDelete">Xóa</button>
            </div>
        </div>
    </div>
}
`;

files.forEach(f => {
    let c = fs.readFileSync(f, 'utf8');
    if (!c.includes('Xóa dữ liệu?')) {
        c = c.replace('@code {', modalHtml + '\n@code {');
        fs.writeFileSync(f, c, 'utf8');
        console.log('Injected modal to ' + f);
    }
});
