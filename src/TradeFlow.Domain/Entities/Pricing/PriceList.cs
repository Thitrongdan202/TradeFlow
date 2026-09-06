using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.Pricing;

/// <summary>
/// Thực thể Bảng giá / Báo giá (Price List / Quotation).
/// Quản lý chính sách giá theo từng kỳ (tháng, quý, năm) độc lập khỏi thông tin danh mục sản phẩm gốc.
/// </summary>
public class PriceList : AuditableEntity<int>
{
    public PriceList() { }

    /// <summary>Mã bảng giá tự sinh theo chuẩn hệ thống (vd: BG000001)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Tên bảng giá (vd: BẢNG GIÁ ĐẠI LÝ KM, BẢNG GIÁ DỰ ÁN Q3/2026)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Số báo giá (vd: 06/LCS/2026)</summary>
    public string? QuotationNumber { get; set; }

    /// <summary>Ngày phát hành báo giá</summary>
    public DateTime? QuotationDate { get; set; }

    /// <summary>Thời gian bắt đầu áp dụng</summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>Thời gian kết thúc áp dụng</summary>
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Tháng áp dụng (1-12, tùy chọn)</summary>
    public int? Month { get; set; }

    /// <summary>Quý áp dụng (1-4, tùy chọn)</summary>
    public int? Quarter { get; set; }

    /// <summary>Năm tài chính áp dụng (vd: 2026)</summary>
    public int Year { get; set; } = DateTime.UtcNow.Year;

    /// <summary>Nội dung chương trình / Lời ngỏ báo giá</summary>
    public string? ProgramTitle { get; set; }

    /// <summary>Điều kiện giá (vd: Giá bán tại kho Lacasa Cần Thơ)</summary>
    public string? PriceCondition { get; set; }

    /// <summary>Ghi chú thuế VAT (vd: Giá chưa bao gồm VAT 8% Sứ và 10% Inox)</summary>
    public string? VatNote { get; set; }

    /// <summary>Trạng thái hiệu lực (Nháp, Đang áp dụng, Hết hiệu lực, Đã hủy)</summary>
    public PriceListStatus Status { get; set; } = PriceListStatus.Draft;

    /// <summary>Tên file Excel gốc khi tải lên</summary>
    public string? OriginalFileName { get; set; }

    /// <summary>Đường dẫn lưu trữ file Excel gốc (storage reference)</summary>
    public string? OriginalFileStorageRef { get; set; }

    /// <summary>Tổng số mặt hàng trong bảng giá</summary>
    public int TotalItems { get; set; }

    /// <summary>Ghi chú nội bộ</summary>
    public string? Notes { get; set; }

    /// <summary>Danh sách các dòng sản phẩm trong bảng giá</summary>
    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}