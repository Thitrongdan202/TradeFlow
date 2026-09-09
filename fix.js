const fs = require('fs');

let invCode = fs.readFileSync('src/TradeFlow.Domain/Entities/Sales/Invoice.cs', 'utf8');
invCode = invCode.replace(/'TM\/CK'/g, '"TM/CK"');
fs.writeFileSync('src/TradeFlow.Domain/Entities/Sales/Invoice.cs', invCode);

let dtoCode = fs.readFileSync('src/TradeFlow.Application/Common/Models/Sales/InvoiceDto.cs', 'utf8');
dtoCode = dtoCode.replace(/'TM\/CK'/g, '"TM/CK"');
fs.writeFileSync('src/TradeFlow.Application/Common/Models/Sales/InvoiceDto.cs', dtoCode);
