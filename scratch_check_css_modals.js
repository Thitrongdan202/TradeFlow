const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');
const lines = content.split('\n');
lines.forEach((l, idx) => {
  if (l.includes('.tf-modal-overlay') || l.includes('@keyframes tf-modal-enter') || l.includes('.tf-modal {')) {
    console.log((idx+1) + ': ' + l);
  }
});
