using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Partners;

namespace TradeFlow.Application.Common.Interfaces;

public interface IPartnerDebtService
{
    /// <summary>
    /// Lấy báo cáo tổng hợp công nợ hợp nhất (Phải thu khách hàng, Phải trả nhà cung cấp, Chênh lệch)
    /// </summary>
    Task<PartnerDebtSummaryDto> GetDebtSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Truy vấn danh sách bản ghi công nợ hợp nhất với bộ lọc và phân trang server-side
    /// </summary>
    Task<PagedResult<DebtRecordDto>> GetDebtRecordsAsync(DebtFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy lịch sử chi tiết giao dịch công nợ của một đối tác (Khách hàng hoặc NCC) bao gồm hóa đơn, đơn hàng và các khoản thu/chi/điều chỉnh
    /// </summary>
    Task<List<PartnerLinkedTransactionDto>> GetPartnerDebtHistoryAsync(PartnerType partnerType, int partnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ghi nhận thanh toán / thu tiền / chi tiền / điều chỉnh / chiết khấu công nợ trực tiếp
    /// </summary>
    Task<RecordDebtPaymentResult> RecordPaymentAsync(RecordDebtPaymentDto dto, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất file công nợ — Báo cáo chi tiết công nợ cho 1 hoặc nhiều đối tác được chọn
    /// </summary>
    Task<byte[]> ExportPartnerDebtStatementAsync(PartnerDebtStatementExportRequest request, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất file quản lý công nợ Excel — Báo cáo quản lý tổng hợp công nợ toàn doanh nghiệp
    /// </summary>
    Task<byte[]> ExportDebtManagementWorkbookAsync(DebtManagementExportRequest request, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
