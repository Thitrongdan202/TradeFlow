const fs = require('fs');
let s = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Sales/Pos/PosIndex.razor', 'utf8');
console.log('CRLF:', s.includes('\r\n'));
console.log('index of <header class="tf-sales-header">:', s.indexOf('<header class="tf-sales-header">'));
console.log('index of </header>:', s.indexOf('</header>'));
