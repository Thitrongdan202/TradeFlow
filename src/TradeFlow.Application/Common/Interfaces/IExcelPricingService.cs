using TradeFlow.Application.Common.Models.Pricing;

namespace TradeFlow.Application.Common.Interfaces;

public interface IExcelPricingService
{
    /// <summary>
    /// Phân tích file Excel tải lên, phát hiện các vùng tiêu đề, chuỗi hàng, và trích xuất hình ảnh nhúng.
    /// </summary>
    Task<ExcelAnalysisResultDto> AnalyzeAndDryRunAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xác nhận và ghi nhận Bảng giá vào cơ sở dữ liệu từ kết quả phân tích.
    /// </summary>
    Task<PriceListDto> CommitImportAsync(ExcelImportCommitRequest request, string currentUserName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất dữ liệu Bảng giá từ hệ thống ra file Excel (.xlsx) chuẩn mẫu tài liệu công ty.
    /// </summary>
    Task<byte[]> ExportPriceListAsync(int priceListId, CancellationToken cancellationToken = default);
}