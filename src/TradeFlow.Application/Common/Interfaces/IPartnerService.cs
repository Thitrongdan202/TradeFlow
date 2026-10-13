using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Partners;

namespace TradeFlow.Application.Common.Interfaces;

public interface IPartnerService
{
    // Customer operations
    Task<PagedResult<CustomerListItemDto>> GetCustomersAsync(CustomerFilterDto filter, CancellationToken cancellationToken = default);
    Task<CustomerDetailDto?> GetCustomerDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<DeletePartnerResult> DeleteCustomerAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<BulkDeletePartnerResult> BulkDeleteCustomersAsync(List<int> ids, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    // Supplier operations
    Task<PagedResult<SupplierListItemDto>> GetSuppliersAsync(SupplierFilterDto filter, CancellationToken cancellationToken = default);
    Task<SupplierDetailDto?> GetSupplierDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<DeletePartnerResult> DeleteSupplierAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<BulkDeletePartnerResult> BulkDeleteSuppliersAsync(List<int> ids, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    // Distinct creators lookup for filter dropdowns
    Task<List<string>> GetCustomerCreatorsAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetSupplierCreatorsAsync(CancellationToken cancellationToken = default);
}
