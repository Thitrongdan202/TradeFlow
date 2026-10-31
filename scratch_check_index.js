const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Employees/Index.razor', 'utf8');
const lines = content.split('\n');
const idx = lines.findIndex(l => l.includes('SupplyParameterFromQuery(Name = "tab")'));
console.log(lines.slice(idx - 5, idx + 45).join('\n'));
