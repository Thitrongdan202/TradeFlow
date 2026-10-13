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
}
