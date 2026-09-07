using TradeFlow.Application.Common.Models.Sales;

namespace TradeFlow.Application.Common.Interfaces;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateInvoiceFromOrderAsync(int salesOrderId, TradeFlow.Domain.Enums.InvoiceType type = TradeFlow.Domain.Enums.InvoiceType.SalesInvoice, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default);
    Task<bool> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken cancellationToken = default);
}
