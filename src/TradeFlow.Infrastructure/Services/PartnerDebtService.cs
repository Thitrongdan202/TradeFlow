using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TradeFlow.Application.Common.Interfaces;
using TradeFlow.Application.Common.Models.Partners;
using TradeFlow.Domain.Entities.Partners;
using TradeFlow.Domain.Entities.Sales;
using TradeFlow.Domain.Entities.Settings;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class PartnerDebtService : IPartnerDebtService
{
    private readonly TradeFlowDbContext _context;
    private readonly IPartnerDebtExportService _exportService;
    private readonly IAuthorizationService _authService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<PartnerDebtService> _logger;

    public PartnerDebtService(
        TradeFlowDbContext context,
        IPartnerDebtExportService exportService,
        IAuthorizationService authService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<PartnerDebtService> logger)
    {
        _context = context;
        _exportService = exportService;
        _authService = authService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<PartnerDebtSummaryDto> GetDebtSummaryAsync(CancellationToken cancellationToken = default)
    {
        // 1. Total Customers & Suppliers
        int totalCustomers = await _context.Customers.CountAsync(cancellationToken);
        int totalSuppliers = await _context.Suppliers.CountAsync(cancellationToken);

        // 2. Customer Receivables Calculation
        // Authoritative obligations: Uncancelled Invoices + Confirmed Uninvoiced Orders
        var invoices = await _context.Invoices.AsNoTracking()
            .Where(i => i.Status != InvoiceStatus.Cancelled && i.CustomerId.HasValue)
            .Select(i => new { CustomerId = i.CustomerId!.Value, Amount = i.GrandTotal, i.Id })
            .ToListAsync(cancellationToken);

        var uninvoicedOrders = await _context.SalesOrders.AsNoTracking()
            .Where(o => o.Status == SalesOrderStatus.Confirmed && !o.Invoices.Any(i => i.Status != InvoiceStatus.Cancelled))
            .Select(o => new { CustomerId = o.CustomerId, Amount = o.GrandTotal, o.Id })
            .ToListAsync(cancellationToken);

        // Payments and discounts recorded in PartnerPayments
        var customerPayments = await _context.PartnerPayments.AsNoTracking()
            .Where(p => p.PartnerType == (int)PartnerType.Customer)
            .GroupBy(p => p.PartnerId)
            .Select(g => new { CustomerId = g.Key, TotalPaid = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.CustomerId, x => x.TotalPaid, cancellationToken);

        // Group gross sales by customer
        var customerGrossDebits = invoices.GroupBy(i => i.CustomerId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        foreach (var ord in uninvoicedOrders)
        {
            if (customerGrossDebits.ContainsKey(ord.CustomerId))
                customerGrossDebits[ord.CustomerId] += ord.Amount;
            else
                customerGrossDebits[ord.CustomerId] = ord.Amount;
        }

        decimal totalCustomerReceivables = 0m;
        var customersWithDebtIds = new HashSet<int>();

        foreach (var (custId, gross) in customerGrossDebits)
        {
            customerPayments.TryGetValue(custId, out decimal paid);
            decimal netDue = Math.Max(0m, gross - paid);
            if (netDue > 0)
            {
                totalCustomerReceivables += netDue;
                customersWithDebtIds.Add(custId);
            }
        }

        // 3. Supplier Payables: Adjustments (increases) minus Payments/Discounts (reductions)
        var supplierPayments = await _context.PartnerPayments.AsNoTracking()
            .Where(p => p.PartnerType == (int)PartnerType.Supplier)
            .ToListAsync(cancellationToken);

        decimal totalSupplierPayables = 0m;
        var supGrouped = supplierPayments.GroupBy(p => p.PartnerId);
        int suppliersWithDebtCount = 0;
        foreach (var grp in supGrouped)
        {
            decimal debits = grp.Where(p => p.TransactionType == DebtTransactionType.Adjustment).Sum(p => p.Amount);
            decimal credits = grp.Where(p => p.TransactionType == DebtTransactionType.Payment || p.TransactionType == DebtTransactionType.PaymentDiscount).Sum(p => p.Amount);
            decimal net = debits - credits;
            if (net > 0)
            {
                totalSupplierPayables += net;
                suppliersWithDebtCount++;
            }
        }

        return new PartnerDebtSummaryDto
        {
            TotalCustomerReceivables = totalCustomerReceivables,
            TotalSupplierPayables = totalSupplierPayables,
            CustomersWithDebtCount = customersWithDebtIds.Count,
            SuppliersWithDebtCount = suppliersWithDebtCount,
            TotalCustomersCount = totalCustomers,
            TotalSuppliersCount = totalSuppliers,
            FinancialDataSourceNote = "Dữ liệu công nợ phải thu được tổng hợp tự động từ Hóa đơn bán lẻ, Hóa đơn VAT, Đơn bán hàng và các Phiếu thu tiền / Điều chỉnh. Công nợ phải trả nhà cung cấp được tổng hợp từ chứng từ phát sinh và phiếu chi thanh toán."
        };
    }

    public async Task<PagedResult<DebtRecordDto>> GetDebtRecordsAsync(DebtFilterDto filter, CancellationToken cancellationToken = default)
    {
        var records = new List<DebtRecordDto>();

        // 1. Build Customer Debt Records if not filtering out customers
        if (!filter.PartnerType.HasValue || filter.PartnerType.Value == PartnerType.Customer)
        {
            // Load customer payments to calculate paid amount per invoice or customer
            var payments = await _context.PartnerPayments.AsNoTracking()
                .Where(p => p.PartnerType == (int)PartnerType.Customer)
                .ToListAsync(cancellationToken);

            var paymentsByInvoice = payments.Where(p => p.InvoiceId.HasValue)
                .GroupBy(p => p.InvoiceId!.Value)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            var generalPaymentsByCust = payments.Where(p => !p.InvoiceId.HasValue)
                .GroupBy(p => p.PartnerId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

            // A. Invoices
            var invoiceQuery = _context.Invoices.AsNoTracking()
                .Include(i => i.Customer)
                .Where(i => i.Status != InvoiceStatus.Cancelled && i.CustomerId.HasValue);

            var invoices = await invoiceQuery
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync(cancellationToken);

            foreach (var inv in invoices)
            {
                bool isMarkedCredit = inv.PaymentMethod != null &&
                                     (inv.PaymentMethod.Contains("Công nợ") ||
                                      inv.PaymentMethod.Contains("Ghi nợ") ||
                                      inv.PaymentMethod.Contains("Chưa thanh toán") ||
                                      inv.Status == InvoiceStatus.Pending);

                decimal specificPaid = 0m;
                if (paymentsByInvoice.TryGetValue(inv.Id, out decimal pAmount))
                {
                    specificPaid = pAmount;
                }

                decimal paid = !isMarkedCredit ? inv.GrandTotal : specificPaid;
                decimal outstanding = Math.Max(0m, inv.GrandTotal - paid);

                var debtStatus = outstanding == 0m ? DebtStatus.Settled
                    : (paid > 0m ? DebtStatus.PartiallyPaid : DebtStatus.Outstanding);

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

        // 2. Build Supplier Debt Records if not filtering out suppliers
        if (!filter.PartnerType.HasValue || filter.PartnerType.Value == PartnerType.Supplier)
        {
            var supPayments = await _context.PartnerPayments.AsNoTracking()
                .Include(p => p.Supplier)
                .Where(p => p.PartnerType == (int)PartnerType.Supplier)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            foreach (var p in supPayments)
            {
                records.Add(new DebtRecordDto
                {
                    Id = p.Id,
                    PartnerType = PartnerType.Supplier,
                    Direction = DebtDirection.Payable,
                    PartnerId = p.PartnerId,
                    PartnerCode = p.Supplier?.Code ?? $"NCC{p.PartnerId:D6}",
                    PartnerName = !string.IsNullOrEmpty(p.Supplier?.CompanyName) ? p.Supplier.CompanyName : (p.Supplier?.Name ?? "Nhà cung cấp"),
                    Phone = p.Supplier?.Phone,
                    DocumentType = p.TransactionType == DebtTransactionType.Payment ? "Phiếu chi thanh toán" : "Điều chỉnh nợ NCC",
                    DocumentCode = p.PaymentCode,
                    DocumentDate = p.PaymentDate,
                    OriginalAmount = p.Amount,
                    PaidAmount = p.Amount,
                    OutstandingAmount = 0m,
                    DebtStatus = DebtStatus.Settled,
                    PaymentMethod = p.PaymentMethod,
                    LinkUrl = null
                });
            }
        }

        // 3. Apply Filters
        var query = records.AsQueryable();

        if (filter.PartnerType.HasValue)
        {
            query = query.Where(r => r.PartnerType == filter.PartnerType.Value);
        }

        if (filter.DebtStatus.HasValue)
        {
            query = query.Where(r => r.DebtStatus == filter.DebtStatus.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim().ToLower();
            query = query.Where(r => (r.PartnerCode != null && r.PartnerCode.ToLower().Contains(term))
                                  || (r.PartnerName != null && r.PartnerName.ToLower().Contains(term))
                                  || (r.Phone != null && r.Phone.Contains(term))
                                  || (r.DocumentCode != null && r.DocumentCode.ToLower().Contains(term)));
        }

        if (filter.MinBalance.HasValue)
        {
            query = query.Where(r => r.OutstandingAmount >= filter.MinBalance.Value);
        }
        if (filter.MaxBalance.HasValue)
        {
            query = query.Where(r => r.OutstandingAmount <= filter.MaxBalance.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(r => r.DocumentDate >= filter.FromDate.Value.Date);
        }
        if (filter.ToDate.HasValue)
        {
            query = query.Where(r => r.DocumentDate <= filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
        }

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

    public async Task<List<PartnerLinkedTransactionDto>> GetPartnerDebtHistoryAsync(
        PartnerType partnerType,
        int partnerId,
        CancellationToken cancellationToken = default)
    {
        var history = new List<PartnerLinkedTransactionDto>();

        if (partnerType == PartnerType.Customer)
        {
            // Invoices
            var invoices = await _context.Invoices.AsNoTracking()
                .Include(i => i.Items)
                .Where(i => i.CustomerId == partnerId && i.Status != InvoiceStatus.Cancelled)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync(cancellationToken);

            var payments = await _context.PartnerPayments.AsNoTracking()
                .Where(p => p.PartnerType == (int)PartnerType.Customer && p.PartnerId == partnerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            var paymentsByInv = payments.Where(p => p.InvoiceId.HasValue).ToLookup(p => p.InvoiceId!.Value);

            foreach (var inv in invoices)
            {
                var invPayments = paymentsByInv[inv.Id].ToList();
                decimal paidSoFar = invPayments.Sum(x => x.Amount);

                bool isCredit = inv.PaymentMethod != null &&
                                (inv.PaymentMethod.Contains("Công nợ") ||
                                 inv.PaymentMethod.Contains("Ghi nợ") ||
                                 inv.PaymentMethod.Contains("Chưa thanh toán") ||
                                 inv.Status == InvoiceStatus.Pending);

                decimal effectivePaid = !isCredit ? inv.GrandTotal : paidSoFar;
                decimal remaining = Math.Max(0m, inv.GrandTotal - effectivePaid);

                history.Add(new PartnerLinkedTransactionDto
                {
                    DocumentId = inv.Id,
                    DocumentType = inv.Type == InvoiceType.VatInvoice ? "Hóa đơn VAT" : "Hóa đơn bán hàng",
                    DocumentCode = !string.IsNullOrEmpty(inv.InvoiceNo) ? inv.InvoiceNo : inv.InvoiceNumber,
                    DocumentDate = inv.InvoiceDate,
                    Amount = inv.GrandTotal,
                    DebitAmount = inv.GrandTotal,
                    CreditAmount = 0m,
                    PaidAmount = effectivePaid,
                    OutstandingAmount = remaining,
                    Status = remaining == 0m ? "Đã thanh toán đủ" : (effectivePaid > 0 ? "Nợ một phần" : "Chờ thanh toán"),
                    PaymentMethod = inv.PaymentMethod,
                    LinkUrl = $"/hoa-don/chi-tiet/{inv.Id}",
                    Notes = inv.Notes
                });
            }

            // Uninvoiced orders
            var invoicedOrderIds = invoices.Where(i => i.SalesOrderId.HasValue).Select(i => i.SalesOrderId!.Value).ToHashSet();
            var orders = await _context.SalesOrders.AsNoTracking()
                .Where(o => o.CustomerId == partnerId && o.Status == SalesOrderStatus.Confirmed && !invoicedOrderIds.Contains(o.Id))
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync(cancellationToken);

            foreach (var ord in orders)
            {
                history.Add(new PartnerLinkedTransactionDto
                {
                    DocumentId = ord.Id,
                    DocumentType = "Đơn bán hàng",
                    DocumentCode = ord.Code,
                    DocumentDate = ord.OrderDate,
                    Amount = ord.GrandTotal,
                    DebitAmount = ord.GrandTotal,
                    CreditAmount = 0m,
                    PaidAmount = 0m,
                    OutstandingAmount = ord.GrandTotal,
                    Status = "Đã xác nhận",
                    PaymentMethod = "Chờ lập hóa đơn",
                    LinkUrl = $"/ban-hang/don-ban-hang/chi-tiet/{ord.Id}",
                    Notes = ord.Notes
                });
            }

            // Standalone Payments & Adjustments
            foreach (var pay in payments)
            {
                string docType = pay.TransactionType switch
                {
                    DebtTransactionType.Payment => "Phiếu thu tiền",
                    DebtTransactionType.Adjustment => "Điều chỉnh công nợ",
                    DebtTransactionType.PaymentDiscount => "Chiết khấu thanh toán",
                    _ => "Phiếu thu"
                };

                history.Add(new PartnerLinkedTransactionDto
                {
                    DocumentId = pay.Id,
                    DocumentType = docType,
                    DocumentCode = pay.PaymentCode,
                    DocumentDate = pay.PaymentDate,
                    Amount = pay.Amount,
                    DebitAmount = 0m,
                    CreditAmount = pay.Amount,
                    PaidAmount = pay.Amount,
                    OutstandingAmount = 0m,
                    Status = "Đã thu tiền",
                    PaymentMethod = pay.PaymentMethod,
                    ReferenceCode = pay.ReferenceCode,
                    Notes = pay.Notes
                });
            }
        }
        else
        {
            // Supplier history
            var supPayments = await _context.PartnerPayments.AsNoTracking()
                .Where(p => p.PartnerType == (int)PartnerType.Supplier && p.PartnerId == partnerId)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            foreach (var pay in supPayments)
            {
                string docType = pay.TransactionType switch
                {
                    DebtTransactionType.Payment => "Thanh toán (Sổ quỹ)",
                    DebtTransactionType.Adjustment => "Điều chỉnh nợ NCC",
                    _ => "Phiếu chi"
                };

                history.Add(new PartnerLinkedTransactionDto
                {
                    DocumentId = pay.Id,
                    DocumentType = docType,
                    DocumentCode = pay.PaymentCode,
                    DocumentDate = pay.PaymentDate,
                    Amount = pay.Amount,
                    DebitAmount = 0m,
                    CreditAmount = pay.Amount,
                    PaidAmount = pay.Amount,
                    OutstandingAmount = 0m,
                    Status = "Đã chi tiền",
                    PaymentMethod = pay.PaymentMethod,
                    ReferenceCode = pay.ReferenceCode,
                    Notes = pay.Notes
                });
            }
        }

        // Sort chronologically ascending to calculate running balance, then descending for UI
        var chronological = history.OrderBy(t => t.DocumentDate).ToList();
        decimal running = 0m;
        foreach (var t in chronological)
        {
            running += t.DebitAmount - t.CreditAmount;
            t.RunningBalance = running;
        }

        return chronological.OrderByDescending(t => t.DocumentDate).ToList();
    }

    public async Task<RecordDebtPaymentResult> RecordPaymentAsync(
        RecordDebtPaymentDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        // 1. Authorization check
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Customers:Edit");
        if (!authResult.Succeeded)
        {
            return new RecordDebtPaymentResult
            {
                Succeeded = false,
                ErrorMessage = "Bạn không có quyền thực hiện giao dịch thanh toán công nợ."
            };
        }

        if (dto.Amount <= 0)
        {
            return new RecordDebtPaymentResult
            {
                Succeeded = false,
                ErrorMessage = "Số tiền thanh toán phải lớn hơn 0 ₫."
            };
        }

        // 2. Validate Partner Existence
        string partnerName = "";
        string partnerCode = "";
        if (dto.PartnerType == PartnerType.Customer)
        {
            var cust = await _context.Customers.FindAsync(new object[] { dto.PartnerId }, cancellationToken);
            if (cust == null)
            {
                return new RecordDebtPaymentResult { Succeeded = false, ErrorMessage = "Không tìm thấy khách hàng tương ứng." };
            }
            partnerName = cust.Name;
            partnerCode = cust.Code;
        }
        else
        {
            var sup = await _context.Suppliers.FindAsync(new object[] { dto.PartnerId }, cancellationToken);
            if (sup == null)
            {
                return new RecordDebtPaymentResult { Succeeded = false, ErrorMessage = "Không tìm thấy nhà cung cấp tương ứng." };
            }
            partnerName = sup.Name;
            partnerCode = sup.Code;
        }

        // 3. Generate Sequence Code
        string prefix = dto.TransactionType switch
        {
            DebtTransactionType.Payment => (dto.PartnerType == PartnerType.Customer ? "PT" : "PC"),
            DebtTransactionType.Adjustment => "DC",
            DebtTransactionType.PaymentDiscount => "CK",
            _ => "PT"
        };

        var seq = await _context.SystemSequences.FirstOrDefaultAsync(x => x.SequenceKey == prefix, cancellationToken);
        if (seq == null)
        {
            seq = new SystemSequence { SequenceKey = prefix, Prefix = prefix, CurrentNumber = 1 };
            _context.SystemSequences.Add(seq);
        }
        else
        {
            seq.CurrentNumber++;
        }

        string paymentCode = $"{prefix}{seq.CurrentNumber:D6}";

        // 4. Create and Save PartnerPayment Entity
        var paymentEntity = new PartnerPayment
        {
            PaymentCode = paymentCode,
            PartnerType = (int)dto.PartnerType,
            PartnerId = dto.PartnerId,
            CustomerId = dto.PartnerType == PartnerType.Customer ? dto.PartnerId : null,
            SupplierId = dto.PartnerType == PartnerType.Supplier ? dto.PartnerId : null,
            TransactionType = dto.TransactionType,
            PaymentDate = DateTime.SpecifyKind(dto.PaymentDate, DateTimeKind.Utc),
            Amount = dto.Amount,
            PaymentMethod = !string.IsNullOrWhiteSpace(dto.PaymentMethod) ? dto.PaymentMethod : "Tiền mặt",
            ReferenceCode = dto.ReferenceCode,
            InvoiceId = dto.InvoiceId,
            SalesOrderId = dto.SalesOrderId,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Identity?.Name ?? _currentUserService.UserName ?? "System"
        };

        _context.PartnerPayments.Add(paymentEntity);

        // If target invoice was selected and paid in full, update its PaymentMethod
        if (dto.InvoiceId.HasValue && dto.TransactionType == DebtTransactionType.Payment)
        {
            var inv = await _context.Invoices.FindAsync(new object[] { dto.InvoiceId.Value }, cancellationToken);
            if (inv != null)
            {
                var priorPaid = await _context.PartnerPayments
                    .Where(p => p.InvoiceId == inv.Id)
                    .SumAsync(p => p.Amount, cancellationToken);

                if (priorPaid + dto.Amount >= inv.GrandTotal)
                {
                    inv.PaymentMethod = dto.PaymentMethod;
                    if (inv.Status == InvoiceStatus.Pending)
                    {
                        inv.Status = InvoiceStatus.Issued;
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // 5. Audit Trail
        var auditEvent = dto.TransactionType switch
        {
            DebtTransactionType.Adjustment => AuditEventType.DebtAdjustmentRecorded,
            DebtTransactionType.PaymentDiscount => AuditEventType.DebtDiscountRecorded,
            _ => AuditEventType.DebtPaymentRecorded
        };

        string desc = $"{dto.TransactionType}: {dto.Amount:N0} ₫ cho {partnerName} ({partnerCode}). Phương thức: {dto.PaymentMethod}. Mã phiếu: {paymentCode}";
        await _auditService.LogAsync(
            auditEvent,
            paymentEntity.CreatedBy,
            "PartnerPayment",
            paymentCode,
            desc,
            cancellationToken: cancellationToken);

        // 6. Recalculate Balance
        var history = await GetPartnerDebtHistoryAsync(dto.PartnerType, dto.PartnerId, cancellationToken);
        decimal newBalance = history.FirstOrDefault()?.RunningBalance ?? 0m;

        return new RecordDebtPaymentResult
        {
            Succeeded = true,
            PaymentId = paymentEntity.Id,
            PaymentCode = paymentCode,
            UpdatedBalance = newBalance
        };
    }

    public async Task<byte[]> ExportPartnerDebtStatementAsync(
        PartnerDebtStatementExportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        return await _exportService.ExportPartnerDebtStatementAsync(request, user, cancellationToken);
    }

    public async Task<byte[]> ExportDebtManagementWorkbookAsync(
        DebtManagementExportRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        return await _exportService.ExportDebtManagementWorkbookAsync(request, user, cancellationToken);
    }
}
