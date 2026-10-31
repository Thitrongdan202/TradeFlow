const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');
const lines = content.split('\n');
lines.forEach((l, idx) => {
  if (l.includes('transition: all') || l.includes('transition:all')) {
    console.log((idx+1) + ': ' + l.trim());
  }
});
