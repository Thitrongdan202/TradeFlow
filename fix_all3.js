const fs = require('fs');

const files = [
    'src/TradeFlow.Web/Components/Pages/MasterData/Units/List.razor',
    'src/TradeFlow.Web/Components/Pages/MasterData/Units/Form.razor'
];

files.forEach(f => {
    let c = fs.readFileSync(f, 'utf8');
    c = c.replace(/DbContext\.UnitsOfMeasure\./g, 'DbContext.UnitOfMeasures.');
    c = c.replace(/DbContext\.UnitsOfMeasure\.Remove/g, 'DbContext.UnitOfMeasures.Remove');
    c = c.replace(/DbContext\.Units\./g, 'DbContext.UnitOfMeasures.');
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
    c = c.replace(/<AuthorizeView Policy="([^"]+)">/g, '<AuthorizeView Policy="$1" Context="authContext">');
    fs.writeFileSync(f, c, 'utf8');
});
