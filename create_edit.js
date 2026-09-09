const fs = require('fs');
let file = 'src/TradeFlow.Web/Components/Pages/Sales/Invoices/Edit.razor';
let content = `@page "/hoa-don/sua/{Id:int}"
@using Microsoft.AspNetCore.Authorization
@using TradeFlow.Application.Common.Interfaces
@using TradeFlow.Application.Common.Models.Sales
@using TradeFlow.Domain.Enums
@inject IInvoiceService InvoiceService
@inject NavigationManager Nav
@rendermode InteractiveServer
@attribute [Authorize(Policy = "Permission:Invoices:Edit")]

<PageTitle>Sửa hóa đơn | TradeFlow</PageTitle>

<div class="tf-page-header">
    <div class="tf-page-header-content">
        <h1 class="tf-page-title">Sửa hóa đơn</h1>
        <p class="tf-page-subtitle">Cập nhật thông tin hóa đơn nháp</p>
    </div>
</div>

@if (_isLoading)
{
    <div class="tf-loading">Đang tải dữ liệu...</div>
}
else if (_invoice == null)
{
    <div class="tf-alert tf-alert-danger">Không tìm thấy hóa đơn hoặc không thể sửa (đã phát hành/hủy).</div>
}
else
{
    <div class="tf-card tf-mb-4">
        <div class="tf-form-group tf-mb-4">
            <label class="tf-form-label">Loại hóa đơn</label>
            <select class="tf-form-control" @bind="_invoice.Type" @bind:after="CalculateTotals" style="max-width:300px;">
                <option value="@InvoiceType.SalesInvoice">Hóa đơn bán hàng (02/BH)</option>
                <option value="@InvoiceType.VatInvoice">Hóa đơn GTGT (01/GTGT)</option>
            </select>
        </div>

        <h3 class="tf-font-semibold tf-text-lg tf-mb-2">Thông tin người mua</h3>
        <div class="tf-grid tf-grid-cols-2 tf-gap-4">
            <div class="tf-form-group">
                <label class="tf-form-label">Họ tên người mua <span class="tf-text-danger">*</span></label>
                <input type="text" class="tf-form-control" @bind="_invoice.CustomerName" />
            </div>
            <div class="tf-form-group">
                <label class="tf-form-label">Tên đơn vị</label>
                <input type="text" class="tf-form-control" @bind="_invoice.CustomerCompanyName" />
            </div>
            <div class="tf-form-group">
                <label class="tf-form-label">MST / CCCD</label>
                <input type="text" class="tf-form-control" @bind="_invoice.CustomerTaxCode" />
            </div>
            <div class="tf-form-group">
                <label class="tf-form-label">Địa chỉ</label>
                <input type="text" class="tf-form-control" @bind="_invoice.CustomerAddress" />
            </div>
            <div class="tf-form-group">
                <label class="tf-form-label">Hình thức thanh toán</label>
                <input type="text" class="tf-form-control" @bind="_invoice.PaymentMethod" />
            </div>
            <div class="tf-form-group">
                <label class="tf-form-label">Số tài khoản</label>
                <input type="text" class="tf-form-control" @bind="_invoice.CustomerBankAccount" />
            </div>
        </div>
    </div>

    <div class="tf-card tf-mb-4">
        <h3 class="tf-font-semibold tf-text-lg tf-mb-4">Hàng hóa, dịch vụ</h3>
        <table class="tf-table">
            <thead>
                <tr>
                    <th>Tên hàng hóa, dịch vụ</th>
                    <th>ĐVT</th>
                    <th style="width:100px;">Số lượng</th>
                    <th style="width:150px;">Đơn giá</th>
                    <th style="width:120px;">Chiết khấu</th>
                    @if (_invoice.Type == InvoiceType.VatInvoice)
                    {
                        <th style="width:100px;">Thuế %</th>
                    }
                    <th style="width:150px; text-align:right;">Thành tiền</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                @foreach (var item in _invoice.Items)
                {
                    <tr>
                        <td><input type="text" class="tf-form-control tf-form-control-sm" @bind="item.ProductName" /></td>
                        <td><input type="text" class="tf-form-control tf-form-control-sm" @bind="item.UnitName" /></td>
                        <td><input type="number" class="tf-form-control tf-form-control-sm" @bind="item.Quantity" @bind:after="CalculateTotals" min="0" step="any" /></td>
                        <td><input type="number" class="tf-form-control tf-form-control-sm" @bind="item.UnitPrice" @bind:after="CalculateTotals" min="0" step="any" /></td>
                        <td><input type="number" class="tf-form-control tf-form-control-sm" @bind="item.DiscountAmount" @bind:after="CalculateTotals" min="0" step="any" /></td>
                        @if (_invoice.Type == InvoiceType.VatInvoice)
                        {
                            <td>
                                <select class="tf-form-control tf-form-control-sm" @bind="item.TaxRate" @bind:after="CalculateTotals">
                                    <option value="0">0%</option>
                                    <option value="5">5%</option>
                                    <option value="8">8%</option>
                                    <option value="10">10%</option>
                                </select>
                            </td>
                        }
                        <td class="tf-text-right"><strong>@item.LineTotal.ToString("N0")</strong></td>
                        <td><button type="button" class="tf-btn tf-btn-sm tf-btn-danger" @onclick="() => RemoveItem(item)">X</button></td>
                    </tr>
                }
            </tbody>
        </table>
        <button type="button" class="tf-btn tf-btn-outline-primary tf-btn-sm tf-mt-2" @onclick="AddManualItem">+ Thêm dòng thủ công</button>
    </div>

    <div class="tf-card" style="margin-bottom:20px;">
        <div style="display:flex; justify-content:flex-end;">
            <div style="width:300px; padding:10px;">
                <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
                    <span style="color:var(--tf-text-muted);">Cộng tiền hàng:</span>
                    <strong>@_invoice.SubTotal.ToString("N0")</strong>
                </div>
                <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
                    <span style="color:var(--tf-text-muted);">Chiết khấu:</span>
                    <strong>@_invoice.TotalDiscount.ToString("N0")</strong>
                </div>
                @if (_invoice.Type == InvoiceType.VatInvoice)
                {
                    <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
                        <span style="color:var(--tf-text-muted);">Tiền thuế GTGT:</span>
                        <strong>@_invoice.TotalTax.ToString("N0")</strong>
                    </div>
                }
                <div style="display:flex; justify-content:space-between; margin-top:16px; padding-top:16px; border-top:1px dashed #cbd5e1; font-size:18px;">
                    <span>Tổng cộng:</span>
                    <strong style="color:var(--tf-color-danger);">@_invoice.GrandTotal.ToString("N0")</strong>
                </div>
            </div>
        </div>
    </div>

    <div class="tf-form-actions" style="margin-bottom: 40px; display:flex; gap:12px;">
        <button type="button" class="tf-btn tf-btn-secondary" @onclick='() => Nav.NavigateTo($"/hoa-don/chi-tiet/{Id}")'>Hủy</button>
        <button type="button" class="tf-btn tf-btn-primary" @onclick="SaveInvoice" disabled="@_isSaving">
            @if (_isSaving) { <span>Đang lưu...</span> } else { <span>Lưu thay đổi</span> }
        </button>
    </div>
}

@code {
    [Parameter]
    public int Id { get; set; }

    private InvoiceDto? _invoice;
    private bool _isLoading = true;
    private bool _isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        _invoice = await InvoiceService.GetInvoiceByIdAsync(Id);
        if (_invoice != null && _invoice.Status != InvoiceStatus.Draft)
        {
            _invoice = null;
        }
        _isLoading = false;
    }

    private void AddManualItem()
    {
        _invoice?.Items.Add(new InvoiceItemDto { Quantity = 1 });
        CalculateTotals();
    }

    private void RemoveItem(InvoiceItemDto item)
    {
        _invoice?.Items.Remove(item);
        CalculateTotals();
    }

    private void CalculateTotals()
    {
        if (_invoice == null) return;
        
        foreach (var item in _invoice.Items)
        {
            var preTax = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            if (_invoice.Type == InvoiceType.VatInvoice)
            {
                item.TaxAmount = preTax * (item.TaxRate / 100m);
            }
            else
            {
                item.TaxRate = 0;
                item.TaxAmount = 0;
            }
            item.LineTotal = preTax + item.TaxAmount;
        }

        _invoice.SubTotal = _invoice.Items.Sum(x => x.Quantity * x.UnitPrice);
        _invoice.TotalDiscount = _invoice.Items.Sum(x => x.DiscountAmount);
        _invoice.TotalTax = _invoice.Items.Sum(x => x.TaxAmount);
        _invoice.GrandTotal = _invoice.SubTotal - _invoice.TotalDiscount + _invoice.TotalTax;
    }

    private async Task SaveInvoice()
    {
        if (_invoice == null) return;
        _isSaving = true;
        try
        {
            CalculateTotals();
            await InvoiceService.UpdateInvoiceAsync(_invoice);
            Nav.NavigateTo($"/hoa-don/chi-tiet/{Id}");
        }
        finally
        {
            _isSaving = false;
        }
    }
}
`;
fs.writeFileSync(file, content, 'utf8');
