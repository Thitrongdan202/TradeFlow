const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');
const lines = content.split('\n');
const idx = lines.findIndex(l => l.includes('--tf-motion-instant'));
console.log(lines.slice(idx - 5, idx + 25).join('\n'));
