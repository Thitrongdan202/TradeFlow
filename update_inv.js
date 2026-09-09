const fs = require('fs');
let serviceCode = fs.readFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', 'utf8');

let startIndex = serviceCode.indexOf('public async Task<bool> UpdateInvoiceAsync');
let endIndex = serviceCode.indexOf('public async Task<bool> IssueInvoiceAsync');

let newUpdate = `public async Task<bool> UpdateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == dto.Id, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        inv.CustomerName = dto.CustomerName;
        inv.CustomerCompanyName = dto.CustomerCompanyName;
        inv.CustomerTaxCode = dto.CustomerTaxCode;
        inv.CustomerAddress = dto.CustomerAddress;
        inv.PaymentMethod = dto.PaymentMethod;
        inv.Notes = dto.Notes;
        inv.Type = dto.Type;

        _context.InvoiceItems.RemoveRange(inv.Items);
        inv.Items.Clear();

        foreach(var item in dto.Items)
        {
            inv.Items.Add(new InvoiceItem
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

        inv.SubTotal = dto.SubTotal;
        inv.TotalDiscount = dto.TotalDiscount;
        inv.TotalTax = dto.TotalTax;
        inv.GrandTotal = dto.GrandTotal;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceUpdated, "Invoice", inv.Id.ToString(), "Draft invoice updated", _currentUserService.UserId);

        return true;
    }

    `;

serviceCode = serviceCode.substring(0, startIndex) + newUpdate + serviceCode.substring(endIndex);
fs.writeFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', serviceCode);
