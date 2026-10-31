const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor', 'utf8');
const lines = content.split('\n');
lines.forEach((l, idx) => {
  if (l.includes('_showProductDetailModal')) {
    console.log((idx+1) + ': ' + l);
  }
});
