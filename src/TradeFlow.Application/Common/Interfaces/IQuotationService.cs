using TradeFlow.Application.Common.Models.Sales;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Application.Common.Interfaces;

public interface IQuotationService
{
    Task<List<QuotationDto>> GetQuotationsAsync(QuotationFilterDto? filter = null, CancellationToken cancellationToken = default);
    Task<QuotationDto?> GetQuotationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<QuotationDto> CreateQuotationAsync(QuotationDto dto, CancellationToken cancellationToken = default);
    Task<QuotationDto> UpdateQuotationAsync(QuotationDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateStatusAsync(int id, QuotationStatus status, CancellationToken cancellationToken = default);
    Task<bool> DeleteQuotationAsync(int id, CancellationToken cancellationToken = default);
    Task<SalesOrderDto> ConvertToSalesOrderAsync(int quotationId, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(int quotationId, CancellationToken cancellationToken = default);
}
