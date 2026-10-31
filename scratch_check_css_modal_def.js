const fs = require('fs');
const content = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');
const lines = content.split('\n');
console.log(lines.slice(1950, 2030).join('\n'));
