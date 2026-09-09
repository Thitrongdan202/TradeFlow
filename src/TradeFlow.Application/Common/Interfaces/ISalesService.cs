using TradeFlow.Application.Common.Models.Sales;

namespace TradeFlow.Application.Common.Interfaces;

public interface ISalesService
{
    Task<List<SalesOrderDto>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<SalesOrderDto?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> CreateOrderAsync(SalesOrderDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateOrderAsync(SalesOrderDto dto, CancellationToken cancellationToken = default);
    Task<bool> ConfirmOrderAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteOrderAsync(int id, CancellationToken cancellationToken = default);
    
    Task<List<TradeFlow.Domain.Entities.MasterData.Customer>> SearchCustomersAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<TradeFlow.Domain.Entities.MasterData.Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<List<TradeFlow.Domain.Entities.Pricing.PriceList>> GetApplicablePriceListsAsync(DateTime targetDate, CancellationToken cancellationToken = default);
    Task<(decimal? Price, string SourceName)> GetProductPriceAsync(int productId, int? priceListId, DateTime targetDate, CancellationToken cancellationToken = default);
    Task<decimal> GetActivePriceAsync(int productId, DateTime targetDate, CancellationToken cancellationToken = default);
}
