using TradeFlow.Application.Common.Models.Pricing;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public interface IPriceListService
{
    Task<List<PriceListDto>> GetPriceListsAsync(int? year = null, int? quarter = null, int? month = null, PriceListStatus? status = null, CancellationToken cancellationToken = default);
    Task<PriceListDto?> GetPriceListDetailAsync(int id, CancellationToken cancellationToken = default);
    Task<PriceListDto> CreatePriceListAsync(PriceListDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdatePriceListAsync(PriceListDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeletePriceListAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> CancelPriceListAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ApprovePriceListAsync(int id, CancellationToken cancellationToken = default);
    Task<PriceComparisonResultDto> ComparePriceListsAsync(int basePriceListId, int targetPriceListId, CancellationToken cancellationToken = default);
    Task<List<PriceListItemDto>> GetProductPriceHistoryAsync(string productCodeOrNewCode, CancellationToken cancellationToken = default);
    Task<bool> DeleteOriginalFileAsync(int id, CancellationToken cancellationToken = default);
}