const fs = require('fs');
let code = fs.readFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', 'utf8');

let methodStr = `
    public async Task<bool> DeleteInvoiceAsync(int id, CancellationToken cancellationToken = default)
    {
        var inv = await _context.Invoices.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (inv == null || inv.Status != InvoiceStatus.Draft) return false;

        _context.Invoices.Remove(inv);
        
        if (inv.SalesOrderId.HasValue)
        {
            var so = await _context.SalesOrders.FindAsync(new object[] { inv.SalesOrderId.Value }, cancellationToken);
            if (so != null && so.Status == SalesOrderStatus.Invoiced)
            {
                so.Status = SalesOrderStatus.Confirmed;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(AuditEventType.InvoiceDeleted, "Invoice", inv.Id.ToString(), "Draft invoice deleted", _currentUserService.UserId);

        return true;
    }
`;

let endIndex = code.lastIndexOf('}');
code = code.substring(0, endIndex) + methodStr + '\n}\n';

fs.writeFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', code);
