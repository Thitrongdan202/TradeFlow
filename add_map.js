const fs = require('fs');
let code = fs.readFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', 'utf8');

let mapMethod = `
    private InvoiceDto MapToDto(Invoice invoice)
    {
        var dto = new InvoiceDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            SalesOrderId = invoice.SalesOrderId,
            CustomerId = invoice.CustomerId,
            CompanyName = invoice.CompanyName,
            CompanyTaxCode = invoice.CompanyTaxCode,
            CompanyAddress = invoice.CompanyAddress,
            CompanyPhone = invoice.CompanyPhone,
            CompanyEmail = invoice.CompanyEmail,
            CompanyLogoUrl = invoice.CompanyLogoUrl,
            CustomerName = invoice.CustomerName,
            CustomerCompanyName = invoice.CustomerCompanyName,
            CustomerTaxCode = invoice.CustomerTaxCode,
            CustomerAddress = invoice.CustomerAddress,
            CustomerEmail = invoice.CustomerEmail,
            PaymentMethod = invoice.PaymentMethod,
            Notes = invoice.Notes,
            Status = invoice.Status,
            Type = invoice.Type,
            SubTotal = invoice.SubTotal,
            TotalDiscount = invoice.TotalDiscount,
            TotalTax = invoice.TotalTax,
            GrandTotal = invoice.GrandTotal
        };

        foreach (var item in invoice.Items)
        {
            dto.Items.Add(new InvoiceItemDto
            {
                Id = item.Id,
                InvoiceId = item.InvoiceId,
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

        return dto;
    }
`;

let endIndex = code.lastIndexOf('}');
code = code.substring(0, endIndex) + mapMethod + '\n}\n';

fs.writeFileSync('src/TradeFlow.Infrastructure/Services/InvoiceService.cs', code);
