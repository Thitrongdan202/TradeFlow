const fs = require('fs');
const path = require('path');

const forms = [
    { file: 'src/TradeFlow.Web/Components/Pages/MasterData/Categories/Form.razor', entity: 'ProductCategory', perm: 'Categories', db: 'ProductCategories', check: 'bool isReferenced = await DbContext.Products.AnyAsync(x => x.CategoryId == _model.Id); if(isReferenced) { _deleteError = "Không thể xóa vì dữ liệu này đang được sử dụng."; return; }' },
    { file: 'src/TradeFlow.Web/Components/Pages/MasterData/Currencies/Form.razor', entity: 'Currency', perm: 'Currencies', db: 'Currencies', check: '' },
    { file: 'src/TradeFlow.Web/Components/Pages/MasterData/Products/Form.razor', entity: 'Product', perm: 'Products', db: 'Products', check: 'bool isReferenced = await DbContext.SalesOrderItems.AnyAsync(x => x.ProductId == _model.Id) || await DbContext.InvoiceItems.AnyAsync(x => x.ProductId == _model.Id); if(isReferenced) { _model.IsActive = false; DbContext.Products.Update(_model); } else {' },
    { file: 'src/TradeFlow.Web/Components/Pages/MasterData/Units/Form.razor', entity: 'UnitOfMeasure', perm: 'UnitsOfMeasure', db: 'UnitsOfMeasure', check: 'bool isReferenced = await DbContext.Products.AnyAsync(x => x.UnitId == _model.Id); if(isReferenced) { _deleteError = "Không thể xóa vì dữ liệu này đang được sử dụng."; return; }' },
    { file: 'src/TradeFlow.Web/Components/Pages/Partners/Customers/Form.razor', entity: 'Customer', perm: 'Customers', db: 'Customers', check: 'bool isReferenced = await DbContext.SalesOrders.AnyAsync(x => x.CustomerId == _model.Id) || await DbContext.Invoices.AnyAsync(x => x.CustomerId == _model.Id); if(isReferenced) { _model.IsActive = false; DbContext.Customers.Update(_model); } else {' },
    { file: 'src/TradeFlow.Web/Components/Pages/Partners/Suppliers/Form.razor', entity: 'Supplier', perm: 'Suppliers', db: 'Suppliers', check: 'bool isReferenced = false; if(isReferenced) { _model.IsActive = false; DbContext.Suppliers.Update(_model); } else {' },
    { file: 'src/TradeFlow.Web/Components/Pages/Warehouses/Form.razor', entity: 'Warehouse', perm: 'Warehouses', db: 'Warehouses', check: 'bool isReferenced = false; if(isReferenced) { _model.IsActive = false; DbContext.Warehouses.Update(_model); } else {' }
];

forms.forEach(f => {
    let c = fs.readFileSync(f.file, 'utf8');

    // Add Xóa button to the action footer
    // Find: <button type="submit" class="tf-btn tf-btn-primary">
    if (!c.includes('ConfirmDelete')) {
        const btnIdx = c.indexOf('<button type="submit" class="tf-btn tf-btn-primary">');
        if (btnIdx > -1) {
            const injectBtn = `
                @if (IsEdit)
                {
                    <AuthorizeView Policy="Permission:${f.perm}:Delete">
                        <button type="button" class="tf-btn tf-btn-danger" style="margin-left: auto;" @onclick="ConfirmDelete">Xóa</button>
                    </AuthorizeView>
                }
`;
            c = c.slice(0, btnIdx) + injectBtn + c.slice(btnIdx);
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
            ${f.check}
            ${f.check.endsWith('{') ? '\n                DbContext.' + f.db + '.Remove(_model);\n            }' : (f.check === '' ? '\nDbContext.' + f.db + '.Remove(_model);' : '\nDbContext.' + f.db + '.Remove(_model);')}

            await DbContext.SaveChangesAsync();
            await AuditService.LogAsync(TradeFlow.Domain.Enums.AuditEventType.${f.entity}Deleted, authState.User.Identity?.Name ?? "System", "${f.entity}", _model.Id.ToString(), "Xóa", authState.User.Identity?.Name);
            Nav.NavigateTo(Nav.Uri.Split('/')[1] == "danh-muc" ? "/danh-muc/" + Nav.Uri.Split('/')[2] : (Nav.Uri.Split('/')[1] == "doi-tac" ? "/doi-tac/" + Nav.Uri.Split('/')[2] : "/kho-hang"));
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
