const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Employees/Index.razor', 'utf8');
const lines = content.split('\n');
const idx = lines.findIndex(l => l.includes('OnParametersSetAsync') || l.includes('OnInitializedAsync'));
console.log(lines.slice(idx, idx + 60).join('\n'));
