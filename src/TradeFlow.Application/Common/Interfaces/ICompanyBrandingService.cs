using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TradeFlow.Application.Common.Models.Settings;

namespace TradeFlow.Application.Common.Interfaces;

/// <summary>
/// Giao diện dịch vụ quản lý thương hiệu và cấu hình màn hình tải toàn cục của công ty.
/// </summary>
public interface ICompanyBrandingService
{
    /// <summary>
    /// Lấy thông tin cấu hình thương hiệu và màn hình tải hiện tại.
    /// </summary>
    Task<CompanyBrandingDto> GetBrandingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật thông tin cấu hình thương hiệu và màn hình tải.
    /// </summary>
    Task<CompanyBrandingDto> UpdateBrandingAsync(CompanyBrandingDto dto, string updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tải lên và xác thực logo mới (PNG, SVG, WebP), lưu trữ an toàn ngoài mã nguồn.
    /// </summary>
    Task<string> UploadLogoAsync(Stream fileStream, string originalFileName, string contentType, string updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa logo đã cấu hình, đưa hệ thống về logo/biểu tượng mặc định.
    /// </summary>
    Task RemoveLogoAsync(string updatedBy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa cache cấu hình thương hiệu để các component cập nhật tức thì.
    /// </summary>
    void InvalidateCache();
}
