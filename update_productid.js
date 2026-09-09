const fs = require('fs');

function replaceFile(path, oldText, newText) {
    let content = fs.readFileSync(path, 'utf8');
    content = content.replace(oldText, newText);
    fs.writeFileSync(path, content, 'utf8');
}

replaceFile(
    'src/TradeFlow.Domain/Entities/Sales/InvoiceItem.cs',
    'public int ProductId { get; set; }\n    public Product Product { get; set; } = null!;',
    'public int? ProductId { get; set; }\n    public Product? Product { get; set; }'
);

replaceFile(
    'src/TradeFlow.Infrastructure/Persistence/Configurations/InvoiceItemConfiguration.cs',
    '.HasForeignKey(x => x.ProductId)\n            .OnDelete(DeleteBehavior.Restrict);',
    '.HasForeignKey(x => x.ProductId)\n            .OnDelete(DeleteBehavior.SetNull);'
);

replaceFile(
    'src/TradeFlow.Application/Common/Models/Sales/InvoiceDto.cs',
    'public int ProductId { get; set; }',
    'public int? ProductId { get; set; }'
);
