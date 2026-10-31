const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Admin/Security/Dashboard.razor', 'utf8');
const lines = content.split('\n');
console.log(lines.slice(1275, 1305).join('\n'));
