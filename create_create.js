const fs = require('fs');
let file = 'src/TradeFlow.Web/Components/Pages/Sales/Invoices/Create.razor';
let content = `@page "/hoa-don/tao-moi"
@using Microsoft.AspNetCore.Authorization
@using TradeFlow.Application.Common.Interfaces
@using TradeFlow.Application.Common.Models.Sales
@using TradeFlow.Domain.Entities.MasterData
@using TradeFlow.Domain.Enums
@inject IInvoiceService InvoiceService
@inject ISalesService SalesService
@inject NavigationManager Nav
@rendermode InteractiveServer
@attribute [Authorize(Policy = "Permission:Invoices:Create")]

<PageTitle>Tạo hóa đơn mới | TradeFlow</PageTitle>

<div class="tf-page-header">
    <div class="tf-page-header-content">
        <h1 class="tf-page-title">Tạo hóa đơn</h1>
        <p class="tf-page-subtitle">Nhập thông tin hóa đơn thủ công</p>
    </div>
    <div class="tf-page-header-actions">
        <a href="/hoa-don" class="tf-btn tf-btn-outline-secondary">Hủy</a>
        <button class="tf-btn tf-btn-primary" @onclick="SaveInvoice" disabled="@_isProcessing">Lưu nháp</button>
    </div>
</div>

@if (!string.IsNullOrEmpty(_errorMessage))
{
    <div class="tf-alert tf-alert-danger tf-mb-4">
        @_errorMessage
    </div>
}

<div class="tf-card tf-mb-4">
    <div class="tf-form-group tf-mb-4">
        <label class="tf-form-label">Loại hóa đơn</label>
        <select class="tf-form-control" @bind="_dto.Type" style="max-width:300px;">
            <option value="1">Hóa đơn bán hàng (02/BH)</option>
            <option value="2">Hóa đơn GTGT (01/GTGT)</option>
        </select>
    </div>

    <h3 class="tf-font-semibold tf-text-lg tf-mb-2">Thông tin người mua</h3>
    <div class="tf-grid tf-grid-cols-2 tf-gap-4 tf-mb-4">
        <div class="tf-form-group" style="grid-column: span 2; position:relative;">
            <label class="tf-form-label">Tìm khách hàng có sẵn (Tùy chọn)</label>
            <input type="text" class="tf-form-control" @bind="_customerSearch" @bind:event="oninput" @onkeyup="SearchCustomers" placeholder="Nhập tên hoặc SĐT để tìm..." />
            @if (_customers.Any())
            {
                <ul style="border:1px solid #ccc; max-height:150px; overflow-y:auto; list-style:none; padding:0; margin:0; background:white; position:absolute; z-index:100; width:100%;">
                    @foreach (var c in _customers)
                    {
                        <li style="padding:8px; cursor:pointer; border-bottom:1px solid #eee;" @onclick="() => SelectCustomer(c)">
                            <strong>@c.Name</strong> - @c.Phone
                        </li>
                    }
                </ul>
            }
        </div>

        <div class="tf-form-group">
            <label class="tf-form-label">Họ tên người mua <span class="tf-text-danger">*</span></label>
            <input type="text" class="tf-form-control" @bind="_dto.CustomerName" />
        </div>
        <div class="tf-form-group">
            <label class="tf-form-label">Tên đơn vị</label>
            <input type="text" class="tf-form-control" @bind="_dto.CustomerCompanyName" />
        </div>
        <div class="tf-form-group">
            <label class="tf-form-label">MST / CCCD</label>
            <input type="text" class="tf-form-control" @bind="_dto.CustomerTaxCode" />
        </div>
        <div class="tf-form-group">
            <label class="tf-form-label">Địa chỉ</label>
            <input type="text" class="tf-form-control" @bind="_dto.CustomerAddress" />
        </div>
        <div class="tf-form-group">
            <label class="tf-form-label">Điện thoại / Email</label>
            <input type="text" class="tf-form-control" @bind="_dto.CustomerEmail" />
        </div>
        <div class="tf-form-group">
            <label class="tf-form-label">Hình thức thanh toán</label>
            <input type="text" class="tf-form-control" @bind="_dto.PaymentMethod" />
        </div>
        <div class="tf-form-group" style="grid-column: span 2;">
            <label class="tf-form-label">Số tài khoản</label>
            <input type="text" class="tf-form-control" @bind="_dto.CustomerBankAccount" />
        </div>
    </div>
</div>

<div class="tf-card tf-mb-4">
    <div style="display:flex; justify-content:space-between; align-items:center;" class="tf-mb-4">
        <h3 class="tf-font-semibold tf-text-lg">Hàng hóa, dịch vụ</h3>
    </div>
    
    <div class="tf-form-group tf-mb-4" style="position:relative;">
        <label class="tf-form-label">Thêm từ danh mục sản phẩm (Tùy chọn)</label>
        <input type="text" class="tf-form-control" @bind="_productSearch" @bind:event="oninput" @onkeyup="SearchProducts" placeholder="Nhập tên sản phẩm để tìm..." />
        @if (_products.Any())
        {
            <ul style="border:1px solid #ccc; max-height:150px; overflow-y:auto; list-style:none; padding:0; margin:0; background:white; position:absolute; z-index:100; width:100%;">
                @foreach (var p in _products)
                {
                    <li style="padding:8px; cursor:pointer; border-bottom:1px solid #eee;" @onclick="() => SelectProduct(p)">
                        <strong>@p.Name</strong>
                    </li>
                }
            </ul>
        }
    </div>

    <table class="tf-table">
        <thead>
            <tr>
                <th>Tên hàng hóa, dịch vụ</th>
                <th>ĐVT</th>
                <th style="width:100px;">Số lượng</th>
                <th style="width:150px;">Đơn giá</th>
                @if (_dto.Type == InvoiceType.VatInvoice)
                {
                    <th style="width:100px;">Thuế %</th>
                }
                <th style="width:150px; text-align:right;">Thành tiền</th>
                <th></th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in _dto.Items)
            {
                <tr>
                    <td><input type="text" class="tf-form-control tf-form-control-sm" @bind="item.ProductName" /></td>
                    <td><input type="text" class="tf-form-control tf-form-control-sm" @bind="item.UnitName" /></td>
                    <td><input type="number" class="tf-form-control tf-form-control-sm" @bind="item.Quantity" @bind:after="CalculateTotals" /></td>
                    <td><input type="number" class="tf-form-control tf-form-control-sm" @bind="item.UnitPrice" @bind:after="CalculateTotals" /></td>
                    @if (_dto.Type == InvoiceType.VatInvoice)
                    {
                        <td>
                            <select class="tf-form-control tf-form-control-sm" @bind="item.TaxRate" @bind:after="CalculateTotals">
                                <option value="0">0</option>
                                <option value="5">5</option>
                                <option value="8">8</option>
                                <option value="10">10</option>
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
        <div style="width:300px; background:#f8fafc; padding:20px; border-radius:8px; border:1px solid #e2e8f0;">
            @if (_dto.Type == InvoiceType.VatInvoice)
            {
                <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
                    <span style="color:var(--tf-text-muted);">Cộng tiền hàng:</span>
                    <strong>@_dto.SubTotal.ToString("N0")</strong>
                </div>
                <div style="display:flex; justify-content:space-between; margin-bottom:8px;">
                    <span style="color:var(--tf-text-muted);">Tiền thuế GTGT:</span>
                    <strong>@_dto.TotalTax.ToString("N0")</strong>
                </div>
            }
            <div style="display:flex; justify-content:space-between; margin-top:16px; padding-top:16px; border-top:1px dashed #cbd5e1; font-size:18px;">
                <span>Tổng cộng:</span>
                <strong style="color:var(--tf-color-danger);">@_dto.GrandTotal.ToString("N0")</strong>
            </div>
        </div>
    </div>
</div>

@code {
    private InvoiceDto _dto = new() { InvoiceDate = DateTime.Now, Type = InvoiceType.SalesInvoice, PaymentMethod = "TM/CK" };
    private bool _isProcessing = false;
    private string? _errorMessage;

    private string _customerSearch = "";
    private List<Customer> _customers = new();
    
    private string _productSearch = "";
    private List<Product> _products = new();

    private CancellationTokenSource? _customerSearchCts;
    private async Task SearchCustomers(KeyboardEventArgs e)
    {
        _customerSearchCts?.Cancel();
        _customerSearchCts = new CancellationTokenSource();
        var token = _customerSearchCts.Token;

        if (_customerSearch.Length >= 2)
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    _customers = await SalesService.SearchCustomersAsync(_customerSearch, token);
                    StateHasChanged();
                }
            }
            catch (TaskCanceledException) { }
        }
        else
        {
            _customers.Clear();
        }
    }

    private void SelectCustomer(Customer c)
    {
        _dto.CustomerId = c.Id;
        _dto.CustomerName = c.Name;
        _dto.CustomerCompanyName = c.CompanyName;
        _dto.CustomerTaxCode = c.TaxCode;
        _dto.CustomerAddress = c.Address;
        _dto.CustomerEmail = c.Phone; // store in snapshot
        _customers.Clear();
        _customerSearch = "";
    }

    private CancellationTokenSource? _productSearchCts;
    private async Task SearchProducts(KeyboardEventArgs e)
    {
        _productSearchCts?.Cancel();
        _productSearchCts = new CancellationTokenSource();
        var token = _productSearchCts.Token;

        if (_productSearch.Length >= 2)
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    _products = await SalesService.SearchProductsAsync(_productSearch, token);
                    StateHasChanged();
                }
            }
            catch (TaskCanceledException) { }
        }
        else
        {
            _products.Clear();
        }
    }

    private async Task SelectProduct(Product p)
    {
        // Get active price
        var price = await SalesService.GetActivePriceAsync(p.Id, _dto.InvoiceDate);

        _dto.Items.Add(new InvoiceItemDto
        {
            ProductId = p.Id,
            ProductCode = p.Code,
            ProductName = p.Name,
            UnitName = p.Unit?.Name ?? string.Empty,
            Quantity = 1,
            UnitPrice = price,
            TaxRate = 0
        });
        _products.Clear();
        _productSearch = "";
        CalculateTotals();
    }

    private void AddManualItem()
    {
        _dto.Items.Add(new InvoiceItemDto { Quantity = 1 });
        CalculateTotals();
    }

    private void RemoveItem(InvoiceItemDto item)
    {
        _dto.Items.Remove(item);
        CalculateTotals();
    }

    private void CalculateTotals()
    {
        _dto.SubTotal = 0;
        _dto.TotalTax = 0;

        foreach (var item in _dto.Items)
        {
            if (_dto.Type != InvoiceType.VatInvoice) item.TaxRate = 0;
            
            var lineAmount = (item.Quantity * item.UnitPrice) - item.DiscountAmount;
            item.TaxAmount = lineAmount * (item.TaxRate / 100m);
            item.LineTotal = lineAmount + item.TaxAmount;

            _dto.SubTotal += lineAmount;
            _dto.TotalTax += item.TaxAmount;
        }

        _dto.GrandTotal = _dto.SubTotal - _dto.TotalDiscount + _dto.TotalTax;
    }

    private async Task SaveInvoice()
    {
        if (string.IsNullOrWhiteSpace(_dto.CustomerName))
        {
            _errorMessage = "Vui lòng nhập họ tên người mua.";
            return;
        }
        if (!_dto.Items.Any())
        {
            _errorMessage = "Vui lòng thêm ít nhất một hàng hóa/dịch vụ.";
            return;
        }

        _isProcessing = true;
        _errorMessage = null;
        try
        {
            CalculateTotals();
            var inv = await InvoiceService.CreateManualInvoiceAsync(_dto);
            Nav.NavigateTo($"/hoa-don/chi-tiet/{inv.Id}");
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isProcessing = false;
        }
    }
}
`;
fs.writeFileSync(file, content, 'utf8');
