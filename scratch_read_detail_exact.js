const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor', 'utf8');
const lines = content.split('\n');
console.log('--- Lines 735-760 ---');
console.log(lines.slice(735, 760).join('\n'));
console.log('--- Lines 960-1045 ---');
console.log(lines.slice(960, 1045).join('\n'));
