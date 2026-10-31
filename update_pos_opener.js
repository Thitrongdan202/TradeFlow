const fs = require('fs');

const path = 'src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor';
let content = fs.readFileSync(path, 'utf8');

// 1. Update modal condition and body
const modalTarget = `@if (_showProductDetailModal && _viewingProductDetail != null)
{
    <div class="tf-modal-backdrop" @onclick="() => _showProductDetailModal = false">
        <div class="tf-modal-dialog tf-product-detail-dialog" @onclick:stopPropagation="true">
            <div class="tf-modal-header">
                <div class="tf-product-detail-header-text">
                    <h3 class="tf-modal-title">Chi tiết sản phẩm</h3>
                    <span class="tf-product-detail-sub">@(_viewingProductDetail.NewCode ?? _viewingProductDetail.Code) — @_viewingProductDetail.Name</span>
                </div>
                <button type="button" class="tf-modal-close-btn" @onclick="() => _showProductDetailModal = false">✕</button>
            </div>
            <div class="tf-modal-body tf-detail-modal-body">`;

const modalReplacement = `@if (_showProductDetailModal)
{
    <div class="tf-modal-backdrop" @onclick="() => _showProductDetailModal = false">
        <div class="tf-modal-dialog tf-product-detail-dialog" @onclick:stopPropagation="true">
            <div class="tf-modal-header">
                <div class="tf-product-detail-header-text">
                    <h3 class="tf-modal-title">Chi tiết sản phẩm</h3>
                    @if (_viewingProductDetail != null)
                    {
                        <span class="tf-product-detail-sub">@(_viewingProductDetail.NewCode ?? _viewingProductDetail.Code) — @_viewingProductDetail.Name</span>
                    }
                </div>
                <button type="button" class="tf-modal-close-btn" @onclick="() => _showProductDetailModal = false">✕</button>
            </div>
            <div class="tf-modal-body tf-detail-modal-body">
                @if (_isLoadingProductDetail || _viewingProductDetail == null)
                {
                    <div style="padding:32px 16px; text-align:center;">
                        <div class="tf-spinner" style="margin:0 auto 12px auto; width:30px; height:30px;"></div>
                        <div style="font-size:13px; color:var(--tf-text-secondary);">Đang nạp thông số sản phẩm...</div>
                    </div>
                }
                else
                {`;

content = content.replace(modalTarget, modalReplacement);

// Close the else block inside the body
const footerTarget = `            </div>
            <div class="tf-modal-footer">
                <button type="button" class="tf-btn tf-btn-primary" @onclick="() => _showProductDetailModal = false">Đóng</button>
            </div>`;

const footerReplacement = `                }
            </div>
            <div class="tf-modal-footer">
                <button type="button" class="tf-btn tf-btn-primary" @onclick="() => _showProductDetailModal = false">Đóng</button>
            </div>`;

content = content.replace(footerTarget, footerReplacement);

// 2. Update method and variables
const varTarget = `    private bool _showProductDetailModal;`;
const varReplacement = `    private bool _showProductDetailModal;
    private bool _isLoadingProductDetail;`;
content = content.replace(varTarget, varReplacement);

const methodTarget = `    private async Task ShowProductDetailsAsync(int productId)
    {
        _openItemMenuId = null;
        _viewingProductDetail = await DbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == productId);
        _showProductDetailModal = true;
    }`;

const methodReplacement = `    private async Task ShowProductDetailsAsync(int productId)
    {
        _openItemMenuId = null;
        _showProductDetailModal = true;
        _isLoadingProductDetail = true;
        _viewingProductDetail = null;
        StateHasChanged();

        _viewingProductDetail = await DbContext.Products
            .Include(p => p.Category)
            .Include(p => p.Unit)
            .FirstOrDefaultAsync(p => p.Id == productId);
        _isLoadingProductDetail = false;
        StateHasChanged();
    }`;

content = content.replace(methodTarget, methodReplacement);

fs.writeFileSync(path, content, 'utf8');
console.log('Successfully updated PosIndex.razor');
