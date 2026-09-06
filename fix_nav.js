const fs = require('fs');
let c = fs.readFileSync('src/TradeFlow.Web/Components/Layout/NavMenu.razor', 'utf8');
c = c.split(' @onclick="OnNavClick"').join('');
c = c.replace('[Parameter] public EventCallback OnNavClick { get; set; }', '');
fs.writeFileSync('src/TradeFlow.Web/Components/Layout/NavMenu.razor', c);
