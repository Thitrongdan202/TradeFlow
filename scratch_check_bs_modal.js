const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');
const lines = content.split('\n');
lines.forEach((l, idx) => {
  if (l.includes('.modal-dialog') || l.includes('.modal.fade') || l.includes('.modal-backdrop')) {
    console.log((idx+1) + ': ' + l);
  }
});
