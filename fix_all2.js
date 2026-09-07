const fs = require('fs');

// Fix the \n issue in Form.razor files
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
    c = c.replace(/\\n@inject/g, '\n@inject');
    c = c.replace(/DbContext\.Units\./g, 'DbContext.UnitsOfMeasure.');
    fs.writeFileSync(f, c, 'utf8');
});

// Also fix DbContext.Units in List.razor
const list = 'src/TradeFlow.Web/Components/Pages/MasterData/Units/List.razor';
let c = fs.readFileSync(list, 'utf8');
c = c.replace(/DbContext\.Units\./g, 'DbContext.UnitsOfMeasure.');
fs.writeFileSync(list, c, 'utf8');

// Fix Pricing List.razor ConfirmDelete issue
// Pricing has ConfirmDelete(PriceListDto item) AND we added ConfirmDelete(int id, string name)
const pricingList = 'src/TradeFlow.Web/Components/Pages/Pricing/List.razor';
let pc = fs.readFileSync(pricingList, 'utf8');
pc = pc.replace(/ConfirmDelete\(item\.Id, item\.Code \?\? string\.Empty\)/g, 'ConfirmDelete(item)');
fs.writeFileSync(pricingList, pc, 'utf8');

