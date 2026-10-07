using System.ComponentModel.DataAnnotations;

namespace TradeFlow.Application.Common.Models.Settings;

/// <summary>
/// DTO chứa cấu hình thương hiệu và màn hình tải (Global Loading Screen) của công ty.
/// </summary>
public class CompanyBrandingDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên công ty")]
    [MaxLength(200, ErrorMessage = "Tên công ty tối đa 200 ký tự")]
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Đường dẫn tương đối tới logo thương hiệu (PNG/SVG/WebP)</summary>
    public string? LogoPath { get; set; }

    /// <summary>Bật/tắt màn hình tải toàn cục</summary>
    public bool ShowLoadingScreen { get; set; } = true;

    /// <summary>Bật/tắt hiển thị tên công ty phía dưới vòng xoay logo</summary>
    public bool ShowCompanyNameOnLoading { get; set; } = true;

    /// <summary>Mã màu HEX của vòng xoay xoay quanh logo (ví dụ #10b981)</summary>
    [Required(ErrorMessage = "Vui lòng chọn màu sắc vòng xoay")]
    [MaxLength(30)]
    public string SpinnerColor { get; set; } = "#10b981";

    /// <summary>Độ trong suốt của vòng xoay (0.1 đến 1.0)</summary>
    [Range(0.1, 1.0, ErrorMessage = "Độ trong suốt từ 0.1 đến 1.0")]
    public double SpinnerOpacity { get; set; } = 0.8;

    /// <summary>Tốc độ xoay: 'slow' (2.5s), 'normal' (1.5s), 'fast' (0.8s)</summary>
    [Required(ErrorMessage = "Vui lòng chọn tốc độ xoay")]
    public string SpinnerSpeed { get; set; } = "normal";
}
