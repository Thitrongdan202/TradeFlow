const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Invoices/Detail.razor', 'utf8');
const lines = content.split('\n');
lines.forEach((l, idx) => {
  if (l.includes('VerifySignature(') || l.includes('OpenSignModal(') || l.includes('_showVerifyModal') || l.includes('_showSignModal')) {
    console.log((idx+1) + ': ' + l);
  }
});
