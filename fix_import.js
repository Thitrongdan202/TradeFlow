const fs = require('fs');
let c = fs.readFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Import.razor', 'utf8');
c = c.replace(/@onclick="ResetAnalysis"/g, '@onclick="CancelAndCleanupAsync"');
fs.writeFileSync('src/TradeFlow.Web/Components/Pages/Pricing/Import.razor', c, 'utf8');
