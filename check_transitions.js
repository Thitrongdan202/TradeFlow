const fs = require('fs');
const css = fs.readFileSync('src/TradeFlow.Web/wwwroot/app.css', 'utf8');
const lines = css.split('\n');
[525, 884, 993, 1066, 1637, 2082, 2389, 2441, 2510, 2973, 3431, 4113, 4143, 4475, 7209, 7254, 7446, 7495, 7564, 7610, 7905].forEach(num => {
  console.log(`L${num}: ${lines[num - 1].trim()}  (context: ${lines.slice(Math.max(0, num - 3), num + 2).map(l => l.trim()).join('; ')})`);
});
