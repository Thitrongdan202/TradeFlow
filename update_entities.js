const fs = require('fs');

function replaceFile(path, oldText, newText) {
    let content = fs.readFileSync(path, 'utf8');
    content = content.replace(oldText, newText);
    fs.writeFileSync(path, content, 'utf8');
}

replaceFile(
    'src/TradeFlow.Domain/Entities/Settings/CompanySettings.cs',
    'public string? Email { get; set; }',
    'public string? Email { get; set; }\n    public string? BankAccount { get; set; }'
);

replaceFile(
    'src/TradeFlow.Domain/Entities/Sales/Invoice.cs',
    'public string? CompanyEmail { get; set; }',
    'public string? CompanyEmail { get; set; }\n    public string? CompanyBankAccount { get; set; }'
);

replaceFile(
    'src/TradeFlow.Domain/Entities/Sales/Invoice.cs',
    'public string? CustomerEmail { get; set; }',
    'public string? CustomerEmail { get; set; }\n    public string? CustomerBankAccount { get; set; }'
);

replaceFile(
    'src/TradeFlow.Application/Common/Models/Sales/InvoiceDto.cs',
    'public string? CompanyEmail { get; set; }',
    'public string? CompanyEmail { get; set; }\n    public string? CompanyBankAccount { get; set; }'
);

replaceFile(
    'src/TradeFlow.Application/Common/Models/Sales/InvoiceDto.cs',
    'public string? CustomerEmail { get; set; }',
    'public string? CustomerEmail { get; set; }\n    public string? CustomerBankAccount { get; set; }'
);
