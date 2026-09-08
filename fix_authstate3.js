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
    
    // Inject var authState at the top of ProcessSingleDelete
    c = c.replace(/private async Task ProcessSingleDelete\(int currentId\)\s*\{/, "private async Task ProcessSingleDelete(int currentId)\n    {\n        var authState = await AuthStateProvider.GetAuthenticationStateAsync();");
    
    fs.writeFileSync(f, c, 'utf8');
    console.log("Fixed ProcessSingleDelete in " + f);
});
