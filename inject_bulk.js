const fs = require('fs');

const files = [
    'src/TradeFlow.Web/Components/Pages/MasterData/Categories/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Currencies/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Products/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Units/List.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Customers/List.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Suppliers/List.razor',
    'src/TradeFlow.Web/Components/Pages/Warehouses/List.razor'
];

files.forEach(f => {
    let c = fs.readFileSync(f, 'utf8');

    // Skip if already has ToggleSelect
    if (c.includes('ToggleSelect')) return;

    // 1. Add column to theader
    c = c.replace(/<thead>\s*<tr>/, `<thead>\n                  <tr>\n                      <th style="width: 40px; text-align: center;"><input type="checkbox" @onchange="ToggleSelectAll" checked="@(FilteredItems.Any() && _selectedItems.Count == FilteredItems.Count())" /></th>`);

    // 2. Adjust colspans
    c = c.replace(/colspan="(\d+)"/g, (match, p1) => `colspan="${parseInt(p1) + 1}"`);

    // 3. Add column to tbody rows
    c = c.replace(/foreach\s*\(\s*var\s+item\s+in\s+FilteredItems\s*\)\s*\{\s*<tr[^>]*>/, match => {
        return match + `\n                              <td style="text-align: center;"><input type="checkbox" @onchange="@(e => ToggleSelect(item.Id, e))" checked="@(_selectedItems.Contains(item.Id))" /></td>`;
    });

    // 4. Extract permission from ExecuteDelete
    let permMatch = c.match(/"(Permission:[^:]+:Delete)"/);
    let perm = permMatch ? permMatch[1] : "Permission:Unknown:Delete";

    // 5. Build Bulk Delete Modal and logic
    const bulkHtml = `
@if (_selectedItems.Count > 0)
{
    <div style="background: #eff6ff; border: 1px solid #bfdbfe; padding: 12px 16px; border-radius: 6px; margin-bottom: 16px; display: flex; justify-content: space-between; align-items: center;">
        <span style="color: #1e3a8a; font-weight: 500;">Đã chọn @_selectedItems.Count bản ghi</span>
        <AuthorizeView Policy="${perm}">
            <button type="button" class="tf-btn tf-btn-danger" @onclick="ConfirmBulkDelete">Xóa đã chọn</button>
        </AuthorizeView>
    </div>
}

@if (_showBulkDeleteModal)
{
    <div style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); z-index: 1000; display: flex; align-items: center; justify-content: center;">
        <div style="background: white; padding: 24px; border-radius: 8px; width: 500px; max-width: 90%; box-shadow: 0 10px 25px rgba(0,0,0,0.2);">
            <h3 style="margin-top: 0; margin-bottom: 16px; font-size: 18px; color: var(--tf-danger);">Xóa @_selectedItems.Count bản ghi đã chọn?</h3>
            <p style="margin-bottom: 24px;">Hành động này không thể hoàn tác. Các bản ghi đang được sử dụng sẽ bị bỏ qua hoặc vô hiệu hóa thay vì xóa hoàn toàn.</p>
            @if (_bulkDeleteErrors.Count > 0)
            {
                <div class="tf-alert tf-alert-danger" style="margin-bottom: 16px; max-height: 150px; overflow-y: auto;">
                    <ul style="margin: 0; padding-left: 20px;">
                        @foreach (var err in _bulkDeleteErrors)
                        {
                            <li>@err</li>
                        }
                    </ul>
                </div>
            }
            <div style="display: flex; justify-content: flex-end; gap: 12px;">
                <button type="button" class="tf-btn tf-btn-outline-secondary" @onclick="CancelBulkDelete">Hủy</button>
                <button type="button" class="tf-btn tf-btn-danger" @onclick="ExecuteBulkDelete" disabled="@_isBulkDeleting">
                    @(_isBulkDeleting ? "Đang xử lý..." : "Xóa tất cả")
                </button>
            </div>
        </div>
    </div>
}
`;

    // Extract the inner logic of ExecuteDelete to replicate in ExecuteBulkDelete
    let executeDeleteMethod = c.match(/private async Task ExecuteDelete\(\)[\s\S]*?catch[^}]*\n\s*\}[^}]*\}/);
    if (!executeDeleteMethod) {
        console.log("Could not find ExecuteDelete in " + f);
    }

    const csharpLogic = `
    private HashSet<int> _selectedItems = new();
    private bool _showBulkDeleteModal = false;
    private List<string> _bulkDeleteErrors = new();
    private bool _isBulkDeleting = false;

    private void ToggleSelectAll(ChangeEventArgs e)
    {
        if (e.Value is bool isChecked && isChecked)
        {
            _selectedItems = new HashSet<int>(FilteredItems.Select(x => x.Id));
        }
        else
        {
            _selectedItems.Clear();
        }
    }

    private void ToggleSelect(int id, ChangeEventArgs e)
    {
        if (e.Value is bool isChecked && isChecked)
        {
            _selectedItems.Add(id);
        }
        else
        {
            _selectedItems.Remove(id);
        }
    }

    private void ConfirmBulkDelete()
    {
        _bulkDeleteErrors.Clear();
        _showBulkDeleteModal = true;
    }

    private void CancelBulkDelete()
    {
        _showBulkDeleteModal = false;
        _isBulkDeleting = false;
    }

    private async Task ExecuteBulkDelete()
    {
        if (_selectedItems.Count == 0) return;
        _isBulkDeleting = true;
        _bulkDeleteErrors.Clear();
        StateHasChanged();

        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        if (!(await AuthService.AuthorizeAsync(authState.User, "${perm}")).Succeeded)
        {
            _bulkDeleteErrors.Add("Bạn không có quyền thực hiện thao tác này.");
            _isBulkDeleting = false;
            return;
        }

        try
        {
            int successCount = 0;
            var idsToProcess = _selectedItems.ToList();
            
            foreach (var id in idsToProcess)
            {
                _itemToDeleteId = id; // reuse the id for the logic block
                try 
                {
                    await ProcessSingleDelete(id);
                    _selectedItems.Remove(id);
                    successCount++;
                } 
                catch (Exception ex)
                {
                    _bulkDeleteErrors.Add($"Bản ghi ID {id}: {ex.Message}");
                }
            }

            if (successCount > 0)
            {
                await OnInitializedAsync();
            }
            
            if (_selectedItems.Count == 0)
            {
                CancelBulkDelete();
            }
        }
        finally
        {
            _isBulkDeleting = false;
            _itemToDeleteId = null;
            StateHasChanged();
        }
    }

    private async Task ProcessSingleDelete(int currentId)
    {
        // Re-implement the original ExecuteDelete inner logic inside this try block
        // I will dynamically extract the try block from ExecuteDelete in the string replacement
    }
`;

    // Wait, the best way to reuse the logic is to refactor ExecuteDelete itself!
    // But since regex refactoring is risky, I will just extract the inside of `try { int id = ...; }` from ExecuteDelete.

    let tryBlockMatch = c.match(/try\s*\{\s*(int id = _itemToDeleteId\.Value;[\s\S]*?CancelDelete\(\);)\s*await OnInitializedAsync\(\);/);
    let tryContent = "";
    if (tryBlockMatch) {
        tryContent = tryBlockMatch[1];
        // Modify tryContent so that if it does `_deleteError = "..." ; return;`, it instead throws an Exception!
        tryContent = tryContent.replace(/_deleteError\s*=\s*"([^"]+)";\s*return;/g, 'throw new Exception("$1");');
        tryContent = tryContent.replace(/CancelDelete\(\);/g, ''); // Remove CancelDelete from inside
    } else {
        console.log("Could not find try block in ExecuteDelete of " + f);
    }

    let finalCsharpLogic = csharpLogic.replace(/\/\/ Re-implement the original ExecuteDelete inner logic inside this try block\s*\/\/ [^\n]*\n/, tryContent + '\n');

    // Inject bulkHtml before <table
    c = c.replace('<table class="tf-table">', bulkHtml + '\n<table class="tf-table">');

    // Inject finalCsharpLogic at the end of @code
    c = c.replace(/}\s*$/, finalCsharpLogic + '}\n');

    fs.writeFileSync(f, c, 'utf8');
    console.log("Processed " + f);
});
