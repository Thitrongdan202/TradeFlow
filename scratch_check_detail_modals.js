const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor', 'utf8');
const lines = content.split('\n');
console.log('--- Lines 600-650 (Sign Modal) ---');
console.log(lines.slice(600, 650).join('\n'));
console.log('--- Lines 735-785 (Verify Modal) ---');
console.log(lines.slice(735, 785).join('\n'));
