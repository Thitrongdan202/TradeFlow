const fs = require('fs');
let code = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Orders/Create.razor', 'utf8');

let searchProductsMatch = /private async Task SearchProducts\(KeyboardEventArgs e\)\s*\{\s*_isProductSearched = true;\s*if \(_productSearch\.Length >= 2\)\s*\{\s*_productSearchResults = await SalesService\.SearchProductsAsync\(_productSearch\);\s*\}\s*else\s*\{\s*_productSearchResults\.Clear\(\);\s*_isProductSearched = false;\s*\}\s*\}/;

let searchProductsNew = `private CancellationTokenSource? _productSearchCts;
    private async Task SearchProducts(KeyboardEventArgs e)
    {
        _productSearchCts?.Cancel();
        _productSearchCts = new CancellationTokenSource();
        var token = _productSearchCts.Token;

        _isProductSearched = true;
        if (_productSearch.Length >= 2)
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    _productSearchResults = await SalesService.SearchProductsAsync(_productSearch, token);
                    StateHasChanged();
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _errorMessage = "Lỗi khi tìm sản phẩm: " + ex.Message;
                StateHasChanged();
            }
        }
        else
        {
            _productSearchResults.Clear();
            _isProductSearched = false;
        }
    }`;

code = code.replace(searchProductsMatch, searchProductsNew);


let searchCustomersMatch = /private async Task SearchCustomers\(KeyboardEventArgs e\)\s*\{\s*_isCustomerSearched = true;\s*if \(_customerSearch\.Length >= 2\)\s*\{\s*_customerSearchResults = await SalesService\.SearchCustomersAsync\(_customerSearch\);\s*\}\s*else\s*\{\s*_customerSearchResults\.Clear\(\);\s*_isCustomerSearched = false;\s*\}\s*\}/;

let searchCustomersNew = `private CancellationTokenSource? _customerSearchCts;
    private async Task SearchCustomers(KeyboardEventArgs e)
    {
        _customerSearchCts?.Cancel();
        _customerSearchCts = new CancellationTokenSource();
        var token = _customerSearchCts.Token;

        _isCustomerSearched = true;
        if (_customerSearch.Length >= 2)
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    _customerSearchResults = await SalesService.SearchCustomersAsync(_customerSearch, token);
                    StateHasChanged();
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                _errorMessage = "Lỗi khi tìm khách hàng: " + ex.Message;
                StateHasChanged();
            }
        }
        else
        {
            _customerSearchResults.Clear();
            _isCustomerSearched = false;
        }
    }`;

code = code.replace(searchCustomersMatch, searchCustomersNew);

fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Sales/Orders/Create.razor', code);
