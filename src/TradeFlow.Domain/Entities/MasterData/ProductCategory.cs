using TradeFlow.Domain.Common;

namespace TradeFlow.Domain.Entities.MasterData;

public class ProductCategory : AuditableEntity<int>
{
    public ProductCategory() { }

    public string Code { get; set; } = string.Empty;          // Mã danh mục
    public string Name { get; set; } = string.Empty;          // Tên danh mục
    public string? Description { get; set; }                  // Mô tả
    public int DisplayOrder { get; set; }                     // Thứ tự hiển thị
    public bool IsActive { get; set; } = true;                // Đang hoạt động
    public int? ParentCategoryId { get; set; }                // Danh mục cha
    public ProductCategory? ParentCategory { get; set; }
    public ICollection<ProductCategory> ChildCategories { get; set; } = new List<ProductCategory>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
