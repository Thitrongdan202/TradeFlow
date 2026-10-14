using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.MasterData;

namespace TradeFlow.Application.Common.Interfaces;

public interface IProductService
{
    Task<DeleteProductResult> DeleteProductAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<BulkDeleteProductResult> BulkDeleteProductsAsync(List<int> ids, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
