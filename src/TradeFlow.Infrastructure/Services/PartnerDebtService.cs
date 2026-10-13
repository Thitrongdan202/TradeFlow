using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Partners;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class PartnerDebtService : IPartnerDebtService
{
    private readonly TradeFlowDbContext _context;
    private readonly ILogger<PartnerDebtService> _logger;

    public PartnerDebtService(
        TradeFlowDbContext context,
        ILogger<PartnerDebtService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PartnerDebtSummaryDto> GetDebtSummaryAsync(CancellationToken cancellationToken = default)
    {
        // 1. Total Customers & Suppliers
        int totalCustomers = await _context.Customers.CountAsync(cancellationToken);
        int totalSuppliers = await _context.Suppliers.CountAsync(cancellationToken);

        // 2. Customer Receivables Calculation (Single obligation rule: Invoices take precedence, then confirmed uninvoiced orders)
        // Invoices with outstanding payment
        var unpaidInvoices = await _context.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled &&
                        i.CustomerId.HasValue &&
                        i.PaymentMethod != null &&
                        (i.PaymentMethod.Contains("Công nợ") || 
                         i.PaymentMethod.Contains("Ghi nợ") || 
                         i.PaymentMethod.Contains("Chưa thanh toán") || 
                         i.Status == InvoiceStatus.Pending))
            .Select(i => new { CustomerId = i.CustomerId!.Value, Amount = i.GrandTotal })
            .ToListAsync(cancellationToken);

        // Standalone confirmed orders without invoice
        var unpaidOrders = await _context.SalesOrders.AsNoTracking()
            .Where(o => o.Status == SalesOrderStatus.Confirmed && 
                        !o.Invoices.Any(i => i.Status != InvoiceStatus.Cancelled))
            .Select(o => new { CustomerId = o.CustomerId, Amount = o.GrandTotal })
            .ToListAsync(cancellationToken);

        decimal totalCustomerReceivables = unpaidInvoices.Sum(x => x.Amount) + unpaidOrders.Sum(x => x.Amount);

        var customersWithDebtIds = unpaidInvoices.Select(x => x.CustomerId)
            .Concat(unpaidOrders.Select(x => x.CustomerId))
            .Distinct()
            .ToHashSet();

        // 3. Supplier Payables (Phase 6 Purchasing is not yet activated)
        decimal totalSupplierPayables = 0m;
        int suppliersWithDebtCount = 0;

        return new PartnerDebtSummaryDto
        {
            TotalCustomerReceivables = totalCustomerReceivables,
            TotalSupplierPayables = totalSupplierPayables,
            CustomersWithDebtCount = customersWithDebtIds.Count,
            SuppliersWithDebtCount = suppliersWithDebtCount,
            TotalCustomersCount = totalCustomers,
            TotalSuppliersCount = totalSuppliers,
            FinancialDataSourceNote = "Dữ liệu công nợ phải thu được tổng hợp tự động từ Hóa đơn bán lẻ, Hóa đơn VAT và Đơn bán hàng xác nhận. Phân hệ Mua hàng (Phase 6) đang chuẩn bị kích hoạt nên công nợ phải trả nhà cung cấp hiện ghi nhận 0 ₫."
        };
    }

    public async Task<PagedResult<DebtRecordDto>> GetDebtRecordsAsync(DebtFilterDto filter, CancellationToken cancellationToken = default)
    {
        var records = new List<DebtRecordDto>();

        // 1. Build Customer Debt Records if not filtering out customers
        if (!filter.PartnerType.HasValue || filter.PartnerType.Value == PartnerType.Customer)
        {
            // A. Invoices
            var invoiceQuery = _context.Invoices.AsNoTracking()
                .Include(i => i.Customer)
                .Where(i => i.Status != InvoiceStatus.Cancelled && i.CustomerId.HasValue);

            var invoices = await invoiceQuery
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync(cancellationToken);

            foreach (var inv in invoices)
            {
                bool isUnpaid = inv.PaymentMethod != null &&
                                (inv.PaymentMethod.Contains("Công nợ") || 
                                 inv.PaymentMethod.Contains("Ghi nợ") || 
                                 inv.PaymentMethod.Contains("Chưa thanh toán") || 
                                 inv.Status == InvoiceStatus.Pending);

                decimal paid = isUnpaid ? 0m : inv.GrandTotal;
                decimal outstanding = isUnpaid ? inv.GrandTotal : 0m;

                var debtStatus = isUnpaid ? DebtStatus.Outstanding : DebtStatus.Settled;

                string docType = inv.Type == InvoiceType.VatInvoice ? "Hóa đơn VAT" : "Hóa đơn bán hàng";
                string docCode = !string.IsNullOrEmpty(inv.InvoiceNo) ? inv.InvoiceNo : inv.InvoiceNumber;

                records.Add(new DebtRecordDto
                {
                    Id = inv.Id,
                    PartnerType = PartnerType.Customer,
                    Direction = DebtDirection.Receivable,
                    PartnerId = inv.CustomerId!.Value,
                    PartnerCode = inv.Customer?.Code ?? $"KH{inv.CustomerId:D6}",
                    PartnerName = !string.IsNullOrEmpty(inv.Customer?.CompanyName) ? inv.Customer.CompanyName : (inv.Customer?.Name ?? inv.CustomerName),
                    Phone = inv.Customer?.Phone,
                    DocumentType = docType,
                    DocumentCode = docCode,
                    DocumentDate = inv.InvoiceDate,
                    OriginalAmount = inv.GrandTotal,
                    PaidAmount = paid,
                    OutstandingAmount = outstanding,
                    DebtStatus = debtStatus,
                    PaymentMethod = inv.PaymentMethod,
                    LinkUrl = $"/hoa-don/chi-tiet/{inv.Id}"
                });
            }

            // B. Uninvoiced confirmed SalesOrders
            var invoicedOrderIds = invoices.Where(i => i.SalesOrderId.HasValue).Select(i => i.SalesOrderId!.Value).ToHashSet();

            var orderQuery = _context.SalesOrders.AsNoTracking()
                .Include(o => o.Customer)
                .Where(o => o.Status == SalesOrderStatus.Confirmed && !invoicedOrderIds.Contains(o.Id));

            var orders = await orderQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            foreach (var ord in orders)
            {
                records.Add(new DebtRecordDto
                {
                    Id = ord.Id,
                    PartnerType = PartnerType.Customer,
                    Direction = DebtDirection.Receivable,
                    PartnerId = ord.CustomerId,
                    PartnerCode = ord.Customer?.Code ?? $"KH{ord.CustomerId:D6}",
                    PartnerName = !string.IsNullOrEmpty(ord.Customer?.CompanyName) ? ord.Customer.CompanyName : (ord.Customer?.Name ?? ord.CustomerName),
                    Phone = ord.Customer?.Phone ?? ord.CustomerPhone,
                    DocumentType = "Đơn bán hàng",
                    DocumentCode = ord.Code,
                    DocumentDate = ord.OrderDate,
                    OriginalAmount = ord.GrandTotal,
                    PaidAmount = 0m,
                    OutstandingAmount = ord.GrandTotal,
                    DebtStatus = DebtStatus.Outstanding,
                    PaymentMethod = "Chờ lập hóa đơn",
                    LinkUrl = $"/ban-hang/don-ban-hang/chi-tiet/{ord.Id}"
                });
            }
        }

        // 2. Apply Filters
        var query = records.AsQueryable();

        // Partner Type
        if (filter.PartnerType.HasValue)
        {
            query = query.Where(r => r.PartnerType == filter.PartnerType.Value);
        }

        // Debt Status
        if (filter.DebtStatus.HasValue)
        {
            query = query.Where(r => r.DebtStatus == filter.DebtStatus.Value);
        }

        // Search Term
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(r => (r.PartnerCode != null && r.PartnerCode.ToLower().Contains(term))
                                  || (r.PartnerName != null && r.PartnerName.ToLower().Contains(term))
                                  || (r.Phone != null && r.Phone.Contains(term))
                                  || (r.DocumentCode != null && r.DocumentCode.ToLower().Contains(term)));
        }

        // Balance Range
        if (filter.MinBalance.HasValue)
        {
            query = query.Where(r => r.OutstandingAmount >= filter.MinBalance.Value);
        }
        if (filter.MaxBalance.HasValue)
        {
            query = query.Where(r => r.OutstandingAmount <= filter.MaxBalance.Value);
        }

        // Date Range
        if (filter.FromDate.HasValue)
        {
            query = query.Where(r => r.DocumentDate >= filter.FromDate.Value.Date);
        }
        if (filter.ToDate.HasValue)
        {
            query = query.Where(r => r.DocumentDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
        }

        // 3. Sorting & Pagination
        var filteredList = query.OrderByDescending(r => r.DocumentDate).ToList();
        int totalCount = filteredList.Count;

        int pageIndex = Math.Max(1, filter.PageIndex);
        int pageSize = Math.Max(1, filter.PageSize);
        var pagedItems = filteredList
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<DebtRecordDto>
        {
            Items = pagedItems,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }
}
