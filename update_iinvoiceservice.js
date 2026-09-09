const fs = require('fs');
let file = 'src/TradeFlow.Application/Common/Interfaces/IInvoiceService.cs';
let content = fs.readFileSync(file, 'utf8');

content = content.replace('Task<InvoiceDto> CreateInvoiceFromOrderAsync', 'Task<InvoiceDto> CreateManualInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default);\n    Task<InvoiceDto> CreateInvoiceFromOrderAsync');

fs.writeFileSync(file, content, 'utf8');
