const fs = require('fs');
const files = [
    'src/TradeFlow.Web/Components/Pages/MasterData/Categories/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Currencies/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Products/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Units/List.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Customers/List.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Suppliers/List.razor',
    'src/TradeFlow.Web/Components/Pages/Pricing/List.razor',
    'src/TradeFlow.Web/Components/Pages/Sales/Invoices/List.razor',
    'src/TradeFlow.Web/Components/Pages/Sales/Orders/List.razor',
    'src/TradeFlow.Web/Components/Pages/Warehouses/List.razor'
];

files.forEach(f => {
    let c = fs.readFileSync(f, 'utf8');
    
    // Replace ConfirmDelete(item) with ConfirmDelete(item.Id, name)
    c = c.replace(/@onclick="\(\) => ConfirmDelete\(item\)"/g, match => {
        if (f.includes('Orders')) return '@onclick="() => ConfirmDelete(item.Id, item.Code ?? \\"\\")"';
        if (f.includes('Invoices')) return '@onclick="() => ConfirmDelete(item.Id, item.InvoiceNumber ?? \\"\\")"';
        if (f.includes('Pricing')) return '@onclick="() => ConfirmDelete(item.Id, item.Code ?? \\"\\")"';
        return '@onclick="() => ConfirmDelete(item.Id, item.Name ?? \\"\\")"';
    });
    
    // Replace object? _itemToDelete
    c = c.replace(/private object\? _itemToDelete;/g, 'private int? _itemToDeleteId;');
    
    // Replace modal check @if (_itemToDelete != null)
    c = c.replace(/@if \(_itemToDelete != null\)/g, '@if (_itemToDeleteId != null)');
    
    // Replace ConfirmDelete method
    const oldConfirm = /private void ConfirmDelete\(object item\)[\s\S]*?private void CancelDelete\(\)/;
    c = c.replace(oldConfirm, 'private void ConfirmDelete(int id, string name)\n    {\n        _itemToDeleteId = id;\n        _itemToDeleteName = name;\n        _deleteError = null;\n    }\n\n    private void CancelDelete()');
    
    // Replace _itemToDelete = null
    c = c.replace(/_itemToDelete = null;/g, '_itemToDeleteId = null;');
    c = c.replace(/if \(_itemToDelete == null\)/g, 'if (_itemToDeleteId == null)');
    c = c.replace(/dynamic item = _itemToDelete;\s*int id = item\.Id;/g, 'int id = _itemToDeleteId.Value;');
    
    c = c.replace(/var entity = await DbContext\.FindAsync\(item\.GetType\(\), id\);/g, match => {
        let dbSet = 'Products';
        if (f.includes('Categories')) dbSet = 'ProductCategories';
        else if (f.includes('Currencies')) dbSet = 'Currencies';
        else if (f.includes('Units')) dbSet = 'UnitsOfMeasure';
        else if (f.includes('Customers')) dbSet = 'Customers';
        else if (f.includes('Suppliers')) dbSet = 'Suppliers';
        else if (f.includes('Warehouses')) dbSet = 'Warehouses';
        return `var entity = await DbContext.${dbSet}.FindAsync(id);`;
    });
    
    fs.writeFileSync(f, c, 'utf8');
});
