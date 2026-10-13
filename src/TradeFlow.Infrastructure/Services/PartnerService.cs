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
using TradeFlow.Domain.Entities.MasterData;
using TradeFlow.Domain.Enums;
using TradeFlow.Infrastructure.Persistence;

namespace TradeFlow.Infrastructure.Services;

public class PartnerService : IPartnerService
{
    private readonly TradeFlowDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IAuthorizationService _authService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PartnerService> _logger;

    public PartnerService(
        TradeFlowDbContext context,
        IAuditService auditService,
        IAuthorizationService authService,
        ICurrentUserService currentUser,
        ILogger<PartnerService> logger)
    {
        _context = context;
        _auditService = auditService;
        _authService = authService;
        _currentUser = currentUser;
        _logger = logger;
    }

    // =========================================================================
    // CUSTOMER OPERATIONS
    // =========================================================================

    public async Task<PagedResult<CustomerListItemDto>> GetCustomersAsync(CustomerFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Customers.AsNoTracking().AsQueryable();

        // 1. Text Search
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim();
            string pattern = $"%{term}%";
            query = query.Where(c => EF.Functions.ILike(c.Code, pattern)
                                  || EF.Functions.ILike(c.Name, pattern)
                                  || (c.CompanyName != null && EF.Functions.ILike(c.CompanyName, pattern))
                                  || (c.Phone != null && EF.Functions.ILike(c.Phone, pattern))
                                  || (c.TaxCode != null && EF.Functions.ILike(c.TaxCode, pattern))
                                  || (c.Email != null && EF.Functions.ILike(c.Email, pattern))
                                  || (c.BusinessCode != null && EF.Functions.ILike(c.BusinessCode, pattern)));
        }

        // 2. Customer Type Filter (Individual vs Company)
        if (filter.CustomerType.HasValue)
        {
            if (filter.CustomerType.Value == CustomerClassification.Individual)
            {
                query = query.Where(c => (c.CompanyName == null || c.CompanyName == "") && (c.TaxCode == null || c.TaxCode == ""));
            }
            else if (filter.CustomerType.Value == CustomerClassification.Company)
            {
                query = query.Where(c => (c.CompanyName != null && c.CompanyName != "") || (c.TaxCode != null && c.TaxCode != ""));
            }
        }

        // 3. Status Filter
        if (filter.IsActive.HasValue)
        {
            query = query.Where(c => c.IsActive == filter.IsActive.Value);
        }

        // 4. Date Range Filter
        if (filter.CreatedFrom.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.CreatedFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt >= fromUtc);
        }
        if (filter.CreatedTo.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.CreatedTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(c => c.CreatedAt <= toUtc);
        }

        // 5. Creator Filter
        if (!string.IsNullOrWhiteSpace(filter.CreatedBy))
        {
            query = query.Where(c => c.CreatedBy == filter.CreatedBy);
        }

        // Total count before pagination
        int totalCount = await query.CountAsync(cancellationToken);

        // Fetch paged customers
        int pageIndex = Math.Max(1, filter.PageIndex);
        int pageSize = Math.Max(1, filter.PageSize);
        var customers = await query
            .OrderByDescending(c => c.CreatedAt)
            .ThenByDescending(c => c.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (!customers.Any())
        {
            return new PagedResult<CustomerListItemDto>
            {
                Items = new List<CustomerListItemDto>(),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        var customerIds = customers.Select(c => c.Id).ToList();

        // Financial metrics aggregation without double counting
        // Invoices are the authoritative billing documents.
        var invoiceStats = await _context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId.HasValue && customerIds.Contains(i.CustomerId.Value) && i.Status != InvoiceStatus.Cancelled)
            .GroupBy(i => i.CustomerId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalSales = g.Sum(x => x.GrandTotal),
                Receivables = g.Where(x => x.PaymentMethod != null && 
                                           (x.PaymentMethod.Contains("Công nợ") || 
                                            x.PaymentMethod.Contains("Ghi nợ") || 
                                            x.PaymentMethod.Contains("Chưa thanh toán") || 
                                            x.Status == InvoiceStatus.Pending))
                               .Sum(x => x.GrandTotal),
                LastInvoiceDate = g.Max(x => (DateTime?)x.InvoiceDate)
            })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        // Standalone confirmed sales orders not yet invoiced
        var uninvoicedOrders = await _context.SalesOrders.AsNoTracking()
            .Where(o => customerIds.Contains(o.CustomerId) && o.Status == SalesOrderStatus.Confirmed && !o.Invoices.Any(i => i.Status != InvoiceStatus.Cancelled))
            .GroupBy(o => o.CustomerId)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalOrderSales = g.Sum(x => x.GrandTotal),
                LastOrderDate = g.Max(x => (DateTime?)x.OrderDate)
            })
            .ToDictionaryAsync(x => x.CustomerId, cancellationToken);

        // Cross-role matching suppliers (by tax code or phone)
        var taxCodes = customers.Where(c => !string.IsNullOrEmpty(c.TaxCode)).Select(c => c.TaxCode!).Distinct().ToList();
        var phones = customers.Where(c => !string.IsNullOrEmpty(c.Phone)).Select(c => c.Phone!).Distinct().ToList();

        var matchingSuppliers = await _context.Suppliers.AsNoTracking()
            .Where(s => (s.TaxCode != null && taxCodes.Contains(s.TaxCode)) || (s.Phone != null && phones.Contains(s.Phone)))
            .Select(s => new { s.Id, s.Code, s.TaxCode, s.Phone })
            .ToListAsync(cancellationToken);

        var items = new List<CustomerListItemDto>();
        foreach (var c in customers)
        {
            invoiceStats.TryGetValue(c.Id, out var invStat);
            uninvoicedOrders.TryGetValue(c.Id, out var ordStat);

            decimal totalSales = (invStat?.TotalSales ?? 0m) + (ordStat?.TotalOrderSales ?? 0m);
            decimal receivable = (invStat?.Receivables ?? 0m) + (ordStat?.TotalOrderSales ?? 0m);

            DateTime? lastDate = invStat?.LastInvoiceDate;
            if (ordStat?.LastOrderDate != null && (lastDate == null || ordStat.LastOrderDate > lastDate))
            {
                lastDate = ordStat.LastOrderDate;
            }

            // Check if supplier match exists
            var matchSup = matchingSuppliers.FirstOrDefault(s => 
                (!string.IsNullOrEmpty(c.TaxCode) && s.TaxCode == c.TaxCode) ||
                (!string.IsNullOrEmpty(c.Phone) && s.Phone == c.Phone));

            bool isCompany = !string.IsNullOrEmpty(c.CompanyName) || !string.IsNullOrEmpty(c.TaxCode);

            items.Add(new CustomerListItemDto
            {
                Id = c.Id,
                Code = c.Code,
                BusinessCode = c.BusinessCode,
                Name = c.Name,
                CompanyName = c.CompanyName,
                TaxCode = c.TaxCode,
                ContactPerson = c.ContactPerson,
                Phone = c.Phone,
                Email = c.Email,
                Address = c.Address,
                IsActive = c.IsActive,
                IsCompany = isCompany,
                CurrentReceivable = receivable,
                TotalSales = totalSales,
                LastTransactionDate = lastDate,
                CreatedAt = c.CreatedAt,
                CreatedBy = c.CreatedBy,
                MatchingSupplierId = matchSup?.Id,
                MatchingSupplierCode = matchSup?.Code
            });
        }

        // Apply in-memory receivable filter if specified
        if (filter.MinReceivable.HasValue)
        {
            items = items.Where(x => x.CurrentReceivable >= filter.MinReceivable.Value).ToList();
        }
        if (filter.MaxReceivable.HasValue)
        {
            items = items.Where(x => x.CurrentReceivable <= filter.MaxReceivable.Value).ToList();
        }

        return new PagedResult<CustomerListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDetailDto?> GetCustomerDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var c = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (c == null) return null;

        bool isCompany = !string.IsNullOrEmpty(c.CompanyName) || !string.IsNullOrEmpty(c.TaxCode);

        // Fetch linked orders
        var orders = await _context.SalesOrders.AsNoTracking()
            .Where(o => o.CustomerId == id && o.Status != SalesOrderStatus.Cancelled)
            .OrderByDescending(o => o.OrderDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Fetch linked invoices
        var invoices = await _context.Invoices.AsNoTracking()
            .Where(i => i.CustomerId == id && i.Status != InvoiceStatus.Cancelled)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(20)
            .ToListAsync(cancellationToken);

        // Map recent transactions
        var recentTransactions = new List<PartnerLinkedTransactionDto>();

        foreach (var inv in invoices)
        {
            bool isUnpaid = inv.PaymentMethod != null && 
                            (inv.PaymentMethod.Contains("Công nợ") || 
                             inv.PaymentMethod.Contains("Ghi nợ") || 
                             inv.PaymentMethod.Contains("Chưa thanh toán") || 
                             inv.Status == InvoiceStatus.Pending);

            decimal paid = isUnpaid ? 0m : inv.GrandTotal;
            decimal debt = isUnpaid ? inv.GrandTotal : 0m;

            recentTransactions.Add(new PartnerLinkedTransactionDto
            {
                DocumentType = inv.Type == InvoiceType.VatInvoice ? "Hóa đơn VAT" : "Hóa đơn bán hàng",
                DocumentCode = !string.IsNullOrEmpty(inv.InvoiceNo) ? inv.InvoiceNo : inv.InvoiceNumber,
                DocumentDate = inv.InvoiceDate,
                Amount = inv.GrandTotal,
                PaidAmount = paid,
                OutstandingAmount = debt,
                Status = inv.Status switch
                {
                    InvoiceStatus.Issued => "Đã phát hành",
                    InvoiceStatus.Pending => "Chờ thanh toán",
                    InvoiceStatus.Draft => "Bản nháp",
                    _ => inv.Status.ToString()
                },
                PaymentMethod = inv.PaymentMethod,
                LinkUrl = $"/hoa-don/chi-tiet/{inv.Id}"
            });
        }

        // Add uninvoiced confirmed orders
        var invoicedOrderIds = invoices.Where(i => i.SalesOrderId.HasValue).Select(i => i.SalesOrderId!.Value).ToHashSet();
        foreach (var ord in orders.Where(o => !invoicedOrderIds.Contains(o.Id)))
        {
            recentTransactions.Add(new PartnerLinkedTransactionDto
            {
                DocumentType = "Đơn bán hàng",
                DocumentCode = ord.Code,
                DocumentDate = ord.OrderDate,
                Amount = ord.GrandTotal,
                PaidAmount = 0m,
                OutstandingAmount = ord.GrandTotal,
                Status = ord.Status == SalesOrderStatus.Confirmed ? "Đã xác nhận" : "Bản nháp",
                PaymentMethod = "Chờ xuất hóa đơn",
                LinkUrl = $"/ban-hang/don-ban-hang/chi-tiet/{ord.Id}"
            });
        }

        recentTransactions = recentTransactions.OrderByDescending(t => t.DocumentDate).ToList();

        decimal totalSales = recentTransactions.Sum(x => x.Amount);
        decimal receivable = recentTransactions.Sum(x => x.OutstandingAmount);
        DateTime? lastDate = recentTransactions.FirstOrDefault()?.DocumentDate;

        // Check matching supplier overlap
        var matchSup = await _context.Suppliers.AsNoTracking()
            .Where(s => (!string.IsNullOrEmpty(c.TaxCode) && s.TaxCode == c.TaxCode) ||
                        (!string.IsNullOrEmpty(c.Phone) && s.Phone == c.Phone))
            .Select(s => new { s.Id, s.Code })
            .FirstOrDefaultAsync(cancellationToken);

        return new CustomerDetailDto
        {
            Id = c.Id,
            Code = c.Code,
            BusinessCode = c.BusinessCode,
            Name = c.Name,
            CompanyName = c.CompanyName,
            TaxCode = c.TaxCode,
            ContactPerson = c.ContactPerson,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            Notes = c.Notes,
            IsActive = c.IsActive,
            IsCompany = isCompany,
            CurrentReceivable = receivable,
            TotalSales = totalSales,
            OrderCount = orders.Count,
            InvoiceCount = invoices.Count,
            LastTransactionDate = lastDate,
            CreatedAt = c.CreatedAt,
            CreatedBy = c.CreatedBy,
            UpdatedAt = c.UpdatedAt,
            UpdatedBy = c.UpdatedBy,
            MatchingSupplierId = matchSup?.Id,
            MatchingSupplierCode = matchSup?.Code,
            RecentTransactions = recentTransactions
        };
    }

    public async Task<DeletePartnerResult> DeleteCustomerAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        // 1. Server-side Authorization Check
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Customers:Delete");
        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Unauthorized attempt to delete Customer ID {CustomerId} by user {User}", id, user.Identity?.Name);
            return new DeletePartnerResult
            {
                Succeeded = false,
                ErrorMessage = "Bạn không có quyền xóa khách hàng. Thao tác xóa chỉ dành cho Quản trị viên (Administrator) hoặc Quản lý có thẩm quyền."
            };
        }

        var customer = await _context.Customers.FindAsync(new object[] { id }, cancellationToken);
        if (customer == null)
        {
            return new DeletePartnerResult { Succeeded = false, ErrorMessage = "Không tìm thấy khách hàng cần xóa." };
        }

        // 2. Referential Integrity Protection: Inspect Historical Transactions
        int orderCount = await _context.SalesOrders.CountAsync(x => x.CustomerId == id, cancellationToken);
        int invoiceCount = await _context.Invoices.CountAsync(x => x.CustomerId == id, cancellationToken);
        int totalRefs = orderCount + invoiceCount;

        if (totalRefs > 0)
        {
            // Protect historical financial documents: DO NOT cascade delete! Soft-deactivate instead.
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;
            customer.UpdatedBy = user.Identity?.Name ?? _currentUser.UserName ?? "System";

            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.LogAsync(
                AuditEventType.CustomerUpdated,
                customer.UpdatedBy,
                "Customer",
                customer.Id.ToString(),
                $"Chuyển trạng thái sang 'Ngừng hoạt động' thay vì xóa vật lý do có {orderCount} đơn hàng và {invoiceCount} hóa đơn liên kết.",
                cancellationToken: cancellationToken);

            return new DeletePartnerResult
            {
                Succeeded = false,
                IsReferenced = true,
                SoftDeleted = true,
                ReferenceCount = totalRefs,
                ErrorMessage = $"Khách hàng '{customer.Name}' ({customer.Code}) đang gắn với {orderCount} đơn hàng và {invoiceCount} hóa đơn lịch sử. Để bảo toàn tính toàn vẹn chứng từ tài chính, hệ thống đã chuyển khách hàng sang trạng thái 'Ngừng hoạt động' thay vì xóa vật lý."
            };
        }

        // 3. Physical deletion when zero references exist
        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            AuditEventType.CustomerDeleted,
            user.Identity?.Name ?? _currentUser.UserName ?? "System",
            "Customer",
            customer.Code,
            $"Đã xóa vĩnh viễn khách hàng {customer.Name} ({customer.Code}).",
            cancellationToken: cancellationToken);

        return new DeletePartnerResult { Succeeded = true };
    }

    public async Task<BulkDeletePartnerResult> BulkDeleteCustomersAsync(List<int> ids, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Customers:Delete");
        if (!authResult.Succeeded)
        {
            return new BulkDeletePartnerResult
            {
                RequestedCount = ids.Count,
                Messages = new List<string> { "Bạn không có quyền thực hiện thao tác xóa khách hàng." }
            };
        }

        var result = new BulkDeletePartnerResult { RequestedCount = ids.Count };

        foreach (var id in ids)
        {
            var singleResult = await DeleteCustomerAsync(id, user, cancellationToken);
            if (singleResult.Succeeded)
            {
                result.DeletedCount++;
            }
            else if (singleResult.SoftDeleted)
            {
                result.DeactivatedCount++;
                if (!string.IsNullOrEmpty(singleResult.ErrorMessage))
                {
                    result.Messages.Add(singleResult.ErrorMessage);
                }
            }
            else if (!string.IsNullOrEmpty(singleResult.ErrorMessage))
            {
                result.Messages.Add(singleResult.ErrorMessage);
            }
        }

        return result;
    }

    // =========================================================================
    // SUPPLIER OPERATIONS
    // =========================================================================

    public async Task<PagedResult<SupplierListItemDto>> GetSuppliersAsync(SupplierFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Suppliers.AsNoTracking().AsQueryable();

        // 1. Text Search
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            string term = filter.SearchTerm.Trim();
            string pattern = $"%{term}%";
            query = query.Where(s => EF.Functions.ILike(s.Code, pattern)
                                  || EF.Functions.ILike(s.Name, pattern)
                                  || (s.CompanyName != null && EF.Functions.ILike(s.CompanyName, pattern))
                                  || (s.Phone != null && EF.Functions.ILike(s.Phone, pattern))
                                  || (s.TaxCode != null && EF.Functions.ILike(s.TaxCode, pattern))
                                  || (s.Email != null && EF.Functions.ILike(s.Email, pattern))
                                  || (s.BusinessCode != null && EF.Functions.ILike(s.BusinessCode, pattern)));
        }

        // 2. Status Filter
        if (filter.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == filter.IsActive.Value);
        }

        // 3. Date Range Filter
        if (filter.CreatedFrom.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.CreatedFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt >= fromUtc);
        }
        if (filter.CreatedTo.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.CreatedTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(s => s.CreatedAt <= toUtc);
        }

        // 4. Creator Filter
        if (!string.IsNullOrWhiteSpace(filter.CreatedBy))
        {
            query = query.Where(s => s.CreatedBy == filter.CreatedBy);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int pageIndex = Math.Max(1, filter.PageIndex);
        int pageSize = Math.Max(1, filter.PageSize);
        var suppliers = await query
            .OrderByDescending(s => s.CreatedAt)
            .ThenByDescending(s => s.Id)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (!suppliers.Any())
        {
            return new PagedResult<SupplierListItemDto>
            {
                Items = new List<SupplierListItemDto>(),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        // Check cross-role matching customers
        var taxCodes = suppliers.Where(s => !string.IsNullOrEmpty(s.TaxCode)).Select(s => s.TaxCode!).Distinct().ToList();
        var phones = suppliers.Where(s => !string.IsNullOrEmpty(s.Phone)).Select(s => s.Phone!).Distinct().ToList();

        var matchingCustomers = await _context.Customers.AsNoTracking()
            .Where(c => (c.TaxCode != null && taxCodes.Contains(c.TaxCode)) || (c.Phone != null && phones.Contains(c.Phone)))
            .Select(c => new { c.Id, c.Code, c.TaxCode, c.Phone })
            .ToListAsync(cancellationToken);

        var items = new List<SupplierListItemDto>();
        foreach (var s in suppliers)
        {
            var matchCust = matchingCustomers.FirstOrDefault(c =>
                (!string.IsNullOrEmpty(s.TaxCode) && c.TaxCode == s.TaxCode) ||
                (!string.IsNullOrEmpty(s.Phone) && c.Phone == s.Phone));

            // Purchasing module is Phase 6, currently no payables have occurred.
            decimal payable = 0m;
            decimal totalPurchases = 0m;

            items.Add(new SupplierListItemDto
            {
                Id = s.Id,
                Code = s.Code,
                BusinessCode = s.BusinessCode,
                Name = s.Name,
                CompanyName = s.CompanyName,
                TaxCode = s.TaxCode,
                ContactPerson = s.ContactPerson,
                Phone = s.Phone,
                Email = s.Email,
                Address = s.Address,
                IsActive = s.IsActive,
                CurrentPayable = payable,
                TotalPurchases = totalPurchases,
                LastTransactionDate = null,
                CreatedAt = s.CreatedAt,
                CreatedBy = s.CreatedBy,
                MatchingCustomerId = matchCust?.Id,
                MatchingCustomerCode = matchCust?.Code
            });
        }

        return new PagedResult<SupplierListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageIndex = pageIndex,
            PageSize = pageSize
        };
    }

    public async Task<SupplierDetailDto?> GetSupplierDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var s = await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (s == null) return null;

        var matchCust = await _context.Customers.AsNoTracking()
            .Where(c => (!string.IsNullOrEmpty(s.TaxCode) && c.TaxCode == s.TaxCode) ||
                        (!string.IsNullOrEmpty(s.Phone) && c.Phone == s.Phone))
            .Select(c => new { c.Id, c.Code })
            .FirstOrDefaultAsync(cancellationToken);

        return new SupplierDetailDto
        {
            Id = s.Id,
            Code = s.Code,
            BusinessCode = s.BusinessCode,
            Name = s.Name,
            CompanyName = s.CompanyName,
            TaxCode = s.TaxCode,
            ContactPerson = s.ContactPerson,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            Notes = s.Notes,
            IsActive = s.IsActive,
            CurrentPayable = 0m,
            TotalPurchases = 0m,
            LastTransactionDate = null,
            CreatedAt = s.CreatedAt,
            CreatedBy = s.CreatedBy,
            UpdatedAt = s.UpdatedAt,
            UpdatedBy = s.UpdatedBy,
            MatchingCustomerId = matchCust?.Id,
            MatchingCustomerCode = matchCust?.Code,
            RecentTransactions = new List<PartnerLinkedTransactionDto>()
        };
    }

    public async Task<DeletePartnerResult> DeleteSupplierAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        // 1. Authorization check
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Suppliers:Delete");
        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Unauthorized attempt to delete Supplier ID {SupplierId} by user {User}", id, user.Identity?.Name);
            return new DeletePartnerResult
            {
                Succeeded = false,
                ErrorMessage = "Bạn không có quyền xóa nhà cung cấp. Thao tác xóa chỉ dành cho Quản trị viên (Administrator) hoặc Quản lý có thẩm quyền."
            };
        }

        var supplier = await _context.Suppliers.FindAsync(new object[] { id }, cancellationToken);
        if (supplier == null)
        {
            return new DeletePartnerResult { Succeeded = false, ErrorMessage = "Không tìm thấy nhà cung cấp cần xóa." };
        }

        // 2. Physical delete when zero references exist
        _context.Suppliers.Remove(supplier);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.LogAsync(
            AuditEventType.SupplierDeleted,
            user.Identity?.Name ?? _currentUser.UserName ?? "System",
            "Supplier",
            supplier.Code,
            $"Đã xóa nhà cung cấp {supplier.Name} ({supplier.Code}).",
            cancellationToken: cancellationToken);

        return new DeletePartnerResult { Succeeded = true };
    }

    public async Task<BulkDeletePartnerResult> BulkDeleteSuppliersAsync(List<int> ids, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var authResult = await _authService.AuthorizeAsync(user, "Permission:Suppliers:Delete");
        if (!authResult.Succeeded)
        {
            return new BulkDeletePartnerResult
            {
                RequestedCount = ids.Count,
                Messages = new List<string> { "Bạn không có quyền thực hiện thao tác xóa nhà cung cấp." }
            };
        }

        var result = new BulkDeletePartnerResult { RequestedCount = ids.Count };

        foreach (var id in ids)
        {
            var singleResult = await DeleteSupplierAsync(id, user, cancellationToken);
            if (singleResult.Succeeded)
            {
                result.DeletedCount++;
            }
            else if (!string.IsNullOrEmpty(singleResult.ErrorMessage))
            {
                result.Messages.Add(singleResult.ErrorMessage);
            }
        }

        return result;
    }

    public async Task<List<string>> GetCustomerCreatorsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers.AsNoTracking()
            .Where(c => c.CreatedBy != null && c.CreatedBy != "")
            .Select(c => c.CreatedBy!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetSupplierCreatorsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Suppliers.AsNoTracking()
            .Where(s => s.CreatedBy != null && s.CreatedBy != "")
            .Select(s => s.CreatedBy!)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }
}
