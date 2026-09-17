using TradeFlow.Domain.Common;
using TradeFlow.Domain.Enums;

namespace TradeFlow.Domain.Entities.MasterData;

public class Product : AuditableEntity<int>
{
    public Product() { }

    /// <summary>Mã hệ thống tự sinh (SP000001) - duy nhất</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Mã hàng mới do doanh nghiệp quy định (vd: TL2138)</summary>
    public string? NewCode { get; set; }

    /// <summary>Mã hàng cũ dùng để tra cứu lịch sử</summary>
    public string? LegacyCode { get; set; }

    /// <summary>Tên sản phẩm</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Tên viết tắt</summary>
    public string? ShortName { get; set; }

    /// <summary>Danh mục sản phẩm</summary>
    public int? CategoryId { get; set; }
    public ProductCategory? Category { get; set; }

    /// <summary>Đơn vị tính</summary>
    public int? UnitId { get; set; }
    public UnitOfMeasure? Unit { get; set; }

    /// <summary>Mô tả sản phẩm</summary>
    public string? Description { get; set; }

    /// <summary>Thông số kỹ thuật</summary>
    public string? Specifications { get; set; }

    /// <summary>Phân loại thuế suất GTGT</summary>
    public TaxTreatment TaxTreatment { get; set; } = TaxTreatment.Standard10;

    /// <summary>Được áp dụng chính sách giảm thuế 10% -> 8% (Nghị định 72/2024 đến 31/12/2026)</summary>
    public bool IsTaxReductionEligible { get; set; } = true;

    /// <summary>Thuế suất tùy chỉnh ghi đè (nếu được chỉ định riêng)</summary>
    public decimal? TaxRate { get; set; }

    /// <summary>Ngày bắt đầu hiệu lực của chính sách thuế tùy chỉnh</summary>
    public DateTime? TaxEffectiveFrom { get; set; }

    /// <summary>Ngày kết thúc hiệu lực của chính sách thuế tùy chỉnh</summary>
    public DateTime? TaxEffectiveTo { get; set; }

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Danh sách hình ảnh sản phẩm</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}