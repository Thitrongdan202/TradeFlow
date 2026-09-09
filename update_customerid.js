const fs = require('fs');

function replaceFile(path, oldText, newText) {
    let content = fs.readFileSync(path, 'utf8');
    content = content.replace(oldText, newText);
    fs.writeFileSync(path, content, 'utf8');
}

replaceFile(
    'src/TradeFlow.Domain/Entities/Sales/Invoice.cs',
    'public int CustomerId { get; set; }\n    public Customer Customer { get; set; } = null!;',
    'public int? CustomerId { get; set; }\n    public Customer? Customer { get; set; }'
);

replaceFile(
    'src/TradeFlow.Infrastructure/Persistence/Configurations/InvoiceConfiguration.cs',
    '.HasForeignKey(x => x.CustomerId)\n            .OnDelete(DeleteBehavior.Restrict);',
    '.HasForeignKey(x => x.CustomerId)\n            .OnDelete(DeleteBehavior.SetNull);'
);

replaceFile(
    'src/TradeFlow.Application/Common/Models/Sales/InvoiceDto.cs',
    'public int CustomerId { get; set; }',
    'public int? CustomerId { get; set; }'
);
