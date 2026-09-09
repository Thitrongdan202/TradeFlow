const fs = require('fs');
let file = 'src/TradeFlow.Infrastructure/Services/InvoiceService.cs';
let content = fs.readFileSync(file, 'utf8');

const newMethod = `
    public async Task<InvoiceDto> CreateManualInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var company = await _context.CompanySettings.FirstOrDefaultAsync(cancellationToken);
        
        var sequence = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == "Invoice", cancellationToken);
        if (sequence == null)
        {
            sequence = new Domain.Entities.Settings.SystemSequence { SequenceKey = "Invoice", Prefix = "INV-", CurrentNumber = 1 };
            _context.SystemSequences.Add(sequence);
        }
        string invNumber = $"{sequence.Prefix}{DateTime.Now.Year}-{sequence.CurrentNumber:D4}";
        sequence.CurrentNumber++;

        var invoice = new Invoice
        {
            InvoiceNumber = invNumber,
            InvoiceDate = dto.InvoiceDate == default ? DateTime.UtcNow : dto.InvoiceDate,
            CustomerId = dto.CustomerId,
            
            CompanyName = company?.CompanyName ?? "TradeFlow Company",
            CompanyTaxCode = company?.TaxCode,
            CompanyAddress = company?.Address,
            CompanyPhone = company?.Phone,
            CompanyEmail = company?.Email,
            CompanyLogoUrl = company?.LogoPath,
            CompanyBankAccount = company?.BankAccount,
            
            CustomerName = dto.CustomerName,
            CustomerCompanyName = dto.CustomerCompanyName,
            CustomerTaxCode = dto.CustomerTaxCode,
            CustomerAddress = dto.CustomerAddress,
            CustomerEmail = dto.CustomerEmail,
            PaymentMethod = dto.PaymentMethod ?? "TM/CK",
            CustomerBankAccount = dto.CustomerBankAccount,
            
            Notes = dto.Notes,
            Status = InvoiceStatus.Draft, 
            Type = dto.Type,
            
            SubTotal = dto.SubTotal,
            TotalDiscount = dto.TotalDiscount,
            TotalTax = dto.TotalTax,
            GrandTotal = dto.GrandTotal
        };

        foreach (var item in dto.Items)
        {
            invoice.Items.Add(new InvoiceItem
            {
                ProductId = item.ProductId,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                DiscountAmount = item.DiscountAmount,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                LineTotal = item.LineTotal
            });
        }

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceCreated, "Invoice", invoice.Id.ToString(), "Manual invoice created", _currentUserService.UserId);

        return MapToDto(invoice);
    }
`;

content = content.replace('public async Task<InvoiceDto> CreateInvoiceFromOrderAsync', newMethod + '\n    public async Task<InvoiceDto> CreateInvoiceFromOrderAsync');
fs.writeFileSync(file, content, 'utf8');
