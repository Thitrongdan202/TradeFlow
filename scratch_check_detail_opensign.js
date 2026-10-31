const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor', 'utf8');
const lines = content.split('\n');
console.log(lines.slice(955, 990).join('\n'));
