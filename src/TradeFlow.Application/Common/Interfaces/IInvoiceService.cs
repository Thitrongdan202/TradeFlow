using TradeFlow.Application.Common.Models.Sales;

namespace TradeFlow.Application.Common.Interfaces;

public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetInvoicesAsync(CancellationToken cancellationToken = default);
    Task<InvoiceDto?> GetInvoiceByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateManualInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateInvoiceFromOrderAsync(int salesOrderId, TradeFlow.Domain.Enums.InvoiceType type = TradeFlow.Domain.Enums.InvoiceType.VatInvoice, CancellationToken cancellationToken = default);
    Task<InvoiceDto> CreateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default);
    Task<bool> UpdateInvoiceAsync(InvoiceDto dto, CancellationToken cancellationToken = default);
    Task<bool> IssueInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> DeleteInvoiceAsync(int id, CancellationToken cancellationToken = default);
    Task<byte[]> GeneratePdfAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<string> GenerateXmlAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<bool> SignInvoiceAsync(int invoiceId, string? signedBy = null, CancellationToken cancellationToken = default);

    // Chứng từ nội bộ: Đơn đặt hàng
    Task<OrderDocumentDto?> GetOrderDocumentByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
    Task<OrderDocumentDto?> GetOrderDocumentByOrderIdAsync(int salesOrderId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateOrderDocumentPdfAsync(OrderDocumentDto model, CancellationToken cancellationToken = default);
}
