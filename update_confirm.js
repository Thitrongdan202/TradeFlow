const fs = require('fs');
let file = 'src/TradeFlow.Web/Components/Pages/Sales/Orders/Detail.razor';
let content = fs.readFileSync(file, 'utf8');

let newMethod = `    private async Task ConfirmOrder()
    {
        _isProcessing = true;
        _errorMessage = null;
        try
        {
            await SalesService.ConfirmOrderAsync(Id);
            try {
                var inv = await InvoiceService.CreateInvoiceFromOrderAsync(Id, (InvoiceType)_selectedInvoiceType);
                Nav.NavigateTo($"/hoa-don/chi-tiet/{inv.Id}");
                return;
            } catch (Exception ex) {
                _errorMessage = $"Đơn hàng đã được xác nhận nhưng không thể tự động tạo hóa đơn: " + ex.Message;
                await LoadDataAsync();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isProcessing = false;
        }
    }`;

content = content.replace(/private async Task ConfirmOrder\(\)[\s\S]*?finally[\s\S]*?_isProcessing = false;[\s\S]*?\}[\s\S]*?\}/, newMethod);
fs.writeFileSync(file, content, 'utf8');
