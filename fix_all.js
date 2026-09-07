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
    c = c.replace(/\?\? \\"\\"/g, '?? string.Empty');
    c = c.replace(/\?\? ""/g, '?? string.Empty');
    c = c.replace(/DbContext\.UnitsOfMeasure/g, 'DbContext.Units');
    fs.writeFileSync(f, c, 'utf8');
});

const forms = [
    'src/TradeFlow.Web/Components/Pages/MasterData/Categories/Form.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Currencies/Form.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Products/Form.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Units/Form.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Customers/Form.razor',
    'src/TradeFlow.Web/Components/Pages/Partners/Suppliers/Form.razor',
    'src/TradeFlow.Web/Components/Pages/Warehouses/Form.razor'
];

forms.forEach(f => {
    let c = fs.readFileSync(f, 'utf8');
    
    // Inject auth services
    if (!c.includes('IAuthorizationService')) {
        c = c.replace('@inject NavigationManager Nav', '@inject NavigationManager Nav\n@inject Microsoft.AspNetCore.Authorization.IAuthorizationService AuthService\n@inject Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider');
    }
    
    // Fix AuditEventType
    c = c.replace(/AuditEventType\.ProductCategoryDeleted/g, 'AuditEventType.CategoryDeleted');
    c = c.replace(/AuditEventType\.UnitOfMeasureDeleted/g, 'AuditEventType.UnitDeleted');
    
    // Fix DbContext.UnitsOfMeasure
    c = c.replace(/DbContext\.UnitsOfMeasure/g, 'DbContext.Units');
    
    fs.writeFileSync(f, c, 'utf8');
});

const details = [
    'src/TradeFlow.Web/Components/Pages/Pricing/Detail.razor',
    'src/TradeFlow.Web/Components/Pages/Sales/Orders/Detail.razor',
    'src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor'
];

details.forEach(f => {
    let c = fs.readFileSync(f, 'utf8');
    if (!c.includes('NavigationManager Nav')) {
        c = c.replace('@inject IInvoiceService InvoiceService', '@inject IInvoiceService InvoiceService\n@inject NavigationManager Nav');
    }
    if (!c.includes('IAuthorizationService')) {
        c = c.replace('@inject NavigationManager Nav', '@inject NavigationManager Nav\n@inject Microsoft.AspNetCore.Authorization.IAuthorizationService AuthService\n@inject Microsoft.AspNetCore.Components.Authorization.AuthenticationStateProvider AuthStateProvider');
    }
    fs.writeFileSync(f, c, 'utf8');
});
