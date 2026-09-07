const fs = require('fs');

const details = [
    { file: 'src/TradeFlow.Web/Components/Pages/Pricing/Detail.razor', perm: 'PriceLists', execute: 'var ok = await PriceListService.DeletePriceListAsync(Id); if(ok) { Nav.NavigateTo("/bang-gia"); } else { _deleteError = "Không thể xóa."; }' },
    { file: 'src/TradeFlow.Web/Components/Pages/Sales/Orders/Detail.razor', perm: 'SalesOrders', execute: 'var ok = await SalesService.DeleteOrderAsync(Id); if(ok) { Nav.NavigateTo("/ban-hang/don-ban-hang"); } else { _deleteError = "Không thể xóa."; }' },
    { file: 'src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor', perm: 'Invoices', execute: 'var ok = await InvoiceService.DeleteInvoiceAsync(Id); if(ok) { Nav.NavigateTo("/hoa-don"); } else { _deleteError = "Không thể xóa."; }' }
];

details.forEach(f => {
    let c = fs.readFileSync(f.file, 'utf8');

    if (!c.includes('ConfirmDelete')) {
        // Inject auth services if missing
        if (!c.includes('IAuthorizationService')) {
            c = c.replace('@inject NavigationManager Nav', '@inject NavigationManager Nav\n@inject Microsoft.AspNetCore.Authorization.IAuthorizationService AuthService\n@inject Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider');
        }

        // Add Delete Button to top right actions
        const btnIdx = c.indexOf('class="tf-page-actions"');
        if (btnIdx > -1) {
            const innerDiv = c.indexOf('>', btnIdx);
            const injectBtn = `
            <AuthorizeView Policy="Permission:${f.perm}:Delete">
                <button type="button" class="tf-btn tf-btn-danger" @onclick="ConfirmDelete">Xóa</button>
            </AuthorizeView>
`;
            c = c.slice(0, innerDiv + 1) + injectBtn + c.slice(innerDiv + 1);
        }

        const modalHtml = `
@if (_showDeleteModal)
{
    <div style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 1000; display: flex; align-items: center; justify-content: center;">
        <div style="background: white; padding: 24px; border-radius: 8px; width: 400px; max-width: 90%; box-shadow: 0 10px 25px rgba(0,0,0,0.2);">
            <h3 style="margin-top: 0; margin-bottom: 16px; font-size: 18px; color: var(--tf-danger);">Xóa dữ liệu?</h3>
            <p style="margin-bottom: 24px;">Bạn có chắc chắn muốn xóa bản ghi này?</p>
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
        c = c.replace('@code {', modalHtml + '\n@code {');

        const methods = `
    private bool _showDeleteModal = false;
    private string? _deleteError;

    private void ConfirmDelete()
    {
        _showDeleteModal = true;
        _deleteError = null;
    }

    private void CancelDelete()
    {
        _showDeleteModal = false;
        _deleteError = null;
    }

    private async Task ExecuteDelete()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        if (!(await AuthService.AuthorizeAsync(authState.User, "Permission:${f.perm}:Delete")).Succeeded)
        {
            _deleteError = "Bạn không có quyền thực hiện thao tác này.";
            return;
        }

        try
        {
            ${f.execute}
        }
        catch (Exception ex)
        {
            _deleteError = "Đã xảy ra lỗi: " + ex.Message;
        }
    }
`;
        const lastBracketIndex = c.lastIndexOf('}');
        c = c.substring(0, lastBracketIndex) + methods + '\n}';
        fs.writeFileSync(f.file, c, 'utf8');
    }
});
