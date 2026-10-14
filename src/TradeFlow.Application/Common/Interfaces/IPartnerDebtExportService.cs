using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Partners;

namespace TradeFlow.Application.Common.Interfaces;

public interface IPartnerDebtExportService
{
    /// <summary>
    /// Xuất file công nợ — Báo cáo chi tiết công nợ cho 1 hoặc nhiều đối tác được chọn
    /// </summary>
    Task<byte[]> ExportPartnerDebtStatementAsync(PartnerDebtStatementExportRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất file quản lý công nợ Excel — Báo cáo quản lý tổng hợp công nợ toàn doanh nghiệp
    /// </summary>
    Task<byte[]> ExportDebtManagementWorkbookAsync(DebtManagementExportRequest request, ClaimsPrincipal user, CancellationToken cancellationToken = default);
}
