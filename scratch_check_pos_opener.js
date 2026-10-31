const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor', 'utf8');
const lines = content.split('\n');
const idx = lines.findIndex(l => l.includes('ShowProductDetailsAsync') || l.includes('_showProductDetailModal') || l.includes('ShowProductDetail'));
console.log(lines.slice(idx - 5, idx + 40).join('\n'));
