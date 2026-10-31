const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor', 'utf8');
const lines = content.split('\n');
lines.forEach((l, idx) => {
  if (l.includes('Task ShowProductDetailsAsync')) {
    console.log((idx+1) + ': ' + l);
    console.log(lines.slice(idx, idx + 25).join('\n'));
  }
});
