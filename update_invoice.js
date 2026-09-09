const fs = require('fs');
let file = 'src/TradeFlow.Infrastructure/Services/InvoiceService.cs';
let content = fs.readFileSync(file, 'utf8');

content = content.replace('InvoiceDate = DateTime.UtcNow,', 'InvoiceDate = order.OrderDate,');

fs.writeFileSync(file, content, 'utf8');
