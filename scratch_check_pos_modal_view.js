const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor', 'utf8');
const lines = content.split('\n');
console.log(lines.slice(2020, 2060).join('\n'));
