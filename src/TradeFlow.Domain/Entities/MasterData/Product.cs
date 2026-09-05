using TradeFlow.Domain.Common;

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

    /// <summary>Đang hoạt động</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Danh sách hình ảnh sản phẩm</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}